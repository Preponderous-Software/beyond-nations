using System;
using System.Runtime.InteropServices;

namespace beyondnations.web.strata {

    /**
    * Strata's C ABI as the browser build calls it. The calls are the ones the
    * desktop's StrataNative declares, with the same layouts (include/strata/
    * strata.h, ABI 1). The library is not loaded at run time: strata.a is
    * linked into dotnet.native.wasm (native/strata.targets), and "strata"
    * names that archive. This is a separate declaration, not the desktop
    * file, so that the desktop's own loading code (which looks for
    * libstrata.so or strata.dll beside the executable) stays out of the page.
    */
    public static unsafe class StrataWeb {
        public const string Library = "strata";
        public const uint AbiVersion = 1;

        public const int Ok = 0;
        public const int SurfaceHostContext = 2;
        public const int LightingSun = 1;
        public const uint ClearSky = 1u << 2;
        public const int StructRenderItem = 0;

        [StructLayout(LayoutKind.Sequential)]
        public struct EngineDesc {
            public uint struct_size;
            public uint abi_version;
            public int surface;
            public int width;
            public int height;
            public uint flags;
            public IntPtr title;
            public IntPtr get_proc_address;
            public IntPtr get_proc_address_user;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Camera {
            public fixed float view[16];
            public fixed float projection[16];
            public fixed float eye[3];
            public float near_plane;
            public float far_plane;
        }

        // ABI 1.1: a point light, for the night lights (NightLighting).
        [StructLayout(LayoutKind.Sequential)]
        public struct PointLight {
            public fixed float position[3];
            public float range;
            public fixed float color[3];
            public float intensity;
            public uint flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Lighting {
            public int mode;
            public fixed float legacy_direction[3];
            public float legacy_ambient;
            public float time_of_day;
            public float latitude_degrees;
            public int cascade_count;
            public int shadow_map_size;
            public float shadow_distance;
            public float split_lambda;
            public int fog;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FrameDesc {
            public uint clear_flags;
            public fixed float clear_color[4];
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FrameStats {
            public int draw_calls;
            public int instances;
            public fixed int instances_per_kind[4];
            public int items_rejected;
            public int shadow_cascades;
            public float cpu_ms_shadow;
            public float cpu_ms_opaque;
            public float cpu_ms_sky;
            public float cpu_ms_frame;
            public float gpu_finish_ms;
        }

        [DllImport(Library)] public static extern uint strata_abi_version();
        [DllImport(Library)] public static extern uint strata_abi_minor();
        [DllImport(Library)] public static extern IntPtr strata_version_string();
        [DllImport(Library)] public static extern int strata_struct_size(int structId);
        [DllImport(Library)] public static extern IntPtr strata_last_error();
        [DllImport(Library)] public static extern int strata_create(EngineDesc* desc, IntPtr* engine);
        [DllImport(Library)] public static extern void strata_destroy(IntPtr engine);
        [DllImport(Library)] public static extern IntPtr strata_gpu_description(IntPtr engine);
        [DllImport(Library)] public static extern int strata_resize(IntPtr engine, int width, int height);
        [DllImport(Library)] public static extern int strata_set_camera(IntPtr engine, Camera* camera);
        [DllImport(Library)] public static extern int strata_set_lighting(IntPtr engine, Lighting* lighting);
        [DllImport(Library)] public static extern int strata_set_point_lights(IntPtr engine, PointLight* lights, int count);
        [DllImport(Library)] public static extern int strata_lighting_defaults(int mode, Lighting* lighting);
        [DllImport(Library)] public static extern int strata_begin_frame(IntPtr engine, FrameDesc* desc);
        [DllImport(Library)] public static extern int strata_submit_primitives(IntPtr engine, RenderItem* items, int count);
        [DllImport(Library)] public static extern int strata_end_frame(IntPtr engine);
        [DllImport(Library)] public static extern int strata_get_frame_stats(IntPtr engine, FrameStats* stats);

        public static string lastError() {
            return Marshal.PtrToStringUTF8(strata_last_error()) ?? "";
        }

        public static void check(int status, string what) {
            if (status != Ok) {
                throw new InvalidOperationException("Strata " + what + " failed (status " + status + "): " + lastError());
            }
        }
    }

    /** native/bnweb_gl.c */
    public static class BnWebGl {
        public const string Library = "bnweb";

        /** The Emscripten context handle (> 0), or a negative EMSCRIPTEN_RESULT. */
        [DllImport(Library, CharSet = CharSet.Ansi)] public static extern int bnweb_gl_attach(string canvasSelector);

        [DllImport(Library)] public static extern IntPtr bnweb_gl_get_proc_address_function();
    }
}
