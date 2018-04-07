using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConcurrentCollections;
using LiteDB;
using Priority_Queue;
using SharpDX.Direct3D11;
using SurviveCore.World.Rendering;
using SurviveCore.World.Saving;
using SurviveCore.World.Utils;

namespace SurviveCore.World {

    public class BlockWorld : IDisposable {

	    private const ThreadPriority LoadingThreadPriority = ThreadPriority.BelowNormal;
	    private const int MaxLoadingThreads = 1;
	    private const int MaxUpdateTime = 5;
        private const int Height = 4;
        private const int LoadDistance = 9;
	    private const int UnloadDistance = LoadDistance + 1;

        private int centerX;
	    private int centerZ;

	    private readonly ChunkMesher mesher;
	    private readonly WorldRenderer renderer;
	    private readonly WorldSave save;

        private readonly SimplePriorityQueue<ChunkLocation, int> meshUpdateQueue;
        private readonly Dictionary<ChunkLocation, Chunk> chunkMap;
        private readonly SimplePriorityQueue<ChunkLocation, int> chunkLoadQueue;
        private readonly Stack<Chunk> chunkUnloadStack;
	    private readonly ConcurrentHashSet<ChunkLocation> currentlyLoading;
	    private readonly ConcurrentQueue<WorldChunk> loadedChunks;

	    private readonly Stopwatch updateTimer;
	    private int averageChunkUpdates;

	    private readonly Thread[] loadingthreads;
	    private readonly object loadingthreadlock = new object();
	    private bool disposing = false;
	    
        public BlockWorld(WorldRenderer renderer, WorldSave save) {
	        this.renderer = renderer;
	        this.save = save;
	        
            meshUpdateQueue = new SimplePriorityQueue<ChunkLocation, int>();
            chunkMap = new Dictionary<ChunkLocation, Chunk>();
            chunkLoadQueue = new SimplePriorityQueue<ChunkLocation, int>();
            chunkUnloadStack = new Stack<Chunk>();
	        currentlyLoading = new ConcurrentHashSet<ChunkLocation>();
	        loadedChunks = new ConcurrentQueue<WorldChunk>();
	        
            mesher = new ChunkMesher();
	        updateTimer = new Stopwatch();
	        
	        loadingthreads = new Thread[MaxLoadingThreads];
	        for (int i = 0; i < MaxLoadingThreads; i++) {
		        loadingthreads[i] = new Thread(() => {
			        ChunkLocation l = new ChunkLocation(0, 0, 0);
			        while (true) {
				        lock (loadingthreadlock) {
					        while (!disposing && !chunkLoadQueue.TryDequeue(out l))
						        Monitor.Wait(loadingthreadlock);
				        }

				        if (disposing)
					        break;

				        if (GetDistanceSquared(l) > UnloadDistance * UnloadDistance)
					        continue;

				        currentlyLoading.Add(l);
				        WorldChunk chunk = WorldChunk.CreateWorldChunk(l, this);
				        save.FillChunk(chunk);
				        loadedChunks.Enqueue(chunk);
			        }
		        }) {
			        Name = "LoadingThread " + (i + 1),
			        Priority = LoadingThreadPriority
		        };
		        loadingthreads[i].Start();
	        }

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
			    sb.AppendFormat("AveraSavingTime: {0}", save.AverageSavingTime)          .Append("\n");
			    sb.AppendFormat("AveCompressTime: {0}", save.AverageCompressingTime)     .Append("\n");
			    sb.AppendFormat("AverLoadingTime: {0}", save.AverageLoadingTime)         .Append("\n");
			    sb.AppendFormat("AveGenerateTime: {0}", save.AverageGeneratingTime);
			    return sb.ToString();
		    }
	    }
	    
        public void Update(Vector3 pos) {
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

		    lock (loadingthreadlock) {
			    Monitor.PulseAll(loadingthreadlock);
		    }

		    foreach (Chunk chunk in chunkMap.Values.Where(c => GetDistanceSquared(c.Location) > UnloadDistance * UnloadDistance)) {
			    chunkUnloadStack.Push(chunk);
		    }
	    }

        private void LoadChunks() {
	        while(!loadedChunks.IsEmpty) {
		        loadedChunks.TryDequeue(out WorldChunk chunk);
		        for (int d = 0; d < 6; d++)
			        chunk.SetNeighbor(d, GetChunk(chunk.Location.GetAdjecent(d)));
		        chunk.SetMeshUpdates(true);
		        chunkMap.Add(chunk.Location, chunk);
		        currentlyLoading.TryRemove(chunk.Location);
	        }
	        
        }

        private void UnloadChunks(bool final) {
            while (chunkUnloadStack.Count > 0) {
                Chunk chunk = chunkUnloadStack.Pop();
	            if(!final && GetDistanceSquared(chunk.Location) < LoadDistance * LoadDistance)
		            continue;
                chunkMap.Remove(chunk.Location);
	            save.QueueChunkForSaving(chunk);
	            chunk.CleanUp();
            }
	    	save.Save();    
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

	        if (i != 0 && meshUpdateQueue.Count != 0) {
		        averageChunkUpdates += i;
		        averageChunkUpdates /= 2;
	        }
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
	        disposing = true;
	        lock (loadingthreadlock) {
		        Monitor.PulseAll(loadingthreadlock);
	        }
	        foreach(Thread t in loadingthreads) {
		        t.Join();
	        }
	        foreach(Chunk c in chunkMap.Values)
		        chunkUnloadStack.Push(c);
	        UnloadChunks(true);
        }
	    
	    
    }

}