using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SurviveCore.DirectX {
    public static class MathHelper {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float v, float min, float max) {
            if(v < min)
                return min;
            if(v > max)
                return max;
            return v;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Clamp(int v, int min, int max) {
            if(v < min)
                return min;
            if(v > max)
                return max;
            return v;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Round(this Vector3 v) {
            return new Vector3(MathF.Round(v.X),MathF.Round(v.Y),MathF.Round(v.Z));
        }
    }
}