using System;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using SurviveCore.DirectX;
using SurviveCore.Gui;
using SurviveCore.Physics;
using SurviveCore.World;
using SurviveCore.World.Saving;
using WinApi.User32;

namespace SurviveCore {

    public class SurvivalGame : IScene {
        
        private BlockWorld world;
        private PhysicsWorld physics;
        private WorldSave savegame;

        private readonly Block[] inventory = { Blocks.Bricks, Blocks.Stone, Blocks.Grass, Blocks.Dirt, Blocks.Sand, Blocks.Wood, Blocks.Leaves };
        private int slot;

        private float veloctiy;
        private Vector3 lastMovement;

        private SectionRendererInfo info;
     
        private Camera camera;
        private Action<Vector3, float, Color> spawnParticle;
        private Action clearParticles;
        
        public void OnActivate(Client client)
        {
            camera = client.camera;
            savegame = new WorldSave(client.WorldRenderer, "Test");
            world = savegame.GetWorld();
            physics = new PhysicsWorld(world);
            savegame.GetPlayerData("default", out Vector3 pos, out Quaternion rot);
            camera.Position = pos;
            camera.Rotation = rot;

            spawnParticle = client.ParticleRenderer.AddParticle;
            clearParticles = client.ParticleRenderer.Clear;

            info = client.SelectionRenderer.Info;
        }

        private readonly Stopwatch fpstimer = Stopwatch.StartNew();
        private long fps;
        private int cfps = 1;
        public void OnGui(InputManager.InputState input, GuiRenderer gui)
        {
            if (fpstimer.ElapsedMilliseconds >= 130) {
                fps = (Stopwatch.Frequency / (fpstimer.ElapsedTicks / cfps));
                cfps = 0;
                fpstimer.Restart();
            }
            cfps++;
            
            if(input.MouseCaptured) {
                gui.Text(new Point(5,5), "Block: " + TextFormat.LightPink + inventory[slot].Name, size:30);
                gui.Text(new Point(gui.ScreenSize.Width/2, gui.ScreenSize.Height/2), "+", size:25, origin:Origin.Center);
                if (Settings.Instance.DebugInfo)
                    gui.Text(new Point(gui.ScreenSize.Width-200, 5), 
                        "FPS: " + fps + "\n" + 
                        "Pos: " + String.Format("[{0}|{1}|{2}]", (int)MathF.Round(camera.Position.X),(int)MathF.Round(camera.Position.Y),(int)MathF.Round(camera.Position.Z)) + "\n" +  
                        world.DebugText);
            } else {
                int w = gui.ScreenSize.Width  / 2 - 150;
                int h = gui.ScreenSize.Height / 2 - 200;
                if(gui.Button(new Rectangle(w - 180, h +   0, 300, 90), "Wireframe " + (Settings.Instance.Wireframe ? "on" : "off")))
                    Settings.Instance.ToggleWireframe();
                if(gui.Button(new Rectangle(w - 180, h + 100, 300, 90), "Ambient Occlusion " + (Settings.Instance.AmbientOcclusion ? "on" : "off")))
                    Settings.Instance.ToggleAmbientOcclusion();
                if(gui.Button(new Rectangle(w - 180, h + 200, 300, 90), "Fog " + (Settings.Instance.Fog ? "on" : "off")))
                    Settings.Instance.ToggleFog();
                if(gui.Button(new Rectangle(w - 180, h + 300, 300, 90), "Physics " + (Settings.Instance.Physics ? "on" : "off")))
                    Settings.Instance.TogglePhysics();
                
                if(gui.Button(new Rectangle(w + 180, h +  0, 300, 90), "Debug info " + (Settings.Instance.DebugInfo ? "on" : "off")))
                    Settings.Instance.ToggleDebugInfo();
                if(gui.Button(new Rectangle(w + 180, h + 100, 300, 90), "Camera updates " + (Settings.Instance.UpdateCamera ? "on" : "off")))
                    Settings.Instance.ToggleUpdateCamera();
                if(gui.Button(new Rectangle(w + 180, h + 200, 300, 90), "VSync " + (Settings.Instance.VSync ? "on" : "off")))
                    Settings.Instance.ToggleVSync();
                if(gui.Button(new Rectangle(w + 180, h + 300, 300, 90), "Fullscreen " + (Settings.Instance.Fullscreen ? "on" : "off")))
                    Settings.Instance.ToggleFullscreen();
            }
        }

        public void OnUpdate(InputManager.InputState input)
        {
            if (input.IsForeground)
            {
                if (input.IsKeyDown(VirtualKey.F1))
                    camera.Rotation = Quaternion.Identity;
                if (input.IsKeyDown(VirtualKey.ESCAPE))
                    input.MouseCaptured = !input.MouseCaptured;
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

            }

            slot = Math.Abs(input.MouseWheel / 120) % inventory.Length;
        }

        public void OnRenderUpdate(InputManager.InputState input)
        {
            if(Settings.Instance.UpdateCamera)
                world.Update(camera.Position);
        }

        public void OnDeactivate()
        {
            world.Dispose();
            savegame.SavePlayerData("default", camera.Position, camera.Rotation);
            savegame.Dispose();
        }
        
        
        
        
        private unsafe bool GetHitNormal(Vector3 pos, out Vector3 hitnormal) {
            hitnormal = Vector3.Zero;
            Vector3 forward = camera.Forward;
            Vector3 position = camera.Position;
            for(int i = 0; i < 6; i++) {
                if (!RayFaceIntersection(&forward.X, &position.X, &pos.X, i, out float d)) continue;
                if(User32Methods.GetKeyState(VirtualKey.T).IsPressed)
                    spawnParticle(position + forward * d, 0.04f, Color.Red);
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
                clearParticles();
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
                    spawnParticle(camera.Position + forward * f, 0.03f, Color.Aquamarine);
                if (world.GetBlock(camera.Position + forward * f) == Blocks.Air) continue;
                pos = camera.Position + forward * f;
                return true;
            }
            return false;
        }
        
        
        
    }

}
