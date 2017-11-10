using System;
using System.Collections.Generic;
using System.Text;

namespace SurviveCore.World{

    class ChunkTower {

        public const int Height = 8;
        private Chunk[] chunks;

        public ChunkTower(int x, int y, int z, FastNoise noise) {
            float[,,] noisecache = new float[WorldChunk.Size, WorldChunk.Size * Height + 1, WorldChunk.Size];
            for (int bx = 0; bx < WorldChunk.Size; bx++) {
                for (int by = 0; by < WorldChunk.Size * Height + 1; by++) {
                    for (int bz = 0; bz < WorldChunk.Size; bz++) {
                        noisecache[bx, by, bz] = 0.5f - ((float)(y * WorldChunk.Size + by) / 40) + noise.GetSimplexFractal(x * WorldChunk.Size + bx, y * WorldChunk.Size + by, z * WorldChunk.Size + bz);
                    }
                }
            }

            chunks = new Chunk[Height];
            for(int i = 0; i < chunks.Length; i++) {
                chunks[i] = new WorldChunk();
                if(i > 0)
                    chunks[i].SetNeighbor(Direction.PositiveY, chunks[i - 1]);

                for (int bx = 0; bx < WorldChunk.Size; bx++) {
                    for (int by = 0; by < WorldChunk.Size; by++) {
                        for (int bz = 0; bz < WorldChunk.Size; bz++) {
                            chunks[i].SetBlockDirect(bx, by, bz, noisecache[bx, i * WorldChunk.Size + by + 1, bz] > 0 ? Blocks.Stone : noisecache[bx, i * WorldChunk.Size + by, bz] > 0 ? Blocks.Grass : Blocks.Air);
                        }
                    }
                }
            }
        }

    }

}
