using System;
using System.Diagnostics;
using System.Numerics;
using SurviveCore.OpenGL.Helper;
using SurviveCore.World.Rendering;

namespace SurviveCore.World {

    class BlockWorld : IDisposable{

        public const int Height = 8;

        private readonly ChunkRenderer[,][] chunks;

        public BlockWorld() {
            chunks = new ChunkRenderer[10, 10][];

            FastNoise noise = new FastNoise((int)Stopwatch.GetTimestamp());
            
            for(int x = 0; x < chunks.GetLength(0); x++) {
                for(int z = 0; z < chunks.GetLength(1); z++) {
                    chunks[x, z] = ChunkManager.GetChunkRenderer(x, z, noise);
                }
            }

            for(int x = 0; x < chunks.GetLength(0); x++) {
                for(int z = 0; z < chunks.GetLength(1); z++) {
                    for (int y = 0; y < Height; y++) {
                        if(x > 0)
                            chunks[x, z][y].Chunk.SetNeighbor(Direction.NegativeX, chunks[x - 1, z][y].Chunk);
                        if(z > 0)
                            chunks[x, z][y].Chunk.SetNeighbor(Direction.NegativeZ, chunks[x, z - 1][y].Chunk);
                    }
                }
            }
            


        }

        public void Draw(Frustum frustum) {
            for (int x = 0; x < chunks.GetLength(0); x++) {
                for (int z = 0; z < chunks.GetLength(1); z++) {
                    for (int y = 0; y < Height; y++) {
                        if (frustum.Intersection(chunks[x, z][y].Position, chunks[x, z][y].Position + new Vector3(WorldChunk.Size)))
                            chunks[x, z][y].Draw();
                    }
                }
            }
        }
 
        public Block GetBlock(int x, int y, int z) {
            return chunks[0, 0][Height / 2].Chunk.GetBlock(x,y - Height / 2 * WorldChunk.Size, z);
        }

        public bool SetBlock(int x, int y, int z, Block b) {
            return chunks[0, 0][Height / 2].Chunk.SetBlock(x, y - Height / 2 * WorldChunk.Size, z, b);
        }

        public bool SetBlock(int x, int y, int z, Block b, byte m) {
            return chunks[0, 0][Height / 2].Chunk.SetBlock(x, y - Height / 2 * WorldChunk.Size, z, b, m);
        }
        
        public Block GetBlock(Vector3 vec) {
            return GetBlock((int)Math.Round(vec.X), (int)Math.Round(vec.Y), (int)Math.Round(vec.Z));
        }

        public bool SetBlock(Vector3 vec, Block b) {
            return SetBlock((int)Math.Round(vec.X), (int)Math.Round(vec.Y), (int)Math.Round(vec.Z), b);
        }

        public bool SetBlock(Vector3 vec, Block b, byte m) {
            return SetBlock((int)Math.Round(vec.X), (int)Math.Round(vec.Y), (int)Math.Round(vec.Z), b, m);
        }

        public void Dispose() {
            for(int x = 0; x < chunks.GetLength(0); x++) {
                for(int z = 0; z < chunks.GetLength(1); z++) {
                    for(int y = 0; y < Height; y++) {
                        chunks[x, z][y].Dispose();
                    }
                }
            }
        }

    }

}
