using System;
using System.Numerics;
using System.Runtime.InteropServices;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SurviveCore.DirectX;
using SurviveCore.World.Utils;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace SurviveCore.World.Rendering {
    
    public class WorldRenderer : IDisposable{

        private const int RendererPoolSize = 256;
        
        private readonly DirectXContext dx;
        private readonly ObjectPool<ChunkRenderer> rendererPool;
        
        private delegate void OnDraw(Frustum f);
        private event OnDraw DrawEvent;

        private readonly VertexShader worldvs;
        private readonly PixelShader worldps;
        private readonly InputLayout layout;
        private readonly Buffer vsbuffer;

        private readonly ShaderResourceView aotexture;
        private readonly SamplerState aosampler;
        
        public WorldRenderer(DirectXContext dx) {
            this.dx = dx;
            rendererPool = new ObjectPool<ChunkRenderer>(RendererPoolSize, ()=>new ChunkRenderer(dx));

            CompilationResult vscode = ShaderBytecode.CompileFromFile("./Assets/Shader/World.hlsl", "VS", "vs_5_0");
            CompilationResult pscode = ShaderBytecode.CompileFromFile("./Assets/Shader/World.hlsl", "PS", "ps_5_0");
            
            if(vscode.HasErrors)
                Console.WriteLine(vscode.Message);
            if(pscode.HasErrors)
                Console.WriteLine(pscode.Message);
            
            worldvs = new VertexShader(dx.Device, vscode);
            worldps = new PixelShader (dx.Device, pscode);
            
            vsbuffer = new Buffer(dx.Device, new BufferDescription(Marshal.SizeOf<Matrix4x4>(), BindFlags.ConstantBuffer, ResourceUsage.Default));
            
            layout = new InputLayout(dx.Device, vscode, new [] {
                new InputElement("POSITION", 0, Format.R32G32B32_Float,  0, 0, InputClassification.PerVertexData, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float   , 12, 0, InputClassification.PerVertexData, 0),
                new InputElement("AOCASE"  , 0, Format.R8_UInt        , 20, 0, InputClassification.PerVertexData, 0)
            });

            aotexture = DDSLoader.LoadDDS(dx.Device, "./Assets/Textures/AmbientOcclusion.dds");
            aosampler = new SamplerState(dx.Device, new SamplerStateDescription {
                Filter = Filter.Anisotropic,
                AddressU = TextureAddressMode.Mirror,
                AddressV = TextureAddressMode.Mirror,
                AddressW = TextureAddressMode.Mirror,
                ComparisonFunction = Comparison.Never,
                MipLodBias = 0f,
                MaximumLod = 30,
                MinimumLod = 0
            });
        }
        
        public void Draw(Camera camera) {
            dx.Context.VertexShader.Set(worldvs);
            dx.Context.PixelShader.Set(worldps);
            dx.Context.UpdateSubresource(ref camera.CameraMatrix, vsbuffer);
            dx.Context.VertexShader.SetConstantBuffer(0, vsbuffer);
            dx.Context.InputAssembler.InputLayout = layout;
            dx.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            dx.Context.PixelShader.SetShaderResource(0, aotexture);
            dx.Context.PixelShader.SetSampler(0, aosampler);
            DrawEvent?.Invoke(camera.Frustum);
        }

        public ChunkRenderer CreateChunkRenderer(Chunk c) {
            ChunkRenderer r = rendererPool.Get().SetUp(c);
            DrawEvent += r.Draw;
            return r;
        }
        
        public void DisposeChunkRenderer(ChunkRenderer c) {
            DrawEvent -= c.Draw;
            if(rendererPool.Add(c))
                c.CleanUp();
            else
                c.Dispose();
        }

        public int NumberOfRenderers => DrawEvent?.GetInvocationList().Length ?? 0;
        
        public void Dispose() {
            worldps.Dispose();
            worldvs.Dispose();
            vsbuffer.Dispose();
            layout.Dispose();
            aosampler.Dispose();
            aotexture.Dispose();
            while (rendererPool.Count > 0)
                rendererPool.Get().Dispose();
        }
    }
    
}