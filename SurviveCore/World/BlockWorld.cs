using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using Priority_Queue;
using SurviveCore.OpenGL.Helper;
using SurviveCore.World.Rendering;
using SurviveCore.World.Utils;

namespace SurviveCore.World {

    class BlockWorld : IDisposable{

        private const int RendererPoolSize = 256;
        
        public const int Height = 8;
        public const int LoadDistance = 8;
	    public const int UnloadDistance = LoadDistance + 1;

        private readonly ChunkMesher mesher;
        
        private int centerX;
	    private int centerZ;

	    private readonly WorldGenerator generator;

        private readonly ObjectPool<ChunkRenderer> rendererPool;
        private readonly Queue<ChunkLocation> meshUpdateQueue;
        private readonly Dictionary<ChunkLocation, Chunk> chunkMap;
        private readonly ChunkQueue chunkLoadQueue;
        private readonly Stack<Chunk> chunkUnloadStack;
        
        private delegate void OnDraw(Frustum f);

        private event OnDraw DrawEvent;
        
        public BlockWorld() {
            rendererPool = new ObjectPool<ChunkRenderer>(RendererPoolSize);
            meshUpdateQueue = new Queue<ChunkLocation>();
            chunkMap = new Dictionary<ChunkLocation, Chunk>();
            chunkLoadQueue = new ChunkQueue();
            chunkUnloadStack = new Stack<Chunk>();
            
            mesher = new ChunkMesher();

            generator = new WorldGenerator((int)Stopwatch.GetTimestamp());
	        
	        for (int x = -LoadDistance; x <= LoadDistance; x++) {
		        for (int z = -LoadDistance; z <= LoadDistance; z++) {
			        for (int y = 0; y < Height; y++) {
				        ChunkLocation l = new ChunkLocation(centerX + x,y, centerZ + z);
				        if (GetDistanceSquared(l) <= LoadDistance * LoadDistance && !chunkLoadQueue.Contains(l) && !chunkMap.ContainsKey(l))
					        chunkLoadQueue.Enqueue(GetDistanceSquared(l, true), l);
			        }
		        }
	        }
	        
            t.Start();
        }

        public void Draw(Frustum frustum) {
            DrawEvent?.Invoke(frustum);
        }

        public void Update(int cx, int cz) {
            
            if(centerX != cx || centerZ != cz) {
	            
                centerX = cx;
                centerZ = cz;
	            
	            for (int x = -LoadDistance; x <= LoadDistance; x++) {
		            for (int z = -LoadDistance; z <= LoadDistance; z++) {
			            for (int y = 0; y < Height; y++) {
				            ChunkLocation l = new ChunkLocation(centerX + x,y, centerZ + z);
				            if (GetDistanceSquared(l) <= LoadDistance * LoadDistance && !chunkLoadQueue.Contains(l) && !chunkMap.ContainsKey(l))
					            chunkLoadQueue.Enqueue(GetDistanceSquared(l, true), l);
			            }
		            }
	            }

	            foreach (Chunk chunk in chunkMap.Values.Where(c => GetDistanceSquared(c.Location) > UnloadDistance * UnloadDistance)) {
		            chunkUnloadStack.Push(chunk);
	            }
	            
            }
	        UnloadChunks();
	        LoadChunks();
            MeshChunks();

	        if (t.ElapsedMilliseconds >= 1000) {
		        Console.WriteLine("");
		        Console.WriteLine("Loaded Chunks: {0}", chunkMap.Count);
		        Console.WriteLine("Loading Queue: {0}", chunkLoadQueue.Count);
		        Console.WriteLine("ChunkRenderer: {0}", DrawEvent?.GetInvocationList().Length);
		        t.Restart();
	        }
	        
        }

	    private Stopwatch t = new Stopwatch();
	    
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

        private void LoadChunks() {
	        int i = 5;
	        while (i > 0 && chunkLoadQueue.Count > 0) {
		        ChunkLocation l = chunkLoadQueue.Dequeue();
		        
		        if(GetDistanceSquared(l) > UnloadDistance * UnloadDistance) 
			        continue;

		        
		        WorldChunk chunk = WorldChunk.CreateWorldChunk(l, this);
			        
		        generator.FillChunk(chunk);

		        for (int d = 0; d < 6; d++)
			        chunk.SetNeighbor(d, GetChunk(l.GetAdjecent(d)));
		        chunkMap.Add(l, chunk);
		        chunkLoadQueue.Remove(l);
		        i--;
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
            int i = 5;
            while (meshUpdateQueue.Count > 0 && i > 0) {
                if(chunkMap.TryGetValue(meshUpdateQueue.Dequeue(), out Chunk c) && c.RegenerateMesh(mesher))
                    i--;
            }
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


	    private class ChunkQueue {

		    private readonly SimplePriorityQueue<ChunkLocation, int> items = new SimplePriorityQueue<ChunkLocation, int>();
		    private readonly HashSet<ChunkLocation> hashSet = new HashSet<ChunkLocation>();

		    public int Count => items.Count;

		    public void Enqueue(int distance, ChunkLocation pos) {
			    items.Enqueue(pos, distance);
			    hashSet.Add(pos);
		    }

		    public ChunkLocation Dequeue() {
			    return items.Dequeue();
		    }

		    public void Remove(ChunkLocation pos) {
			    hashSet.Remove(pos);
		    }

		    public bool Contains(ChunkLocation pos) {
			    return hashSet.Contains(pos);
		    }

		    public void Clear() {
			    items.Clear();
			    hashSet.Clear();
		    }
	    }
    }

}
