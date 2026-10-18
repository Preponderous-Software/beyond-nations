using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * RenderItem crosses into the native renderer (Strata, ABI 1) as a raw
    * pointer, so its layout is a contract with a C struct:
    *
    *   typedef struct StrataRenderItem {
    *       int32_t kind;          offset  0
    *       float position[3];     offset  4
    *       float scale[3];        offset 16
    *       float color[4];        offset 28
    *   } StrataRenderItem;        44 bytes
    *
    * These numbers are pinned here so that a field added, removed or moved in
    * Core fails a test instead of silently corrupting what the native side
    * reads. The Desktop tests check the same layout against the native
    * library itself.
    */
    public class TestRenderItemLayout {

        [Fact]
        public void testRenderItemIsFortyFourBytes() {
            Assert.Equal(44, Unsafe.SizeOf<RenderItem>());
            Assert.Equal(44, Marshal.SizeOf<RenderItem>());
        }

        [Fact]
        public void testRenderItemFieldOffsetsMatchTheNativeStruct() {
            Assert.Equal(0, (int) Marshal.OffsetOf<RenderItem>("kind"));
            Assert.Equal(4, (int) Marshal.OffsetOf<RenderItem>("position"));
            Assert.Equal(16, (int) Marshal.OffsetOf<RenderItem>("scale"));
            Assert.Equal(28, (int) Marshal.OffsetOf<RenderItem>("color"));
        }

        [Fact]
        public void testPrimitiveKindIsAFourByteIntWithTheNativeValues() {
            Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(PrimitiveKind)));
            Assert.Equal(0, (int) PrimitiveKind.Cube);
            Assert.Equal(1, (int) PrimitiveKind.Capsule);
            Assert.Equal(2, (int) PrimitiveKind.Cylinder);
            Assert.Equal(3, (int) PrimitiveKind.Sphere);
        }

        [Fact]
        public void testRgbaIsFourFloatsInChannelOrder() {
            Assert.Equal(16, Unsafe.SizeOf<Rgba>());
            Assert.Equal(0, (int) Marshal.OffsetOf<Rgba>("r"));
            Assert.Equal(4, (int) Marshal.OffsetOf<Rgba>("g"));
            Assert.Equal(8, (int) Marshal.OffsetOf<Rgba>("b"));
            Assert.Equal(12, (int) Marshal.OffsetOf<Rgba>("a"));
        }

        [Fact]
        public void testRenderItemAndRgbaDeclareSequentialLayout() {
            Assert.True(typeof(RenderItem).IsLayoutSequential);
            Assert.True(typeof(Rgba).IsLayoutSequential);
            Assert.Equal(LayoutKind.Sequential, typeof(RenderItem).StructLayoutAttribute.Value);
        }
    }
}
