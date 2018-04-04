using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SurviveCore.World {

    public static class Blocks {
        public static readonly Block Air = new Block("Air", "", false, true);
        public static readonly Block Stone = new Block("Stone", "Stone.png");
        public static readonly Block Grass = new Block("Grass", "Grass_Side.png").SetTexture(1, "Grass_Top.png").SetTexture(4, "Dirt.png");
        public static readonly Block Bricks = new Block("Bricks", "Bricks.png");
        public static readonly Block Dirt = new Block("Dirt", "Dirt.png");
    }

    public class Block {
        
        private static readonly List<Block> blocks = new List<Block>();

        public static Block GetBlock(int id) {
            return blocks[id];
        }
        
        private readonly string name;
        private readonly string[] textures;
        private readonly int id;
        private readonly bool solid, unrendered;

        public Block(string name, string texture, bool solid = true, bool unrendered = false) {
            blocks.Add(this);
            id = blocks.IndexOf(this);
            this.name = name;
            this.solid = solid;
            this.unrendered = unrendered;
            textures = new []{texture, texture, texture, texture, texture, texture };
        }

        public int ID => id;
        
        public Block SetTexture(int side, string texture) {
            textures[side] = texture;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetTexture(int side) {
            return textures[side];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSolid() {
            return solid;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUnrendered() {
            return unrendered;
        }

        public virtual string Name => name;
    }

}
