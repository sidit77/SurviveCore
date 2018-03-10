using System;
using System.IO;

namespace AOTextureGenerator {
    static class Program {
        static void Main(string[] args) {

            //Array.ForEach(Reduce(new byte[] {
            //    1,1,2,2,
            //    1,1,2,2,
            //    3,3,4,4,
            //    3,3,4,4
            //}), x => Console.WriteLine(x));
            
            
            Console.WriteLine("Starting to generate file");

            const int resolution = 32;
            const int mipmap = 6;
            
            using (FileStream fs = File.Create("./Assets/Textures/AmbientOcclusion.dds")) {
                using (BinaryWriter bw = new BinaryWriter(fs)) {
                    foreach(int i in new[]{
                        0x20534444,
                        124,
                        0x1 | 0x2 | 0x4 | 0x8 | 0x1000 | 0x20000,
                        resolution,
                        resolution,
                        (resolution * 8 + 7)/8,
                        1,
                        mipmap,
                        0,0,0,0,0,0,0,0,0,0,0,
                        
                        32,
                        0x4,
                        0x30315844,
                        0,0,0,0,0,
                        
                        0x8 | 0x400000 | 0x1000,
                        0, 0, 0, 0,
                        
                        65,
                        3,
                        0,
                        256,
                        0
                        
                    }) {
                        bw.Write(i);
                    }

                    for (int i = 0; i < 256; i++) {
                        byte[] tex = GetAOTexture(i);
                        bw.Write(tex);
                        for (int j = 1; j < mipmap; j++) {
                            tex = Reduce(tex);
                            bw.Write(tex);
                        }
                    }
                }
            }
            
            Console.WriteLine("Complete!");
        }
        
        private static byte[] GetAOTexture(int level) {
            //Texture texture = new Texture(TextureTarget.Texture2DArray, 6, SizedInternalFormat.R8, 32, 32, 256);

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
            for(int j = 0; j < f.GetLength(0); j++) {
                int c1 = 180 + 25 * GetAOValue(f[j, 7], f[j, 0], f[j, 1], level);
                int c2 = 180 + 25 * GetAOValue(f[j, 1], f[j, 2], f[j, 3], level);
                int c3 = 180 + 25 * GetAOValue(f[j, 3], f[j, 4], f[j, 5], level);
                int c4 = 180 + 25 * GetAOValue(f[j, 5], f[j, 6], f[j, 7], level);

                for (int x = 0; x < f[j, 10]; x++) {
                    for (int y = 0; y < f[j, 11]; y++) {
                        float dx = (float)x / f[j, 10];
                        float dy = (float)y / f[j, 11];
                        data[(y + f[j, 9]) * 32 + x + f[j, 8]] = (byte)(Mix(c1, c2, c3, c4, dx, dy));
                    }
                }
            }
            return data;
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

        private static byte[] Reduce(byte[] input) {
            int size = (int)Math.Sqrt(input.Length) / 2;
            byte[] output = new byte[size * size];
            for (int x = 0; x < size; x++) {
                for (int y = 0; y < size; y++) {
                    //Console.WriteLine("{0} + {1} + {2} + {3}", 
                    //    (y * 2 + 0) * size * 2 + x * 2 + 0,
                    //    (y * 2 + 1) * size * 2 + x * 2 + 0,
                    //    (y * 2 + 0) * size * 2 + x * 2 + 1,
                    //    (y * 2 + 1) * size * 2 + x * 2 + 1);
                    output[y * size + x] = (byte)Math.Round(0.25 * input[(y * 2 + 0) * size * 2 + x * 2 + 0] +
                                                            0.25 * input[(y * 2 + 1) * size * 2 + x * 2 + 0] +
                                                            0.25 * input[(y * 2 + 0) * size * 2 + x * 2 + 1] +
                                                            0.25 * input[(y * 2 + 1) * size * 2 + x * 2 + 1]);
                }
            }

            return output;
        }
        
    }
}