using System;
using System.Drawing;
using System.Numerics;
using SurviveCore.DirectX;
using SurviveCore.Particles;
using SurviveCore.Physics;
using SurviveCore.World;
using SurviveCore.World.Rendering;
using SurviveCore.World.Saving;
using WinApi.User32;

namespace SurviveCore {

    public class SurvivalGame : IDisposable {

        private WorldRenderer worldrenderer;
        private ParticleRenderer particlerenderer;
        private SelectionRenderer selectionrenderer;
        
        private BlockWorld world;
        private PhysicsWorld physics;
        private WorldSave savegame;

        private readonly Block[] inventory = { Blocks.Bricks, Blocks.Stone, Blocks.Grass, Blocks.Dirt, Blocks.Sand, Blocks.Wood, Blocks.Leaves };
        private int slot;
        public Block SelectedBlock => inventory[slot];

        private float veloctiy;
        private Vector3 lastMovement;

        private SectionRendererInfo info;
     
        private Camera camera;

        private Client client;

        public string DebugText =>
            $"Pos: [{(int) MathF.Round(camera.Position.X)}|{(int) MathF.Round(camera.Position.Y)}|{(int) MathF.Round(camera.Position.Z)}]\n" +
            world.DebugText;
        
        public SurvivalGame(Client c)
        {
            client = c;
            client.OnDispose += Dispose;
            
            
            worldrenderer = new WorldRenderer(c.Dx.Device);
            particlerenderer = new ParticleRenderer(c.Dx.Device);
            selectionrenderer = new SelectionRenderer(c.Dx.Device);
            savegame = new WorldSave(worldrenderer, "Test");
            world = savegame.GetWorld();
            physics = new PhysicsWorld(world);
            savegame.GetPlayerData("default", out Vector3 pos, out Quaternion rot);
            camera = new Camera(75f * (float) Math.PI / 180, (float) c.ScreenSize.Width / c.ScreenSize.Height,
                0.3f, 620.0f) {Position = pos, Rotation = rot};
            
            info = selectionrenderer.Info;
        }
       
        
        public void Render()
        {
            if(Settings.Instance.UpdateCamera)
                world.Update(camera.Position);
            
            camera.Aspect = (float)client.ScreenSize.Width / client.ScreenSize.Height;
            camera.Update(Settings.Instance.UpdateCamera);
            
            client.Dx.Context.Rasterizer.State = Settings.Instance.Wireframe ? client.WireframeRenderState : client.DefaultRenderState;
            worldrenderer.Draw(client.Dx.Context, camera);
            client.Dx.Context.Rasterizer.State = client.DefaultRenderState;
            
            particlerenderer.Render(client.Dx.Context, camera);
            selectionrenderer.Render(client.Dx.Context, camera);
        }

        public void Update(InputManager.InputState input)
        {
            if (input.IsKeyDown(VirtualKey.F1))
                camera.Rotation = Quaternion.Identity;
            Vector3 movement = Vector3.Zero;
            if (input.IsKey(VirtualKey.W))
                movement += camera.Forward;
            if (input.IsKey(VirtualKey.S))
                movement += camera.Back;
            if (input.IsKey(VirtualKey.A))
                movement += camera.Left;
            if (input.IsKey(VirtualKey.D))
                movement += camera.Right;
            movement.Y = 0;
            movement = movement.LengthSquared() > 0 ? Vector3.Normalize(movement) : Vector3.Zero;
            //TODO Investigate the glitch up instead of jump problem

            if (Settings.Instance.Physics)
            {
                //TODO fix the stuttering (Mouse precision?)
                movement *= input.IsKey(VirtualKey.SHIFT) ? 0.06f : 0.03f;
                veloctiy -= 0.0010f;
                if (physics.IsGrounded(camera.Position))
                    veloctiy = input.IsKeyDown(VirtualKey.SPACE) ? 0.055f : 0;
                movement = Vector3.Lerp(lastMovement, movement, 0.05f);
                movement.Y += veloctiy;
                movement = physics.ClampToWorld(camera.Position, movement);
                lastMovement = movement * new Vector3(1, 0, 1);
                camera.Position += movement;
            }
            else
            {
                if (input.IsKey(VirtualKey.SPACE))
                    movement += Vector3.UnitY;
                if (input.IsKey(VirtualKey.SHIFT))
                    movement -= Vector3.UnitY;
                movement *= input.IsKey(VirtualKey.CONTROL) ? 0.3f : 0.06f;
                camera.Position += lastMovement = Vector3.Lerp(lastMovement, movement, 0.03f);
            }

            info.Enabled = FindIntersection(out info.Position, out info.Normal);
            if (input.MouseCaptured)
            {
                var mpos = input.DeltaMousePosition;
                camera.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, (1f / 600) * mpos.X);

                const float anglediff = 0.001f;
                float angle = MathF.Acos(Vector3.Dot(camera.Forward, Vector3.UnitY));
                camera.Rotation *= Quaternion.CreateFromAxisAngle(camera.Right,
                    MathHelper.Clamp((1f / 600) * mpos.Y + angle, anglediff, MathF.PI - anglediff) - angle);

                if (input.IsKeyDown(VirtualKey.LBUTTON) || input.IsKeyDown(VirtualKey.Q))
                {
                    if (info.Enabled && world.SetBlock(info.Position, Blocks.Air))
                    {
                    }
                }

                if (input.IsKeyDown(VirtualKey.RBUTTON) || input.IsKeyDown(VirtualKey.E))
                {
                    if (info.Enabled
                        && world.GetBlock(info.Position + info.Normal) == Blocks.Air
                        && world.SetBlock(info.Position + info.Normal, inventory[slot])
                        && !physics.CanMoveTo(camera.Position))
                    {
                        world.SetBlock(info.Position + info.Normal, Blocks.Air);
                    }
                }

                if (info.Enabled && input.IsKeyDown(VirtualKey.MBUTTON))
                {
                    Console.WriteLine(world.GetChunk(ChunkLocation.FromPos(info.Position))?.GenerationLevel);
                }
            }


