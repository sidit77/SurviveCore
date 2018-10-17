using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using SurviveCore.DirectX;
using SurviveCore.World.Generating.WorldBiomes;
using SurviveCore.World.Utils;

namespace SurviveCore.World.Generating {
    public class AdvancedWorldGenerator : IWorldGenerator {
        
        public const float Hot = 0.5f;
        public const int SeaLevel = 60;
        public const int WorldHeight = 8 * Chunk.Size;
        public const float Scale = 2;
        
        private readonly Random random;
        private readonly FastNoise noise;
        private readonly NoiseMapper terrainRoughness;
        private readonly NoiseMapper heightBlock;
        private readonly NoiseMapper heightAreaSmall;
        private readonly NoiseMapper heightAreaLarge;
        private readonly NoiseMapper heightRegionNoise;
        private readonly NoiseMapper temperatureChunkNoise;
        private readonly NoiseMapper temperatureAreaNoise;
        private readonly NoiseMapper temperatureRegionNoise;
        private readonly NoiseMapper humidityLocalNoise;
        private readonly NoiseMapper humidityAreaNoise;
        private readonly NoiseMapper humidityRegionNoise;
        private readonly NoiseMapper stretchForestSmallNoise;

        
        private readonly NoiseMapper hillsNoise;
        private readonly NoiseMapper hillsNoiseAreaSmall;
        private readonly NoiseMapper hillsNoiseAreaLarge;
        
        private readonly NoiseMapper corrosionNoise;
        
        private readonly ISet<BiomeSelector> biomeSelectors;
        
        
        
