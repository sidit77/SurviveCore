using System;

namespace SurviveCore.World.Generating {
    public class SimpleWorldGenerator : IWorldGenerator{
        private readonly FastNoise fn;

        [ThreadStatic]
        private static float[,,] noisecache;
        
        public SimpleWorldGenerator(int seed) {
            fn = new FastNoise(seed);
        }
        
        public void FillChunk(Chunk chunk) {
            if (noisecache == null)
                noisecache = new float[Chunk.Size, Chunk.Size + 1, Chunk.Size];

            int x = chunk.Location.X << Chunk.BPC;
            int y = chunk.Location.Y << Chunk.BPC;
            int z = chunk.Location.Z << Chunk.BPC;
            
            for(int bx = 0; bx < Chunk.Size; bx++) {
                for(int by = 0; by < Chunk.Size + 1; by++) {
                    for(int bz = 0; bz < Chunk.Size; bz++) {
                        noisecache[bx, by, bz] = 0.5f - ((float)(y + by) / 40) + fn.GetSimplexFractal(x + bx, y + by, z + bz);
                    }
                }
            }

            for(int bx = 0; bx < Chunk.Size; bx++) {
                for(int by = 0; by < Chunk.Size; by++) {
                    for(int bz = 0; bz < Chunk.Size; bz++) {
                        chunk.SetBlockDirect(bx, by, bz, noisecache[bx, by + 1, bz] > 0 ? Blocks.Stone : noisecache[bx, by, bz] > 0 ? Blocks.Grass : Blocks.Air, UpdateSource.Generation);
                    }
                }
            }
        }
        public void DecorateChunk(Chunk c){}
    }
}