            slot = Math.Abs(input.MouseWheel / 120) % inventory.Length;
        }

        public void Dispose()
        {
            client.OnDispose -= Dispose;
            
            particlerenderer.Dispose();
            worldrenderer.Dispose();
            selectionrenderer.Dispose();
            world.Dispose();
            savegame.SavePlayerData("default", camera.Position, camera.Rotation);
            savegame.Dispose();
            Console.WriteLine("Dispose");
        }

        
        
        
        private unsafe bool GetHitNormal(Vector3 pos, out Vector3 hitnormal) {
            hitnormal = Vector3.Zero;
            Vector3 forward = camera.Forward;
            Vector3 position = camera.Position;
            for(int i = 0; i < 6; i++) {
                if (!RayFaceIntersection(&forward.X, &position.X, &pos.X, i, out float d)) continue;
                if(User32Methods.GetKeyState(VirtualKey.T).IsPressed)
                    particlerenderer.AddParticle(position + forward * d, 0.04f, Color.Red);
                fixed(float* n = &hitnormal.X)
                    n[i % 3] = i < 3 ? -1 : 1;
                return true;
            }
            return false;
        }
        
        private unsafe bool RayFaceIntersection(float* r, float* p, float* d, int i, out float distance) {
            distance = float.PositiveInfinity;
            int normal = i < 3 ? -1 : 1;
            int i0 = (i + 0) % 3;
            int i1 = (i + 1) % 3;
            int i2 = (i + 2) % 3;
            float ndotdir = r[i0] * normal;
            if (-ndotdir < 0.0001f)
                return false;
            float center = d[i0] + normal * 0.5f;
            distance = -(normal * p[i0] - center * normal) / ndotdir;
            if (distance < 0)
                return false;
            return MathF.Abs(p[i1] + r[i1] * distance - d[i1]) <= 0.5f
                && MathF.Abs(p[i2] + r[i2] * distance - d[i2]) <= 0.5f;
        }
        
        private bool FindIntersection(out Vector3 pos, out Vector3 normal)
        {
            if (User32Methods.GetKeyState(VirtualKey.T).IsPressed)
                particlerenderer.Clear();
            normal = Vector3.Zero;
            if (!Raycast(out pos))
                return false;
            pos = pos.Round();
            int counter = 0;
            do {
                pos += normal;
                if (!GetHitNormal(pos, out normal) || counter++ > 5)
                    return false;
            } while (world.GetBlock(pos + normal) != Blocks.Air);
            return true;
        }

        private bool Raycast(out Vector3 pos) {
            pos = Vector3.Zero;
            Vector3 forward = camera.Forward;
            for(float f = 0; f < 7; f += 0.25f) {
                if(User32Methods.GetKeyState(VirtualKey.T).IsPressed)
                    particlerenderer.AddParticle(camera.Position + forward * f, 0.03f, Color.Aquamarine);
                if (world.GetBlock(camera.Position + forward * f) == Blocks.Air) continue;
                pos = camera.Position + forward * f;
                return true;
            }
            return false;
        }

        
    }

}
