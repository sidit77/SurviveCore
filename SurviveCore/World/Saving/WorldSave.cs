using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using LiteDB;
using SurviveCore.World.Generating;
using SurviveCore.World.Utils;

namespace SurviveCore.World.Saving {
    
    public class WorldSave : IDisposable{
        
        private readonly IWorldGenerator generator;
        
        private readonly LiteDatabase blockDatabase;
        private readonly LiteCollection<ChunkData> savedchunks;
	    private readonly LiteCollection<Setting> settings;
	    private readonly LiteCollection<PlayerState> players;
	    
	    private readonly Stack<ChunkData> savedata;
        
        private readonly AverageTimer savingtimer;
        private readonly AverageTimer loadingtimer;
        private readonly AverageTimer compresstimer;
        private readonly AverageTimer generationtimer;

        public long AverageLoadingTime => loadingtimer.AverageTicks;
        public long AverageSavingTime => savingtimer.AverageTicks;
        public long AverageCompressingTime => compresstimer.AverageTicks;
        public long AverageGeneratingTime => generationtimer.AverageTicks;
        
        public WorldSave(string path) {
            blockDatabase = new LiteDatabase(path);
            savedchunks = blockDatabase.GetCollection<ChunkData>("chunks");
	        settings = blockDatabase.GetCollection<Setting>("settings");
	        players = blockDatabase.GetCollection<PlayerState>("players");
	        
            if(settings.FindById("seed") == null) {
	            settings.Insert(new Setting("seed", new Random().Next()));
            }

	        Console.WriteLine("Loaded world {0} with seed {1} and {2} chunks", Path.GetFileName(path), settings.FindById("seed").Value, savedchunks.Count());
	        
	        savedata = new Stack<ChunkData>();
	        
            generator = new AdvancedWorldGenerator(settings.FindById("seed").Value);//(int)Stopwatch.GetTimestamp()
            
            savingtimer = new AverageTimer();
            loadingtimer = new AverageTimer();
            compresstimer = new AverageTimer();
            generationtimer = new AverageTimer();
        }

	    public void FillChunk(Chunk c) {
			loadingtimer.Start();
			ChunkData data = savedchunks.FindById(BsonMapper.Global.ToDocument(c.Location));
			loadingtimer.Stop();
			if (data == null) {
				generationtimer.Start();
			    generator.FillChunk(c);
				generationtimer.Stop();
			}else {
			    ChunkSerializer.Deserialize(c, data);
			}
	    }

	    public void QueueChunkForSaving(Chunk c) {
		    if(c.IsDirty || c.IsGenerated) {
			    compresstimer.Start();
			    ChunkData d = ChunkSerializer.Serialize(c);
			    compresstimer.Stop();
			    savedata.Push(d);
		    }
	    }

	    public void Save() {
		    if(savedata.Count > 0) {
			    savingtimer.Start();
			    savedchunks.Upsert(savedata);
			    savingtimer.Stop(savedata.Count);
			    savedata.Clear();
		    }
	    }

	    public void GetPlayerData(string name, out Vector3 pos, out Quaternion rot) {
		    PlayerState ps = players.FindById(name);
		    pos = ps?.Position ?? new Vector3(0, 50, 0);
		    rot = ps?.Rotation ?? Quaternion.Identity;
	    }
	    
	    public void SavePlayerData(string name, Vector3 pos, Quaternion rot) {
		    players.Upsert(new PlayerState(name, pos, rot));
	    }
	    
        public void Dispose() {
            blockDatabase.Dispose();
        }
        
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
	    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
	    [SuppressMessage("ReSharper", "UnusedMember.Local")]
	    private class ChunkData {
		    public ChunkLocation Id { get; set; }
		    public byte[] Meta { get; set; }
		    public byte[] Blocks { get; set; }

		    public ChunkData() {
			    
		    }
		    
		    public ChunkData(ChunkLocation id, byte[] meta, byte[] blocks) {
			    Id = id;
			    Meta = meta;
			    Blocks = blocks;
		    }
	    }

	    private static class ChunkSerializer {

		    public static unsafe ChunkData Serialize(Chunk c) {
			    byte* start = stackalloc byte[Chunk.Size * Chunk.Size * Chunk.Size];
				int size = 2;
				byte* pointer = start + 0;
				byte* counter = start + 1;
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
							    size += 2;
							    (*counter) = 1;
							    (*pointer) = b;
						    }
					    }
				    }
				}
				byte[] meta = new byte[size];
				Marshal.Copy((IntPtr)start,meta,0,size);
				
				size = 2;
				pointer = start + 0;
				counter = start + 1;
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
							    size += 2;
							    (*counter) = 1;
							    (*pointer) = b;
						    }
					    }
				    }
				}
				byte[] blocks = new byte[size];
				Marshal.Copy((IntPtr)start,blocks,0,size);
			    
			    
			    return new ChunkData(c.Location, meta, blocks);
		    }

		    public static unsafe void Deserialize(Chunk c, ChunkData cd) {
			    //TODO combine both arrays and make some size checks
			    fixed(byte* bstart = cd.Blocks)
			    fixed(byte* mstart = cd.Meta) {
				    byte* bpointer = bstart + 0;
				    byte* bcounter = bstart + 1;
				    byte* mpointer = mstart + 0;
				    byte* mcounter = mstart + 1;

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
							    c.SetBlockDirect(x, y, z, Block.GetBlock(*bpointer), *mpointer, UpdateSource.Loading);
							    (*bcounter)--;
							    (*mcounter)--;
						    }
					    }
				    }
			    }
		    }
		    
	    }

	    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
	    [SuppressMessage("ReSharper", "UnusedMember.Global")]
	    public class Setting {
		    [BsonId]
		    public string Name { get; set; }
		    public int Value { get; set; }

		    public Setting() {
		    }

		    public Setting(string name, int value) {
			    Name = name;
			    Value = value;
		    }
	    }
	    
	    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
	    [SuppressMessage("ReSharper", "UnusedMember.Global")]
	    public class PlayerState {
		    [BsonId]
		    public string Name { get; set; }
		    public float px { get; set; }
		    public float py { get; set; }
		    public float pz { get; set; }
		    
		    public float rx { get; set; }
		    public float ry { get; set; }
		    public float rz { get; set; }
		    public float rw { get; set; }

		    [BsonIgnore]
		    public Vector3 Position => new Vector3(px,py,pz);
		    
		    [BsonIgnore]
		    public Quaternion Rotation => new Quaternion(rx,ry,rz,rw);
		    
		    public PlayerState() {
		    }

		    public PlayerState(string name, Vector3 position, Quaternion rotation) {
			    Name = name;
			    
			    px = position.X;
			    py = position.Y;
			    pz = position.Z;
			    
			    rx = rotation.X;
			    ry = rotation.Y;
			    rz = rotation.Z;
			    rw = rotation.W;
		    }
	    }
        
    }
    
}
