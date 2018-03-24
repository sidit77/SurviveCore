using System;
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
        
        public static unsafe void UpdateBuffer<T>(this DeviceContext context, Buffer buffer, T[] newValues, int count, int sourceOffset = 0, int destinationOffset = 0) where T : struct{
            var handle = GCHandle.Alloc(newValues, GCHandleType.Pinned);
            var bytes = (byte*)handle.AddrOfPinnedObject();
            var strideInBytes = Marshal.SizeOf<T>();
            context.UpdateSubresource(
                new DataBox(new IntPtr(bytes + sourceOffset * strideInBytes)), buffer, 0,
                new ResourceRegion(
                    destinationOffset * strideInBytes, 0, 0,
                    (destinationOffset + count) * strideInBytes, 1, 1));
            handle.Free();
        }
        
        public static unsafe void UpdateBuffer<T>(this DeviceContext context, Buffer buffer, T[] newValues) where T : struct{
            UpdateBuffer(context, buffer, newValues, newValues.Length);
        }
        
    }
    
}