using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using beyondnations;

namespace beyondnations.desktop.render {

    /**
    * Draws the world snapshot through Strata, the shared native engine and
    * the game's only world renderer, inside the window and GL context
    * Silk.NET owns (engine phase B1, #274; Strata-only since the Silk world
    * renderer was removed).
    *
    * Strata is created on the host's context: it loads GL through Silk's own
    * GetProcAddress, draws into whatever framebuffer is bound, and puts back
    * every piece of GL state it touched, so the labels and the ImGui layer
    * drawn after it are none the wiser.
    *
    * The light is Strata's (#280): a sun and a moon following the
    * simulation's time of day, cascaded shadows, hemisphere ambient, fog and
    * the analytic sky. The game owns time and passes it in every frame with
    * setTimeOfDay; Strata only evaluates it.
    *
    * Culling stays in C#: RenderCuller decides what is drawn. The items that
    * survive are written into one array that is reused between frames, and
    * that array goes across as a pointer -- RenderItem is Strata's
    * StrataRenderItem byte for byte (#273), so nothing is converted. Six
    * calls cross the boundary per frame.
    */
    public unsafe class NativeWorldRenderer : IWorldRenderer {

        private IntPtr engine;
        private readonly string gpuDescription;
        private RenderItem[] visible = new RenderItem[1024];
        private int visibleCount;
        private readonly int[] perKind = new int[4];

        private int width;
        private int height;
        private int drawCalls;
        private int instancesDrawn;
        private int instancesCulled;

        // --- #280 ---
        private readonly StrataNative.Lighting sun;
        private float timeOfDay = TimeOfDay.DefaultStartFraction;
        // --- end #280 ---

        /**
        * getProcAddress resolves GL entry points in the current context; Silk
        * provides it as window.GLContext.GetProcAddress. It is only called
        * while this constructor runs.
        */
        public NativeWorldRenderer(Func<string, IntPtr> getProcAddress, int width, int height) {
            this.width = Math.Max(1, width);
            this.height = Math.Max(1, height);

            uint abi = StrataNative.strata_abi_version();
            if (abi != StrataNative.AbiVersion) {
                throw new InvalidOperationException(
                    "libstrata implements ABI " + abi + " but this build expects ABI " + StrataNative.AbiVersion);
            }
            int nativeItemSize = StrataNative.strata_struct_size(StrataNative.StructRenderItem);
            if (nativeItemSize != Unsafe.SizeOf<RenderItem>()) {
                throw new InvalidOperationException(
                    "RenderItem is " + Unsafe.SizeOf<RenderItem>() + " bytes but Strata expects " + nativeItemSize);
            }

            GCHandle callback = GCHandle.Alloc(getProcAddress);
            try {
                StrataNative.EngineDesc desc = new StrataNative.EngineDesc {
                    struct_size = (uint) sizeof(StrataNative.EngineDesc),
                    abi_version = StrataNative.AbiVersion,
                    surface = StrataNative.SurfaceHostContext,
                    width = this.width,
                    height = this.height,
                    get_proc_address = (IntPtr) (delegate* unmanaged<byte*, IntPtr, IntPtr>) &resolveGlFunction,
                    get_proc_address_user = GCHandle.ToIntPtr(callback),
                };
                IntPtr created;
                StrataNative.check(StrataNative.strata_create(&desc, &created), "create");
                engine = created;
            } finally {
                callback.Free();
            }

            gpuDescription = Marshal.PtrToStringUTF8(StrataNative.strata_gpu_description(engine)) ?? "";
            // --- #280 ---
            // Strata's sun and moon, following the simulation's time of day.
            StrataNative.Lighting defaults;
            StrataNative.check(StrataNative.strata_lighting_defaults(StrataNative.LightingSun, &defaults), "lighting_defaults");
            sun = defaults;
            // --- end #280 ---

            Log.info("native renderer ready: Strata " + Marshal.PtrToStringUTF8(StrataNative.strata_version_string())
                + " (ABI " + abi + ") on " + gpuDescription);
        }

        [UnmanagedCallersOnly]
        private static IntPtr resolveGlFunction(byte* name, IntPtr user) {
            Func<string, IntPtr> getProcAddress = (Func<string, IntPtr>) GCHandle.FromIntPtr(user).Target;
            return resolveOptional(getProcAddress, Marshal.PtrToStringUTF8((IntPtr) name));
        }

        /**
        * Looks up one GL function, giving null when the driver lacks it.
        * Strata's loader asks for every GL 4.5 entry point and leaves the
        * missing ones null; an exception here would instead escape an
        * [UnmanagedCallersOnly] callback and end the process.
        */
        public static IntPtr resolveOptional(Func<string, IntPtr> getProcAddress, string name) {
            try {
                return getProcAddress(name);
            } catch (Exception) {
                return IntPtr.Zero;
            }
        }


        // --- night lights ---
        private Vector3 playerPosition;
        private readonly NightLighting.Light[] nightLights = new NightLighting.Light[NightLighting.LightCount];
        private int lastNightLightCount = -1;

        /** Where the player stands, so the night lights can follow them. */
        public void setPlayerPosition(Vector3 position) {
            playerPosition = position;
        }