        public AdvancedWorldGenerator(int seed) {

            biomeSelectors = new SortedSet<BiomeSelector> {
                new BeachSelector(0),
                new OceanSelector(1),
                new PlainsSelector(2)
            };

            Console.WriteLine("Loaded " + biomeSelectors.Count + " biome selectors");
            
            random = new Random(seed);
            noise = new FastNoise(seed);
            noise.SetNoiseType(FastNoise.NoiseType.Simplex);
            noise.SetFrequency(1);
            
            terrainRoughness        = new NoiseMapper(noise, 1524.0f, 1798.0f, (float)random.NextDouble(), (float)random.NextDouble());
            heightBlock             = new NoiseMapper(noise,   23.0f,   27.0f, (float)random.NextDouble(), (float)random.NextDouble());
            heightAreaSmall         = new NoiseMapper(noise,  413.0f,  467.0f, (float)random.NextDouble(), (float)random.NextDouble());
            heightAreaLarge         = new NoiseMapper(noise,  913.0f,  967.0f, (float)random.NextDouble(), (float)random.NextDouble());
            heightRegionNoise       = new NoiseMapper(noise, 1920.0f, 1811.0f, (float)random.NextDouble(), (float)random.NextDouble());
            temperatureChunkNoise   = new NoiseMapper(noise,    2.1f,    2.2f, (float)random.NextDouble(), (float)random.NextDouble());
            temperatureAreaNoise    = new NoiseMapper(noise,  260.0f,  273.0f, (float)random.NextDouble(), (float)random.NextDouble());
            temperatureRegionNoise  = new NoiseMapper(noise, 2420.0f, 2590.0f, (float)random.NextDouble(), (float)random.NextDouble());
            humidityLocalNoise      = new NoiseMapper(noise,    6.0f,    7.0f, (float)random.NextDouble(), (float)random.NextDouble());
            humidityAreaNoise       = new NoiseMapper(noise,  320.0f,  273.0f, (float)random.NextDouble(), (float)random.NextDouble());
            humidityRegionNoise     = new NoiseMapper(noise, 1080.0f,  919.0f, (float)random.NextDouble(), (float)random.NextDouble());
            stretchForestSmallNoise = new NoiseMapper(noise,   93.0f,  116.0f, (float)random.NextDouble(), (float)random.NextDouble());

            hillsNoise          = new NoiseMapper(noise, 1397.0f, 1357.0f, (float)random.NextDouble(), (float)random.NextDouble());
            hillsNoiseAreaSmall = new NoiseMapper(noise, 91.0f,  95.0f, (float)random.NextDouble(), (float)random.NextDouble());
            hillsNoiseAreaLarge = new NoiseMapper(noise, 147.0f, 149.0f, (float)random.NextDouble(), (float)random.NextDouble());

            corrosionNoise = new NoiseMapper(noise, 2.0f, 2.0f, (float)random.NextDouble(), (float)random.NextDouble());
        }
        
        
        //public void FillChunk(Chunk chunk) {
//
        //    ChunkLocation l = chunk.Location;
        //    
        //    for(int x = 0; x < Chunk.Size; x++) {
        //        for(int z = 0; z < Chunk.Size; z++) {
        //            
        //            float roughness        = terrainRoughness .GetNoise(l.WX + x, l.WZ + z) + 1.0f;
        //            float localBlockHeight = heightBlock      .GetNoise(l.WX + x, l.WZ + z) * roughness * 0.5f      * Scale;
        //            float areaSmallHeight  = heightAreaSmall  .GetNoise(l.WX + x, l.WZ + z) * roughness * 6.0f      * Scale;
        //            float areaLargeHeight  = heightAreaLarge  .GetNoise(l.WX + x, l.WZ + z) * roughness * 10.0f     * Scale;
        //            float regionHeight     =(heightRegionNoise.GetNoise(l.WX + x, l.WZ + z) + 0.25f) * 8.0f / 1.25f * Scale;
        //            float baseHeight       = SeaLevel + regionHeight + areaLargeHeight + areaSmallHeight + localBlockHeight;
//
        //            int height = ((int)MathF.Round(baseHeight)).Clamp(0, WorldHeight - 1);
        //            
        //            float heightShifted = height + SeaLevel - WorldHeight / 2;
        //            float worldHeightMod = heightShifted < 0.0f ? 0.0f : -MathF.Pow(heightShifted / WorldHeight, 3.0f) * MathF.Pow(heightShifted * 0.4f, 1.001f);
        //            float temperature = temperatureRegionNoise.GetNoise(l.WX + x, l.WZ + z) * 0.80f +
        //                                temperatureAreaNoise  .GetNoise(l.WX + x, l.WZ + z) * 0.15f + 
        //                                temperatureChunkNoise .GetNoise(l.WX + x, l.WZ + z) * 0.05f + 
        //                                worldHeightMod;
        //            float humidity = humidityRegionNoise     .GetNoise(l.WX + x, l.WZ + z) * 0.40f +
        //                             humidityAreaNoise       .GetNoise(l.WX + x, l.WZ + z) * 0.55f +
        //                             humidityLocalNoise      .GetNoise(l.WX + x, l.WZ + z) * 0.05f +
        //                             (stretchForestSmallNoise.GetNoise(l.WX + x, l.WZ + z) > (temperature >= Hot ? 0.85f : 0.60f) ? 0.5f : 0.0f) +
        //                             worldHeightMod;
        //            
        //            for(int y = 0; y < Chunk.Size; y++) {
        //                int loc = (l.WY + y);
        //                chunk.SetBlockDirect(x, y, z, loc < height ? (height == SeaLevel || height - 1 == SeaLevel ? Blocks.Sand : Blocks.Stone) : (loc < SeaLevel ? Blocks.Water : Blocks.Air));
        //            }
        //        }
        //    }
        //    
        //}
        
        public void FillChunk(Chunk chunk) {

            ChunkLocation l = chunk.Location;
            
            
            ChunkInfo ci = GenerateChunkInfo(l.X, l.Z);
            
            Random r = new Random(chunk.Location.GetHashCode());
            
            for(int x = 0; x < Chunk.Size; x++) {
                for(int z = 0; z < Chunk.Size; z++) {
                    
                    ci.GetBiome(x,z).FillChunk(chunk, r, x,z, ci.GetHeight(x,z));

                    //for (int y = 0; y < Chunk.Size; y++) {
                    //    int loc = (l.WY + y);
                    //    
                    //    
                    //    
                    //    chunk.SetBlockDirect(x, y, z,
                    //        loc - ci.GetHeight(x, z) < 0
                    //            ? (ci.GetHeight(x, z) == SeaLevel || ci.GetHeight(x, z) - 1 == SeaLevel
                    //                ? Blocks.Sand
                    //                : Blocks.Stone)
                    //            : (loc < SeaLevel ? Blocks.Water : Blocks.Air));
                    //}
                }
            }
            
        }

