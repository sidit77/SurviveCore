using System;

namespace SurviveCore.World.Generating.WorldBiomes {
    public class PlainsBiome : Biome {
        public override void FillChunk(Chunk c, Random r, int x, int z, float height) {
            for (int y = 0; y < Chunk.Size; y++) {
                if(c.Location.WY + y <= height - 4)
                    c.SetBlockDirect(x, y, z, Blocks.Stone);
                else if(c.Location.WY + y <= height - 1)
                    c.SetBlockDirect(x, y, z, Blocks.Dirt);
                else if(c.Location.WY + y <= height)
                    c.SetBlockDirect(x, y, z, Blocks.Grass);
            }
        }
    }
    
    public class PlainsSelector : BiomeSelector {
        
        private readonly Biome plains = new PlainsBiome();
        
        public PlainsSelector(int p) : base(p) {
        }

        public override Biome GetBiome(int x, int z, float height, float temperature, float humidity) {
            return height >= AdvancedWorldGenerator.SeaLevel ? plains : null;
        }
    }
}