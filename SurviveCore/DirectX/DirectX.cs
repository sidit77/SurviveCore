using System;
using System.Drawing;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Device = SharpDX.Direct3D11.Device;
using Size = NetCoreEx.Geometry.Size;

namespace SurviveCore.DirectX {
    
    public class DirectXContext : IDisposable {

        private const int bufferCount = 1;

        private SwapChain swapChain;
        private Device device;
        private DeviceContext context;
        private RenderTargetView renderTargetView;
        private DepthStencilView depthStencilView;


        public SwapChain SwapChain => swapChain;
        public Device Device => device;
        public DeviceContext Context => context;


        public void Clear(Color c) => Clear(c.Raw());
        public void Clear(RawColor4 c) {
            context.ClearRenderTargetView(renderTargetView, c);
            context.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
        }

        public DirectXContext(IntPtr handle, Size size) {
            var modeDescription = new ModeDescription {
                Width = size.Width,
                Height = size.Height,
                RefreshRate = new Rational(60, 1),
                Format = Format.R8G8B8A8_UNorm,
                ScanlineOrdering = DisplayModeScanlineOrder.Unspecified,
                Scaling = DisplayModeScaling.Unspecified
            };

            var swapChainDescription = new SwapChainDescription {
                BufferCount = bufferCount,
                ModeDescription = modeDescription,
                IsWindowed = true,
                OutputHandle = handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, swapChainDescription, out device, out swapChain);
            context = device.ImmediateContext;

            var factory = swapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(handle, WindowAssociationFlags.IgnoreAll);

            Resize(size);
        }

        public void Resize(Size size) {
            
            Utilities.Dispose(ref renderTargetView);
            Utilities.Dispose(ref depthStencilView);
            
            swapChain.ResizeBuffers(bufferCount, size.Width, size.Height, Format.Unknown, SwapChainFlags.None);
            
            Texture2D backBuffer = SharpDX.Direct3D11.Resource.FromSwapChain<Texture2D>(swapChain, 0);
            renderTargetView = new RenderTargetView(device, backBuffer);
            Utilities.Dispose(ref backBuffer);

            Texture2D depthBuffer = new Texture2D(device, new Texture2DDescription() {
                Format = Format.D32_Float_S8X24_UInt,
                ArraySize = 1,
                MipLevels = 1,
                Width = size.Width,
                Height = size.Height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });
            depthStencilView = new DepthStencilView(device, depthBuffer);
            Utilities.Dispose(ref depthBuffer);

            context.Rasterizer.SetViewport(0, 0, size.Width, size.Height, 0.0f, 1.0f);
            context.OutputMerger.SetTargets(depthStencilView, renderTargetView);
        }

        public void Dispose() {
            Utilities.Dispose(ref renderTargetView);
            Utilities.Dispose(ref depthStencilView);
            Utilities.Dispose(ref swapChain);
            Utilities.Dispose(ref device);
            Utilities.Dispose(ref context);
        }
    }
}