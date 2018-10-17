using System;
using System.Runtime.CompilerServices;
using SurviveCore.World.Rendering;
using SurviveCore.World.Utils;

namespace SurviveCore.World {

    public class Chunk {

        //TODO move to world generator
        public const int FinalGenerationLevel = 1;
        public const int BPC = 5;
        public const int Size = 1 << BPC;
        public static readonly Block Border = new Block("Border", "", true, true);
        
        private static readonly ConcurrentObjectPool<Chunk> pool = new ConcurrentObjectPool<Chunk>(256, () => new Chunk());

        public static Chunk CreateChunk(ChunkLocation l, BlockWorld w) {
            Chunk c = pool.Get();
            c.SetUp(l, w);
            return c;
        }
        
        private ChunkLocation location;
        private bool dirty;
        private bool generated;
        private bool meshready;
        private int renderedblocks;
        private readonly Chunk[] neighbors;
        private readonly Block[] blocks;
        private readonly byte[] metadata;
        private ChunkRenderer renderer;
        private BlockWorld world;
        private int genlevel;
        
        private Chunk(){
            neighbors = new Chunk[]{null, null, null, null, null, null};
            blocks = new Block[Size * Size * Size];
            metadata = new byte[Size * Size * Size];
        }
        
        public void SetNeighbor(int d, Chunk c, bool caller = true) {
            if(neighbors[d] != c && c != null) {
                Update(UpdateSource.Neighbor);
            }
            neighbors[d] = c;
            if(caller) {
                c?.SetNeighbor((d + 3) % 6, this, false);
            }
        }

        public bool CanBeDecorated() {
            if (IsGenerationFinal)
                return false;
            
            for(int x = -genlevel - 1; x <= genlevel + 1; x++)
            for(int y = -genlevel - 1; y <= genlevel + 1; y++)
            for(int z = -genlevel - 1; z <= genlevel + 1; z++)
                if(!CheckChunkAccess(x << BPC, y << BPC, z << BPC))
                    return false;
            return true;
        }

        public void Update(UpdateSource source) {
            if(!meshready) //TODO investigate generation only after decoration
                return;
            switch (source) {
                case UpdateSource.Modification:
                    world.QueueChunkForRemesh(this, 0);
                    break;
                case UpdateSource.Generation:
                    world.QueueChunkForRemesh(this, 1);
                    break;
                case UpdateSource.Neighbor:
                    world.QueueChunkForRemesh(this, 2);
                    break;
            }
        }

        public void IncrementGenerationLevel() {
            if(genlevel < FinalGenerationLevel)
                genlevel++;
        }

        public void SetGenerationLevel(int level) {
            genlevel = level;
        }

        public void SetMeshUpdates(bool enabled) {
            meshready = enabled;
        }
        
