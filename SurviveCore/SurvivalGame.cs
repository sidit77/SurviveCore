using System;
using System.Numerics;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using SurviveCore.OpenGL;
using SurviveCore.OpenGL.Helper;
using SurviveCore.World;
using SurviveCore.World.Rendering;

namespace SurviveCore {

    class SurvivalGame : OpenTK.GameWindow{

        public SurvivalGame() : base(1280, 720, GraphicsMode.Default, "Test Game", OpenTK.GameWindowFlags.Default , OpenTK.DisplayDevice.Default, 4, 5, GraphicsContextFlags.ForwardCompatible) {
            
        }

        private ShaderProgram program;
        private ShaderProgram hudprogram;
        private Texture texture;
        private Texture aoTexture;
        private Camera camera;
        private Frustum frustum;
        private BlockWorld world;

        protected override void OnLoad(EventArgs e) {
            
            GL.ClearColor(Color4.DarkSlateGray);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.VertexProgramPointSize);

            program = new ShaderProgram()
                .AttachShader(Shader.FromFile("./Assets/Shader/Fragment.glsl", ShaderType.FragmentShader))
                .AttachShader(Shader.FromFile("./Assets/Shader/Vertex.glsl", ShaderType.VertexShader))
                .Link();

            hudprogram = new ShaderProgram()
                .AttachShader(Shader.FromFile("./Assets/Shader/HudFragment.glsl", ShaderType.FragmentShader))
                .AttachShader(Shader.FromFile("./Assets/Shader/HudVertex.glsl", ShaderType.VertexShader))
                .Link();

            texture = Texture.FromFiles(256, Block.Textures);
            texture.SetFiltering(TextureMinFilter.LinearMipmapLinear, TextureMagFilter.Linear);
            texture.SetLODBias(-0.7f);

            aoTexture = AmbientOcclusion.GetAOTexture4();//Texture.FromFile("./Assets/Textures/ao.png");
            aoTexture.SetWarpMode(TextureWrapMode.MirroredRepeat);

            Console.WriteLine(GL.GetError());

            camera = new Camera(75f * (float)Math.PI / 180, (float)Width / (float)Height, 0.1f, 250.0f) {
                Position = new Vector3(8, 50, 8)
            };
            frustum = new Frustum(camera.CameraMatrix);
            
            world = new BlockWorld();

            Resize += (sender, ea) => {
                GL.Viewport(0, 0, Width, Height);
                camera.Aspect = (float)Width / (float)Height;
            };
            KeyDown += (sender, ea) => {
                if(!ea.IsRepeat && ea.Key == Key.F12) {
                    VSync = VSync == OpenTK.VSyncMode.Adaptive ? OpenTK.VSyncMode.Off : OpenTK.VSyncMode.Adaptive;
                    Console.WriteLine(VSync);
                }
                if (!ea.IsRepeat && ea.Key == Key.F11)
                {
                    WindowState = WindowState == OpenTK.WindowState.Fullscreen ? OpenTK.WindowState.Normal : OpenTK.WindowState.Fullscreen;
                    Console.WriteLine(WindowState);
                }
                if (!ea.IsRepeat && ea.Key == Key.Escape && CursorVisible == false) {
                    CursorVisible = true;
                }
                if (!ea.IsRepeat && ea.Key == Key.Space) {
                    //velocity += 1;
                    Console.WriteLine(ChunkLocation.FromPos(camera.Position));
                }
            };
            MouseDown += (sender, ea) => {
                if (ea.Button == MouseButton.Left && !CursorVisible) {
                    Vector3? intersection = FindIntersection(false);
                    if (intersection.HasValue && world.SetBlock(intersection.Value, Blocks.Air)) {
                    }
                }
                if (ea.Button == MouseButton.Right && !CursorVisible) {
                    Vector3? intersection = FindIntersection(true);
                    if (intersection.HasValue && world.SetBlock(intersection.Value, inventory[slot]) && !CanMoveTo(camera.Position)) {
                        world.SetBlock(intersection.Value, Blocks.Air);
                    }
                }
                if (ea.Button == MouseButton.Left &&  CursorVisible) {
                    CursorVisible = false;
                }
            };
            GC.Collect();
            base.OnLoad(e);
        }
        
