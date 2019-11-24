using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq.Expressions;
using System.Net.Http;
using System.Numerics;
using System.Runtime.InteropServices;
using DataTanker;
using DataTanker.Settings;
using LiteDB;
using SurviveCore.DirectX;
using SurviveCore.World.Generating;
using SurviveCore.World.Rendering;
using SurviveCore.World.Utils;

namespace SurviveCore.World.Saving {
    
    public class WorldSave : IDisposable{

	    private readonly IWorldGenerator generator;
	    private readonly BlockWorld world;
        
	    private readonly IKeyValueStorage<ComparableKeyOf<ChunkLocation>, ValueOf<Chunk>> chunkStorage;
	    private readonly IKeyValueStorage<KeyOf<string>, ValueOf<byte[]>> stateStorage;

	    private readonly AverageTimer savingtimer;
        private readonly AverageTimer loadingtimer;
        private readonly AverageTimer compresstimer;
        private readonly AverageTimer generationtimer;

        public long AverageLoadingTime => loadingtimer.AverageTicks;
        public long AverageSavingTime => savingtimer.AverageTicks;
        public long AverageCompressingTime => compresstimer.AverageTicks;
        public long AverageGeneratingTime => generationtimer.AverageTicks;
        
        public WorldSave(WorldRenderer renderer, string path)
        {
	        this.world = new BlockWorld(renderer, this);
	        StorageFactory factory = new StorageFactory();
	        chunkStorage = factory.CreateBPlusTreeStorage(new ChunkLocationSerializer(), 
		        new ChunkSerializer(world), BPlusTreeStorageSettings.Default());
	        stateStorage = factory.CreateRadixTreeByteArrayStorage<string>(RadixTreeStorageSettings.Default());
	        if (Directory.Exists($"./Worlds/{path}/"))
	        {
		        stateStorage.OpenExisting($"./Worlds/{path}/");
	        }
	        else
	        {
		        Directory.CreateDirectory($"./Worlds/{path}/chunks/");
		        stateStorage.CreateNew($"./Worlds/{path}/");
		        stateStorage.Set("world/seed", new Random().Next().ToBytes());
	        }
	        chunkStorage.OpenOrCreate($"./Worlds/{path}/chunks/");

	        Console.WriteLine("Loaded world {0} with seed {1}", path, stateStorage.Get("world/seed").ToStruct<int>());

	        generator = new AdvancedWorldGenerator(stateStorage.Get("world/seed").ToStruct<int>());
            
            savingtimer = new AverageTimer();
            loadingtimer = new AverageTimer();
            compresstimer = new AverageTimer();
            generationtimer = new AverageTimer();

        }

	    public Chunk GetChunk(ChunkLocation l) {
			loadingtimer.Start();
			Chunk c = chunkStorage.Get(l);
			loadingtimer.Stop();
			
			if (c != null)
				return c;
			
			generationtimer.Start();
			c = Chunk.CreateChunk(l, world);
			generator.FillChunk(c);
			c.IncrementGenerationLevel();
			generationtimer.Stop();
			return c;
	    }

	    public void DecorateChunk(Chunk c) {
		    generator.DecorateChunk(c);
		    c.IncrementGenerationLevel();
	    }
	    
	    public void Save(Chunk c) {
		    if (c.IsDirty || c.IsGenerated)
		    {
			    savingtimer.Start();
			    chunkStorage.Set(c.Location, c);
			    savingtimer.Stop();
		    }

		    c.CleanUp();
	    }

	    public BlockWorld GetWorld()
	    {
		    return world;
	    }
	    
	    public void GetPlayerData(string name, out Vector3 pos, out Quaternion rot) {
		    pos = stateStorage.Get($"player/{name}/position")?.ToStruct<Vector3>() ?? new Vector3(0, 50, 0);
		    rot = stateStorage.Get($"player/{name}/rotation")?.ToStruct<Quaternion>() ?? Quaternion.Identity;
	    }
	    
	    public void SavePlayerData(string name, Vector3 pos, Quaternion rot) {
		    stateStorage.Set($"player/{name}/position", pos.ToBytes());
		    stateStorage.Set($"player/{name}/rotation", rot.ToBytes());
	    }
	    
        public void Dispose() {
            chunkStorage.Dispose();
            stateStorage.Dispose();
        }