        public bool RegenerateMesh(ChunkMesher mesher) {
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

        public void CleanUp() {
            for(int i = 0; i < 6; i++) {
                neighbors[i]?.SetNeighbor((i + 3) % 6, null, false);
                neighbors[i] = null;
            }
            if (renderer != null) {
                world.Renderer.DisposeChunkRenderer(renderer);
                renderer = null;
            }
            world = null;
            pool.Add(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsFull() {
            //return renderedblocks == 1 << (BPC * 3);
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool isEmpty() {
            return renderedblocks == 0;
        }

        private void SetUp(ChunkLocation l, BlockWorld world) {
            location = l;
            this.world = world;
            renderedblocks = 0;
            genlevel = -1;
            generated = false;
            meshready = false;
            dirty = false;
            for (int i = 0; i < blocks.Length; i++)
                blocks[i] = Blocks.Air;
            for (int i = 0; i < metadata.Length; i++)
                metadata[i] = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Chunk GetNeightbor(int d) {
            return neighbors[d];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Block GetBlockDirect(int x, int y, int z) {
            return blocks[x | (y << BPC) | (z << 2 * BPC)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetBlockDirect(int x, int y, int z, Block block, UpdateSource source = UpdateSource.Modification) {
            Block pre = GetBlockDirect(x, y, z);
            blocks[x | (y << BPC) | (z << 2 * BPC)] = block;
            if(pre != block)
                CallChunkUpdate(x, y, z, source);
            //TODO add IsSolid somewhere to fix the invisiblilty
            if( pre.IsUnrendered() && !block.IsUnrendered())
                renderedblocks++;
            if(!pre.IsUnrendered() &&  block.IsUnrendered())
                renderedblocks--;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetMetaDataDirect(int x, int y, int z) {
            return metadata[x | (y << BPC) | (z << 2 * BPC)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetMetaDataDirect(int x, int y, int z, byte data, UpdateSource source = UpdateSource.Modification) {
            int pre = GetMetaDataDirect(x, y, z);
            metadata[x | (y << BPC) | (z << 2 * BPC)] = data;
            if(pre != data)
                CallChunkUpdate(x, y, z, source);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CheckChunkAccess(int x, int y, int z) {
            if (y < 0 || (y >> BPC) > BlockWorld.Height)
                return true;
            return FindChunk(ref x, ref y, ref z) != null;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Chunk FindChunk(ref int x, ref int y, ref int z) {
            if(x < 0) {
                x += Size;
                return neighbors[(int)Direction.NegativeX]?.FindChunk(ref x, ref y, ref z);
            }
            if(y < 0) {
                y += Size;
                return neighbors[(int)Direction.NegativeY]?.FindChunk(ref x, ref y, ref z);
            }
            if(z < 0) {
                z += Size;
                return neighbors[(int)Direction.NegativeZ]?.FindChunk(ref x, ref y, ref z);
            }

            if(x >= Size) {
                x -= Size;
                return neighbors[(int)Direction.PositiveX]?.FindChunk(ref x, ref y, ref z);
            }
            if(y >= Size) {
                y -= Size;
                return neighbors[(int)Direction.PositiveY]?.FindChunk(ref x, ref y, ref z);
            }
            if(z >= Size) {
                z -= Size;
                return neighbors[(int)Direction.PositiveZ]?.FindChunk(ref x, ref y, ref z);
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
                neighbors[(int)Direction.NegativeX]?.Update(source);
            if(y == 0)
                neighbors[(int)Direction.NegativeY]?.Update(source);
            if(z == 0)
                neighbors[(int)Direction.NegativeZ]?.Update(source);
            if(x == Size - 1)
                neighbors[(int)Direction.PositiveX]?.Update(source);
            if(y == Size - 1)
                neighbors[(int)Direction.PositiveY]?.Update(source);
            if(z == Size - 1)
                neighbors[(int)Direction.PositiveZ]?.Update(source);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetBlockDirect(int x, int y, int z, Block block, byte meta, UpdateSource source = UpdateSource.Modification) {
            return SetBlockDirect(x, y, z, block, source) && SetMetaDataDirect(x, y, z, meta, source);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Block GetBlock(int x, int y, int z) {
            return FindChunk(ref x, ref y, ref z)?.GetBlockDirect(x, y, z) ?? Border;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetMetaData(int x, int y, int z) {
            return FindChunk(ref x, ref y, ref z)?.GetMetaDataDirect(x,y,z) ?? 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetMetaData(int x, int y, int z, byte data, UpdateSource source = UpdateSource.Modification) {
            return FindChunk(ref x, ref y, ref z)?.SetMetaDataDirect(x, y, z, data, source) ?? false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetBlock(int x, int y, int z, Block block, UpdateSource source = UpdateSource.Modification) {
            return FindChunk(ref x, ref y, ref z)?.SetBlockDirect(x, y, z, block, source) ?? false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetBlock(int x, int y, int z, Block block, byte meta, UpdateSource source = UpdateSource.Modification) {
            return FindChunk(ref x, ref y, ref z)?.SetBlockDirect(x, y, z, block, meta, source) ?? false;
        }
        
        public ChunkLocation Location => location;
        public bool IsDirty => dirty;
        public bool IsGenerated => generated;
        public bool IsGenerationFinal => genlevel == FinalGenerationLevel;
        public int GenerationLevel => genlevel;

        public override bool Equals(object obj) {
            return obj is Chunk o && o.location.Equals(location);
        }

        public override int GetHashCode() {
            return location.GetHashCode();
        }
    }

    public enum UpdateSource {
        Modification,
        Generation,
        Neighbor
        //TODO Add a AO remesh source to solve outdated ao-aocases
    }
    
}
