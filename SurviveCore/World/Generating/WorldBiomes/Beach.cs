using System;

namespace SurviveCore.World.Generating.WorldBiomes {
    
    public class BeachBiome : Biome {
        
        public override void FillChunk(Chunk c, Random r, int x, int z, int height) {
            for (int y = 0; y < Chunk.Size; y++) {
                if(c.Location.WY + y <= height - 4)
                    c.SetBlockDirect(x, y, z, Blocks.Stone);
                else if(c.Location.WY + y <= height)
                    c.SetBlockDirect(x, y, z, Blocks.Sand);
                else if(c.Location.WY + y <= AdvancedWorldGenerator.SeaLevel)
                    c.SetBlockDirect(x, y, z, Blocks.Water);
            }
        }
    }

    public class BeachSelector : BiomeSelector {
        
        private readonly Biome beach = new BeachBiome();
        
        public BeachSelector(int p) : base(p) {
        }
        
        public override Biome GetBiome(int x, int z, int height, float temperature, float humidity) {
            if (height == AdvancedWorldGenerator.SeaLevel - 1 || height == AdvancedWorldGenerator.SeaLevel || height == AdvancedWorldGenerator.SeaLevel + 1)
                return beach;
            return null;
        }

        
    }
    
}