using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using beyondnations;
using beyondnations.desktop.render;
using beyondnations.web.strata;

namespace beyondnations.web {

    /**
    * The browser's world renderer: Strata, linked into dotnet.native.wasm,
    * drawing into the page's WebGL 2 context. The desktop's
    * NativeWorldRenderer, call for call: the same culling in C#, the same
    * RenderItem array handed across as a pointer, Strata's sun lighting
    * following the time of day, and its sky behind the world.
    *
    * Strata is created with STRATA_SURFACE_HOST_CONTEXT on the canvas's
    * context, which native/bnweb_gl.c has made current through Emscripten.
    * The labels and Dear ImGui are drawn after it by wwwroot/webhost/gfx.js
    * in the same context; Strata puts back the GL state it touches.
    */
    public unsafe class StrataWorldRenderer : IWorldRenderer {

        private IntPtr engine;
        private readonly string gpuDescription;
        private readonly StrataWeb.Lighting sun;
        private RenderItem[] visible = new RenderItem[1024];
        private int visibleCount;
        private readonly int[] perKind = new int[4];

        private int width = 1;
        private int height = 1;
        private int drawCalls;
        private int instancesDrawn;
        private int instancesCulled;
        private float timeOfDay = TimeOfDay.DefaultStartFraction;

        /** canvasSelector: the page's canvas, e.g. "#game". */
        public StrataWorldRenderer(string canvasSelector) {
            int context = BnWebGl.bnweb_gl_attach(canvasSelector);
            if (context <= 0) {
                throw new InvalidOperationException("no WebGL 2 context for Strata on " + canvasSelector + " (" + context + ")");
            }

            uint abi = StrataWeb.strata_abi_version();
            if (abi != StrataWeb.AbiVersion) {
                throw new InvalidOperationException("Strata implements ABI " + abi + " but this build expects ABI " + StrataWeb.AbiVersion);
            }
            int nativeItemSize = StrataWeb.strata_struct_size(StrataWeb.StructRenderItem);
            if (nativeItemSize != Unsafe.SizeOf<RenderItem>()) {
                throw new InvalidOperationException(
                    "RenderItem is " + Unsafe.SizeOf<RenderItem>() + " bytes but Strata expects " + nativeItemSize);
            }

            StrataWeb.EngineDesc desc = new StrataWeb.EngineDesc {
                struct_size = (uint) sizeof(StrataWeb.EngineDesc),
                abi_version = StrataWeb.AbiVersion,
                surface = StrataWeb.SurfaceHostContext,
                width = 1,
                height = 1,
                get_proc_address = BnWebGl.bnweb_gl_get_proc_address_function(),
                get_proc_address_user = IntPtr.Zero,
            };
            IntPtr created;
            StrataWeb.check(StrataWeb.strata_create(&desc, &created), "create");
            engine = created;

            StrataWeb.Lighting defaults;
            StrataWeb.check(StrataWeb.strata_lighting_defaults(StrataWeb.LightingSun, &defaults), "lighting_defaults");
            sun = defaults;

            gpuDescription = Marshal.PtrToStringUTF8(StrataWeb.strata_gpu_description(engine)) ?? "";
            Log.info("Strata " + Marshal.PtrToStringUTF8(StrataWeb.strata_version_string()) + " (ABI " + abi + "."
                + StrataWeb.strata_abi_minor() + ") on " + gpuDescription);
        }

        public string getName() { return "strata"; }
        public string getGpuDescription() { return gpuDescription; }

        public void setTimeOfDay(float fraction) {
            if (!float.IsNaN(fraction) && !float.IsInfinity(fraction)) {
                timeOfDay = fraction;
            }
        }

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
            appendAll(snapshot.getGroundItems(), culler);
            appendAll(snapshot.getEntityItems(), culler);

            StrataWeb.check(StrataWeb.strata_resize(engine, width, height), "resize");

            StrataWeb.Camera camera = new StrataWeb.Camera();
            Buffer.MemoryCopy(&view, camera.view, 64, 64);
            Buffer.MemoryCopy(&projection, camera.projection, 64, 64);
            if (Matrix4x4.Invert(view, out Matrix4x4 inverseView)) {
                camera.eye[0] = inverseView.M41;
                camera.eye[1] = inverseView.M42;
                camera.eye[2] = inverseView.M43;
            }
            camera.near_plane = projection.M43 / projection.M33;
            camera.far_plane = projection.M43 / (projection.M33 + 1f);
            StrataWeb.check(StrataWeb.strata_set_camera(engine, &camera), "set_camera");

            StrataWeb.Lighting lighting = sun;
            lighting.time_of_day = timeOfDay;
            StrataWeb.check(StrataWeb.strata_set_lighting(engine, &lighting), "set_lighting");

            // The host cleared the frame; Strata paints its sky where nothing
            // was drawn, then the world.
            StrataWeb.FrameDesc frame = new StrataWeb.FrameDesc { clear_flags = StrataWeb.ClearSky };
            StrataWeb.check(StrataWeb.strata_begin_frame(engine, &frame), "begin_frame");
            fixed (RenderItem* items = visible) {
                StrataWeb.check(StrataWeb.strata_submit_primitives(engine, items, visibleCount), "submit_primitives");
            }
            StrataWeb.check(StrataWeb.strata_end_frame(engine), "end_frame");

            StrataWeb.FrameStats stats;
            StrataWeb.check(StrataWeb.strata_get_frame_stats(engine, &stats), "get_frame_stats");
            drawCalls = stats.draw_calls;
            instancesDrawn = stats.instances;
            for (int kind = 0; kind < perKind.Length; kind++) {
                perKind[kind] = stats.instances_per_kind[kind];
            }
        }

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

        public void Dispose() {
            if (engine != IntPtr.Zero) {
                StrataWeb.strata_destroy(engine);
                engine = IntPtr.Zero;
            }
        }
    }
}