        private void submitNightLights(float latitudeDegrees) {
            int count = NightLighting.compute(timeOfDay, latitudeDegrees, playerPosition, nightLights);
            if (count == 0 && lastNightLightCount == 0) {
                return;
            }
            lastNightLightCount = count;
            StrataNative.PointLight* lights = stackalloc StrataNative.PointLight[NightLighting.LightCount];
            for (int i = 0; i < count; i++) {
                NightLighting.Light light = nightLights[i];
                lights[i].position[0] = light.Position.X;
                lights[i].position[1] = light.Position.Y;
                lights[i].position[2] = light.Position.Z;
                lights[i].range = light.Range;
                lights[i].color[0] = light.Colour.X;
                lights[i].color[1] = light.Colour.Y;
                lights[i].color[2] = light.Colour.Z;
                lights[i].intensity = light.Intensity;
                lights[i].flags = 0;
            }
            StrataNative.check(StrataNative.strata_set_point_lights(engine, lights, count), "set_point_lights");
        }
        // --- end night lights ---

        public string getName() { return "strata"; }

        // --- #280 ---
        /** The simulation's time of day, [0, 1); used on the next render with sun lighting. */
        public void setTimeOfDay(float fraction) {
            if (float.IsNaN(fraction) || float.IsInfinity(fraction)) {
                throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "the time of day must be finite");
            }
            timeOfDay = fraction;
        }
        // --- end #280 ---
        public string getGpuDescription() { return gpuDescription; }

        public void setViewportSize(int width, int height) {
            this.width = Math.Max(1, width);
            this.height = Math.Max(1, height);
        }

        public int getDrawCallCount() { return drawCalls; }
        public int getInstancesDrawn() { return instancesDrawn; }
        public int getInstancesCulled() { return instancesCulled; }
        public int getInstanceCount(PrimitiveKind kind) { return perKind[(int) kind]; }

        public void render(WorldSnapshot snapshot, Matrix4x4 view, Matrix4x4 projection, RenderCuller culler) {
            visibleCount = 0;
            instancesCulled = 0;
            // Ground first, then entities, so the culler sees them in the
            // order it always has.
            appendAll(snapshot.getGroundItems(), culler);
            appendAll(snapshot.getEntityItems(), culler);

            StrataNative.check(StrataNative.strata_resize(engine, width, height), "resize");

            StrataNative.Camera camera = new StrataNative.Camera();
            // System.Numerics rows are GL columns, so both matrices go across
            // exactly as GL expects them.
            Buffer.MemoryCopy(&view, camera.view, 64, 64);
            Buffer.MemoryCopy(&projection, camera.projection, 64, 64);
            Matrix4x4 inverseView;
            if (Matrix4x4.Invert(view, out inverseView)) {
                camera.eye[0] = inverseView.M41;
                camera.eye[1] = inverseView.M42;
                camera.eye[2] = inverseView.M43;
            }
            // CreatePerspectiveFieldOfView stores M33 = f / (n - f) and
            // M43 = n f / (n - f), so both planes can be read back out.
            camera.near_plane = projection.M43 / projection.M33;
            camera.far_plane = projection.M43 / (projection.M33 + 1f);
            StrataNative.check(StrataNative.strata_set_camera(engine, &camera), "set_camera");

            // --- #280 ---
            StrataNative.Lighting lighting = sun;
            lighting.time_of_day = timeOfDay;
            StrataNative.check(StrataNative.strata_set_lighting(engine, &lighting), "set_lighting");
            submitNightLights(lighting.latitude_degrees);
            // --- end #280 ---

            // No clear: the host cleared the frame before the world is drawn.
            // With sun lighting Strata paints its sky behind the world, where
            // the host's depth buffer is still at the far plane.
            StrataNative.FrameDesc frame = new StrataNative.FrameDesc();
            frame.clear_flags = StrataNative.ClearSky;
            StrataNative.check(StrataNative.strata_begin_frame(engine, &frame), "begin_frame");
            fixed (RenderItem* items = visible) {
                StrataNative.check(StrataNative.strata_submit_primitives(engine, items, visibleCount), "submit_primitives");
            }
            StrataNative.check(StrataNative.strata_end_frame(engine), "end_frame");

            StrataNative.FrameStats stats;
            StrataNative.check(StrataNative.strata_get_frame_stats(engine, &stats), "get_frame_stats");
            drawCalls = stats.draw_calls;
            instancesDrawn = stats.instances;
            // What the engine reports it drew, not what was sent.
            for (int kind = 0; kind < perKind.Length; kind++) {
                perKind[kind] = stats.instances_per_kind[kind];
            }
        }

        /** Indexed, not enumerated: enumerating through the interface allocates. */
        private void appendAll(IReadOnlyList<RenderItem> items, RenderCuller culler) {
            int count = items.Count;
            for (int i = 0; i < count; i++) {
                RenderItem item = items[i];
                if (culler != null && !culler.shouldDraw(ref item)) {
                    instancesCulled++;
                    continue;
                }
                if (visibleCount == visible.Length) {
                    Array.Resize(ref visible, visible.Length * 2);
                }
                visible[visibleCount++] = item;
            }
        }

        /**
        * The host disposes renderers in its Closing handler, while the context
        * still exists, and again in Dispose(); the second call is a no-op.
        */
        public void Dispose() {
            if (engine != IntPtr.Zero) {
                StrataNative.strata_destroy(engine);
                engine = IntPtr.Zero;
            }
        }
    }
}