        [StructLayout(LayoutKind.Sequential)]
	    private struct ChunkHeader
	    {
		    public ChunkLocation location;
		    public int genlevel;
		    public int blength;
		    public int mlength;
	    }

	    private class ChunkSerializer : ISerializer<Chunk>
	    {

		    private int hsize;
		    
		    private BlockWorld world;

		    public ChunkSerializer(BlockWorld world) {
			    this.world = world;
			    hsize = Marshal.SizeOf<ChunkHeader>();
		    }
		    
		    public unsafe Chunk Deserialize(byte[] bytes) {
			    if(bytes.Length < hsize)
				    throw new Exception("Not big enough for header");
			    fixed (byte* pstart = bytes)
			    {
				    ChunkHeader header = (ChunkHeader) Marshal.PtrToStructure((IntPtr) pstart, typeof(ChunkHeader));
				    if(bytes.Length != hsize + header.blength + header.mlength)
					    throw new Exception("Wrong length");
				    Chunk c = Chunk.CreateChunk(header.location, world);
				    c.SetGenerationLevel(header.genlevel);
				    byte* bpointer = pstart + hsize + 0;
				    byte* bcounter = pstart + hsize + 1;
				    byte* mpointer = bpointer + header.blength;
				    byte* mcounter = bcounter + header.blength;

				    for(int x = 0; x < Chunk.Size; x++) {
					    for(int y = 0; y < Chunk.Size; y++) {
						    for(int z = 0; z < Chunk.Size; z++) {
							    if(*bcounter == 0) {
								    bpointer += 2;
								    bcounter += 2;
							    }
							    if(*mcounter == 0) {
								    mpointer += 2;
								    mcounter += 2;
							    }
							    c.SetBlockDirect(x, y, z, Block.GetBlock(*bpointer), *mpointer, UpdateSource.Generation);
							    (*bcounter)--;
							    (*mcounter)--;
						    }
					    }
				    }

				    return c;

			    }
			    
		    }

		    public unsafe byte[] Serialize(Chunk c) {
			    byte* start = stackalloc byte[Chunk.Size * Chunk.Size * Chunk.Size * 2 + hsize];

			    int bsize = 2;
			    byte* pointer = start + hsize + 0;
			    byte* counter = start + hsize + 1;
			    (*pointer) = (byte)c.GetBlockDirect(0, 0, 0).ID;
			    (*counter) = 0;
			    for(int x = 0; x < Chunk.Size; x++) {
				    for(int y = 0; y < Chunk.Size; y++) {
					    for(int z = 0; z < Chunk.Size; z++) {
						    byte b = (byte)c.GetBlockDirect(x, y, z).ID;
						    if(b == *pointer && *counter < 255) {
							    (*counter)++;
						    }else {
							    counter += 2;
							    pointer += 2;
							    bsize += 2;
							    (*counter) = 1;
							    (*pointer) = b;
						    }
					    }
				    }
			    }
			    
			    int msize = 2;
			    pointer += 2;
			    counter += 2;
			    (*pointer) = c.GetMetaDataDirect(0, 0, 0);
			    (*counter) = 0;
			    for(int x = 0; x < Chunk.Size; x++) {
				    for(int y = 0; y < Chunk.Size; y++) {
					    for(int z = 0; z < Chunk.Size; z++) {
						    byte b = c.GetMetaDataDirect(x, y, z);
						    if(b == *pointer && *counter < 255) {
							    (*counter)++;
						    }else {
							    counter += 2;
							    pointer += 2;
							    msize += 2;
							    (*counter) = 1;
							    (*pointer) = b;
						    }
					    }
				    }
			    }
			    byte[] data = new byte[hsize + bsize + msize];
			    ChunkHeader* header = (ChunkHeader*)start;
			    header->location = c.Location;
			    header->genlevel = c.GenerationLevel;
			    header->blength = bsize;
			    header->mlength = msize;
			    Marshal.Copy((IntPtr)start,data,0,hsize + bsize + msize);

			    return data;
		    }
	    }

	    private class ChunkLocationSerializer : ISerializer<ChunkLocation>
	    {
		    public ChunkLocation Deserialize(byte[] bytes)
		    {
			    return bytes.ToStruct<ChunkLocation>();
		    }

		    public byte[] Serialize(ChunkLocation obj)
		    {
			    return obj.ToBytes();
		    }
	    }

    }
    
}
