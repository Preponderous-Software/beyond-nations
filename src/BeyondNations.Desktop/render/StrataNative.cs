using System;
using System.IO;
using System.Runtime.InteropServices;

namespace beyondnations.desktop.render {

    /**
    * The slice of Strata's C ABI (include/strata/strata.h, ABI 1) that the
    * native renderer uses. Strata is MIT-licensed, lives in
    * Preponderous-Software/strata, and ships as one native library per
    * platform, which the build places under runtimes/<rid>/native/ beside
    * BN's other natives (#274).
    *
    * Every struct here is laid out sequentially and blittable, and
    * TestStrataAbi checks each one's size and offsets against the library
    * itself. RenderItem is not mirrored at all: Core's struct is the native
    * StrataRenderItem byte for byte (#273), so a snapshot list is handed over
    * as a pointer.
    */
    public static unsafe class StrataNative {
        public const string Library = "strata";
        public const uint AbiVersion = 1;

        public const int Ok = 0;
        public const int SurfaceHostContext = 2;
        public const int LightingLegacy = 0;
        public const int LightingSun = 1; // #280
        public const uint ClearSky = 1u << 2; // #280: draw Strata's analytic sky where nothing else was drawn
        public const int StructRenderItem = 0, StructEngineDesc = 1, StructCamera = 2, StructLighting = 3,
            StructFrameDesc = 4, StructFrameStats = 5, StructPointLight = 7;

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

        static StrataNative() {
            NativeLibrary.SetDllImportResolver(typeof(StrataNative).Assembly, resolve);
        }

        /**
        * Looks in runtimes/<rid>/native/ under the application directory
        * first, since that is where the build puts the library, and then
        * falls back to the platform's normal search.
        */
        private static IntPtr resolve(string name, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath) {
            if (name != Library) {
                return IntPtr.Zero;
            }
            string path = getBundledLibraryPath();
            if (path != null && File.Exists(path) && NativeLibrary.TryLoad(path, out IntPtr handle)) {
                return handle;
            }
            return IntPtr.Zero;
        }

        /** Where the build places the library for this platform, or null if it ships none. */
        public static string getBundledLibraryPath() {
            string rid, file;
            if (OperatingSystem.IsWindows()) {
                rid = "win-x64";
                file = "strata.dll";
            } else if (OperatingSystem.IsLinux()) {
                rid = "linux-x64";
                file = "libstrata.so";
            } else {
                return null;
            }
            if (RuntimeInformation.ProcessArchitecture != Architecture.X64) {
                return null;
            }
            return Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native", file);
        }

        [DllImport(Library)] public static extern uint strata_abi_version();
        [DllImport(Library)] public static extern IntPtr strata_version_string();
        [DllImport(Library)] public static extern int strata_struct_size(int structId);
        [DllImport(Library)] public static extern int strata_struct_field_offset(int structId, int fieldId);
        [DllImport(Library)] public static extern IntPtr strata_last_error();
        [DllImport(Library)] public static extern int strata_create(EngineDesc* desc, IntPtr* engine);
        [DllImport(Library)] public static extern void strata_destroy(IntPtr engine);
        [DllImport(Library)] public static extern IntPtr strata_gpu_description(IntPtr engine);
        [DllImport(Library)] public static extern int strata_resize(IntPtr engine, int width, int height);
        [DllImport(Library)] public static extern int strata_set_camera(IntPtr engine, Camera* camera);
        [DllImport(Library)] public static extern int strata_set_lighting(IntPtr engine, Lighting* lighting);
        [DllImport(Library)] public static extern int strata_set_point_lights(IntPtr engine, PointLight* lights, int count);
        [DllImport(Library)] public static extern int strata_lighting_defaults(int mode, Lighting* lighting); // #280
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
}
