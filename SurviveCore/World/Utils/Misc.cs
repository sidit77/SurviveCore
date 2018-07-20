using System;
using System.Numerics;

namespace SurviveCore.World.Utils {
    
    public enum Direction {
        PositiveY = 1,
        NegativeY = 4,
        NegativeZ = 5,
        PositiveZ = 2,
        NegativeX = 3,
        PositiveX = 0
    }

    public static class DirectionUtils {
        public static Vector3 GetDirection(this Direction d) {
            switch (d) {
                case Direction.PositiveY:
                    return  Vector3.UnitY;
                case Direction.NegativeY:
                    return -Vector3.UnitY;
                case Direction.NegativeZ:
                    return -Vector3.UnitZ;
                case Direction.PositiveZ:
                    return  Vector3.UnitZ;
                case Direction.NegativeX:
                    return -Vector3.UnitX;
                case Direction.PositiveX:
                    return  Vector3.UnitX;
                default:
                    throw new ArgumentOutOfRangeException(nameof(d), d, null);
            }
        }
    }
    
}
