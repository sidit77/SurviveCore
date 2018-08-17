using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using SharpDX.Direct3D11;
using SurviveCore.DirectX;
using SurviveCore.Gui;
using SurviveCore.Particles;
using SurviveCore.Physics;
using SurviveCore.World;
using SurviveCore.World.Rendering;
using SurviveCore.World.Saving;
using SurviveCore.World.Utils;
using WinApi.User32;
using WinApi.Windows;
using WinApi.Windows.Controls;

namespace SurviveCore {

    public class SurvivalGame : Window {

        private DirectXContext dx;
        private RasterizerState defaultrenderstate;
        private RasterizerState wireframerenderstate;
        
        private Camera camera;
        private WorldRenderer worldrenderer;
        private BlockWorld world;
        private PhysicsWorld physics;
        private WorldSave savegame;
        private GuiRenderer gui;
        private ParticleRenderer particlerenderer;
        private SelectionRenderer selectionrenderer;
        
        protected override void OnCreate(ref CreateWindowPacket packet) {
            base.OnCreate(ref packet);
            dx = new DirectXContext(Handle, GetClientSize());
            
            defaultrenderstate = new RasterizerState(dx.Device, new RasterizerStateDescription {
                CullMode = CullMode.Front,
                FillMode = FillMode.Solid
            });
            wireframerenderstate = new RasterizerState(dx.Device, new RasterizerStateDescription {
                CullMode = CullMode.None,
                FillMode = FillMode.Wireframe
            });
            
            worldrenderer = new WorldRenderer(dx.Device);
            particlerenderer = new ParticleRenderer(dx.Device);
            selectionrenderer = new SelectionRenderer(dx.Device);
            savegame = new WorldSave("./Assets/World.db");
            world = new BlockWorld(worldrenderer, savegame);
            physics = new PhysicsWorld(world);
            savegame.GetPlayerData("default", out Vector3 pos, out Quaternion rot);
            camera = new Camera(75f * (float)Math.PI / 180,  (float) GetClientSize().Width / GetClientSize().Height, 0.3f, 620.0f) {
                Position = pos,
                Rotation = rot
            };
            
            gui = new GuiRenderer(dx.Device);
            
            GC.Collect();
        }
        
        private readonly Block[] inventory = { Blocks.Bricks, Blocks.Stone, Blocks.Grass, Blocks.Dirt, Blocks.Sand, Blocks.Wood, Blocks.Leaves };
        private int slot;

        private float veloctiy;
        private Vector3 lastMovement;

        private bool blockFocused = false;
        private Vector3 focusedBlock;
        private Vector3 focusedBlockNormal;
        
        public void Update(InputManager.InputState input) {
            if (input.IsForeground) {
                if(input.IsKeyDown(VirtualKey.F1))
                    camera.Rotation = Quaternion.Identity;
                if(input.IsKeyDown(VirtualKey.ESCAPE))
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
                
                if(Settings.Instance.Physics) {
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
                }else {
                    if (input.IsKey(VirtualKey.SPACE))
                        movement += Vector3.UnitY;
                    if (input.IsKey(VirtualKey.SHIFT))
                        movement -= Vector3.UnitY;
                    movement *= input.IsKey(VirtualKey.CONTROL) ? 0.3f : 0.06f;
                    camera.Position += lastMovement = Vector3.Lerp(lastMovement, movement, 0.03f);
                }

                blockFocused = FindIntersection(out focusedBlock, out focusedBlockNormal);
                if (input.MouseCaptured) {
                    var mpos = input.DeltaMousePosition;
                    camera.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, (1f / 600) * mpos.X);

                    const float anglediff = 0.001f;
                    float angle = MathF.Acos(Vector3.Dot(camera.Forward, Vector3.UnitY));
                    camera.Rotation *= Quaternion.CreateFromAxisAngle(camera.Right, MathHelper.Clamp((1f / 600) * mpos.Y + angle, anglediff, MathF.PI - anglediff) - angle);
                    
                    if (input.IsKeyDown(VirtualKey.LBUTTON) || input.IsKeyDown(VirtualKey.Q)) {
                        if (blockFocused && world.SetBlock(focusedBlock, Blocks.Air)) {
                        }
                    }
                    if (input.IsKeyDown(VirtualKey.RBUTTON) || input.IsKeyDown(VirtualKey.E)) {
                        if (blockFocused 
                            && world.GetBlock(focusedBlock + focusedBlockNormal) == Blocks.Air 
                            && world.SetBlock(focusedBlock + focusedBlockNormal, inventory[slot]) 
                            && !physics.CanMoveTo(camera.Position)) {
                            world.SetBlock(focusedBlock + focusedBlockNormal, Blocks.Air);
                        }
                    }
                }
                
            }
               