        public void DecorateChunk(Chunk c) {
            ChunkLocation l = c.Location;
            
            
            ChunkInfo ci = GenerateChunkInfo(l.X, l.Z);
            
            Random r = new Random(c.Location.GetHashCode());
            
            for(int x = 0; x < Chunk.Size; x++) {
                for(int z = 0; z < Chunk.Size; z++) {
                    int chunkheight = (int)(ci.GetHeight(x, z) - l.WY);
                    if (ci.GetBiome(x, z) is PlainsBiome && chunkheight > 0 && chunkheight < Chunk.Size) {
                        if (r.NextDouble() < 0.02 * ci.GetHumidity(x,z)) {
                            GenerateTree(c,x,chunkheight,z, r);
                        }
                    }
                }
            }
        }

        private void GenerateTree(Chunk c, int x, int y, int z, Random r) {
            int height = r.Next(5, 8);
            for (int i = 0; i < height; i++) {
                c.SetBlock(x, y + i, z, Blocks.Wood, UpdateSource.Generation);
            }
            for(int wx = -4; wx <= 4; wx++)
            for(int wy = -4; wy <= 4; wy++)
            for(int wz = -4; wz <= 4; wz++)
                if (wx * wx + wy * wy + wz * wz < 3 * 4 && c.GetBlock(x + wx, y + height + wy, z + wz) == Blocks.Air)
                    c.SetBlock(x + wx, y + height + wy, z + wz, Blocks.Leaves, UpdateSource.Generation);
                    
        }
        
        private ConcurrentDictionary<(int,int), ChunkInfo> cicache = new ConcurrentDictionary<(int, int), ChunkInfo>();
        
        private ChunkInfo GenerateChunkInfo(int cx, int cz) {
            if (cicache.TryGetValue((cx, cz), out ChunkInfo ci))
                return ci;
            
            
            float[] temperaturemap = new float[Chunk.Size * Chunk.Size];
            float[] humiditymap = new float[Chunk.Size * Chunk.Size];
            float[] heightmap = new float[Chunk.Size * Chunk.Size];
            Biome[] biomes = new Biome[Chunk.Size * Chunk.Size];
            
            int i = 0;
            for (int x = cx << Chunk.BPC; x < (cx + 1) << Chunk.BPC; x++) {
                for (int z = cz << Chunk.BPC; z < (cz + 1) << Chunk.BPC; z++, i++) {
                    float roughness        = terrainRoughness .GetNoise(x, z) + 1.0f;
                    float localBlockHeight = heightBlock      .GetNoise(x, z) * roughness * 0.5f      * Scale;
                    float areaSmallHeight  = heightAreaSmall  .GetNoise(x, z) * roughness * 6.0f      * Scale;
                    float areaLargeHeight  = heightAreaLarge  .GetNoise(x, z) * roughness * 10.0f     * Scale;
                    float regionHeight     =(heightRegionNoise.GetNoise(x, z) + 0.25f) * 8.0f / 1.25f * Scale;
                    float baseHeight       = SeaLevel + regionHeight + areaLargeHeight + areaSmallHeight + localBlockHeight;

                    baseHeight += (baseHeight - SeaLevel) * 0.25f * MathF.Abs((baseHeight - SeaLevel) * 0.15f);
                    baseHeight += GetHills(x, z);

                    baseHeight += corrosionNoise.GetNoise(x, z) * MathF.Min(1, MathF.Pow(MathF.Max(0, baseHeight - SeaLevel - 20) * 0.02f, 1.5f));
                    
                    float height = MathHelper.Clamp(baseHeight,0, WorldHeight - 1);
                    
                    float heightShifted = height + SeaLevel - WorldHeight / 2;
                    float worldHeightMod = heightShifted < 0.0f ? 0.0f : -MathF.Pow(heightShifted / WorldHeight, 3.0f) * MathF.Pow(heightShifted * 0.4f, 1.001f);
                    float temperature = temperatureRegionNoise.GetNoise(x, z) * 0.80f +
                                        temperatureAreaNoise  .GetNoise(x, z) * 0.15f + 
                                        temperatureChunkNoise .GetNoise(x, z) * 0.05f + 
                                        worldHeightMod;
                    float humidity = humidityRegionNoise     .GetNoise(x, z) * 0.40f +
                                     humidityAreaNoise       .GetNoise(x, z) * 0.55f +
                                     humidityLocalNoise      .GetNoise(x, z) * 0.05f +
                                     (stretchForestSmallNoise.GetNoise(x, z) > (temperature >= Hot ? 0.85f : 0.60f) ? 0.5f : 0.0f) +
                                     worldHeightMod;

                    foreach (var selector in biomeSelectors) {
                        biomes[i] = selector.GetBiome(x, z, height, temperature, humidity);
                        if(biomes[i] != null)
                            break;;
                    }
                    if(biomes[i] == null)
                        throw new ApplicationException(string.Format("Couldnt find a matching biome for {0}, {1}, {2}", height, temperature, humidity));
                    
                    heightmap[i] = height;
                    temperaturemap[i] = temperature;
                    humiditymap[i] = humidity;
                }
            }

            return cicache.GetOrAdd((cx,cz), new ChunkInfo(heightmap, temperaturemap, humiditymap, biomes));
            
        }

