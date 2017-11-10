using SurviveCore.World.Rendering;

namespace SurviveCore.World {

    class ChunkManager {

        public static WorldChunk GetEmptyChunk() {
            WorldChunk chunk = new WorldChunk();
            for (int bx = 0; bx < Chunk.Size; bx++) {
                for (int by = 0; by < Chunk.Size; by++) {
                    for (int bz = 0; bz < Chunk.Size; bz++) {
                        chunk.SetBlockDirect(bx, by, bz, Blocks.Air);
                    }
                }
            }
            chunk.SetBlockDirect(8, 8, 8, Blocks.Bricks);
            return chunk;
        }

        public static WorldChunk GetChunk(int x, int y, int z, FastNoise fn) {
            float[,,] noisecache = new float[Chunk.Size, Chunk.Size + 1, Chunk.Size];

            WorldChunk chunk = new WorldChunk();

            for(int bx = 0; bx < Chunk.Size; bx++) {
                for(int by = 0; by < Chunk.Size + 1; by++) {
                    for(int bz = 0; bz < Chunk.Size; bz++) {
                        noisecache[bx, by, bz] = 0.5f - ((float)(y * Chunk.Size + by) / 40) + fn.GetSimplexFractal(x * WorldChunk.Size + bx, y * WorldChunk.Size + by, z * WorldChunk.Size + bz);
                    }
                }
            }

            for(int bx = 0; bx < Chunk.Size; bx++) {
                for(int by = 0; by < Chunk.Size; by++) {
                    for(int bz = 0; bz < Chunk.Size; bz++) {
                        chunk.SetBlockDirect(bx, by, bz, noisecache[bx, by + 1, bz] > 0 ? Blocks.Stone : noisecache[bx, by, bz] > 0 ? Blocks.Grass : Blocks.Air);
                    }
                }
            }
            
            return chunk;
        }

        public static ChunkRenderer[] GetChunkRenderer(int x, int z, FastNoise noise) {
            ChunkRenderer[] chunks = new ChunkRenderer[BlockWorld.Height];
            for (int i = 0; i < chunks.Length; i++) {
                chunks[i] = new ChunkRenderer(x, i, z, GetChunk(x, i, z, noise));
                if (i > 0)
                    chunks[i].Chunk.SetNeighbor(Direction.NegativeY, chunks[i - 1].Chunk);
            }
            return chunks;
        }

    }

}
