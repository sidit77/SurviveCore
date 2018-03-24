using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SurviveCore.DirectX;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace SurviveCore.Gui.Text {
    public class TextRenderer : IDisposable {

        private readonly VertexShader vs;
        private readonly PixelShader ps;

        private readonly Buffer vertexbuffer;
        private readonly Buffer instancebuffer;
        private readonly Buffer constantbuffer;
        private readonly InputLayout layout;
        private readonly SamplerState sampler;
        private readonly BlendState blendstate;

        private ConstantBufferData constdata;

        public Matrix4x4 Screen {
            get => constdata.ScreenMatrix;
            set => constdata.ScreenMatrix = value;
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
            
            instancebuffer = new Buffer(device, new BufferDescription(Marshal.SizeOf<CharInstance>() * 500, BindFlags.VertexBuffer, ResourceUsage.Default));
            vertexbuffer = Buffer.Create(device, BindFlags.VertexBuffer, new float[] {0,0,0,1,1,0,0,1,1,1,1,0});
            constantbuffer = new Buffer(device, new BufferDescription(Marshal.SizeOf<ConstantBufferData>(), BindFlags.ConstantBuffer, ResourceUsage.Default));
            
            layout = new InputLayout(device, vscode, new [] {
                new InputElement("POSITION", 0, Format.R32G32_Float,    0, 0, InputClassification.PerVertexData, 0),
                new InputElement("OFFSET"  , 0, Format.R32G32_Float,    0, 1, InputClassification.PerInstanceData, 1),
                new InputElement("SCALE"   , 0, Format.R32_Float,       8, 1, InputClassification.PerInstanceData, 1),
                new InputElement("CHARID"  , 0, Format.R32_UInt,       12, 1, InputClassification.PerInstanceData, 1),
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

        }

        public void DrawText(DeviceContext context, Vector2 position, Font font, string s, int size = 20) {
            DrawText(context, position, new Text(font, s, size));
        }
        
        public void DrawText(DeviceContext context, Vector2 position, Font font, string s, Color c, int size = 20) {
            DrawText(context, position, new Text(font, s, size), c);
        }
        
        public void DrawText(DeviceContext context, Vector2 position, Text t) {
            DrawText(context, position, t, Color.White);
        }
        
        public void DrawTextCentered(DeviceContext context, Vector2 position, Font font, string text, Color color, int size = 20) {
            Text t = new Text(font, text, size);
            DrawText(context, position - new Vector2(t.Size.Width / 2, t.Size.Height / 2), t, color);
        }

        public void DrawText(DeviceContext context, Vector2 position, Text t, Color color) {
            context.InputAssembler.InputLayout = layout;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexbuffer, 2 * sizeof(float), 0));
            context.InputAssembler.SetVertexBuffers(1, new VertexBufferBinding(instancebuffer, Marshal.SizeOf<CharInstance>(), 0));
            context.VertexShader.Set(vs);
            context.VertexShader.SetShaderResource(0, t.Font.CharData);
            context.VertexShader.SetConstantBuffer(0, constantbuffer);
            context.PixelShader.Set(ps);
            context.PixelShader.SetShaderResource(0, t.Font.Texture);
            context.PixelShader.SetSampler(0, sampler);
            context.OutputMerger.BlendState = blendstate;
            constdata.Color = color.Raw();
            constdata.Position = position;
            context.UpdateBuffer(instancebuffer, t.Data);
            context.UpdateSubresource(ref constdata, constantbuffer);
            context.DrawInstanced(6,t.Data.Length,0,0);
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
            instancebuffer.Dispose();
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CharInstance {
            public Vector2 Pos;
            public float Scale;
            public int Id;
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 96)]
        private struct ConstantBufferData {
            public Matrix4x4 ScreenMatrix;
            public RawColor4 Color;
            public Vector2 Position;
        }
        
    }

    public struct Text {
        
        private const int DeltaWidth = -6;
        private const int DeltaHeight = -17;
        
        public readonly Font Font;
        public readonly string message;
        public readonly TextRenderer.CharInstance[] Data;
        public readonly Size Size;
        
        public Text(Font font, string text, int size = 20) {
            Font = font;
            message = text;
            Data = new TextRenderer.CharInstance[text.Length];
            float scale = (float) size / font.Size;
            int x = 0, y = 0, mx = 0;
            for (int i = 0; i < text.Length; i++) {
                if (text[i] == '\n') {
                    y += font.LineHeight + DeltaHeight;
                    mx = Math.Max(mx, x);
                    x = 0;
                    continue;
                }
                Font.CharInfo ci = font.GetCharInfo(text[i]);
                int kerning = 0;
                if (i > 0)
                    kerning = font.GetKerning((text[i - 1], text[i]));

                Data[i].Scale = scale;
                Data[i].Id = ci.RenderId;
                Data[i].Pos = new Vector2(ci.Pos.X + x + kerning, ci.Pos.Y + y + DeltaHeight) * scale;

                x += ci.Advance + kerning + DeltaWidth;
            }
            Size = new Size((int)MathF.Round(mx * scale), (int)MathF.Round((y + font.LineHeight + DeltaHeight) * scale));
        }

        
        
    }
    
}