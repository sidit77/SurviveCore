using System;
using System.Diagnostics;
using System.Numerics;
using SurviveCore.OpenGL.Helper;
using SurviveCore.World.Rendering;

namespace SurviveCore.World {

    class BlockWorld : IDisposable{

        ChunkRenderer[,,] chunks;

        public BlockWorld() {
            chunks = new ChunkRenderer[10, 6, 10];

            FastNoise noise = new FastNoise((int)Stopwatch.GetTimestamp());
            
            for(int x = 0; x < chunks.GetLength(0); x++) {
                for(int y = 0; y < chunks.GetLength(1); y++) {
                    for(int z = 0; z < chunks.GetLength(2); z++) {
                        chunks[x, y, z] = new ChunkRenderer(x, y, z, ChunkManager.GetChunk(x, y, z, noise));
                    }
                }
            }

            for(int x = 0; x < chunks.GetLength(0); x++) {
                for(int y = 0; y < chunks.GetLength(1); y++) {
                    for(int z = 0; z < chunks.GetLength(2); z++) {
                        if(x > 0)
                            chunks[x, y, z].Chunk.SetNeighbor(Direction.NegativeX, chunks[x - 1, y, z].Chunk);
                        if(y > 0)
                            chunks[x, y, z].Chunk.SetNeighbor(Direction.NegativeY, chunks[x, y - 1, z].Chunk);
                        if(z > 0)
                            chunks[x, y, z].Chunk.SetNeighbor(Direction.NegativeZ, chunks[x, y, z - 1].Chunk);
                        if(x < chunks.GetLength(0) - 1)
                            chunks[x, y, z].Chunk.SetNeighbor(Direction.PositiveX, chunks[x + 1, y, z].Chunk);
                        if(y < chunks.GetLength(1) - 1)
                            chunks[x, y, z].Chunk.SetNeighbor(Direction.PositiveY, chunks[x, y + 1, z].Chunk);
                        if(z < chunks.GetLength(2) - 1)
                            chunks[x, y, z].Chunk.SetNeighbor(Direction.PositiveZ, chunks[x, y, z + 1].Chunk);
                    }
                }
            }
            


        }

        public void Draw(Frustum frustum) {
            for(int x = 0; x < chunks.GetLength(0); x++) {
                for(int y = 0; y < chunks.GetLength(1); y++) {
                    for(int z = 0; z < chunks.GetLength(2); z++) {
                        if(frustum.Intersection(chunks[x, y, z].Position, chunks[x, y, z].Position + new Vector3(WorldChunk.Size)))
                            chunks[x, y, z].Draw();
                    }
                }
            }
        }
 
        public Block GetBlock(int x, int y, int z) {
            return chunks[0, 0, 0].Chunk.GetBlock(x,y,z);
        }

        public bool SetBlock(int x, int y, int z, Block b) {
            return chunks[0, 0, 0].Chunk.SetBlock(x, y, z, b);
        }

        public bool SetBlock(int x, int y, int z, Block b, byte m) {
            return chunks[0, 0, 0].Chunk.SetBlock(x, y, z, b, m);
        }

        public Block GetBlock(Vector3 vec) {
            return chunks[0, 0, 0].Chunk.GetBlock((int)Math.Round(vec.X), (int)Math.Round(vec.Y), (int)Math.Round(vec.Z));
        }

        public bool SetBlock(Vector3 vec, Block b) {
            return chunks[0, 0, 0].Chunk.SetBlock((int)Math.Round(vec.X), (int)Math.Round(vec.Y), (int)Math.Round(vec.Z), b);
        }

        public bool SetBlock(Vector3 vec, Block b, byte m) {
            return chunks[0, 0, 0].Chunk.SetBlock((int)Math.Round(vec.X), (int)Math.Round(vec.Y), (int)Math.Round(vec.Z), b, m);
        }

        public void Dispose() {
            for(int x = 0; x < chunks.GetLength(0); x++) {
                for(int y = 0; y < chunks.GetLength(1); y++) {
                    for(int z = 0; z < chunks.GetLength(2); z++) {
                        chunks[x, y, z].Dispose();
                    }
                }
            }
        }

    }

}
