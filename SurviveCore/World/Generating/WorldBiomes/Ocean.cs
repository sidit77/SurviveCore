using System;

namespace SurviveCore.World.Generating.WorldBiomes {
    public class OceanBiome : Biome {
        public override void FillChunk(Chunk c, Random r, int x, int z, int height) {
            for (int y = 0; y < Chunk.Size; y++) {
                if(c.Location.WY + y <= height - 1 - (height / 20))
                    c.SetBlockDirect(x, y, z, Blocks.Stone);
                else if(c.Location.WY + y <= height)
                    c.SetBlockDirect(x, y, z, Blocks.Sand);
                else if(c.Location.WY + y <= AdvancedWorldGenerator.SeaLevel)
                    c.SetBlockDirect(x, y, z, Blocks.Water);
            }
        }
    }

    public class OceanSelector : BiomeSelector {
        
        private readonly Biome ocean = new OceanBiome();
        
        public OceanSelector(int p) : base(p) {
        }

        public override Biome GetBiome(int x, int z, int height, float temperature, float humidity) {
            return height < AdvancedWorldGenerator.SeaLevel ? ocean : null;
        }
    }
    
}