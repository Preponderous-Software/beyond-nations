using System;
using System.IO;
using Silk.NET.OpenGL;
using StbImageWriteSharp;
using beyondnations;

namespace beyondnations.desktop {

    /**
    * Captures the current framebuffer with glReadPixels and encodes it as a
    * PNG with StbImageWriteSharp. Replaces Unity's
    * ScreenCapture.CaptureScreenshot, which no longer exists on this host
    * (#224).
    */
    public static class ScreenshotCapture {

        private const int BytesPerPixel = 4; // RGBA

        /**
        * Reads the current framebuffer and writes it as a timestamped PNG
        * under the given directory (created if missing). Returns the path
        * written to.
        */
        public static string capture(GL gl, int width, int height, string directory) {
            if (gl == null) {
                throw new ArgumentNullException(nameof(gl));
            }
            if (width <= 0 || height <= 0) {
                throw new ArgumentException("width and height must both be positive, got " + width + "x" + height);
            }

            Directory.CreateDirectory(directory);

            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            string filename = "screenshot_" + timestamp + ".png";
            string path = Path.Combine(directory, filename);

            byte[] pixels = new byte[width * height * BytesPerPixel];

            // RGBA is always 4-byte aligned regardless of width, so the pack
            // alignment does not strictly matter here, but setting it to 1
            // keeps this correct if the format above is ever narrowed to RGB.
            gl.PixelStore(PixelStoreParameter.PackAlignment, 1);
            gl.ReadPixels(0, 0, (uint) width, (uint) height, GLEnum.Rgba, GLEnum.UnsignedByte, out pixels[0]);

            byte[] topDown = flipVertically(pixels, width, height, BytesPerPixel);

            using (FileStream stream = File.Create(path)) {
                ImageWriter writer = new ImageWriter();
                writer.WritePng(topDown, width, height, ColorComponents.RedGreenBlueAlpha, stream);
            }

            Log.info("Screenshot saved to " + path);
            return path;
        }

        /**
        * glReadPixels returns rows bottom-up (row 0 is the bottom of the
        * framebuffer), but PNG rows are stored top-down. Skipping this flip
        * is the classic screenshot bug: the file is a valid PNG at the
        * right size, so nothing looks wrong until a person actually opens
        * it upside down.
        */
        internal static byte[] flipVertically(byte[] pixels, int width, int height, int bytesPerPixel) {
            int stride = width * bytesPerPixel;
            byte[] flipped = new byte[pixels.Length];
            for (int row = 0; row < height; row++) {
                int sourceOffset = row * stride;
                int destinationOffset = (height - 1 - row) * stride;
                Array.Copy(pixels, sourceOffset, flipped, destinationOffset, stride);
            }
            return flipped;
        }
    }
}
