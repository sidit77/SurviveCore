using System;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Xml;
using SharpDX.Direct3D11;
using SurviveCore.DirectX;
using SurviveCore.Gui;
using SurviveCore.Gui.Text;
using SurviveCore.World;
using SurviveCore.World.Rendering;
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
        private GuiRenderer gui;
        
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
            
            
            camera = new Camera(75f * (float)Math.PI / 180,  (float) GetClientSize().Width / GetClientSize().Height, 0.1f, 300.0f) {
                Position = new Vector3(8, 50, 8)
            };
            
            worldrenderer = new WorldRenderer(dx.Device);
            world = new BlockWorld(worldrenderer);

            gui = new GuiRenderer(dx.Device);
            
            GC.Collect();
        }
        
        private readonly Block[] inventory = { Blocks.Bricks, Blocks.Stone, Blocks.Grass, Blocks.Dirt };
        private int slot;

        //private float velocity;

        public void Update(InputManager.InputState input) {
            if (input.IsForeground) {
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
                movement = movement.LengthSquared() > 0 ? Vector3.Normalize(movement) : Vector3.Zero;
                movement *= input.IsKey(VirtualKey.SHIFT) ? 0.3f : 0.06f;
                if(Settings.Instance.Physics) {
                    camera.Position = ClampToWorld(camera.Position, movement);
                }else {
                    camera.Position += movement;
                }
                if (input.MouseCaptured) {
                    var mpos = input.DeltaMousePosition;
                    camera.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)(mpos.X) / 600);
                    camera.Rotation *= Quaternion.CreateFromAxisAngle(camera.Right , (float)(mpos.Y) / 600);
                    if (input.IsKeyDown(VirtualKey.LBUTTON)) {
                        Vector3? intersection = FindIntersection(false);
                        if (intersection.HasValue && world.SetBlock(intersection.Value, Blocks.Air)) {
                        }
                    }
                    if (input.IsKeyDown(VirtualKey.RBUTTON)) {
                        Vector3? intersection = FindIntersection(true);
                        if (intersection.HasValue && world.SetBlock(intersection.Value, inventory[slot]) && !CanMoveTo(camera.Position)) {
                            world.SetBlock(intersection.Value, Blocks.Air);
                        }
                    }
                }
            }
               
            slot = Math.Abs(input.MouseWheel / 120) % inventory.Length;
            
            //if(Keyboard[Key.Left])  camera.Rotation *= Quaternion.CreateFromAxisAngle(camera.Up, -0.1f);
            //if(Keyboard[Key.Right]) camera.Rotation *= Quaternion.CreateFromAxisAngle(camera.Up, 0.1f);
            //if(Keyboard[Key.Up])    camera.Rotation *= Quaternion.CreateFromAxisAngle(camera.Left, 0.1f);
            //if(Keyboard[Key.Down])  camera.Rotation *= Quaternion.CreateFromAxisAngle(camera.Left, -0.1f);
            //if(Keyboard[Key.PageUp]) camera.Rotation *= Quaternion.CreateFromAxisAngle(camera.Forward, 0.1f);
            //if(Keyboard[Key.PageDown]) camera.Rotation *= Quaternion.CreateFromAxisAngle(camera.Forward, -0.1f);
//
            //slot = Math.Abs(Mouse.Wheel / 2 % inventory.Length);
