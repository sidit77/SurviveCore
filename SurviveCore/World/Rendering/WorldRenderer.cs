using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SurviveCore.DirectX;
using SurviveCore.World.Utils;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace SurviveCore.World.Rendering {
    
    public class WorldRenderer : IDisposable{

        private const int RendererPoolSize = 512;
        
        private readonly ObjectPool<ChunkRenderer> rendererPool;
        private readonly HashSet<ChunkRenderer> renderer;

        private readonly VertexShader worldvs;
        private readonly PixelShader worldps;
        private readonly InputLayout layout;
        private readonly Buffer vsbuffer;
        private readonly Buffer psbuffer;

        private readonly ShaderResourceView aotexture;
        private readonly SamplerState aosampler;

        private readonly ShaderResourceView colortexture;
        private readonly SamplerState colorsampler;

        private readonly ImmutableDictionary<string, int> blockmapping;
        
        public int CurrentlyRenderedChunks {
            get;
            private set;
        }
        
        public WorldRenderer(Device device) {
            rendererPool = new ObjectPool<ChunkRenderer>(RendererPoolSize, ()=>new ChunkRenderer(device));
            renderer = new HashSet<ChunkRenderer>();

            CompilationResult vscode = ShaderBytecode.CompileFromFile("./Assets/Shader/World.hlsl", "VS", "vs_5_0");
            CompilationResult pscode = ShaderBytecode.CompileFromFile("./Assets/Shader/World.hlsl", "PS", "ps_5_0");
            
            if(vscode.HasErrors)
                Console.WriteLine(vscode.Message);
            if(pscode.HasErrors)
                Console.WriteLine(pscode.Message);
            
            worldvs = new VertexShader(device, vscode);
            worldps = new PixelShader (device, pscode);
            
            vsbuffer = new Buffer(device, new BufferDescription(Marshal.SizeOf<Matrix4x4>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, Marshal.SizeOf<Matrix4x4>()));
            psbuffer = new Buffer(device, new BufferDescription(Marshal.SizeOf<ConstantPixelData>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, Marshal.SizeOf<ConstantPixelData>()));
            
            layout = new InputLayout(device, vscode, new [] {
                new InputElement("POSITION", 0, Format.R32G32B32_Float,  0, 0, InputClassification.PerVertexData, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float   , 12, 0, InputClassification.PerVertexData, 0),
                new InputElement("AOCASE"  , 0, Format.R8_UInt        , 20, 0, InputClassification.PerVertexData, 0),
                new InputElement("TEXID"   , 0, Format.R8_UInt        , 21, 0, InputClassification.PerVertexData, 0)
            });
            
            aotexture = DDSLoader.LoadDDS(device, "./Assets/Textures/AmbientOcclusion.dds");
            aosampler = new SamplerState(device, new SamplerStateDescription {
                Filter = Filter.Anisotropic,
                AddressU = TextureAddressMode.Mirror,
                AddressV = TextureAddressMode.Mirror,
                AddressW = TextureAddressMode.Mirror,
                ComparisonFunction = Comparison.Never,
                MipLodBias = 0f,
                MaximumLod = 30,
                MinimumLod = 0
            });

            colortexture = DDSLoader.LoadDDS(device, "./Assets/Textures/Blocks.dds");
            Dictionary<string, int> temp = new Dictionary<string, int>();
            foreach (string b in File.ReadAllLines("./Assets/Textures/Blocks.txt")) {
                temp.Add(b, temp.Count);
            }
            blockmapping = temp.ToImmutableDictionary();

            colorsampler = new SamplerState(device, new SamplerStateDescription {
                Filter = Filter.MinPointMagMipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                ComparisonFunction = Comparison.Never,
                MipLodBias = -0.3f,
                MaximumLod = 30,
                MinimumLod = 0
            });
        }
        
        public void Draw(DeviceContext context, Camera camera) {
            context.VertexShader.Set(worldvs);
            context.PixelShader.Set(worldps);
            ConstantPixelData cpd = new ConstantPixelData {
                FogColor = Color.DarkSlateGray.Raw(),
                Pos = camera.Position,
                flags  = (Settings.Instance.AmbientOcclusion ? 1 : 0) | (Settings.Instance.Fog ? 2 : 0),
            };
            context.MapAndUpdate(ref camera.CameraMatrix, vsbuffer);
            context.MapAndUpdate(ref cpd, psbuffer);
            context.VertexShader.SetConstantBuffer(0, vsbuffer);
            context.InputAssembler.InputLayout = layout;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.PixelShader.SetConstantBuffer(0, psbuffer);
            context.PixelShader.SetShaderResource(0, aotexture);
            context.PixelShader.SetSampler(0, aosampler);
            context.PixelShader.SetShaderResource(1, colortexture);
            context.PixelShader.SetSampler(1, colorsampler);
            CurrentlyRenderedChunks = 0;
            foreach(ChunkRenderer cr in renderer)
                if(cr.Draw(context, camera.Frustum))
                    CurrentlyRenderedChunks++;
        }

        public ChunkRenderer CreateChunkRenderer(Chunk c) {
            ChunkRenderer r = rendererPool.Get().SetUp(c);
            renderer.Add(r);
            return r;
        }
        
        public void DisposeChunkRenderer(ChunkRenderer c) {
            renderer.Remove(c);
            if(rendererPool.Add(c))
                c.CleanUp();
            else
                c.Dispose();
        }

        public ImmutableDictionary<string, int> GetBlockMapping() {
            return blockmapping;
        }
        
        public int NumberOfRenderers => renderer.Count;
        
        public void Dispose() {
            worldps.Dispose();
            worldvs.Dispose();
            vsbuffer.Dispose();
            psbuffer.Dispose();
            layout.Dispose();
            aosampler.Dispose();
            aotexture.Dispose();
            colorsampler.Dispose();
            colortexture.Dispose();
            while (rendererPool.Count > 0)
                rendererPool.Get().Dispose();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ConstantPixelData {
            public RawColor4 FogColor;
            public Vector3 Pos;
            public int flags;
        }
        
    }
    
}