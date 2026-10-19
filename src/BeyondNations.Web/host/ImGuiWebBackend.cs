using System;
using System.Numerics;
using ImGuiNET;

namespace beyondnations.web.host {

    /**
    * Dear ImGui's platform and renderer backend for the browser: the job
    * Silk.NET.OpenGL.Extensions.ImGui's ImGuiController does on the desktop.
    *
    * Platform: display size and framebuffer scale come from the canvas each
    * frame (CSS pixels and devicePixelRatio), and pointer, wheel and text
    * events are queued into ImGui as the browser delivers them. ImGui's input
    * queue plays a press and a release that arrive between two frames back
    * over two frames, so a tap shorter than a frame is never lost -- what
    * UiHost's PressLatch replay does on the desktop, where the controller
    * polls (#290).
    *
    * Renderer: the draw lists are handed to wwwroot/webhost/gfx.js by
    * pointer, which draws them with WebGL2 the way imgui_impl_opengl3 does.
    * WebGL2 has no glDrawElementsBaseVertex, so RendererHasVtxOffset is left
    * unset and ImGui keeps every list's indices within 16 bits by itself.
    */
    public unsafe class ImGuiWebBackend : IDisposable {
        private const int FloatsPerCommand = 8;

        private readonly IntPtr context;
        private int fontTexture;
        private float[] commands = new float[64 * FloatsPerCommand];
        private double framebufferScale = 1.0;
        private double displayWidth;
        private double displayHeight;

        /**
        * fontRasterScale is the devicePixelRatio the font atlas is rasterised
        * for. The font is the desktop's (ImGui's built-in ProggyClean at 13
        * px) and lays out at exactly the same size; on a 2x or 3x screen its
        * glyphs are rasterised at 26 or 39 px and drawn at half or a third,
        * so text is as sharp as the screen rather than a 13 px atlas
        * stretched over it.
        */
        public ImGuiWebBackend(double fontRasterScale) {
            context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);

            ImGuiIOPtr io = ImGui.GetIO();
            // No imgui.ini: there is no file system worth writing to, and the
            // screens place every window themselves anyway.
            io.NativePtr->IniFilename = null;

            float scale = (float) Math.Clamp(Math.Ceiling(fontRasterScale * 2.0) / 2.0, 1.0, 3.0);
            ImFontConfig* config = ImGuiNative.ImFontConfig_ImFontConfig();
            config->SizePixels = 13f * scale;
            config->OversampleH = 1;
            config->OversampleV = 1;
            config->PixelSnapH = 1;
            io.Fonts.AddFontDefault(new ImFontConfigPtr(config));
            ImGuiNative.ImFontConfig_destroy(config);
            io.FontGlobalScale = 1f / scale;

            ImGui.StyleColorsDark();
            uploadFontAtlas();
        }

        private void uploadFontAtlas() {
            ImGuiIOPtr io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);
            fontTexture = WebGpu.createTexture(width, height, true);
            WebGpu.updateTexture(fontTexture, 0, 0, width, height, (int) pixels, width * height * bytesPerPixel);
            io.Fonts.SetTexID((IntPtr) fontTexture);
            io.Fonts.ClearTexData();
        }

        /** Starts an ImGui frame for a canvas of the given CSS size. */
        public void newFrame(double deltaSeconds, double cssWidth, double cssHeight, double devicePixelRatio) {
            ImGuiIOPtr io = ImGui.GetIO();
            displayWidth = cssWidth;
            displayHeight = cssHeight;
            framebufferScale = devicePixelRatio;
            io.DisplaySize = new Vector2((float) cssWidth, (float) cssHeight);
            io.DisplayFramebufferScale = new Vector2((float) devicePixelRatio, (float) devicePixelRatio);
            io.DeltaTime = (float) Math.Max(deltaSeconds, 1e-4);
            ImGui.NewFrame();
        }

        /** Ends the frame and draws it over whatever is in the framebuffer. */
        public void render() {
            ImGui.Render();
            ImDrawDataPtr drawData = ImGui.GetDrawData();
            Vector2 origin = drawData.DisplayPos;

            for (int n = 0; n < drawData.CmdListsCount; n++) {
                ImDrawListPtr list = drawData.CmdLists[n];
                int commandCount = list.CmdBuffer.Size;
                if (commands.Length < commandCount * FloatsPerCommand) {
                    commands = new float[commandCount * FloatsPerCommand * 2];
                }

                int written = 0;
                for (int c = 0; c < commandCount; c++) {
                    ImDrawCmdPtr command = list.CmdBuffer[c];
                    if (command.UserCallback != IntPtr.Zero || command.ElemCount == 0) {
                        continue;
                    }
                    int at = written * FloatsPerCommand;
                    commands[at + 0] = command.ClipRect.X - origin.X;
                    commands[at + 1] = command.ClipRect.Y - origin.Y;
                    commands[at + 2] = command.ClipRect.Z - origin.X;
                    commands[at + 3] = command.ClipRect.W - origin.Y;
                    commands[at + 4] = (float) command.TextureId;
                    commands[at + 5] = command.VtxOffset;
                    commands[at + 6] = command.IdxOffset;
                    commands[at + 7] = command.ElemCount;
                    written++;
                }
                if (written == 0) {
                    continue;
                }

                fixed (float* commandData = commands) {
                    WebGpu.drawImGuiList(
                        (int) list.VtxBuffer.Data, list.VtxBuffer.Size,
                        (int) list.IdxBuffer.Data, list.IdxBuffer.Size,
                        (int) commandData, written,
                        displayWidth, displayHeight, framebufferScale);
                }
            }
        }

        // --- input, queued as it arrives ---

        public void pointerMove(double x, double y) {
            ImGui.GetIO().AddMousePosEvent((float) x, (float) y);
        }

        public void pointerButton(int button, bool down) {
            if (button < 0 || button > 4) {
                return;
            }
            ImGui.GetIO().AddMouseButtonEvent(button, down);
        }

        /** DOM wheel deltas are pixels, positive down; ImGui's are steps, positive up. */
        public void wheel(double dx, double dy) {
            ImGui.GetIO().AddMouseWheelEvent((float) (-dx / 100.0), (float) (-dy / 100.0));
        }

        public void character(int codepoint) {
            if (codepoint > 0 && codepoint < 0x10000) {
                ImGui.GetIO().AddInputCharacter((uint) codepoint);
            }
        }

        public void focusLost() {
            ImGuiIOPtr io = ImGui.GetIO();
            for (int button = 0; button < 3; button++) {
                io.AddMouseButtonEvent(button, false);
            }
            io.AddFocusEvent(false);
        }

        public void focusGained() {
            ImGui.GetIO().AddFocusEvent(true);
        }

        public void Dispose() {
            ImGui.DestroyContext(context);
        }
    }
}