            slot = Math.Abs(input.MouseWheel / 120) % inventory.Length;
            
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
        
        private bool FindIntersection(out Vector3 pos, out Vector3 normal) {
            if(User32Methods.GetKeyState(VirtualKey.T).IsPressed)
                particles.Clear();
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
        
        private readonly List<Particle> particles = new List<Particle>();
        
        private void spawnParticle(Vector3 pos, float radius, Color color) {
            particles.Add(new Particle(pos, radius, (uint)color.ToRgba())); 
        }
        
        private readonly Stopwatch fpstimer = Stopwatch.StartNew();
        private long fps;
        private int cfps = 1;
        public void Draw(InputManager.InputState input) {
            if (fpstimer.ElapsedMilliseconds >= 130) {
                fps = (Stopwatch.Frequency / (fpstimer.ElapsedTicks / cfps));
                cfps = 0;
                fpstimer.Restart();
            }
            cfps++;
            
            camera.Update(Settings.Instance.UpdateCamera);
            if(Settings.Instance.UpdateCamera)
                world.Update(camera.Position);

            dx.Clear(Color.DarkSlateGray);
            dx.Context.Rasterizer.State = Settings.Instance.Wireframe ? wireframerenderstate : defaultrenderstate;
            worldrenderer.Draw(dx.Context, camera);
            dx.Context.Rasterizer.State = defaultrenderstate;
            
            if(particles.Count > 0)
                particlerenderer.Render(dx.Context, particles.ToArray(), camera);

            if (blockFocused)
                selectionrenderer.Render(dx.Context, focusedBlock, focusedBlockNormal, camera);
            
            gui.Begin(input);
            if(input.MouseCaptured) {
                gui.Text(new Point(5,5), "Block: " + TextFormat.LightPink + inventory[slot].Name, size:30);
                gui.Text(new Point(GetClientSize().Width/2, GetClientSize().Height/2), "+", size:25, origin:Origin.Center);
                if (Settings.Instance.DebugInfo)
                    gui.Text(new Point(GetClientSize().Width-200, 5), 
                        "FPS: " + fps + "\n" + 
                        "Pos: " + String.Format("[{0}|{1}|{2}]", (int)MathF.Round(camera.Position.X),(int)MathF.Round(camera.Position.Y),(int)MathF.Round(camera.Position.Z)) + "\n" +  
                        world.DebugText);
            } else {
                int w = GetClientSize().Width  / 2 - 150;
                int h = GetClientSize().Height / 2 - 200;
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
            
            gui.Render(dx.Context, GetClientSize());
            dx.SwapChain.Present(Settings.Instance.VSync ? 1 : 0, 0);
            if(Settings.Instance.Fullscreen != dx.SwapChain.IsFullScreen)
                dx.SwapChain.IsFullScreen = Settings.Instance.Fullscreen;
        }

        protected override void OnSize(ref SizePacket packet) {
            base.OnSize(ref packet);
            dx.Resize(packet.Size);
            camera.Aspect = (float)packet.Size.Width / packet.Size.Height;
        }

        protected override void Dispose(bool d) {
            base.Dispose(d);
            dx.Dispose();
            defaultrenderstate.Dispose();
            wireframerenderstate.Dispose();
            particlerenderer.Dispose();
            world.Dispose();
            worldrenderer.Dispose();
            gui.Dispose();
            selectionrenderer.Dispose();
            savegame.SavePlayerData("default", camera.Position, camera.Rotation);
            savegame.Dispose();
        }

    }

}
