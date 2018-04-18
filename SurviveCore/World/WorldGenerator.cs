using System;

namespace SurviveCore.World {
    public interface IWorldGenerator {
        void FillChunk(Chunk chunk);
    }
    
    public class DefaultWorldGenerator : IWorldGenerator{

        private readonly FastNoise fn;

        [ThreadStatic]
        private static float[,,] noisecache;
        
        public DefaultWorldGenerator(int seed) {
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
        
    }


    public class NewWorldGenerator : IWorldGenerator {

        private FastNoise fn;
        
        public NewWorldGenerator(int seed) {
            fn = new FastNoise(seed);
        }
        
        public void FillChunk(Chunk chunk) {

            ChunkLocation l = chunk.Location;
            
            
            fn.SetFrequency(0.0025f);
            fn.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            fn.SetFractalOctaves(3);
            fn.SetFractalGain(0.3f);
            fn.SetFractalLacunarity(3);
            fn.SetGradientPerturbAmp(30);
            for(int x = 0; x < Chunk.Size; x++) {
                for(int y = 0; y < Chunk.Size; y++) {
                    for(int z = 0; z < Chunk.Size; z++) {
                        int loc = (l.WY + y - 20);
                        float noise = 0.4f + fn.GetNoise(x + l.WX, z + l.WZ) * 0.7f;
                        chunk.SetBlockDirect(x, y, z, (loc - 130 * noise * noise)  < 0 ? Blocks.Stone : Blocks.Air);
                    }
                }
            }
            
        }
    }
    
   
}

