using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using Priority_Queue;
using SurviveCore.World.Rendering;
using SurviveCore.World.Utils;

namespace SurviveCore.World {

    public class BlockWorld : IDisposable {

	    public const int MaxLoadTasks = 30;
	    public const int MaxUpdateTime = 5;
        public const int Height = 8;
        public const int LoadDistance = 16;
	    public const int UnloadDistance = LoadDistance + 1;


	    private Vector3 playerpos;
        private int centerX;
	    private int centerZ;

	    private readonly IWorldGenerator generator;
	    private readonly ChunkMesher mesher;
	    private readonly WorldRenderer renderer;

        private readonly SimplePriorityQueue<ChunkLocation, int> meshUpdateQueue;
        private readonly Dictionary<ChunkLocation, Chunk> chunkMap;
        private readonly SimplePriorityQueue<ChunkLocation, int> chunkLoadQueue;
        private readonly Stack<Chunk> chunkUnloadStack;
	    private readonly HashSet<ChunkLocation> currentlyLoading;
	    private readonly ConcurrentQueue<WorldChunk> loadedChunks;
	    private readonly Stack<ChunkData> savedata;

	    private readonly LiteDatabase blockDatabase;
	    private readonly LiteCollection<ChunkData> savedchunks;

	    private readonly Stopwatch updateTimer;
	    private int averageChunkUpdates;

	    private readonly AverageTimer savingtimer;
	    private readonly AverageTimer loadingtimer;
	    private readonly AverageTimer compresstimer;
	    
        public BlockWorld(WorldRenderer renderer, out Vector3 pos) {
	        blockDatabase = new LiteDatabase("./Assets/World.db");
	        savedchunks = blockDatabase.GetCollection<ChunkData>("chunks");
	        if(!blockDatabase.CollectionExists("settings")) {
		        blockDatabase.GetCollection<Setting>("settings").Insert(new Setting("seed", new Random().Next()));
		        blockDatabase.GetCollection<Setting>("settings").Insert(new Setting("playerX",  0));
		        blockDatabase.GetCollection<Setting>("settings").Insert(new Setting("playerY", 50));
		        blockDatabase.GetCollection<Setting>("settings").Insert(new Setting("playerZ",  0));
		       
	        }

	        this.renderer = renderer;
            meshUpdateQueue = new SimplePriorityQueue<ChunkLocation, int>();
            chunkMap = new Dictionary<ChunkLocation, Chunk>();
            chunkLoadQueue = new SimplePriorityQueue<ChunkLocation, int>();
            chunkUnloadStack = new Stack<Chunk>();
	        currentlyLoading = new HashSet<ChunkLocation>();
	        loadedChunks = new ConcurrentQueue<WorldChunk>();
	        savedata = new Stack<ChunkData>();
            
            mesher = new ChunkMesher();
            generator = new DefaultWorldGenerator(blockDatabase.GetCollection<Setting>("settings").FindById("seed").Value);//(int)Stopwatch.GetTimestamp()
	        
	        updateTimer = new Stopwatch();
	        
	        savingtimer = new AverageTimer();
	        loadingtimer = new AverageTimer();
	        compresstimer = new AverageTimer();

	        pos = new Vector3 {
		        X = blockDatabase.GetCollection<Setting>("settings").FindById("playerX").Value,
		        Y = blockDatabase.GetCollection<Setting>("settings").FindById("playerY").Value,
		        Z = blockDatabase.GetCollection<Setting>("settings").FindById("playerZ").Value
	        };

	        UpdateChunkQueues();
	        
	        
        }

	    public WorldRenderer Renderer => renderer;

