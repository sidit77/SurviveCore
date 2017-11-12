using System;

namespace SurviveCore.World {

    abstract class Chunk {

        // ReSharper disable once InconsistentNaming
        public const int BPC = 4;
        public const int Size = 1 << BPC;

        public event EventHandler ChunkUpdate;

        public abstract Chunk FindChunk(ref int x, ref int y, ref int z);
        public abstract void SetNeighbor(int d, Chunk c, bool caller = true);
        public abstract Chunk GetNeightbor(int d);

        public abstract Block GetBlockDirect(int x, int y, int z);
        public abstract bool SetBlockDirect(int x, int y, int z, Block block);
        public abstract byte GetMetaDataDirect(int x, int y, int z);
        public abstract bool SetMetaDataDirect(int x, int y, int z, byte data);

        public abstract void Delete();

        public virtual bool SetBlockDirect(int x, int y, int z, Block block, byte meta) {
            return SetBlockDirect(x, y, z, block) && SetMetaDataDirect(x, y, z, meta);
        }
        public virtual Block GetBlock(int x, int y, int z) {
            return FindChunk(ref x, ref y, ref z).GetBlockDirect(x, y, z);
        }
        public virtual byte GetMetaData(int x, int y, int z) {
            return FindChunk(ref x, ref y, ref z).GetMetaDataDirect(x,y,z);
        }
        public virtual bool SetMetaData(int x, int y, int z, byte data) {
            return FindChunk(ref x, ref y, ref z).SetMetaDataDirect(x, y, z, data);
        }
        public virtual bool SetBlock(int x, int y, int z, Block block) {
            return FindChunk(ref x, ref y, ref z).SetBlockDirect(x, y, z, block);
        }
        public virtual bool SetBlock(int x, int y, int z, Block block, byte meta) {
            return FindChunk(ref x, ref y, ref z).SetBlockDirect(x, y, z, block, meta);
        }
        public virtual void Update() {
            ChunkUpdate?.Invoke(this, EventArgs.Empty);
        }

    }

    class WorldChunk : Chunk {

        private readonly Chunk[] neighbors;
        private readonly Block[] blocks;
        private readonly byte[] metadata;

        public WorldChunk() {
            neighbors = new Chunk[]{BorderChunk.Instance, BorderChunk.Instance, BorderChunk.Instance, BorderChunk.Instance, BorderChunk.Instance, BorderChunk.Instance};
            blocks = new Block[Size * Size * Size];
            metadata = new byte[Size * Size * Size];
        }

        public override void SetNeighbor(int d, Chunk c, bool caller = true) {
            if(neighbors[d] != c) {
                Update();
            }
            neighbors[d] = c;
            if(caller) {
                c.SetNeighbor((d + 3) % 6, this, false);
            }
        }

        public override void Delete() {
            for(int i = 0; i < 6; i++) {
                neighbors[i].SetNeighbor((i + 3) % 6, BorderChunk.Instance, false);
            }
        }

        public override Chunk GetNeightbor(int d) {
            return neighbors[d];
        }

        public override Block GetBlockDirect(int x, int y, int z) {
            return blocks[x + (y << BPC) + (z << 2 * BPC)];
        }

        public override bool SetBlockDirect(int x, int y, int z, Block block) {
            Block pre = GetBlockDirect(x, y, z);
            blocks[x + (y << BPC) + (z << 2 * BPC)] = block;
            if(pre != block)
                CallChunkUpdate(x, y, z);
            return true;
        }

        public override byte GetMetaDataDirect(int x, int y, int z) {
            return metadata[x + (y << BPC) + (z << 2 * BPC)];
        }

        public override bool SetMetaDataDirect(int x, int y, int z, byte data) {
            int pre = GetMetaDataDirect(x, y, z);
            metadata[x + (y << BPC) + (z << 2 * BPC)] = data;
            if(pre != data)
                CallChunkUpdate(x, y, z);
            return true;
        }

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

        private void CallChunkUpdate(int x, int y, int z) {
            Update();
            if(x == 0)
                neighbors[Direction.NegativeX].Update();
            if(y == 0)
                neighbors[Direction.NegativeY].Update();
            if(z == 0)
                neighbors[Direction.NegativeZ].Update();
            if(x == Size - 1)
                neighbors[Direction.PositiveX].Update();
            if(y == Size - 1)
                neighbors[Direction.PositiveY].Update();
            if(z == Size - 1)
                neighbors[Direction.PositiveZ].Update();
        }

    }

    class BorderChunk : Chunk {
        public static BorderChunk Instance { get; } = new BorderChunk();


        private static readonly Block Border = new BorderBlock();
        private BorderChunk() { }
        
        public override void SetNeighbor(int d, Chunk c, bool caller = true) { }
        public override void Delete() { }

        public override Chunk GetNeightbor(int d) {
            return this;
        }

        public override Block GetBlockDirect(int x, int y, int z) {
            return Border;
        }

        public override bool SetBlockDirect(int x, int y, int z, Block block) {
            return false;
        }

        public override byte GetMetaDataDirect(int x, int y, int z) {
            return 0;
        }

        public override bool SetMetaDataDirect(int x, int y, int z, byte data) {
            return false;
        }

        public override Chunk FindChunk(ref int x, ref int y, ref int z) {
            return this;
        }

        private class BorderBlock : Block {
            public BorderBlock() : base("World Border") {
            }
            public override bool IsUnrendered => true;

            public override bool IsSolid() {
                return true;
            }
        }
    }

}
