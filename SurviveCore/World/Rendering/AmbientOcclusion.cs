using System;
using System.IO;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using SurviveCore.OpenGL;

namespace SurviveCore.World.Rendering {
    class AmbientOcclusion {

        public static Texture GetAOTexture2() {
            using(BinaryReader r = new BinaryReader(new FileStream("./Assets/Ao.data", FileMode.Open, FileAccess.Read), Encoding.UTF8, false)) {
                int size = r.ReadInt32();
                Texture texture = new Texture(TextureTarget.Texture2DArray, (int)Math.Log(size, 2), SizedInternalFormat.R8, size, size, 256);

                GL.TextureSubImage3D(texture.ID, 0, 0, 0, 0, size, size, 256, PixelFormat.Red, PixelType.UnsignedByte, r.ReadBytes(size * size * 256));

                texture.SetFiltering(TextureMinFilter.LinearMipmapLinear, TextureMagFilter.Linear);
                texture.GenerateMipmap();
                return texture;
            }
        }

        public static Texture GetAOTexture3() {
            Texture texture = new Texture(TextureTarget.Texture2DArray, 6, SizedInternalFormat.R8, 32, 32, 256);

            byte[] data = new byte[32 * 32];
            for(int i = 0; i < 256; i++) {
                
                int c1 = 195 + 20 * GetAOValue(128, 1, 2, i);
                int c2 = 195 + 20 * GetAOValue(2, 4, 8, i);
                int c3 = 195 + 20 * GetAOValue(8, 16, 32, i);
                int c4 = 195 + 20 * GetAOValue(32, 64, 128, i);
                
                for(int x = 0; x < 32; x++) {
                    for(int y = 0; y < 32; y++) {
                        float dx = (float)x / 32;
                        float dy = (float)y / 32;
                        data[x * 32 + y] = (byte)(Mix(c1, c2, c3, c4, dx, dy));
                    }
                }
                GL.TextureSubImage3D(texture.ID, 0, 0, 0, i, 32, 32, 1, PixelFormat.Red, PixelType.UnsignedByte, data);
            }

            texture.SetFiltering(TextureMinFilter.LinearMipmapLinear, TextureMagFilter.Linear);
            texture.GenerateMipmap();

            return texture;
        }

        public static Texture GetAOTexture4() {
            Texture texture = new Texture(TextureTarget.Texture2DArray, 6, SizedInternalFormat.R8, 32, 32, 256);

            const int i1 = 14;
            const int i2 = 32 - 2 * i1;
            const int i3 = i2+i1;

            int[,] f = {
                {  1,   2,   2, 256, 256, 256, 128, 128,   0,  0, i1, i1},
                {  2,   2,   2, 256, 256, 256, 256, 256,  i1,  0, i2, i1},
                {  2,   2,   4,   8,   8, 256, 256, 256,  i3,  0, i1, i1},
                {128, 256, 256, 256, 256, 256, 128, 128,   0, i1, i1, i2},
                {256, 256, 256, 256, 256, 256, 256, 256,  i1, i1, i2, i2},
                {256, 256,   8,   8,   8, 256, 256, 256,  i3, i1, i1, i2},
                {128, 256, 256, 256,  32,  32,  64, 128,   0, i3, i1, i1},
                {256, 256, 256, 256,  32,  32,  32, 256,  i1, i3, i2, i1},
                {256, 256,   8,   8,  16,  32,  32, 256,  i3, i3, i1, i1}
            };

            byte[] data = new byte[32 * 32];
            for (int i = 0; i < 256; i++) {

                for(int j = 0; j < f.GetLength(0); j++) {
                    int c1 = 180 + 25 * GetAOValue(f[j, 7], f[j, 0], f[j, 1], i);
                    int c2 = 180 + 25 * GetAOValue(f[j, 1], f[j, 2], f[j, 3], i);
                    int c3 = 180 + 25 * GetAOValue(f[j, 3], f[j, 4], f[j, 5], i);
                    int c4 = 180 + 25 * GetAOValue(f[j, 5], f[j, 6], f[j, 7], i);

                    for (int x = 0; x < f[j, 10]; x++) {
                        for (int y = 0; y < f[j, 11]; y++) {
                            float dx = (float)x / f[j, 10];
                            float dy = (float)y / f[j, 11];
                            data[(y + f[j, 9]) * 32 + x + f[j, 8]] = (byte)(Mix(c1, c2, c3, c4, dx, dy));
                        }
                    }
                }

                
                GL.TextureSubImage3D(texture.ID, 0, 0, 0, i, 32, 32, 1, PixelFormat.Red, PixelType.UnsignedByte, data);
            }

            texture.SetFiltering(TextureMinFilter.LinearMipmapLinear, TextureMagFilter.Linear);
            texture.GenerateMipmap();

            return texture;
        }

        public static Texture GetAOTexture() {
            Texture texture = new Texture(TextureTarget.Texture2DArray, 4, SizedInternalFormat.R8, 8, 8, 256);

            byte[] data = new byte[8 * 8];
            for(int i = 0; i < 256; i++) {
                for(int x = 0; x < 8; x++) {
                    for(int y = 0; y < 8; y++) {
                        data[x * 8 + y] = (byte)(Math.Min(200 + GetDistance(x, y, i) * 10, 255));
                    }
                }
                GL.TextureSubImage3D(texture.ID, 0, 0, 0, i, 8, 8, 1, PixelFormat.Red, PixelType.UnsignedByte, data);
            }
            
            texture.SetFiltering(TextureMinFilter.LinearMipmapLinear, TextureMagFilter.Linear);
            texture.GenerateMipmap();

            return texture;
        }

        private static double GetDistance(int x, int y, int config) {
            int e = 400;
            
            if((config & 2) != 0)
                e = Math.Min(e, x * x);
            if((config & 8) != 0)
                e = Math.Min(e, (7 - y) * (7 - y));
            if((config & 32) != 0)
                e = Math.Min(e, (7 - x) * (7 - x));
            if((config & 128) != 0)
                e = Math.Min(e, y * y);
            

            int c = 400;
            if((config & 1) != 0)
                c = Math.Min(c, x * x + y * y);
            if((config & 4) != 0)
                c = Math.Min(c, x * x + (7 - y) * (7 - y));
            if((config & 16) != 0)
                c = Math.Min(c, (7 - x) * (7 - x) + (7 - y) * (7 - y));
            if((config & 64) != 0)
                c = Math.Min(c, (7 - x) * (7 - x) + y * y);

            

            return Math.Sqrt(Math.Min(e, c));//Math.Min(Math.Sqrt(c), Math.Sqrt(e))
        }

        private static int GetAOValue(int side1, int corner, int side2, int config) {
            bool s1 = (config & side1) != 0;
            bool c = (config & corner) != 0;
            bool s2 = (config & side2) != 0;
            if(s1 && s2) {
                return 0;
            }
            return 3 - (s1 ? 1 : 0) - (c ? 1 : 0) - (s2 ? 1 : 0);
        }

        private static int Mix(int c1, int c2, int c3, int c4, float dx, float dy) {
            float c12 = dx * c2 + (1 - dx) * c1;
            float c34 = dx * c3 + (1 - dx) * c4;
            return (int)Math.Round(dy * c34 + (1 - dy) * c12);
        }

    }
}