	    public string DebugText {
		    get {
			    StringBuilder sb = new StringBuilder();
			    sb.AppendFormat("Loaded Chunks: {0}", chunkMap.Count)                    .Append("\n");
			    sb.AppendFormat("Loading Queue: {0}", chunkLoadQueue.Count)              .Append("\n");
			    sb.AppendFormat("Loading Tasks: {0}", currentlyLoading.Count)            .Append("\n");
			    sb.AppendFormat("Meshing Queue: {0}", meshUpdateQueue.Count)             .Append("\n");
			    sb.AppendFormat("Average Meshs: {0}", averageChunkUpdates)               .Append("\n");
			    sb.AppendFormat("ChunkRenderer: {0}", renderer.NumberOfRenderers)        .Append("\n");
			    sb.AppendFormat("Rendered Chunks: {0}", renderer.CurrentlyRenderedChunks).Append("\n");
			    sb.AppendFormat("AverMeshingTime: {0}", mesher.AverageChunkMeshingTime)  .Append("\n");
			    sb.AppendFormat("AveraSavingTime: {0}", savingtimer.AverageTicks)        .Append("\n");
			    sb.AppendFormat("AveCompressTime: {0}", compresstimer.AverageTicks)        .Append("\n");
			    sb.AppendFormat("AverLoadingTime: {0}", loadingtimer.AverageTicks);
			    return sb.ToString();
		    }
	    }
	    
        public void Update(Vector3 pos) {
	        playerpos = pos;
	        int cx = (int)Math.Floor(pos.X) >> Chunk.BPC;
	        int cz = (int)Math.Floor(pos.Z) >> Chunk.BPC;
            updateTimer.Restart();
            if(centerX != cx || centerZ != cz) {
	            
                centerX = cx;
                centerZ = cz;
	            
	            UpdateChunkQueues();
            }
		    UnloadChunks(false);
		    LoadChunks();
		    MeshChunks();
        }
	    
        public Block GetBlock(int x, int y, int z) {
	        ChunkLocation l = ChunkLocation.FromPos(x, y, z);
            return GetChunk(l).GetBlockDirect(x - l.WX, y - l.WY, z - l.WZ);
        }

        public bool SetBlock(int x, int y, int z, Block b) {
	        ChunkLocation l = ChunkLocation.FromPos(x, y, z);
            return GetChunk(l).SetBlockDirect(x - l.WX, y - l.WY, z - l.WZ, b);
        }

        public bool SetBlock(int x, int y, int z, Block b, byte m) {
	        ChunkLocation l = ChunkLocation.FromPos(x, y, z);
            return GetChunk(l).SetBlockDirect(x - l.WX, y - l.WY, z - l.WZ, b, m);
        }
        
        public Block GetBlock(Vector3 vec) {
            return GetBlock((int)Math.Round(vec.X), (int)Math.Round(vec.Y), (int)Math.Round(vec.Z));
        }

        public bool SetBlock(Vector3 vec, Block b) {
            return SetBlock((int)Math.Round(vec.X), (int)Math.Round(vec.Y), (int)Math.Round(vec.Z), b);
        }

        public bool SetBlock(Vector3 vec, Block b, byte m) {
            return SetBlock((int)Math.Round(vec.X), (int)Math.Round(vec.Y), (int)Math.Round(vec.Z), b, m);
        }
        
	    private void UpdateChunkQueues() {
		    for (int x = -LoadDistance; x <= LoadDistance; x++) {
			    for (int z = -LoadDistance; z <= LoadDistance; z++) {
				    for (int y = 0; y < Height; y++) {
					    ChunkLocation l = new ChunkLocation(centerX + x,y, centerZ + z);
					    if (GetDistanceSquared(l) <= LoadDistance * LoadDistance && !currentlyLoading.Contains(l) && !chunkMap.ContainsKey(l)) {
						    if (chunkLoadQueue.Contains(l)) {
							    chunkLoadQueue.TryUpdatePriority(l, GetDistanceSquared(l, true));
						    }else {
							 	chunkLoadQueue.Enqueue(l, GetDistanceSquared(l, true));   
						    }
					    }
				    }
			    }
		    }

		    foreach (Chunk chunk in chunkMap.Values.Where(c => GetDistanceSquared(c.Location) > UnloadDistance * UnloadDistance)) {
			    chunkUnloadStack.Push(chunk);
		    }
	    }

