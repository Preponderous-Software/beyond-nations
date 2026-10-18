using System;
using Xunit;

using beyondnations.desktop.render;

namespace beyondnationstests.desktop {

    /**
    * Some drivers lack GL 4.5 functions that Strata's loader asks for but
    * never calls (glGetnTexImage on Intel UHD 630 under Windows). The lookup
    * has to give null for those rather than throw, or the native renderer
    * cannot start on that hardware.
    */
    public class TestGlFunctionResolver {

        [Fact]
        public void testAFoundFunctionIsReturned() {
            // setup
            Func<string, IntPtr> lookup = name => new IntPtr(42);

            // execute
            IntPtr address = NativeWorldRenderer.resolveOptional(lookup, "glClear");

            // verify
            Assert.Equal(new IntPtr(42), address);
        }

        [Fact]
        public void testAMissingFunctionResolvesToNullInsteadOfThrowing() {
            // setup
            Func<string, IntPtr> lookup = name => throw new InvalidOperationException("Native symbol not found (Symbol: " + name + ")");

            // execute
            IntPtr address = NativeWorldRenderer.resolveOptional(lookup, "glGetnTexImage");

            // verify
            Assert.Equal(IntPtr.Zero, address);
        }
    }
}
