﻿using System;

namespace SurviveCore.World.Generating.WorldBiomes {
    
    public class BeachBiome : Biome {
        
        public override void FillChunk(Chunk c, Random r, int x, int z, float height) {
            for (int y = 0; y < Chunk.Size; y++) {
                if(c.Location.WY + y <= height - 4)
                    c.SetBlockDirect(x, y, z, Blocks.Stone, UpdateSource.Generation);
                else if(c.Location.WY + y <= height)
                    c.SetBlockDirect(x, y, z, Blocks.Sand, UpdateSource.Generation);
                else if(c.Location.WY + y <= AdvancedWorldGenerator.SeaLevel)
                    c.SetBlockDirect(x, y, z, Blocks.Water, UpdateSource.Generation);
            }
        }
    }

    public class BeachSelector : BiomeSelector {
        
        private readonly Biome beach = new BeachBiome();
        
        public BeachSelector(int p) : base(p) {
        }
        
        public override Biome GetBiome(int x, int z, float height, float temperature, float humidity) {
            height = MathF.Round(height);
            if (height >= AdvancedWorldGenerator.SeaLevel - 1 && height <= AdvancedWorldGenerator.SeaLevel + 1)
                return beach;
            return null;
        }

        
    }
    
}