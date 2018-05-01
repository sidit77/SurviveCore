using System;
using System.Collections.Concurrent;
using SurviveCore.DirectX;
using SurviveCore.World.Utils;

namespace SurviveCore.World.Generating {
    public class AdvancedWorldGenerator : IWorldGenerator {
        
        private const float Hot = 0.5f;
        private const int SeaLevel = 60;
        private const int WorldHeight = 8 * Chunk.Size;
        private const float Scale = 2;
        
        private readonly Random random;
        private readonly FastNoise noise;
        private readonly NoiseMapper terrainRoughness;
        private readonly NoiseMapper heightBlock;
        private readonly NoiseMapper heightAreaSmall;
        private readonly NoiseMapper heightAreaLarge;
        private readonly NoiseMapper heightRegionNoise;
        private readonly NoiseMapper fillerNoise;
        private readonly NoiseMapper temperatureChunkNoise;
        private readonly NoiseMapper temperatureAreaNoise;
        private readonly NoiseMapper temperatureRegionNoise;
        private readonly NoiseMapper humidityLocalNoise;
        private readonly NoiseMapper humidityAreaNoise;
        private readonly NoiseMapper humidityRegionNoise;
        private readonly NoiseMapper stretchForestSmallNoise;
        
        public AdvancedWorldGenerator(int seed) {
            
            random = new Random(seed);
            noise = new FastNoise(seed);
            noise.SetNoiseType(FastNoise.NoiseType.Simplex);
            noise.SetFrequency(1);
            
            terrainRoughness        = new NoiseMapper(noise, 1524.0f, 1798.0f, (float)random.NextDouble(), (float)random.NextDouble());
            heightBlock             = new NoiseMapper(noise,   23.0f,   27.0f, (float)random.NextDouble(), (float)random.NextDouble());
            heightAreaSmall         = new NoiseMapper(noise,  413.0f,  467.0f, (float)random.NextDouble(), (float)random.NextDouble());
            heightAreaLarge         = new NoiseMapper(noise,  913.0f,  967.0f, (float)random.NextDouble(), (float)random.NextDouble());
            heightRegionNoise       = new NoiseMapper(noise, 1920.0f, 1811.0f, (float)random.NextDouble(), (float)random.NextDouble());
            fillerNoise             = new NoiseMapper(noise,   16.0f,   16.0f, (float)random.NextDouble(), (float)random.NextDouble());
            temperatureChunkNoise   = new NoiseMapper(noise,    2.1f,    2.2f, (float)random.NextDouble(), (float)random.NextDouble());
            temperatureAreaNoise    = new NoiseMapper(noise,  260.0f,  273.0f, (float)random.NextDouble(), (float)random.NextDouble());
            temperatureRegionNoise  = new NoiseMapper(noise, 2420.0f, 2590.0f, (float)random.NextDouble(), (float)random.NextDouble());
            humidityLocalNoise      = new NoiseMapper(noise,    6.0f,    7.0f, (float)random.NextDouble(), (float)random.NextDouble());
            humidityAreaNoise       = new NoiseMapper(noise,  320.0f,  273.0f, (float)random.NextDouble(), (float)random.NextDouble());
            humidityRegionNoise     = new NoiseMapper(noise, 1080.0f,  919.0f, (float)random.NextDouble(), (float)random.NextDouble());
            stretchForestSmallNoise = new NoiseMapper(noise,   93.0f,  116.0f, (float)random.NextDouble(), (float)random.NextDouble());

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
            
            for(int x = 0; x < Chunk.Size; x++) {
                for(int z = 0; z < Chunk.Size; z++) {
                    
                    for(int y = 0; y < Chunk.Size; y++) {
                        int loc = (l.WY + y);
                        chunk.SetBlockDirect(x, y, z, loc < ci.GetHeight(x,z) ? (ci.GetHeight(x,z) == SeaLevel || ci.GetHeight(x,z) - 1 == SeaLevel ? Blocks.Sand : Blocks.Stone) : (loc < SeaLevel ? Blocks.Water : Blocks.Air));
                    }
                }
            }
            
        }

        private ConcurrentDictionary<(int,int), ChunkInfo> cicache = new ConcurrentDictionary<(int, int), ChunkInfo>();
        
        private ChunkInfo GenerateChunkInfo(int cx, int cz) {
            if (cicache.TryGetValue((cx, cz), out ChunkInfo ci))
                return ci;
            
            
            float[] temperaturemap = new float[Chunk.Size * Chunk.Size];
            float[] humiditymap = new float[Chunk.Size * Chunk.Size];
            int[] heightmap = new int[Chunk.Size * Chunk.Size];
            
            int i = 0;
            for (int x = cx << Chunk.BPC; x < (cx + 1) << Chunk.BPC; x++) {
                for (int z = cz << Chunk.BPC; z < (cz + 1) << Chunk.BPC; z++, i++) {
                    float roughness        = terrainRoughness .GetNoise(x, z) + 1.0f;
                    float localBlockHeight = heightBlock      .GetNoise(x, z) * roughness * 0.5f      * Scale;
                    float areaSmallHeight  = heightAreaSmall  .GetNoise(x, z) * roughness * 6.0f      * Scale;
                    float areaLargeHeight  = heightAreaLarge  .GetNoise(x, z) * roughness * 10.0f     * Scale;
                    float regionHeight     =(heightRegionNoise.GetNoise(x, z) + 0.25f) * 8.0f / 1.25f * Scale;
                    float baseHeight       = SeaLevel + regionHeight + areaLargeHeight + areaSmallHeight + localBlockHeight;

                    int height = MathHelper.Clamp((int)MathF.Round(baseHeight),0, WorldHeight - 1);
                    
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

                    heightmap[i] = height;
                    temperaturemap[i] = temperature;
                    humiditymap[i] = humidity;
                }
            }

            return cicache.GetOrAdd((cx,cz), new ChunkInfo(heightmap, temperaturemap, humiditymap));
            
        }

        private class ChunkInfo {
            private readonly int[] height;
            private readonly float[] temperature;
            private readonly float[] humidity;

            public ChunkInfo(int[] height, float[] temperature, float[] humidity) {
                this.height = height;
                this.temperature = temperature;
                this.humidity = humidity;
            }

            public int GetHeight(int x, int z) {
                return height[z | (x << Chunk.BPC)];
            }

            public float GetTemperature(int x, int z) {
                return temperature[z | (x << Chunk.BPC)];
            }
            
            public float GetHumidity(int x, int z) {
                return humidity[z | (x << Chunk.BPC)];
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