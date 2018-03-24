using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace SurviveCore.Gui.Text {
    public class TextRenderer : IDisposable {

        private readonly VertexShader vs;
        private readonly PixelShader ps;

        private readonly Buffer vertexbuffer;
        private readonly Buffer constantbuffer;
        private readonly InputLayout layout;
        private readonly SamplerState sampler;
        private readonly BlendState blendstate;

        private Matrix4x4 screenmatrix;

        public Matrix4x4 Screen {
            get => screenmatrix;
            set => screenmatrix = value;
        }
        
        public TextRenderer(Device device) {
            CompilationResult vscode = ShaderBytecode.CompileFromFile("./Assets/Shader/Font.hlsl", "VS", "vs_5_0");
            CompilationResult pscode = ShaderBytecode.CompileFromFile("./Assets/Shader/Font.hlsl", "PS", "ps_5_0");
            
            if(vscode.HasErrors)
                Console.WriteLine(vscode.Message);
            if(pscode.HasErrors)
                Console.WriteLine(pscode.Message);
            
            vs = new VertexShader(device, vscode);
            ps = new PixelShader (device, pscode);
            
            vertexbuffer = new Buffer(device, new BufferDescription(6 * 4 * 4 * 100, BindFlags.VertexBuffer, ResourceUsage.Default));
            
            layout = new InputLayout(device, vscode, new [] {
                new InputElement("POSITION", 0, Format.R32G32_Float, 0, 0, InputClassification.PerVertexData, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, 8, 0, InputClassification.PerVertexData, 0)
            });

            sampler = new SamplerState(device, new SamplerStateDescription {
                Filter = Filter.Anisotropic,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                ComparisonFunction = Comparison.Never,
                MipLodBias = 0,
                MaximumLod = 0,
                MinimumLod = 0
            });

            BlendStateDescription blenddesc = new BlendStateDescription {
                AlphaToCoverageEnable = false,
                IndependentBlendEnable = true
            };
            blenddesc.RenderTarget[0] = new RenderTargetBlendDescription {
                IsBlendEnabled = true,
                SourceBlend = BlendOption.SourceAlpha,
                DestinationBlend = BlendOption.InverseSourceAlpha,
                BlendOperation = BlendOperation.Add,
                SourceAlphaBlend = BlendOption.One,
                DestinationAlphaBlend = BlendOption.Zero,
                AlphaBlendOperation = BlendOperation.Add,
                RenderTargetWriteMask = ColorWriteMaskFlags.All
            };
            blendstate = new BlendState(device, blenddesc);

            constantbuffer = new Buffer(device, new BufferDescription(Marshal.SizeOf<Matrix4x4>(), BindFlags.ConstantBuffer, ResourceUsage.Default));
        }

        public void DrawText(DeviceContext context, Font f, string s, float scale) {
            context.InputAssembler.InputLayout = layout;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexbuffer, 4 * 4, 0));
            context.VertexShader.Set(vs);
            context.UpdateSubresource(ref screenmatrix, constantbuffer);
            context.VertexShader.SetConstantBuffer(0, constantbuffer);
            context.PixelShader.Set(ps);
            context.PixelShader.SetShaderResource(0, f.Texture);
            context.PixelShader.SetSampler(0, sampler);
            context.OutputMerger.BlendState = blendstate;
            
            float[] stdt = new float[6 * 4 * 100];
            for (int i = 0, x = 0; i < s.Length; i++) {
                CharInfo ci = f.GetCharInfo(s[i]);
                int kerning = 0;
                if (i > 0)
                    kerning = f.GetKerning((s[i - 1], s[i]));

                stdt[24 * i +  0] = (ci.Pos.X + x + kerning) * scale;
                stdt[24 * i +  1] = (ci.Pos.Y + 0) * scale;
                stdt[24 * i +  2] =  ci.Min.X / f.Size.X;
                stdt[24 * i +  3] =  ci.Min.Y / f.Size.Y;
                
                stdt[24 * i +  4] = (ci.Pos.X + x + kerning) * scale;
                stdt[24 * i +  5] = (ci.Pos.Y + ci.Size.Y) * scale;
                stdt[24 * i +  6] =  ci.Min.X / f.Size.X;
                stdt[24 * i +  7] =  ci.Max.Y / f.Size.Y;
                
                stdt[24 * i +  8] = (ci.Pos.X + x + ci.Size.X + kerning) * scale;
                stdt[24 * i +  9] = (ci.Pos.Y + 0) * scale;
                stdt[24 * i + 10] =  ci.Max.X / f.Size.X;
                stdt[24 * i + 11] =  ci.Min.Y / f.Size.Y;
                
                stdt[24 * i + 12] = (ci.Pos.X + x + kerning) * scale;
                stdt[24 * i + 13] = (ci.Pos.Y + ci.Size.Y) * scale;
                stdt[24 * i + 14] =  ci.Min.X / f.Size.X;
                stdt[24 * i + 15] =  ci.Max.Y / f.Size.Y;
                
                stdt[24 * i + 16] = (ci.Pos.X + x + ci.Size.X + kerning) * scale;
                stdt[24 * i + 17] = (ci.Pos.Y + ci.Size.Y) * scale;
                stdt[24 * i + 18] =  ci.Max.X / f.Size.X;
                stdt[24 * i + 19] =  ci.Max.Y / f.Size.Y;
                
                stdt[24 * i + 20] = (ci.Pos.X + x + ci.Size.X + kerning) * scale;
                stdt[24 * i + 21] = (ci.Pos.Y + 0) * scale;
                stdt[24 * i + 22] =  ci.Max.X / f.Size.X;
                stdt[24 * i + 23] =  ci.Min.Y / f.Size.Y;

                x += ci.Advance + kerning - 6;
            }
            context.UpdateSubresource(stdt, vertexbuffer);
            
            context.Draw(s.Length * 6,0);
            context.OutputMerger.BlendState = null;
        }
        
        public void Dispose() {
            vs.Dispose();
            ps.Dispose();
            vertexbuffer.Dispose();
            layout.Dispose();
            sampler.Dispose();
            blendstate.Dispose();
            constantbuffer.Dispose();
        }
    }

    //public struct Text {
    //    
    //    private static List<Vertex> builder = new List<Vertex>();
    //    
    //    public Font Font;
    //    public string message;
    //    public Vertex[] Data;

    //    public Text(Font f, string t, int size = 20) {
    //        Font = f;
    //        message = t;
    //        builder.Clear();
    //        foreach(char c in message) {
    //            
    //        }

    //        Data = builder.ToArray();
    //    }

    //    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    //    public struct Vertex {
    //        private Vector2 pos;
    //        private Vector2 tex;
    //    }
    //    
    //}
    
}