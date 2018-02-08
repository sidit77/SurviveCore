using System.Collections.Generic;

namespace SurviveCore.World {

    public static class Blocks {
        public static readonly Block Air = new AirBlock();
        public static readonly Block Stone = new Block("Stone", "./Assets/Textures/Stone.png");
        public static readonly Block Grass = new Block("Grass", "./Assets/Textures/Grass_Side.png").SetTexture(1, "./Assets/Textures/Grass_Top.png").SetTexture(4, "./Assets/Textures/Dirt.png");
        public static readonly Block Bricks = new Block("Bricks", "./Assets/Textures/Bricks.png");

        private class AirBlock : Block {
            public AirBlock() : base("Air") {
            }
            public override bool IsUnrendered {
                get {
                    return true;
                }
            }
            public override bool IsSolid() {
                return false;
            }
        }
    }

    public class Block {
        
        private static readonly List<string> textures = new List<string>();
        private static readonly List<Block> blocks = new List<Block>();
        private static int GetTextureID(string path) {
            if (!textures.Contains(path))
                textures.Add(path);
            return textures.IndexOf(path);
        }
        public static string[] Textures => textures.ToArray();

        public static Block GetBlock(int id) {
            return blocks[id];
        }
        
        private string name;
        private int[] textureids;
        private int id;
        
        public Block(string name) {
            blocks.Add(this);
            id = blocks.IndexOf(this);
            this.name = name;
            textureids = new [] { -1, -1, -1, -1, -1, -1 };
        }
        
        public Block(string name, string texture) {
            blocks.Add(this);
            id = blocks.IndexOf(this);
            this.name = name;
            int texid = GetTextureID(texture);
            textureids = new []{texid, texid, texid, texid, texid, texid };
        }

        public int ID => id;
        
        public virtual Block SetTexture(int side, string texture) {
            textureids[side] = GetTextureID(texture);
            return this;
        }

        public virtual int GetTextureID(int side) {
            return textureids[side];
        }

        public virtual bool IsSolid() {
            return true;
        }

        public virtual bool IsUnrendered {
            get {
                return false;
            }
        }

        public virtual string Name {
            get {
                return name;
            }
        }
    }

}
