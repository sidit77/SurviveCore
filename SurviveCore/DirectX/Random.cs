using System;
using System.Drawing;
using System.Numerics;
using SharpDX.D3DCompiler;
using SharpDX.Mathematics.Interop;

namespace SurviveCore.DirectX {
    public static class RandomExtensions {
        public static float NextFloat(this Random random) {
            return (float)random.NextDouble();
        }

        public static float NextFloat(this Random random, int min, int max) {
            return (float)random.NextDouble() * (max - min) + min;
        }

        public static RawColor4 Raw(this Color c) {
            return new RawColor4((float)c.R / 255, (float)c.G / 255, (float)c.B / 255, (float)c.A / 255);
        }

        public static int ToRgba(this Color c) {
            return c.R << 24 | c.G << 16 | c.B << 8 | c.A;
        }

        public static RawVector2 Raw(this Vector2 v) {
            return new RawVector2(v.X, v.Y);
        }

        public static byte[] Get(this CompilationResult cr) {
            if(cr.HasErrors)
                Console.WriteLine(cr.Message);
            return cr.Bytecode.Data;
        }
    }
    
}