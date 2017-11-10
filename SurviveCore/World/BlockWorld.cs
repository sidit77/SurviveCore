using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using SurviveCore.OpenGL.Helper;
using SurviveCore.World.Rendering;

namespace SurviveCore.World {

    class BlockWorld : IDisposable{

        public const int Height = 8;
        public const int Distance = 4;

        private ChunkRenderer[,][] chunks;

        private int centerX;
        private int centerZ;

        private readonly FastNoise noise;

        public BlockWorld() {
            chunks = new ChunkRenderer[Distance * 2 + 1, Distance * 2 + 1][];

            noise = new FastNoise((int)Stopwatch.GetTimestamp());
            
            for(int x = 0; x <= Distance * 2; x++) {
                for(int z = 0; z <= Distance * 2; z++) {
                    chunks[x, z] = ChunkManager.GetChunkRenderer(x - Distance, z - Distance, noise);
                }
            }

            for(int x = 0; x <= Distance * 2; x++) {
                for(int z = 0; z <= Distance * 2; z++) {
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
            for (int x = 0; x <= Distance * 2; x++) {
                for (int z = 0; z <= Distance * 2; z++) {
                    for (int y = 0; y < Height; y++) {
                        if (frustum.Intersection(chunks[x, z][y].Position, chunks[x, z][y].Position + new Vector3(Chunk.Size)))
                            chunks[x, z][y].Draw();
                    }
                }
            }
        }

        public void Recenter(int cx, int cz) {
            if(centerX != cx || centerZ != cz) {
                ChunkRenderer[,][] newchunks = new ChunkRenderer[Distance * 2 + 1, Distance * 2 + 1][];
                int dx = cx - centerX;
                int dz = cz - centerZ;
                for(int x = 0; x <= Distance * 2 - Math.Abs(dx); x++) {
                    for(int z = 0; z <= Distance * 2 - Math.Abs(dz); z++) {
                        newchunks[x - Math.Min(0, dx), z - Math.Min(0, dz)] = chunks[x + Math.Max(0, dx), z + Math.Max(0, dz)];
                    }
                }
                for(int x = 0; x <= Distance * 2; x++) {
                    for(int z = 0; z <= Distance * 2; z++) {
                        if(newchunks[x, z] == null) {
                            newchunks[x, z] = chunks[Distance * 2 - x, Distance * 2 - z];
                            for(int y = 0; y < Height; y++) {
                                newchunks[x, z][y].Chunk.Delete();
                                newchunks[x, z][y].X = cx + x - Distance;
                                newchunks[x, z][y].Z = cz + z - Distance;
                                newchunks[x, z][y].Chunk = ChunkManager.GetChunk(newchunks[x, z][y].X, newchunks[x, z][y].Y, newchunks[x, z][y].Z, noise);
                            }
                        }
                    }
                }
                chunks = newchunks;
                for (int x = 0; x <= Distance * 2; x++) {
                    for (int z = 0; z <= Distance * 2; z++) {
                        for (int y = 0; y < Height; y++) {
                            if (x > 0)
                                chunks[x, z][y].Chunk.SetNeighbor(Direction.NegativeX, chunks[x - 1, z][y].Chunk);
                            if (z > 0)
                                chunks[x, z][y].Chunk.SetNeighbor(Direction.NegativeZ, chunks[x, z - 1][y].Chunk);
                            if (y > 0)
                                chunks[x, z][y].Chunk.SetNeighbor(Direction.NegativeY, chunks[x, z][y - 1].Chunk);
                        }
                    }
                }
                centerX = cx;
                centerZ = cz;
            }
        }

        public Block GetBlock(int x, int y, int z) {
            return chunks[Distance, Distance][Height / 2].Chunk.GetBlock(x - centerX * Chunk.Size,y - Height / 2 * Chunk.Size, z - centerZ * Chunk.Size);
        }

        public bool SetBlock(int x, int y, int z, Block b) {
            return chunks[Distance, Distance][Height / 2].Chunk.SetBlock(x - centerX * Chunk.Size, y - Height / 2 * Chunk.Size, z - centerZ * Chunk.Size, b);
        }

        public bool SetBlock(int x, int y, int z, Block b, byte m) {
            return chunks[Distance, Distance][Height / 2].Chunk.SetBlock(x - centerX * Chunk.Size, y - Height / 2 * Chunk.Size, z - centerZ * Chunk.Size, b, m);
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
            for(int x = 0; x <= Distance * 2; x++) {
                for(int z = 0; z <= Distance * 2; z++) {
                    for(int y = 0; y < Height; y++) {
                        chunks[x, z][y].Dispose();
                    }
                }
            }
        }

    }

}
