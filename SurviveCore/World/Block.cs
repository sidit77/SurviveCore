using System.Collections.Generic;

namespace SurviveCore.World {

    static class Blocks {
        public readonly static Block Air = new AirBlock();
        public readonly static Block Stone = new Block("Stone", "./Assets/Textures/Stone.png");
        public readonly static Block Grass = new Block("Grass", "./Assets/Textures/Grass_Side.png").SetTexture(1, "./Assets/Textures/Grass_Top.png").SetTexture(4, "./Assets/Textures/Dirt.png");
        public readonly static Block Bricks = new Block("Bricks", "./Assets/Textures/Bricks.png");

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

    class Block {
        
        private static List<string> textures = new List<string>();
        private static int GetTextureID(string path) {
            if (!textures.Contains(path))
                textures.Add(path);
            return textures.IndexOf(path);
        }
        public static string[] Textures {
            get {
                return textures.ToArray();
            }
        }

        private string name;
        private int[] textureids;

        public Block(string name) {
            this.name = name;
            textureids = new [] { -1, -1, -1, -1, -1, -1 };
        }

        public Block(string name, string texture) {
            this.name = name;
            int texid = GetTextureID(texture);
            textureids = new []{texid, texid, texid, texid, texid, texid };
        }

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
