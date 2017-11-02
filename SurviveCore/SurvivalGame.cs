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
        private Texture ao_texture;
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

            ao_texture = AmbientOcclusion.GetAOTexture4();//Texture.FromFile("./Assets/Textures/ao.png");
            ao_texture.SetWarpMode(TextureWrapMode.MirroredRepeat);

            Console.WriteLine(GL.GetError());

            camera = new Camera(75f * (float)System.Math.PI / 180, (float)Width / (float)Height, 0.1f, 200.0f);
            frustum = new Frustum(camera.CameraMatrix);
            
            world = new BlockWorld();

            Resize += (object sender, EventArgs ea) => {
                GL.Viewport(0, 0, Width, Height);
                camera.Aspect = (float)Width / (float)Height;
            };
            KeyDown += (object sender, KeyboardKeyEventArgs ea) => {
                if(!ea.IsRepeat && ea.Key == Key.F12) {
                    VSync = VSync == OpenTK.VSyncMode.Adaptive ? OpenTK.VSyncMode.Off : OpenTK.VSyncMode.Adaptive;
                    Console.WriteLine(VSync);
                }
                if (!ea.IsRepeat && ea.Key == Key.F11)
                {
                    WindowState = WindowState == OpenTK.WindowState.Fullscreen ? OpenTK.WindowState.Normal : OpenTK.WindowState.Fullscreen;
                    Console.WriteLine(WindowState);
                }
                if (!ea.IsRepeat && ea.Key == Key.Space) {
                    velocity += 1;
                }
            };
            
            GC.Collect();
            base.OnLoad(e);
        }

        private double cooldown = 0;
        private Block[] inventory = new Block[] { Blocks.Bricks, Blocks.Stone, Blocks.Grass };
        private MouseState oldms;
        private int slot = 0;

        private float velocity;

        protected override void OnUpdateFrame(OpenTK.FrameEventArgs e) {

            if(Mouse[MouseButton.Left] && CursorVisible == true) {
                CursorVisible = false;
            }
            if(Keyboard[Key.Escape] && CursorVisible == false) {
                CursorVisible = true;
            }
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

            if(Keyboard[Key.Left]) camera.Rotation *= Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), -0.1f);
            if(Keyboard[Key.Right]) camera.Rotation *= Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), 0.1f);
            if(Keyboard[Key.Up]) camera.Rotation *= Quaternion.CreateFromAxisAngle(camera.Left, 0.1f);
            if(Keyboard[Key.Down]) camera.Rotation *= Quaternion.CreateFromAxisAngle(camera.Left, -0.1f);

            if(Keyboard[Key.W]) camera.Position += camera.Forward * (Keyboard[Key.ShiftLeft] ? 60 : 10) * (float)e.Time;
            if(Keyboard[Key.S]) camera.Position += camera.Back * (Keyboard[Key.ShiftLeft] ? 60 : 10) * (float)e.Time;
            if(Keyboard[Key.A]) camera.Position += camera.Left * (Keyboard[Key.ShiftLeft] ? 60 : 10) * (float)e.Time;
            if(Keyboard[Key.D]) camera.Position += camera.Right * (Keyboard[Key.ShiftLeft] ? 60 : 10) * (float)e.Time;

            
            if(Mouse[MouseButton.Left] && !Keyboard[Key.LControl] && cooldown > 0.2) {
                Vector3? intersection = FindIntersection(false);
                if(intersection.HasValue && world.SetBlock(intersection.Value, Blocks.Air)) {
                    cooldown = 0;
                }
            }
            if((Mouse[MouseButton.Right] || (Mouse[MouseButton.Left] && Keyboard[Key.LControl])) && cooldown > 0.2) {
                Vector3? intersection = FindIntersection(true);
                if(intersection.HasValue && world.SetBlock(intersection.Value, inventory[slot])) { 
                    cooldown = 0;
                } 
            }
            cooldown += e.Time;

            slot = Math.Abs(Mouse.Wheel / 2 % inventory.Length);

            Title = (ChunkRenderer.time / Math.Max(1, ChunkRenderer.number)) + " - Block: " + inventory[slot].Name;

            if (Keyboard[Key.G]) {
                velocity += -4f * (float)e.Time;
                velocity += (velocity * velocity) / 150 * (float)e.Time * -Math.Sign(velocity);

                Vector3 force = new Vector3(0, velocity, 0);
                float num_steps = (float)Math.Round(force.Length()) + 1;
                Vector3 step = force / num_steps;

                while (num_steps > 0) {
                    if (world.GetBlock(camera.Position + new Vector3(0, -2, 0) + step) == Blocks.Air) {
                        camera.Position += step;
                        num_steps--;
                    } else {

                        velocity = 0;
                        break;
                    }
                }
                    
                
            } else {
                velocity = 0;
            }

            base.OnUpdateFrame(e);
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
            ao_texture.Bind(TextureUnit.Texture1);
            program.Bind();
            program.SetUniform("mvp", false, ref camera.CameraMatrix);
            program.SetUniform("pos", camera.Position);
            program.SetUniform("ao", Keyboard[Key.F3] ? 0 : 1);
            world.Draw(frustum);

            hudprogram.Bind();
            GL.DrawArrays(PrimitiveType.Points, 0, 1);

            base.OnRenderFrame(e);
            this.SwapBuffers();
        }

        protected override void OnUnload(EventArgs e) {
            base.OnUnload(e);
            program.Dispose();
            hudprogram.Dispose();
            world.Dispose();
            ao_texture.Dispose();
            texture.Dispose();
        }

    }

}
