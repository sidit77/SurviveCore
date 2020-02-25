using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SurviveCore.DirectX;
using Device = SharpDX.Direct3D11.Device;

namespace SurviveCore {
    public class SelectionRenderer {
        private readonly VertexShader vs;
        private readonly PixelShader ps;

        private readonly Buffer instancebuffer;
        private readonly Buffer constantbuffer;
        private readonly VertexBufferBinding vertexBufferBinding;
        private readonly InputLayout layout;
        private readonly BlendState blendstate;
        private readonly DepthStencilState depthstate;
        private readonly RasterizerState raststate;
        private readonly Vector3[] vertices;

        public SectionRendererInfo Info;
        
        public SelectionRenderer(Device device) {

            vertices = new Vector3[4];
            
            byte[] vscode = File.ReadAllBytes("Assets/Shader/Selection.vs.fxo");
            byte[] pscode = File.ReadAllBytes("Assets/Shader/Selection.ps.fxo");


            vs = new VertexShader(device, vscode);
            ps = new PixelShader (device, pscode);
            
            instancebuffer = new Buffer(device, new BufferDescription(
                Marshal.SizeOf<Vector3>() * 4, 
                ResourceUsage.Dynamic,
                BindFlags.VertexBuffer, 
                CpuAccessFlags.Write,
                ResourceOptionFlags.None, 
                Marshal.SizeOf<Vector3>()
            ));
            constantbuffer = new Buffer(device, new BufferDescription(
                Marshal.SizeOf<Matrix4x4>(),
                ResourceUsage.Dynamic,
                BindFlags.ConstantBuffer,
                CpuAccessFlags.Write,
                ResourceOptionFlags.None,
                Marshal.SizeOf<Matrix4x4>()
            ));
            
            layout = new InputLayout(device, vscode, new [] {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0)
            });

            BlendStateDescription blenddesc = new BlendStateDescription {
                AlphaToCoverageEnable = false,
                IndependentBlendEnable = true
            };
            blenddesc.RenderTarget[0] = new RenderTargetBlendDescription {
                IsBlendEnabled = false,
                SourceBlend = BlendOption.SourceAlpha,
                DestinationBlend = BlendOption.InverseSourceAlpha,
                BlendOperation = BlendOperation.Add,
                SourceAlphaBlend = BlendOption.One,
                DestinationAlphaBlend = BlendOption.Zero,
                AlphaBlendOperation = BlendOperation.Add,
                RenderTargetWriteMask = ColorWriteMaskFlags.All
            };
            blendstate = new BlendState(device, blenddesc);
            depthstate = new DepthStencilState(device, new DepthStencilStateDescription {
                IsDepthEnabled = true,
                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.LessEqual,
                IsStencilEnabled = false
            });
            raststate = new RasterizerState(device, new RasterizerStateDescription{
                CullMode = CullMode.None,
                FillMode = FillMode.Solid
            });
            vertexBufferBinding = new VertexBufferBinding(instancebuffer, Marshal.SizeOf<Vector3>(), 0);
            
            Info = new SectionRendererInfo()
            {
                Enabled = false
            };
        }
        
        public void Render(DeviceContext context, Camera camera) {
            if(!Info.Enabled)
                return;

            Vector3 normal = Info.Normal;
            Vector3 pos = Info.Position;
            
            Vector3 v1 = new Vector3(normal.Y, normal.Z, normal.X);
            Vector3 v2 = new Vector3(normal.Z, normal.X, normal.Y);

            vertices[0] = pos + 0.505f * (normal - v1 - v2);
            vertices[1] = pos + 0.505f * (normal - v1 + v2);
            vertices[2] = pos + 0.505f * (normal + v1 - v2);
            vertices[3] = pos + 0.505f * (normal + v1 + v2);
            
            DepthStencilState state = context.OutputMerger.DepthStencilState;
            RasterizerState rstate = context.Rasterizer.State;
            context.Rasterizer.State = raststate;
            context.InputAssembler.InputLayout = layout;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            context.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
            context.VertexShader.Set(vs);
            context.GeometryShader.SetConstantBuffer(0, constantbuffer);
            context.PixelShader.Set(ps);
            context.OutputMerger.BlendState = blendstate;
            context.OutputMerger.DepthStencilState = depthstate;
            context.MapAndUpdate(vertices, instancebuffer);
            //TODO Create a shared camera constant buffer
            context.MapAndUpdate(ref camera.CameraMatrix, constantbuffer);
            context.Draw(4,0);
            context.OutputMerger.BlendState = null;
            context.OutputMerger.DepthStencilState = state;
            context.Rasterizer.State = rstate;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        }
        
        public void Dispose() {
            vs?.Dispose();
            ps?.Dispose();
            instancebuffer?.Dispose();
            constantbuffer?.Dispose();
            layout?.Dispose();
            depthstate?.Dispose();
            blendstate?.Dispose();
        }
        
        
    }

    public class SectionRendererInfo
    {
        public bool Enabled;
        public Vector3 Position;
        public Vector3 Normal;
    }
    
}