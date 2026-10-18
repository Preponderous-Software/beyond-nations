using System;
using System.Numerics;
using beyondnations;

namespace beyondnations.desktop.render {

    /**
    * Draws a world snapshot. The seam between the host and whichever
    * renderer --renderer picked (#274):
    *
    *   silk    SilkWorldRenderer, the managed renderer the game has always
    *           used, and the default
    *   native  NativeWorldRenderer, the same snapshot drawn by the Strata
    *           engine through P/Invoke, inside the same window
    *
    * Both read the snapshot and never write it, both honour the culler, and
    * both report the same counters, which is what lets the two be compared
    * draw for draw.
    */
    public interface IWorldRenderer : IDisposable {

        /** "silk" or "native", as --renderer spells it. */
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
