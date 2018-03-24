using System;
using System.Drawing;
using System.Numerics;
using SharpDX.Direct3D11;
using SurviveCore.DirectX;
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
        private Font font;
        private TextRenderer textrenderer;
        
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

            font = new Font(dx.Device, "./Assets/Fonts/Abel.fnt");
            textrenderer = new TextRenderer(dx.Device);
            textrenderer.Screen = Matrix4x4.CreateOrthographicOffCenter(0,GetClientSize().Width, GetClientSize().Height, 0, -1, 1);
            
            GC.Collect();
        }
        
        private readonly Block[] inventory = { Blocks.Bricks, Blocks.Stone, Blocks.Grass, Blocks.Dirt };
        private int slot;
        private bool captured = false;

        //private float velocity;

        public void Update() {
            if (User32Methods.GetForegroundWindow() == Handle) {
                Vector3 movement = Vector3.Zero;
                if (User32Methods.GetKeyState(VirtualKey.W).IsPressed)
                    movement += camera.Forward;
                if (User32Methods.GetKeyState(VirtualKey.S).IsPressed)
                    movement += camera.Back;
                if (User32Methods.GetKeyState(VirtualKey.A).IsPressed)
                    movement += camera.Left;
                if (User32Methods.GetKeyState(VirtualKey.D).IsPressed)
                    movement += camera.Right;
                movement = movement.LengthSquared() > 0 ? Vector3.Normalize(movement) : Vector3.Zero;
                movement *= User32Methods.GetKeyState(VirtualKey.SHIFT).IsPressed ? 0.5f : 0.06f;
                if(Settings.Instance.Physics) {
                    camera.Position = ClampToWorld(camera.Position, movement);
                }else {
                    camera.Position += movement;
                }

                if (captured) {
                    NetCoreEx.Geometry.Rectangle r = GetWindowRect();
                    User32Methods.GetCursorPos(out var p1);
                    User32Methods.SetCursorPos((r.Left + r.Right) / 2, (r.Top + r.Bottom) / 2);
                    User32Methods.GetCursorPos(out var p2);

                    camera.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)(p1.X - p2.X) / 600);
                    camera.Rotation *= Quaternion.CreateFromAxisAngle(camera.Right , (float)(p1.Y - p2.Y) / 600);
                }
            }
            
//
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

        public void Draw() {
            camera.Update(Settings.Instance.UpdateCamera);
            if(Settings.Instance.UpdateCamera)
                world.Update((int)Math.Floor(camera.Position.X) >> Chunk.BPC, (int)Math.Floor(camera.Position.Z) >> Chunk.BPC);

            dx.Clear(Color.DarkSlateGray);
            dx.Context.Rasterizer.State = Settings.Instance.Wireframe ? wireframerenderstate : defaultrenderstate;
            worldrenderer.Draw(dx.Context, camera);
            dx.Context.Rasterizer.State = defaultrenderstate;
            
            textrenderer.DrawText(dx.Context, font, "Block: " + inventory[slot].Name, 0.25f);//"The quick brown fox jumps over the lazy dog. 123456789"

            dx.SwapChain.Present(1, 0);
        }

        protected override void OnKey(ref KeyPacket packet) {
            base.OnKey(ref packet);
            if(!packet.IsKeyDown || packet.InputState.IsPreviousKeyStatePressed)
                return;
            switch (packet.Key) {
                case VirtualKey.ESCAPE:
                    if(captured)
                        User32Methods.ShowCursor(!(captured = false));
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
                    //WindowState = WindowState == WindowState.Fullscreen ? WindowState.Normal : WindowState.Fullscreen;
                    //Console.WriteLine(WindowState);
                    break;
                case VirtualKey.F12:
                    //VSync = VSync == VSyncMode.Adaptive ? VSyncMode.Off : VSyncMode.Adaptive;
                    //Console.WriteLine(VSync);
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
            if (packet.Button == MouseButton.Left && !captured)
                User32Methods.ShowCursor(!(captured = true));
            if (packet.Button == MouseButton.Left && captured) {
                Vector3? intersection = FindIntersection(false);
                if (intersection.HasValue && world.SetBlock(intersection.Value, Blocks.Air)) {
                }
            }
            if (packet.Button == MouseButton.Right && captured) {
                Vector3? intersection = FindIntersection(true);
                if (intersection.HasValue && world.SetBlock(intersection.Value, inventory[slot]) && !CanMoveTo(camera.Position)) {
                    world.SetBlock(intersection.Value, Blocks.Air);
                }
            }
        }

        protected override void OnMouseWheel(ref MouseWheelPacket packet) {
            base.OnMouseWheel(ref packet);
            slot = (slot + (packet.WheelDelta / 120) + inventory.Length) % inventory.Length;
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