        private float GetHills(int blockX, int blockZ) {
            float hillsNoiseEffective = GetEffectiveHillsNoise(blockX, blockZ);
            float hillsHeight = (hillsNoiseAreaLarge.GetNoise(blockX, blockZ) * 2.80f +
                                 hillsNoiseAreaSmall.GetNoise(blockX, blockZ) * 1.59f +
                                 1.5f) *
                                 hillsNoiseEffective * 9.0f * Scale; 
            return hillsHeight;
        }

        private float GetEffectiveHillsNoise(int blockX, int blockZ) {
            float hillsNoiseEffective = (hillsNoise.GetNoise(blockX, blockZ) + 0.3f) * 0.9f;
            if (hillsNoiseEffective >= 0.0) {
                hillsNoiseEffective = -(MathF.Cos(MathF.PI * MathF.Pow(hillsNoiseEffective, 4.0f)) - 1.0f) / 2.0f;
                if (hillsNoiseEffective >= 0.01)
                    return hillsNoiseEffective;
            }
            return 0.0f;
        }
        
        private class ChunkInfo {
            private readonly float[] height;
            private readonly float[] temperature;
            private readonly float[] humidity;
            private readonly Biome[] biomes;

            public ChunkInfo(float[] height, float[] temperature, float[] humidity, Biome[] biomes) {
                this.height = height;
                this.temperature = temperature;
                this.humidity = humidity;
                this.biomes = biomes;
            }

            public float GetHeight(int x, int z) {
                return height[z | (x << Chunk.BPC)];
            }

            public float GetTemperature(int x, int z) {
                return temperature[z | (x << Chunk.BPC)];
            }
            
            public float GetHumidity(int x, int z) {
                return humidity[z | (x << Chunk.BPC)];
            }
            
            public Biome GetBiome(int x, int z) {
                return biomes[z | (x << Chunk.BPC)];
            }
            
        }

        private class NoiseMapper {
            private readonly FastNoise noise;
            private readonly float sx, sy, dx, dy;

            public NoiseMapper(FastNoise noise, float sx, float sy, float dx, float dy) {
                this.noise = noise;
                this.sx = sx;
                this.sy = sy;
                this.dx = dx;
                this.dy = dy;
            }

            public float GetNoise(float x, float y) {
                return noise.GetNoise(x / sx + dx, y / sy + dy);
            }
        }
    }
}