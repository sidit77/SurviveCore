using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using Priority_Queue;
using SurviveCore.World.Rendering;

namespace SurviveCore.World {

    public class BlockWorld : IDisposable {

	    public const int MaxLoadTasks = 30;
	    public const int MaxUpdateTime = 5;
        public const int Height = 8;
        public const int LoadDistance = 16;
	    public const int UnloadDistance = LoadDistance + 1;

        
        private int centerX;
	    private int centerZ;

	    private readonly IWorldGenerator generator;
	    private readonly ChunkMesher mesher;
	    private readonly WorldRenderer renderer;

        private readonly Queue<ChunkLocation> meshUpdateQueue;
        private readonly Dictionary<ChunkLocation, Chunk> chunkMap;
        private readonly SimplePriorityQueue<ChunkLocation, int> chunkLoadQueue;
        private readonly Stack<Chunk> chunkUnloadStack;
	    private readonly HashSet<ChunkLocation> currentlyLoading;
	    private readonly ConcurrentQueue<WorldChunk> loadedChunks;

	    private LiteDatabase blockDatabase;
	    private LiteCollection<WorldChunk> savedchunks;

	    private readonly Stopwatch updateTimer;
	    private int averageChunkUpdates;
	    
        public BlockWorld(WorldRenderer renderer) {
	        blockDatabase = new LiteDatabase("World.db");
	        savedchunks = blockDatabase.GetCollection<WorldChunk>("chunks");
	        this.renderer = renderer;
            meshUpdateQueue = new Queue<ChunkLocation>();
            chunkMap = new Dictionary<ChunkLocation, Chunk>();
            chunkLoadQueue = new SimplePriorityQueue<ChunkLocation, int>();
            chunkUnloadStack = new Stack<Chunk>();
	        currentlyLoading = new HashSet<ChunkLocation>();
	        loadedChunks = new ConcurrentQueue<WorldChunk>();
            
            mesher = new ChunkMesher();
            generator = new DefaultWorldGenerator(1337);//(int)Stopwatch.GetTimestamp()
	        
	        updateTimer = new Stopwatch();
	        
	        UpdateChunkQueues();
	        
        }

	    public WorldRenderer Renderer => renderer;

	    public string DebugText {
		    get {
			    StringBuilder sb = new StringBuilder();
			    sb.AppendFormat("Loaded Chunks: {0}", chunkMap.Count)                  .Append("\n");
			    sb.AppendFormat("Loading Queue: {0}", chunkLoadQueue.Count)            .Append("\n");
			    sb.AppendFormat("Loading Tasks: {0}", currentlyLoading.Count)          .Append("\n");
			    sb.AppendFormat("Meshing Queue: {0}", meshUpdateQueue.Count)           .Append("\n");
			    sb.AppendFormat("Average Meshs: {0}", averageChunkUpdates)             .Append("\n");
			    sb.AppendFormat("ChunkRenderer: {0}", renderer.NumberOfRenderers)      .Append("\n");
			    sb.AppendFormat("Rendered Chunks: {0}", renderer.CurrentlyRenderedChunks).Append("\n");
			    sb.AppendFormat("AverMeshingTime: {0}", mesher.AverageChunkMeshingTime);
			    return sb.ToString();
		    }
	    }
	    
        public void Update(int cx, int cz) {
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
		       
		        Task.Run(() => {
			        WorldChunk chunk = savedchunks.FindById(BsonMapper.Global.ToDocument(l));
			        if(chunk == null) {
				        chunk = new WorldChunk(l);
				        generator.FillChunk(chunk);
			        }
			        chunk.SetWorld(this);
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
	            if(chunk is WorldChunk wc && wc.IsDirty)
	            	savedchunks.Upsert(wc);
            }
        }
        
        private void MeshChunks() {
	        int i = 0;
            while (meshUpdateQueue.Count > 0) {
	            if (chunkMap.TryGetValue(meshUpdateQueue.Dequeue(), out Chunk c))
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
	    
        public void QueueChunkForRemesh(Chunk c) {
	        if(!meshUpdateQueue.Contains(c.Location))
            	meshUpdateQueue.Enqueue(c.Location);
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
	        
            blockDatabase.Dispose();
        }

    }

}