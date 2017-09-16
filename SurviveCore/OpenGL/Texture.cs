using System;
using OpenTK.Graphics.OpenGL4;
using System.IO;
using ImageSharp;

namespace Survive.OpenGL {

    class Texture : IDisposable {

        private int id;
        private TextureTarget target;

        public Texture(TextureTarget target, int levels, SizedInternalFormat format, int width) : this(target) {
            GL.TextureStorage1D(id, levels, format, width);
        }

        public Texture(TextureTarget target, int levels, SizedInternalFormat format, int width, int height) : this(target) {
            GL.TextureStorage2D(id, levels, format, width, height);
        }

        public Texture(TextureTarget target, int levels, SizedInternalFormat format, int width, int height, int depth) : this(target) {
            GL.TextureStorage3D(id, levels, format, width, height, depth);
        }

        public Texture(TextureTarget target) {
            GL.CreateTextures(target, 1, out id);
            this.target = target;
        }

        public int ID {
            get {
                return id;
            }
        }

        public Texture SetSubImage(int level, int xoffset, int width, OpenTK.Graphics.OpenGL4.PixelFormat format, PixelType type, IntPtr pixels) {
            GL.TextureSubImage1D(id, level, xoffset, width, format, type, pixels);
            return this;
        }

        public Texture SetSubImage<T>(int level, int xoffset, int width, OpenTK.Graphics.OpenGL4.PixelFormat format, PixelType type, T[] pixels) where T : struct{
            GL.TextureSubImage1D(id, level, xoffset, width, format, type, pixels);
            return this;
        }

        public Texture SetSubImage(int level, int xoffset, int yoffset, int width, int height, OpenTK.Graphics.OpenGL4.PixelFormat format, PixelType type, IntPtr pixels) {
            GL.TextureSubImage2D(id, level, xoffset, yoffset, width, height, format, type, pixels);
            return this;
        }
        public Texture SetSubImage<T>(int level, int xoffset, int yoffset, int width, int height, OpenTK.Graphics.OpenGL4.PixelFormat format, PixelType type, T[] pixels) where T : struct{
            GL.TextureSubImage2D(id, level, xoffset, yoffset, width, height, format, type, pixels);
            return this;
        }

        public Texture SetSubImage(int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, OpenTK.Graphics.OpenGL4.PixelFormat format, PixelType type, IntPtr pixels) {
            GL.TextureSubImage3D(id, level, xoffset, yoffset, zoffset, width, height, depth, format, type, pixels);
            return this;
        }

        public Texture SetSubImage<T>(int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, OpenTK.Graphics.OpenGL4.PixelFormat format, PixelType type, T[] pixels) where T : struct {
            GL.TextureSubImage3D(id, level, xoffset, yoffset, zoffset, width, height, depth, format, type, pixels);
            return this;
        }

        public Texture Bind(TextureUnit t) {
            GL.ActiveTexture(t);
            GL.BindTexture(target, id);
            return this;
        }

        public Texture SetWarpMode(TextureWrapMode mode) {
            GL.TextureParameter(id, TextureParameterName.TextureWrapS, (int)mode);
            GL.TextureParameter(id, TextureParameterName.TextureWrapT, (int)mode);
            GL.TextureParameter(id, TextureParameterName.TextureWrapR, (int)mode);
            return this;
        }

        public Texture SetLODBias(float v) {
            GL.TextureParameter(id, TextureParameterName.TextureLodBias, v);
            return this;
        }

        public Texture GenerateMipmap() {
            GL.GenerateTextureMipmap(id);
            return this;
        }

        public Texture SetFiltering(TextureMinFilter min, TextureMagFilter mag) {
            GL.TextureParameter(id, TextureParameterName.TextureMinFilter, (int)min);
            GL.TextureParameter(id, TextureParameterName.TextureMagFilter, (int)mag);
            return this;
        }

        public void Dispose() {
            GL.DeleteTexture(id);
        }

        public static Texture FromFile(string path) {

            using (FileStream stream = File.OpenRead(path))
            using (Image<Rgba32> img = Image.Load(stream)) {

                int levels = (int)Math.Round(Math.Log(Math.Min(img.Width, img.Height), 2));
                Texture t = new Texture(TextureTarget.Texture2D, levels, SizedInternalFormat.Rgba8, img.Width, img.Height);
                

                using (var imgdata = img.Lock()) {
                    t.SetSubImage(0, 0, 0, img.Width, img.Height, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, imgdata.PixelArray);
                }

                t.SetFiltering(TextureMinFilter.LinearMipmapLinear, TextureMagFilter.Linear);
                t.GenerateMipmap();
                return t;

            }
            
        }

        public static Texture FromFiles(int size, params string[] paths) {
            int levels = (int)Math.Round(Math.Log(size, 2));
            Texture t = new Texture(TextureTarget.Texture2DArray, levels, SizedInternalFormat.Rgba8, size, size, paths.Length);

            for(int i = 0; i < paths.Length; i++) {
                using(FileStream stream = File.OpenRead(paths[i]))
                using(Image<Rgba32> img = Image.Load(stream))
                using(var imgdata = img.Lock()) {
                    t.SetSubImage(0, 0, 0, i, img.Width, img.Height, 1, PixelFormat.Rgba, PixelType.UnsignedByte, imgdata.PixelArray);
                }
            }
        
            t.SetFiltering(TextureMinFilter.LinearMipmapLinear, TextureMagFilter.Linear);
            t.GenerateMipmap();
            return t;
        }

    }

}
