using System;
using System.Runtime.CompilerServices;
using SurviveCore.World.Rendering;
using SurviveCore.World.Utils;

namespace SurviveCore.World {

    public abstract class Chunk {

        public const int BPC = 4;
        public const int Size = 1 << BPC;

        protected ChunkLocation location;
        protected bool dirty = false;
        protected bool generated = false;
        
        public abstract Chunk FindChunk(ref int x, ref int y, ref int z);
        public abstract void SetNeighbor(int d, Chunk c, bool caller = true);
        public abstract Chunk GetNeightbor(int d);

        public abstract Block GetBlockDirect(int x, int y, int z);
        public abstract bool SetBlockDirect(int x, int y, int z, Block block, UpdateSource source = UpdateSource.Modification);
        public abstract byte GetMetaDataDirect(int x, int y, int z);
        public abstract bool SetMetaDataDirect(int x, int y, int z, byte data, UpdateSource source = UpdateSource.Modification);
        public abstract void Update(UpdateSource source);
        public abstract bool RegenerateMesh(ChunkMesher m);
        public abstract void CleanUp();
        public abstract bool IsFull();
        public abstract bool isEmpty();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetBlockDirect(int x, int y, int z, Block block, byte meta, UpdateSource source = UpdateSource.Modification) {
            return SetBlockDirect(x, y, z, block, source) && SetMetaDataDirect(x, y, z, meta, source);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Block GetBlock(int x, int y, int z) {
            return FindChunk(ref x, ref y, ref z).GetBlockDirect(x, y, z);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetMetaData(int x, int y, int z) {
            return FindChunk(ref x, ref y, ref z).GetMetaDataDirect(x,y,z);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetMetaData(int x, int y, int z, byte data, UpdateSource source = UpdateSource.Modification) {
            return FindChunk(ref x, ref y, ref z).SetMetaDataDirect(x, y, z, data, source);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetBlock(int x, int y, int z, Block block, UpdateSource source = UpdateSource.Modification) {
            return FindChunk(ref x, ref y, ref z).SetBlockDirect(x, y, z, block, source);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetBlock(int x, int y, int z, Block block, byte meta, UpdateSource source = UpdateSource.Modification) {
            return FindChunk(ref x, ref y, ref z).SetBlockDirect(x, y, z, block, meta, source);
        }
        
        public ChunkLocation Location => location;
        public bool IsDirty => dirty;
        public bool IsGenerated => generated;

        public override bool Equals(object obj) {
            return obj is Chunk o && o.location.Equals(location);
        }

        public override int GetHashCode() {
            return location.GetHashCode();
        }
    }

    class WorldChunk : Chunk {

        private static readonly ObjectPool<WorldChunk> pool = new ObjectPool<WorldChunk>(256, () => new WorldChunk());

        public static WorldChunk CreateWorldChunk(ChunkLocation l, BlockWorld w) {
            WorldChunk c = pool.Get();
            c.SetUp(l, w);
            return c;
        }
        
        private int renderedblocks;
        private bool meshready;
        private readonly Chunk[] neighbors;
        private readonly Block[] blocks;
        private readonly byte[] metadata;
        private ChunkRenderer renderer;
        private BlockWorld world;

        private WorldChunk(){
            neighbors = new Chunk[]{BorderChunk.Instance, BorderChunk.Instance, BorderChunk.Instance, BorderChunk.Instance, BorderChunk.Instance, BorderChunk.Instance};
            blocks = new Block[Size * Size * Size];
            metadata = new byte[Size * Size * Size];
        }
        
        public override void SetNeighbor(int d, Chunk c, bool caller = true) {
            if(neighbors[d] != c && c != BorderChunk.Instance) {
                Update(UpdateSource.Neighbor);
            }
            neighbors[d] = c;
            if(caller) {
                c.SetNeighbor((d + 3) % 6, this, false);
            }
        }

        public override void Update(UpdateSource source) {
            if(!meshready)
                return;
            switch (source) {
                case UpdateSource.Modification:
                    world.QueueChunkForRemesh(this, 0);
                    break;
                case UpdateSource.Generation:
                case UpdateSource.Loading:
                    world.QueueChunkForRemesh(this, 1);
                    break;
                case UpdateSource.Neighbor:
                    world.QueueChunkForRemesh(this, 2);
                    break;
            }
        }

        public void SetMeshUpdates(bool enabled) {
            meshready = enabled;
        }
        
        public override bool RegenerateMesh(ChunkMesher mesher) {
            Vertex[] m = mesher.GenerateMesh(this, world.Renderer.GetBlockMapping());
            if (m == null && renderer == null)
                return true;
            if(m != null && renderer == null)
                renderer = world.Renderer.CreateChunkRenderer(this);
            if(m != null)
                renderer.Update(m);
            //TODO free chunkrenderer instead of deleting it. 
            renderer?.SetActive(m != null);
            return true;
        }

        public override void CleanUp() {
            for(int i = 0; i < 6; i++) {
                neighbors[i].SetNeighbor((i + 3) % 6, BorderChunk.Instance, false);
                neighbors[i] = BorderChunk.Instance;
            }
            if (renderer != null) {
                world.Renderer.DisposeChunkRenderer(renderer);
                renderer = null;
            }
            pool.Add(this);
            world = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool IsFull() {
            return renderedblocks == 1 << (BPC * 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool isEmpty() {
            return renderedblocks == 0;
        }

        private void SetUp(ChunkLocation l, BlockWorld world) {
            location = l;
            this.world = world;
            renderedblocks = 0;
            meshready = false;
            generated = false;
            dirty = false;
            for (int i = 0; i < blocks.Length; i++)
                blocks[i] = Blocks.Air;
            for (int i = 0; i < metadata.Length; i++)
                metadata[i] = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Chunk GetNeightbor(int d) {
            return neighbors[d];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Block GetBlockDirect(int x, int y, int z) {
            return blocks[x | (y << BPC) | (z << 2 * BPC)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool SetBlockDirect(int x, int y, int z, Block block, UpdateSource source) {
            Block pre = GetBlockDirect(x, y, z);
            blocks[x | (y << BPC) | (z << 2 * BPC)] = block;
            if(pre != block)
                CallChunkUpdate(x, y, z, source);
            if( pre.IsUnrendered() && !block.IsUnrendered())
                renderedblocks++;
            if(!pre.IsUnrendered() &&  block.IsUnrendered())
                renderedblocks--;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override byte GetMetaDataDirect(int x, int y, int z) {
            return metadata[x | (y << BPC) | (z << 2 * BPC)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool SetMetaDataDirect(int x, int y, int z, byte data, UpdateSource source) {
            int pre = GetMetaDataDirect(x, y, z);
            metadata[x | (y << BPC) | (z << 2 * BPC)] = data;
            if(pre != data)
                CallChunkUpdate(x, y, z, source);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Chunk FindChunk(ref int x, ref int y, ref int z) {
            if(x < 0) {
                x += Size;
                return neighbors[Direction.NegativeX].FindChunk(ref x, ref y, ref z);
            }
            if(y < 0) {
                y += Size;
                return neighbors[Direction.NegativeY].FindChunk(ref x, ref y, ref z);
            }
            if(z < 0) {
                z += Size;
                return neighbors[Direction.NegativeZ].FindChunk(ref x, ref y, ref z);
            }

            if(x >= Size) {
                x -= Size;
                return neighbors[Direction.PositiveX].FindChunk(ref x, ref y, ref z);
            }
            if(y >= Size) {
                y -= Size;
                return neighbors[Direction.PositiveY].FindChunk(ref x, ref y, ref z);
            }
            if(z >= Size) {
                z -= Size;
                return neighbors[Direction.PositiveZ].FindChunk(ref x, ref y, ref z);
            }

            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CallChunkUpdate(int x, int y, int z, UpdateSource source) {
            if(source == UpdateSource.Modification)
                dirty = true;
            if(source == UpdateSource.Generation)
                generated = true;
            Update(source);
            if(x == 0)
                neighbors[Direction.NegativeX].Update(source);
            if(y == 0)
                neighbors[Direction.NegativeY].Update(source);
            if(z == 0)
                neighbors[Direction.NegativeZ].Update(source);
            if(x == Size - 1)
                neighbors[Direction.PositiveX].Update(source);
            if(y == Size - 1)
                neighbors[Direction.PositiveY].Update(source);
            if(z == Size - 1)
                neighbors[Direction.PositiveZ].Update(source);
        }
        
    }

    class BorderChunk : Chunk {
        public static BorderChunk Instance { get; } = new BorderChunk();


        private static readonly Block Border = new Block("Border", "", true, true);
        
        public override void SetNeighbor(int d, Chunk c, bool caller = true) { }
        public override void Update(UpdateSource source) {
            
        }

        public override bool RegenerateMesh(ChunkMesher m) {
            return false;
        }

        public override void CleanUp() { }
        public override bool IsFull() {
            return true;
        }

        public override bool isEmpty() {
            return true;
        }

        public override Chunk GetNeightbor(int d) {
            return this;
        }

        public override Block GetBlockDirect(int x, int y, int z) {
            return Border;
        }

        public override bool SetBlockDirect(int x, int y, int z, Block block, UpdateSource source) {
            return false;
        }

        public override byte GetMetaDataDirect(int x, int y, int z) {
            return 0;
        }

        public override bool SetMetaDataDirect(int x, int y, int z, byte data, UpdateSource source) {
            return false;
        }

        public override Chunk FindChunk(ref int x, ref int y, ref int z) {
            return this;
        }

    }

    public enum UpdateSource {
        Modification,
        Generation,
        Loading,
        Neighbor
    }
    
}
