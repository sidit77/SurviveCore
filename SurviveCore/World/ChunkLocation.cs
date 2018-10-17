using System;
using System.Net.Http.Headers;
using System.Numerics;
using SurviveCore.World.Utils;

namespace SurviveCore.World {
    
    public struct ChunkLocation {

        private readonly int x, y, z;

        public ChunkLocation(int x, int y, int z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static ChunkLocation FromPos(float x, float y, float z) {
            return new ChunkLocation((int)Math.Floor((double)x / Chunk.Size),(int)Math.Floor((double)y / Chunk.Size),(int)Math.Floor((double)z / Chunk.Size));
        }
        
        public static ChunkLocation FromPos(Vector3 pos) {
            return FromPos(pos.X, pos.Y, pos.Z);
        }
        
        public Vector3 Min => new Vector3(x,y,z) * Chunk.Size;
        public Vector3 Max => Min + new Vector3(Chunk.Size);
        public int X => x;
        public int Y => y;
        public int Z => z;
        public int WX => x << Chunk.BPC;
        public int WY => y << Chunk.BPC;
        public int WZ => z << Chunk.BPC;

        public ChunkLocation GetAdjecent(Direction direction) {
            switch(direction) {
                case Direction.NegativeX: return new ChunkLocation(x - 1, y    , z    );
                case Direction.NegativeY: return new ChunkLocation(x    , y - 1, z    );
                case Direction.NegativeZ: return new ChunkLocation(x    , y    , z - 1);
                case Direction.PositiveX: return new ChunkLocation(x + 1, y    , z    );
                case Direction.PositiveY: return new ChunkLocation(x    , y + 1, z    );
                case Direction.PositiveZ: return new ChunkLocation(x    , y    , z + 1);
                default: throw new ArgumentException(direction + " isnt a direction");
            }
        }

        public ChunkLocation GetOffset(int x, int y, int z) {
            return new ChunkLocation(this.x + x, this.y + y, this.z + z);
        }
        
        public override bool Equals(object obj) {
            if (!(obj is ChunkLocation o))
                return false;
            return x == o.x && y == o.y && z == o.z;
        }

        public override int GetHashCode() {
            return (x * 1619) ^ (y * 31337) ^ (z * 6971);
        }

        public override string ToString() {
            return String.Format("[{0}|{1}|{2}]",x,y,z);
        }
    }
    
}