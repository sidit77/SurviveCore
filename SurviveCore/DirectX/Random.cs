using System;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using Buffer = SharpDX.Direct3D11.Buffer;

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
            return c.A << 24 | c.B << 16 | c.G << 8 | c.R;
        }

        public static RawVector2 Raw(this Vector2 v) {
            return new RawVector2(v.X, v.Y);
        }
        
        public static void MapAndUpdate<T>(this DeviceContext context, T[] data, Resource resource) where T : struct {
            DataBox db = context.MapSubresource(resource, 0, MapMode.WriteDiscard, MapFlags.None);
            Utilities.Write(db.DataPointer, data, 0, data.Length);
            context.UnmapSubresource(resource, 0);
        }
        
        public static void MapAndUpdate<T>(this DeviceContext context, ref T data, Resource resource) where T : struct{
            DataBox db = context.MapSubresource(resource, 0, MapMode.WriteDiscard, MapFlags.None);
            Utilities.Write(db.DataPointer, ref data);
            context.UnmapSubresource(resource, 0);
        }
        
    }
    
}