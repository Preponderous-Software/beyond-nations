using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

using beyondnations;
using beyondnations.desktop.render;

namespace beyondnationstests.desktop {

    /**
    * The native renderer hands Strata pointers to managed structs, so every
    * struct that crosses must agree with the native library about its size
    * and every field's offset (#274). These ask the library itself
    * (strata_struct_size, strata_struct_field_offset) rather than trusting
    * numbers copied from its header, so a Strata upgrade that moves a field
    * fails here instead of corrupting a frame.
    *
    * They load libstrata from runtimes/<rid>/native/ in the test output,
    * which is also a check that the build packaged it there.
    */
    public class TestStrataAbi {

        private static void assertLayout<T>(int structId, params string[] fields) where T : struct {
            Assert.Equal(StrataNative.strata_struct_size(structId), Unsafe.SizeOf<T>());
            for (int i = 0; i < fields.Length; i++) {
                Assert.Equal(StrataNative.strata_struct_field_offset(structId, i), (int) Marshal.OffsetOf<T>(fields[i]));
            }
            // No native field is left unmirrored.
            Assert.Equal(-1, StrataNative.strata_struct_field_offset(structId, fields.Length));
        }

        [Fact]
        public void testTheBuildPackagesTheNativeLibraryUnderRuntimes() {
            string path = StrataNative.getBundledLibraryPath();
            Assert.NotNull(path);
            Assert.True(File.Exists(path), "expected the native library at " + path);
        }

        [Fact]
        public void testTheLibraryImplementsTheAbiThisBuildExpects() {
            Assert.Equal(StrataNative.AbiVersion, StrataNative.strata_abi_version());
        }

        [Fact]
        public void testRenderItemMatchesStrataRenderItem() {
            Assert.Equal(StrataNative.strata_struct_size(StrataNative.StructRenderItem), Unsafe.SizeOf<RenderItem>());
            assertLayout<RenderItem>(StrataNative.StructRenderItem, "kind", "position", "scale", "color");
        }

        [Fact]
        public void testEngineDescMatchesNative() {
            assertLayout<StrataNative.EngineDesc>(StrataNative.StructEngineDesc, "struct_size", "abi_version",
                "surface", "width", "height", "flags", "title", "get_proc_address", "get_proc_address_user");
        }

        [Fact]
        public void testCameraMatchesNative() {
            assertLayout<StrataNative.Camera>(StrataNative.StructCamera, "view", "projection", "eye", "near_plane",
                "far_plane");
        }

        [Fact]
        public void testLightingMatchesNative() {
            assertLayout<StrataNative.Lighting>(StrataNative.StructLighting, "mode", "legacy_direction",
                "legacy_ambient", "time_of_day", "latitude_degrees", "cascade_count", "shadow_map_size",
                "shadow_distance", "split_lambda", "fog");
        }

        [Fact]
        public void testFrameDescAndStatsMatchNative() {
            assertLayout<StrataNative.FrameDesc>(StrataNative.StructFrameDesc, "clear_flags", "clear_color");
            assertLayout<StrataNative.FrameStats>(StrataNative.StructFrameStats, "draw_calls", "instances",
                "instances_per_kind", "items_rejected", "shadow_cascades", "cpu_ms_shadow", "cpu_ms_opaque",
                "cpu_ms_sky", "cpu_ms_frame", "gpu_finish_ms");
        }
    }
}