        private void LoadChunks() {
	        while (currentlyLoading.Count <= MaxLoadTasks && chunkLoadQueue.Count > 0) {
		        ChunkLocation l = chunkLoadQueue.Dequeue();
		        
		        if(GetDistanceSquared(l) > UnloadDistance * UnloadDistance) 
			        continue;

		        currentlyLoading.Add(l);
		        
				WorldChunk chunk = WorldChunk.CreateWorldChunk(l, this);
		        
			    
			    Task.Run(() => {
				    loadingtimer.Start();
				    ChunkData data = savedchunks.FindById(BsonMapper.Global.ToDocument(l));
				    loadingtimer.Stop();
				    if(data == null) {
				        generator.FillChunk(chunk);
				    }else {
				    	ChunkSerializer.Deserialize(chunk, data);
				    }
				    loadedChunks.Enqueue(chunk);
			    });
		        
	        }
	        
	        while(!loadedChunks.IsEmpty) {
		        loadedChunks.TryDequeue(out WorldChunk chunk);
		        for (int d = 0; d < 6; d++)
			        chunk.SetNeighbor(d, GetChunk(chunk.Location.GetAdjecent(d)));
		        chunk.SetMeshUpdates(true);
		        chunkMap.Add(chunk.Location, chunk);
		        currentlyLoading.Remove(chunk.Location);
	        }
	        
        }

	    
        private void UnloadChunks(bool final) {
            while (chunkUnloadStack.Count > 0) {
                Chunk chunk = chunkUnloadStack.Pop();
	            if(!final && GetDistanceSquared(chunk.Location) < LoadDistance * LoadDistance)
		            continue;
                chunkMap.Remove(chunk.Location);
                chunk.CleanUp();
	            if(chunk.IsDirty || chunk.IsGenerated) {
		            compresstimer.Start();
		            ChunkData d = ChunkSerializer.Serialize(chunk);
		            compresstimer.Stop();
		            savedata.Push(d);
	            }
            }
	        
	        if(savedata.Count > 0) {
		        savingtimer.Start();
		        savedchunks.Upsert(savedata);
		        savingtimer.Stop(savedata.Count);
		        savedata.Clear();
	        }
        }
        
        private void MeshChunks() {
	        int i = 0;
            while (meshUpdateQueue.Count > 0) {
	            if(chunkMap.TryGetValue(meshUpdateQueue.Dequeue(), out Chunk c))
		            c.RegenerateMesh(mesher);
		        i++;
				if(updateTimer.ElapsedMilliseconds >= MaxUpdateTime)
					break;
            }

	        averageChunkUpdates += i;
	        averageChunkUpdates /= 2;
        }

	    private Chunk GetChunk(ChunkLocation l) {
		    return chunkMap.TryGetValue(l, out Chunk c) ? c : BorderChunk.Instance;
	    }
	    
        public void QueueChunkForRemesh(Chunk c, int priority) {
	        if(!meshUpdateQueue.EnqueueWithoutDuplicates(c.Location, priority) && meshUpdateQueue.GetPriority(c.Location) > priority)
		        meshUpdateQueue.UpdatePriority(c.Location, priority);
        }

	    private int GetDistanceSquared(ChunkLocation l, bool y = false) {
		    return (l.X - centerX) * (l.X - centerX) + 
		           (l.Z - centerZ) * (l.Z - centerZ) +
		           (y ? (l.Y - Height / 2) * (l.Y - Height / 2) : 0);
	    }
	    
        public void Dispose() {
	        foreach(Chunk c in chunkMap.Values)
		        chunkUnloadStack.Push(c);
	        UnloadChunks(true);
	        blockDatabase.GetCollection<Setting>("settings").Update(new Setting("playerX", (int)MathF.Round(playerpos.X)));
	        blockDatabase.GetCollection<Setting>("settings").Update(new Setting("playerY", (int)MathF.Round(playerpos.Y)));
	        blockDatabase.GetCollection<Setting>("settings").Update(new Setting("playerZ", (int)MathF.Round(playerpos.Z)));
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
	    
    }

}