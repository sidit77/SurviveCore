using Survive.World;
using System;
using System.Collections.Generic;

namespace Survive.World {

    static class Blocks {
        public readonly static Block Air = new AirBlock();
        public readonly static Block Stone = new Block("Stone", 1);
        public readonly static Block Grass = new Block("Grass", 3).SetTexture(1, 4).SetTexture(4, 2);
        public readonly static Block Bricks = new Block("Bricks", 0);

        private class AirBlock : Block {
            public AirBlock() : base("Air", -1) {
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

        private string name;
        private int[] textureids;

        public Block(string name, int texid) {
            this.name = name;
            this.textureids = new int[] {texid, texid, texid, texid, texid, texid };
        }

        public virtual Block SetTexture(int side, int id) {
            textureids[side] = id;
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