//
            //Title = "Block: " + inventory[slot].Name;

            input.Update();
        }

        private bool IsAir(Vector3 pos) {
            return !world.GetBlock(pos).IsSolid();
        }


        private Vector3 ClampToWorld(Vector3 pos, Vector3 mov) {
            //bool x = IsAir(pos + new Vector3(mov.X + mov.X < 0 ? -0.4f : 0.4f,  0.4f, -0.4f)) &&
            //         IsAir(pos + new Vector3(mov.X + mov.X < 0 ? -0.4f : 0.4f,  0.4f,  0.4f)) &&
            //         IsAir(pos + new Vector3(mov.X + mov.X < 0 ? -0.4f : 0.4f,  0.0f, -0.4f)) &&
            //         IsAir(pos + new Vector3(mov.X + mov.X < 0 ? -0.4f : 0.4f,  0.0f,  0.4f)) &&
            //         IsAir(pos + new Vector3(mov.X + mov.X < 0 ? -0.4f : 0.4f, -1.5f, -0.4f)) &&
            //         IsAir(pos + new Vector3(mov.X + mov.X < 0 ? -0.4f : 0.4f, -1.5f,  0.4f));
            //bool y = IsAir(pos + new Vector3(-0.4f, mov.Y + mov.Y < 0 ? -1.5f : 0.4f, -0.4f)) &&
            //         IsAir(pos + new Vector3( 0.4f, mov.Y + mov.Y < 0 ? -1.5f : 0.4f, -0.4f)) &&
            //         IsAir(pos + new Vector3( 0.4f, mov.Y + mov.Y < 0 ? -1.5f : 0.4f,  0.4f)) &&
            //         IsAir(pos + new Vector3(-0.4f, mov.Y + mov.Y < 0 ? -1.5f : 0.4f,  0.4f));
            //bool z = IsAir(pos + new Vector3(-0.4f,  0.4f, mov.Z + mov.Z < 0 ? -0.4f : 0.4f)) &&
            //         IsAir(pos + new Vector3( 0.4f,  0.4f, mov.Z + mov.Z < 0 ? -0.4f : 0.4f)) &&
            //         IsAir(pos + new Vector3(-0.4f,  0.0f, mov.Z + mov.Z < 0 ? -0.4f : 0.4f)) &&
            //         IsAir(pos + new Vector3( 0.4f,  0.0f, mov.Z + mov.Z < 0 ? -0.4f : 0.4f)) &&
            //         IsAir(pos + new Vector3(-0.4f, -1.5f, mov.Z + mov.Z < 0 ? -0.4f : 0.4f)) &&
            //         IsAir(pos + new Vector3( 0.4f, -1.5f, mov.Z + mov.Z < 0 ? -0.4f : 0.4f));
            bool x = CanMoveTo(pos + new Vector3(mov.X, 0, 0));
            bool y = CanMoveTo(pos + new Vector3(0, mov.Y, 0));
            bool z = CanMoveTo(pos + new Vector3(0, 0, mov.Z));

            //if (!CanMoveTo(pos + new Vector3(x ? mov.X : 0, y ? mov.Y : 0, z ? mov.Z : 0)))
            //    Console.WriteLine("Error");
            return pos + new Vector3(x ? mov.X : 0, y ? mov.Y : 0, z ? mov.Z : 0); 
        }

        private bool CanMoveTo(Vector3 pos) {
            return IsAir(pos + new Vector3(-0.4f,  0.4f, -0.4f)) &&
                   IsAir(pos + new Vector3( 0.4f,  0.4f, -0.4f)) &&
                   IsAir(pos + new Vector3( 0.4f,  0.4f,  0.4f)) &&
                   IsAir(pos + new Vector3(-0.4f,  0.4f,  0.4f)) &&

                   IsAir(pos + new Vector3(-0.4f, -1.5f, -0.4f)) &&
                   IsAir(pos + new Vector3( 0.4f, -1.5f, -0.4f)) &&
                   IsAir(pos + new Vector3( 0.4f, -1.5f,  0.4f)) &&
                   IsAir(pos + new Vector3(-0.4f, -1.5f,  0.4f)) &&

                   IsAir(pos + new Vector3(-0.4f, -1.0f, -0.4f)) &&
                   IsAir(pos + new Vector3( 0.4f, -1.0f, -0.4f)) &&
                   IsAir(pos + new Vector3( 0.4f, -1.0f,  0.4f)) &&
                   IsAir(pos + new Vector3(-0.4f, -1.0f,  0.4f));
        }

        private Vector3? FindIntersection(bool pre) {
            Vector3 forward = camera.Forward;
            for(float f = 0; f < 7; f += 0.5f) {
                if(world.GetBlock(camera.Position + forward * f) != Blocks.Air) {
                    return camera.Position + forward * (f - (pre ? 0.5f : 0));
                }
            }
            return null;
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
                world.Update((int)Math.Floor(camera.Position.X) >> Chunk.BPC, (int)Math.Floor(camera.Position.Z) >> Chunk.BPC);

            dx.Clear(Color.DarkSlateGray);
            dx.Context.Rasterizer.State = Settings.Instance.Wireframe ? wireframerenderstate : defaultrenderstate;
            worldrenderer.Draw(dx.Context, camera);
            dx.Context.Rasterizer.State = defaultrenderstate;

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
                if(gui.Button(new Rectangle(w - 180, h +   0, 300, 90), "Wireframe " + (!Settings.Instance.Wireframe ? "on" : "off")))
                    Settings.Instance.ToggleWireframe();
                if(gui.Button(new Rectangle(w - 180, h + 100, 300, 90), "Ambient Occlusion " + (!Settings.Instance.AmbientOcclusion ? "on" : "off")))
                    Settings.Instance.ToggleAmbientOcclusion();
                if(gui.Button(new Rectangle(w - 180, h + 200, 300, 90), "Fog " + (!Settings.Instance.Fog ? "on" : "off")))
                    Settings.Instance.ToggleFog();
                if(gui.Button(new Rectangle(w - 180, h + 300, 300, 90), "Physics " + (!Settings.Instance.Physics ? "on" : "off")))
                    Settings.Instance.TogglePhysics();
                
                if(gui.Button(new Rectangle(w + 180, h +  0, 300, 90), "Debug info " + (!Settings.Instance.DebugInfo ? "on" : "off")))
                    Settings.Instance.ToggleDebugInfo();
                if(gui.Button(new Rectangle(w + 180, h + 100, 300, 90), "Camera updates " + (!Settings.Instance.UpdateCamera ? "on" : "off")))
                    Settings.Instance.ToggleUpdateCamera();
                if(gui.Button(new Rectangle(w + 180, h + 200, 300, 90), "VSync " + (!Settings.Instance.VSync ? "on" : "off")))
                    Settings.Instance.ToggleVSync();
                if(gui.Button(new Rectangle(w + 180, h + 300, 300, 90), "Fullscreen " + (!Settings.Instance.Fullscreen ? "on" : "off")))
                    Settings.Instance.ToggleFullscreen();
            }
            
            gui.Render(dx.Context, GetClientSize());
            dx.SwapChain.Present(Settings.Instance.VSync ? 1 : 0, 0);
            if(Settings.Instance.Fullscreen != dx.SwapChain.IsFullScreen)
                dx.SwapChain.IsFullScreen = Settings.Instance.Fullscreen;
        }

        protected override void Dispose(bool d) {
            base.Dispose(d);
            dx.Dispose();
            defaultrenderstate.Dispose();
            wireframerenderstate.Dispose();
            world.Dispose();
            worldrenderer.Dispose();
            gui.Dispose();
        }

    }

}
