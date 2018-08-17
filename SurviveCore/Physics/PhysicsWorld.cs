using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using SurviveCore.World;

namespace SurviveCore.Physics {
    
    public class PhysicsWorld {
        
        private readonly BlockWorld world;

        public PhysicsWorld(BlockWorld world) {
            this.world = world;
        }
        
        public bool IsGrounded(Vector3 pos) {
            return !CanMoveToY(pos, -0.05f);
        }
        
        //TODO use a binary search
        public Vector3 ClampToWorld(Vector3 pos, Vector3 mov) {
            const float pecision = 0.005f;
            bool x = true;
            while(x && !CanMoveToX(pos, mov.X))
                x = Math.Abs(mov.X /= 2) > pecision;

            bool y = true;
            while(y && !CanMoveToY(pos, mov.Y))
                y = Math.Abs(mov.Y /= 2) > pecision;

            bool z = true;
            while(z && !CanMoveToZ(pos, mov.Z))
                z = Math.Abs(mov.Z /= 2) > pecision;
            
            return new Vector3(x ? mov.X : 0, y ? mov.Y : 0, z ? mov.Z : 0); 
        }
        
        public bool CanMoveTo(Vector3 pos) {
            return IsAir(pos + new Vector3(-0.4f,  0.40f, -0.4f)) &&
                   IsAir(pos + new Vector3( 0.4f,  0.40f, -0.4f)) &&
                   IsAir(pos + new Vector3( 0.4f,  0.40f,  0.4f)) &&
                   IsAir(pos + new Vector3(-0.4f,  0.40f,  0.4f)) &&

                   IsAir(pos + new Vector3(-0.4f, -1.50f, -0.4f)) &&
                   IsAir(pos + new Vector3( 0.4f, -1.50f, -0.4f)) &&
                   IsAir(pos + new Vector3( 0.4f, -1.50f,  0.4f)) &&
                   IsAir(pos + new Vector3(-0.4f, -1.50f,  0.4f)) &&

                   IsAir(pos + new Vector3(-0.4f, -0.55f, -0.4f)) &&
                   IsAir(pos + new Vector3( 0.4f, -0.55f, -0.4f)) &&
                   IsAir(pos + new Vector3( 0.4f, -0.55f,  0.4f)) &&
                   IsAir(pos + new Vector3(-0.4f, -0.55f,  0.4f));
        }

        private bool CanMoveToY(Vector3 pos, float dy) {
            float y = (dy < 0 ? -1.5f : 0.4f) + dy;
            return IsAir(pos + new Vector3(-0.4f, y, -0.4f)) &&
                   IsAir(pos + new Vector3( 0.4f, y, -0.4f)) &&
                   IsAir(pos + new Vector3( 0.4f, y,  0.4f)) &&
                   IsAir(pos + new Vector3(-0.4f, y,  0.4f));
        }
        
        private bool CanMoveToX(Vector3 pos, float dx) {
            float x = (dx < 0 ? -0.4f : 0.4f) + dx;
            return IsAir(pos + new Vector3(x,  0.40f, -0.4f)) &&
                   IsAir(pos + new Vector3(x,  0.40f,  0.4f)) &&
                   IsAir(pos + new Vector3(x, -0.55f, -0.4f)) &&
                   IsAir(pos + new Vector3(x, -0.55f,  0.4f)) &&
                   IsAir(pos + new Vector3(x, -1.50f, -0.4f)) &&
                   IsAir(pos + new Vector3(x, -1.50f,  0.4f));
        }
        
        private bool CanMoveToZ(Vector3 pos, float dz) {
            float z = (dz < 0 ? -0.4f : 0.4f) + dz;
            return IsAir(pos + new Vector3( -0.4f,  0.40f, z)) &&
                   IsAir(pos + new Vector3(  0.4f,  0.40f, z)) &&
                   IsAir(pos + new Vector3( -0.4f, -0.55f, z)) &&
                   IsAir(pos + new Vector3(  0.4f, -0.55f, z)) &&
                   IsAir(pos + new Vector3( -0.4f, -1.50f, z)) &&
                   IsAir(pos + new Vector3(  0.4f, -1.50f, z));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsAir(Vector3 pos) {
            return !world.GetBlock(pos).HasHitbox();
        }
        
    }
    
}