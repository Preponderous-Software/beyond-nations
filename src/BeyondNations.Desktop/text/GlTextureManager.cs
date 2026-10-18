using System;
using System.Collections.Generic;
using System.Drawing;
using FontStashSharp.Interfaces;
using Silk.NET.OpenGL;
using beyondnations;

namespace beyondnations.desktop.text {

    /**
    * One OpenGL texture per font atlas, which is all FontStashSharp asks of a
    * host.
    *
    * FontStashSharp rasterises a glyph the first time it is asked for and packs
    * it into the current atlas, telling the host to upload just that rectangle.
    * That is the whole reason this interface exists: the library owns the
    * packing, the host owns the texture object.
    *
    * The atlas is deliberately not mipmapped and is filtered linearly. Nametags
    * are drawn at whatever size perspective gives them, so nearest filtering
    * makes them crawl, and mipmaps would bleed neighbouring glyphs into each
    * other at distance, since the atlas has no gutters wide enough to survive
    * being halved repeatedly.
    */
    public unsafe class GlTextureManager : ITexture2DManager, IDisposable {

        private readonly GL gl;
        private readonly List<uint> textures = new List<uint>();
        private readonly Dictionary<uint, Point> sizes = new Dictionary<uint, Point>();

        public GlTextureManager(GL gl) {
            this.gl = gl;
        }

        /**
        * How many atlas textures have been created. More than one means the
        * first filled up, which costs a draw call per extra atlas, so it is
        * worth being able to see.
        */
        public int getTextureCount() {
            return textures.Count;
        }

        public object CreateTexture(int width, int height) {
            uint texture = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, texture);
            gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                (int) InternalFormat.Rgba8,
                (uint) width,
                (uint) height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                null);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) TextureWrapMode.ClampToEdge);
            gl.BindTexture(TextureTarget.Texture2D, 0);

            textures.Add(texture);
            sizes[texture] = new Point(width, height);
            Log.info("label atlas texture created: " + width + "x" + height + " (atlas " + textures.Count + ")");
            return texture;
        }

        public Point GetTextureSize(object texture) {
            return sizes[(uint) texture];
        }

        public void SetTextureData(object texture, Rectangle bounds, byte[] data) {
            uint handle = (uint) texture;
            gl.BindTexture(TextureTarget.Texture2D, handle);

            // The sub-rectangle is tightly packed RGBA and its width is
            // arbitrary, so the default four-byte unpack alignment would be
            // wrong for any glyph whose row length is not a multiple of four.
            gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            fixed (byte* pixels = data) {
                gl.TexSubImage2D(
                    TextureTarget.Texture2D,
                    0,
                    bounds.X,
                    bounds.Y,
                    (uint) bounds.Width,
                    (uint) bounds.Height,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    pixels);
            }

            gl.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void Dispose() {
            for (int i = 0; i < textures.Count; i++) {
                gl.DeleteTexture(textures[i]);
            }
            textures.Clear();
            sizes.Clear();
        }
    }
}
