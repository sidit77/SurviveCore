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
        
    }
}