using System;
using System.Collections.Generic;

namespace SurviveCore.World.Generating {

    public static class Biomes {
        
    }
    
    public abstract class Biome {

        public abstract void FillChunk(Chunk c, Random r,int x, int z, int height);

    }

    public abstract class BiomeSelector : IComparable<BiomeSelector> {
        
        private readonly int priority;

        protected BiomeSelector(int priority) {
            this.priority = priority;
        }
        
        public abstract Biome GetBiome(int x, int z, int height, float temperature, float humidity);

        public int CompareTo(BiomeSelector other) {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return priority.CompareTo(other.priority);
        }
    }
    
}