using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Priority_Queue;
using SurviveCore.OpenGL.Helper;
using SurviveCore.World.Rendering;
using SurviveCore.World.Utils;

namespace SurviveCore.World {

    class BlockWorld : IDisposable {

	    public const int RendererPoolSize = 256;
	    public const int MaxLoadTasks = 30;
	    public const int MaxUpdateTime = 5;
        public const int Height = 8;
        public const int LoadDistance = 16;
	    public const int UnloadDistance = LoadDistance + 1;

        
        private int centerX;
	    private int centerZ;

	    private readonly IWorldGenerator generator;
	    private readonly ChunkMesher mesher;

        private readonly ObjectPool<ChunkRenderer> rendererPool;
        private readonly Queue<ChunkLocation> meshUpdateQueue;
        private readonly Dictionary<ChunkLocation, Chunk> chunkMap;
        private readonly SimplePriorityQueue<ChunkLocation, int> chunkLoadQueue;
        private readonly Stack<Chunk> chunkUnloadStack;
	    private readonly HashSet<ChunkLocation> currentlyLoading;
	    private readonly ConcurrentQueue<WorldChunk> loadedChunks;
        
        private delegate void OnDraw(Frustum f);
        private event OnDraw DrawEvent;

	    private readonly Stopwatch updateTimer;
	    private readonly Stopwatch debugTimer;
	    private int averageChunkUpdates;
	    
        public BlockWorld() {
            rendererPool = new ObjectPool<ChunkRenderer>(RendererPoolSize, () => new ChunkRenderer());
            meshUpdateQueue = new Queue<ChunkLocation>();
            chunkMap = new Dictionary<ChunkLocation, Chunk>();
            chunkLoadQueue = new SimplePriorityQueue<ChunkLocation, int>();
            chunkUnloadStack = new Stack<Chunk>();
	        currentlyLoading = new HashSet<ChunkLocation>();
	        loadedChunks = new ConcurrentQueue<WorldChunk>();
            
            mesher = new ChunkMesher();
            generator = new DefaultWorldGenerator((int)Stopwatch.GetTimestamp());
	        
	        updateTimer = new Stopwatch();
	        debugTimer = new Stopwatch();
	        debugTimer.Start();
	        
	        UpdateChunkQueues();
	        
        }

        public void Draw(Frustum frustum) {
            DrawEvent?.Invoke(frustum);
        }

        public void Update(int cx, int cz) {
            updateTimer.Restart();
            if(centerX != cx || centerZ != cz) {
	            
                centerX = cx;
                centerZ = cz;
	            
	            UpdateChunkQueues();
            }
		    UnloadChunks();
		    LoadChunks();
		    MeshChunks();

	        if (Settings.Instance.DebugInfo && debugTimer.ElapsedMilliseconds >= 250) {
		        Console.WriteLine("");
		        Console.WriteLine("Loaded Chunks: {0}", chunkMap.Count);
		        Console.WriteLine("Loading Queue: {0}", chunkLoadQueue.Count);
		        Console.WriteLine("Loading Tasks: {0}", currentlyLoading.Count);
		        Console.WriteLine("Meshing Queue: {0}", meshUpdateQueue.Count);
		        Console.WriteLine("Average Meshs: {0}", averageChunkUpdates);
		        Console.WriteLine("ChunkRenderer: {0}", DrawEvent?.GetInvocationList().Length);
		        debugTimer.Restart();
	        }
	        
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

        public ChunkRenderer CreateChunkRenderer(Chunk c) {
            ChunkRenderer r = rendererPool.Get().SetUp(c);
            DrawEvent += r.Draw;
            return r;
        }
        
        public void DisposeChunkRenderer(ChunkRenderer c) {
            DrawEvent -= c.Draw;
            if(rendererPool.Add(c))
                c.CleanUp();
            else
                c.Dispose();
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
			        generator.FillChunk(chunk);
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

        private void UnloadChunks() {
            while (chunkUnloadStack.Count > 0) {
                Chunk chunk = chunkUnloadStack.Pop();
	            if(GetDistanceSquared(chunk.Location) < LoadDistance * LoadDistance)
		            continue;
                chunkMap.Remove(chunk.Location);
                chunk.CleanUp();
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
	        UnloadChunks();
            while (rendererPool.Count > 0)
                rendererPool.Get().Dispose();
        }

    }

}
