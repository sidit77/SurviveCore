using System;
using System.Drawing;
using SharpDX.Direct3D11;
using SurviveCore.DirectX;
using SurviveCore.Gui;
using SurviveCore.Particles;
using SurviveCore.World.Rendering;
using WinApi.Windows;
using WinApi.Windows.Controls;

namespace SurviveCore
{
    public class Client : Window
    {
        private DirectXContext dx;
        private RasterizerState defaultrenderstate;
        private RasterizerState wireframerenderstate;
        
        public GuiRenderer GuiRenderer { get; private set; }
        public WorldRenderer WorldRenderer { get; private set; }
        public ParticleRenderer ParticleRenderer { get; private set; }
        public SelectionRenderer SelectionRenderer  { get; private set; }

        public Camera camera { get; set; }

        private IScene scene;

        public IScene CurrentScene
        {
            get => scene;
            set
            {
                scene?.OnDeactivate();
                scene = value;
                scene?.OnActivate(this);
            }
        }

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
            
            WorldRenderer = new WorldRenderer(dx.Device);
            ParticleRenderer = new ParticleRenderer(dx.Device);
            SelectionRenderer = new SelectionRenderer(dx.Device);
            GuiRenderer = new GuiRenderer(dx.Device);

            camera = new Camera(75f * (float) Math.PI / 180, (float) GetClientSize().Width / GetClientSize().Height,
                0.3f, 620.0f);
            GC.Collect();
        }

        public void Draw(InputManager.InputState input)
        {
            camera.Update(Settings.Instance.UpdateCamera);
            CurrentScene?.OnRenderUpdate(input);
            
            dx.Clear(Color.DarkSlateGray);
            dx.Context.Rasterizer.State = Settings.Instance.Wireframe ? wireframerenderstate : defaultrenderstate;
            WorldRenderer.Draw(dx.Context, camera);
            dx.Context.Rasterizer.State = defaultrenderstate;
            
            ParticleRenderer.Render(dx.Context, camera);
            SelectionRenderer.Render(dx.Context, camera);
            
            GuiRenderer.Begin(input, GetClientSize());
            CurrentScene?.OnGui(input, GuiRenderer);
            GuiRenderer.Render(dx.Context);
            dx.SwapChain.Present(Settings.Instance.VSync ? 1 : 0, 0);
            if(Settings.Instance.Fullscreen != dx.SwapChain.IsFullScreen)
                dx.SwapChain.IsFullScreen = Settings.Instance.Fullscreen;
        }

        public void Update(InputManager.InputState input)
        {
            CurrentScene?.OnUpdate(input);
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
            ParticleRenderer.Dispose();
            WorldRenderer.Dispose();
            GuiRenderer.Dispose();
            SelectionRenderer.Dispose();
            CurrentScene = null;
        }
        
    }

    public interface IScene
    {
        void OnActivate(Client client);
        void OnGui(InputManager.InputState input, GuiRenderer gui);
        void OnUpdate(InputManager.InputState input);
        
        void OnRenderUpdate(InputManager.InputState input);

        void OnDeactivate();
    }
    
}