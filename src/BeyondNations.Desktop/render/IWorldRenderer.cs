using System;
using System.Numerics;
using beyondnations;

namespace beyondnations.desktop.render {

    /**
    * Draws a world snapshot. The seam between the host and the renderer
    * (#274). The desktop draws with NativeWorldRenderer, the Strata engine
    * through P/Invoke inside the Silk.NET window. A renderer reads the
    * snapshot and never writes it, and honours the culler.
    */
    public interface IWorldRenderer : IDisposable {

        /** A short name for logs, e.g. "strata". */
        string getName();

        /**
        * Draws everything in the snapshot that the culler keeps (all of it
        * when the culler is null) into the current framebuffer, over whatever
        * the host has already cleared or drawn.
        */
        void render(WorldSnapshot snapshot, Matrix4x4 view, Matrix4x4 projection, RenderCuller culler);

        /** The framebuffer size, in pixels, after a resize. */
        void setViewportSize(int width, int height);

        int getDrawCallCount();
        int getInstancesDrawn();
        int getInstancesCulled();
        int getInstanceCount(PrimitiveKind kind);
    }
}
