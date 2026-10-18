using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using beyondnations;

namespace beyondnations.desktop.render {

    /**
    * Draws the world snapshot through Strata, the shared native engine, inside
    * the window and GL context Silk.NET already owns (engine phase B1, #274).
    *
    * Strata is created on the host's context: it loads GL through Silk's own
    * GetProcAddress, draws into whatever framebuffer is bound, and puts back
    * every piece of GL state it touched, so the labels and the ImGui layer
    * drawn after it are none the wiser. Lighting is Strata's LEGACY mode,
    * which is SilkWorldRenderer's model term for term (the same light
    * direction and ambient are passed across), and Strata's primitive meshes
    * are ports of PrimitiveMesh, so the two renderers can be compared image
    * for image.
    *
    * With sun lighting (#280) the light is Strata's instead: a sun and a moon
    * following the simulation's time of day, cascaded shadows, hemisphere
    * ambient, fog and the analytic sky. The game owns time and passes it in
    * every frame with setTimeOfDay; Strata only evaluates it. The parity
    * check constructs the renderer without sun lighting, so it still compares
    * like with like.
    *
    * Culling stays in C#: the same RenderCuller decides, in the same order,
    * so the culled count is SilkWorldRenderer's by construction. The items
    * that survive are written into one array that is reused between frames,
    * and that array goes across as a pointer -- RenderItem is Strata's
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
        private readonly bool sunLighting;
        private StrataNative.Lighting sun;
        private float timeOfDay = TimeOfDay.DefaultStartFraction;
        // --- end #280 ---

        /**
        * getProcAddress resolves GL entry points in the current context; Silk
        * provides it as window.GLContext.GetProcAddress. It is only called
        * while this constructor runs.
        */
        public NativeWorldRenderer(Func<string, IntPtr> getProcAddress, int width, int height, bool sunLighting = false) {
            this.sunLighting = sunLighting;
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
            StrataNative.Lighting lighting = new StrataNative.Lighting {
                mode = StrataNative.LightingLegacy,
                legacy_ambient = SilkWorldRenderer.Ambient,
            };
            lighting.legacy_direction[0] = SilkWorldRenderer.LightDirection.X;
            lighting.legacy_direction[1] = SilkWorldRenderer.LightDirection.Y;
            lighting.legacy_direction[2] = SilkWorldRenderer.LightDirection.Z;
            StrataNative.check(StrataNative.strata_set_lighting(engine, &lighting), "set_lighting");

            // --- #280 ---
            if (sunLighting) {
                StrataNative.Lighting defaults;
                StrataNative.check(StrataNative.strata_lighting_defaults(StrataNative.LightingSun, &defaults), "lighting_defaults");
                sun = defaults;
            }
            // --- end #280 ---

            Log.info("native renderer ready: Strata " + Marshal.PtrToStringUTF8(StrataNative.strata_version_string())
                + " (ABI " + abi + ") on " + gpuDescription);
        }

        [UnmanagedCallersOnly]
        private static IntPtr resolveGlFunction(byte* name, IntPtr user) {
            Func<string, IntPtr> getProcAddress = (Func<string, IntPtr>) GCHandle.FromIntPtr(user).Target;
            return getProcAddress(Marshal.PtrToStringUTF8((IntPtr) name));
        }

        public string getName() { return "native"; }

        // --- #280 ---
        public bool usesSunLighting() { return sunLighting; }

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
            // Ground first, then entities: the order SilkWorldRenderer appends
            // them in, so the culler sees the same sequence.
            appendAll(snapshot.getGroundItems(), culler);
            appendAll(snapshot.getEntityItems(), culler);

            StrataNative.check(StrataNative.strata_resize(engine, width, height), "resize");

            StrataNative.Camera camera = new StrataNative.Camera();
            // System.Numerics rows are GL columns, so both matrices go across
            // exactly as SilkWorldRenderer uploads them.
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
            if (sunLighting) {
                StrataNative.Lighting lighting = sun;
                lighting.time_of_day = timeOfDay;
                StrataNative.check(StrataNative.strata_set_lighting(engine, &lighting), "set_lighting");
            }
            // --- end #280 ---

            // No clear: the host cleared the frame before the world is drawn.
            // With sun lighting Strata paints its sky behind the world, where
            // the host's depth buffer is still at the far plane.
            StrataNative.FrameDesc frame = new StrataNative.FrameDesc();
            if (sunLighting) {
                frame.clear_flags = StrataNative.ClearSky;
            }
            StrataNative.check(StrataNative.strata_begin_frame(engine, &frame), "begin_frame");
            fixed (RenderItem* items = visible) {
                StrataNative.check(StrataNative.strata_submit_primitives(engine, items, visibleCount), "submit_primitives");
            }
            StrataNative.check(StrataNative.strata_end_frame(engine), "end_frame");

            StrataNative.FrameStats stats;
            StrataNative.check(StrataNative.strata_get_frame_stats(engine, &stats), "get_frame_stats");
            drawCalls = stats.draw_calls;
            instancesDrawn = stats.instances;
            // What the engine reports it drew, not what was sent: the parity
            // check compares these with SilkWorldRenderer's batch counts.
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
