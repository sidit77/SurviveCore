using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;

namespace Survive.World {
    
    static class Direction {
        public const int PositiveY = 1;
        public const int NegativeY = 4;
        public const int NegativeZ = 5;
        public const int PositiveZ = 2;
        public const int NegativeX = 3;
        public const int PositiveX = 0;
    }

    

    static class Misc {
        public static Vector3 Round(this Vector3 v) {
            v.X = (float)Math.Round(v.X);
            v.Y = (float)Math.Round(v.Y);
            v.Z = (float)Math.Round(v.Z);
            return v;
        }

    }
    
    
}
