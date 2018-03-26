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

        private InputManager input;
        private DirectXContext dx;
        private RasterizerState defaultrenderstate;
        private RasterizerState wireframerenderstate;
        
        private Camera camera;
        private WorldRenderer worldrenderer;
        private BlockWorld world;
        private Font font;
        private TextRenderer textrenderer;
        private GuiRenderer gui;
        
        protected override void OnCreate(ref CreateWindowPacket packet) {
            base.OnCreate(ref packet);
            input = new InputManager(this);
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

            font = new Font(dx.Device, "./Assets/Fonts/Abel.fnt");
            textrenderer = new TextRenderer(dx.Device);
            textrenderer.Screen = Matrix4x4.CreateOrthographicOffCenter(0,GetClientSize().Width, GetClientSize().Height, 0, -1, 1);
            gui = new GuiRenderer(dx.Device, input);
            
            GC.Collect();
        }
        
        private readonly Block[] inventory = { Blocks.Bricks, Blocks.Stone, Blocks.Grass, Blocks.Dirt };
        private int slot;
        private bool vsync = true;

        //private float velocity;

        public void Update() {
            if (input.Default.IsForeground) {
                Vector3 movement = Vector3.Zero;
                if (input.Default.IsKey(VirtualKey.W))
                    movement += camera.Forward;
                if (input.Default.IsKey(VirtualKey.S))
                    movement += camera.Back;
                if (input.Default.IsKey(VirtualKey.A))
                    movement += camera.Left;
                if (input.Default.IsKey(VirtualKey.D))
                    movement += camera.Right;
                movement = movement.LengthSquared() > 0 ? Vector3.Normalize(movement) : Vector3.Zero;
                movement *= input.Default.IsKey(VirtualKey.SHIFT) ? 0.3f : 0.06f;
                if(Settings.Instance.Physics) {
                    camera.Position = ClampToWorld(camera.Position, movement);
                }else {
                    camera.Position += movement;
                }
                if (input.Default.MouseCaptured) {
                    var mpos = input.Default.DeltaMousePosition;
                    camera.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)(mpos.X) / 600);
                    camera.Rotation *= Quaternion.CreateFromAxisAngle(camera.Right , (float)(mpos.Y) / 600);
                }
            }
               
            slot = Math.Abs(input.Default.MouseWheel / 120) % inventory.Length;
            
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
        public void Draw() {
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
            
            textrenderer.DrawText(dx.Context, new Vector2(5,5), font, "Block: " + inventory[slot].Name, Color.White, 25);//"The quick brown fox jumps over the lazy dog. 123456789"
            textrenderer.DrawTextCentered(dx.Context, new Vector2(GetClientSize().Width/2, GetClientSize().Height/2), font, "+", Color.White, 25);

            if(gui.Button(new Rectangle(50, 50, 400, 400), "Hallo")) {
                Console.WriteLine("Hallo");
            }
            
            if (Settings.Instance.DebugInfo) {
                Text text = new Text(font, "FPS: " + fps + "\n" + world.DebugText);
                textrenderer.DrawText(dx.Context, new Vector2(GetClientSize().Width-Math.Max(text.Size.Width - 6, 200), 5), text);
            }
            gui.Render(dx.Context);
            dx.SwapChain.Present(vsync ? 1 : 0, 0);
        }

        protected override void OnKey(ref KeyPacket packet) {
            base.OnKey(ref packet);
            if(!packet.IsKeyDown || packet.InputState.IsPreviousKeyStatePressed)
                return;
            switch (packet.Key) {
                case VirtualKey.ESCAPE:
                    //if(input.Default.MouseCaptured)
                        input.Default.MouseCaptured = !input.Default.MouseCaptured;
                    break;
                case VirtualKey.F1:
                    Settings.Instance.ToggleUpdateCamera();
                    break;
                case VirtualKey.F2:
                    Settings.Instance.ToggleWireframe();
                    break;
                case VirtualKey.F3:
                    Settings.Instance.ToggleAmbientOcclusion();
                    break;
                case VirtualKey.F4:
                    Settings.Instance.ToggleFog();
                    break;
                case VirtualKey.F5:
                    Settings.Instance.TogglePhysics();
                    break;
                case VirtualKey.F6:
                    Settings.Instance.ToggleDebugInfo();
                    break;
                case VirtualKey.F11:
                    dx.SwapChain.IsFullScreen = !dx.SwapChain.IsFullScreen;
                    break;
                case VirtualKey.F12:
                    vsync = !vsync;
                    break;
                case VirtualKey.SPACE:
                    Console.WriteLine(ChunkLocation.FromPos(camera.Position));
                    break;    
            }
        }

        protected override void OnMouseButton(ref MouseButtonPacket packet) {
            base.OnMouseButton(ref packet);
            if(!packet.IsButtonDown)
                return;
            //if(packet.Button == MouseButton.Left && !input.Default.MouseCaptured)
            //    input.Default.MouseCaptured = true;
            if (packet.Button == MouseButton.Left && input.Default.MouseCaptured) {
                Vector3? intersection = FindIntersection(false);
                if (intersection.HasValue && world.SetBlock(intersection.Value, Blocks.Air)) {
                }
            }
            if (packet.Button == MouseButton.Right && input.Default.MouseCaptured) {
                Vector3? intersection = FindIntersection(true);
                if (intersection.HasValue && world.SetBlock(intersection.Value, inventory[slot]) && !CanMoveTo(camera.Position)) {
                    world.SetBlock(intersection.Value, Blocks.Air);
                }
            }
        }

        protected override void OnMouseWheel(ref MouseWheelPacket packet) {
            base.OnMouseWheel(ref packet);
            input.MouseWheelEvent = packet.WheelDelta;
        }

        protected override void OnSize(ref SizePacket packet) {
            base.OnSize(ref packet);
            dx.Resize(packet.Size);
            camera.Aspect = (float) packet.Size.Width / packet.Size.Height;
            textrenderer.Screen = Matrix4x4.CreateOrthographicOffCenter(0,GetClientSize().Width, GetClientSize().Height, 0, -1, 1);
        }
        
        protected override void Dispose(bool d) {
            base.Dispose(d);
            dx.Dispose();
            defaultrenderstate.Dispose();
            world.Dispose();
            worldrenderer.Dispose();
            font.Dispose();
            textrenderer.Dispose();
        }

    }

}
