using System;
using System.Diagnostics;
using System.Drawing;
using SharpDX.Direct3D11;
using SurviveCore.DirectX;
using SurviveCore.Gui;
using WinApi.Windows;
using WinApi.Windows.Controls;
using Size = NetCoreEx.Geometry.Size;

namespace SurviveCore
{
    public class Client : Window
    {

        public RasterizerState DefaultRenderState { get; private set; }
        public RasterizerState WireframeRenderState { get; private set;}
        
        public event Action OnDispose; 
        public DirectXContext Dx { get; private set; }

        public GuiRenderer GuiRenderer { get; private set; }
        
        public Color ClearColor { get; set; }
        public long Fps { get; private set; }

        public Size ScreenSize { get; private set; }
        
        public InputManager.InputState Input { get; private set; }
        
        private GuiScene scene;

        public GuiScene CurrentScene
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
            Dx = new DirectXContext(Handle, GetClientSize());
            
            DefaultRenderState = new RasterizerState(Dx.Device, new RasterizerStateDescription {
                CullMode = CullMode.Front,
                FillMode = FillMode.Solid
            });
            WireframeRenderState = new RasterizerState(Dx.Device, new RasterizerStateDescription {
                CullMode = CullMode.None,
                FillMode = FillMode.Wireframe
            });
            
            GuiRenderer = new GuiRenderer(Dx.Device);

            GC.Collect();
        }

        private readonly Stopwatch fpstimer = Stopwatch.StartNew();
        private int cfps = 1;
        public void Draw(InputManager.InputState input)
        {
            Input = input;
            ScreenSize = GetClientSize();
            if (fpstimer.ElapsedMilliseconds >= 130) {
                Fps = (Stopwatch.Frequency / (fpstimer.ElapsedTicks / cfps));
                cfps = 0;
                fpstimer.Restart();
            }
            cfps++;
            
            Dx.Clear(ClearColor);
            
            CurrentScene?.OnRenderUpdate(input);
            
            Dx.Context.Rasterizer.State = DefaultRenderState;
            GuiRenderer.Begin(input);
            CurrentScene?.OnGui(input, GuiRenderer);
            GuiRenderer.Render(Dx.Context, ScreenSize);
            Dx.SwapChain.Present(Settings.Instance.VSync ? 1 : 0, 0);
            if(Settings.Instance.Fullscreen != Dx.SwapChain.IsFullScreen)
                Dx.SwapChain.IsFullScreen = Settings.Instance.Fullscreen;
        }

        public void Update(InputManager.InputState input)
        {
            Input = input;
            CurrentScene?.OnUpdate(input);
        }

        protected override void OnSize(ref SizePacket packet) {
            base.OnSize(ref packet);
            Dx.Resize(packet.Size);
        }
        
        protected override void Dispose(bool d) {
            base.Dispose(d);
            OnDispose?.Invoke();
            DefaultRenderState.Dispose();
            WireframeRenderState.Dispose();
            GuiRenderer.Dispose();
            CurrentScene = null;
            Dx.Dispose();
        }
        
    }

    public abstract class GuiScene
    {
        protected Client client;
        public virtual void OnActivate(Client c)
        {
            client = c;
        }
        public abstract void OnGui(InputManager.InputState input, GuiRenderer gui);
        public virtual void OnUpdate(InputManager.InputState input)
        {
            
        }
        public virtual void OnRenderUpdate(InputManager.InputState input)
        {
            
        }
        public virtual void OnDeactivate()
        {
            
        }
    }
    
}