        private readonly Block[] inventory = { Blocks.Bricks, Blocks.Stone, Blocks.Grass };
        private MouseState oldms;
        private int slot;

        //private float velocity;

        protected override void OnUpdateFrame(OpenTK.FrameEventArgs e) {
            Vector2 mousepos = Vector2.Zero;
            if(CursorVisible == false) {
                MouseState ms = OpenTK.Input.Mouse.GetState();
                mousepos.X = ms.X - oldms.X;
                mousepos.Y = ms.Y - oldms.Y;
                mousepos /= 600;
                oldms = ms;
            }
            camera.Rotation *= Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), mousepos.X);
            camera.Rotation *= Quaternion.CreateFromAxisAngle(camera.Right, mousepos.Y);

            if(Keyboard[Key.Left])  camera.Rotation *= Quaternion.CreateFromAxisAngle(camera.Up, -0.1f);
            if(Keyboard[Key.Right]) camera.Rotation *= Quaternion.CreateFromAxisAngle(camera.Up, 0.1f);
            if(Keyboard[Key.Up])    camera.Rotation *= Quaternion.CreateFromAxisAngle(camera.Left, 0.1f);
            if(Keyboard[Key.Down])  camera.Rotation *= Quaternion.CreateFromAxisAngle(camera.Left, -0.1f);
            if(Keyboard[Key.PageUp]) camera.Rotation *= Quaternion.CreateFromAxisAngle(camera.Forward, 0.1f);
            if(Keyboard[Key.PageDown]) camera.Rotation *= Quaternion.CreateFromAxisAngle(camera.Forward, -0.1f);

            Vector3 movement = Vector3.Zero;
            if(Keyboard[Key.W]) movement += camera.Forward;
            if(Keyboard[Key.S]) movement += camera.Back;  
            if(Keyboard[Key.A]) movement += camera.Left;   
            if(Keyboard[Key.D]) movement += camera.Right;
            movement *= (Keyboard[Key.ShiftLeft] ? 60 : 10) * (float)e.Time;

            if(!Keyboard[Key.AltLeft]) {
                camera.Position = ClampToWorld(camera.Position, movement);
            }else {
                camera.Position += movement;
            }
            
            slot = Math.Abs(Mouse.Wheel / 2 % inventory.Length);

            Title = "Block: " + inventory[slot].Name;
            
            world.Update((int)Math.Floor(camera.Position.X) >> Chunk.BPC, (int)Math.Floor(camera.Position.Z) >> Chunk.BPC);

            base.OnUpdateFrame(e);
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

        protected override void OnRenderFrame(OpenTK.FrameEventArgs e) {
            camera.Update();
            if(!Keyboard[Key.F1])
                frustum.Update(camera.CameraMatrix);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if(Keyboard[Key.F2]) {
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                GL.Disable(EnableCap.CullFace);
            }else{
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                GL.Enable(EnableCap.CullFace);
            }


            texture.Bind(TextureUnit.Texture0);
            aoTexture.Bind(TextureUnit.Texture1);
            program.Bind();
            program.SetUniform("mvp", false, ref camera.CameraMatrix);
            program.SetUniform("pos", camera.Position);
            program.SetUniform("ao", Keyboard[Key.F3] ? 0 : 1);
            world.Draw(frustum);

            hudprogram.Bind();
            GL.DrawArrays(PrimitiveType.Points, 0, 1);

            base.OnRenderFrame(e);
            SwapBuffers();
        }

        protected override void OnUnload(EventArgs e) {
            base.OnUnload(e);
            program.Dispose();
            hudprogram.Dispose();
            world.Dispose();
            aoTexture.Dispose();
            texture.Dispose();
        }

    }

}
