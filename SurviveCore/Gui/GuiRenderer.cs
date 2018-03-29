using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SurviveCore.DirectX;
using SurviveCore.Gui.Text;
using WinApi.User32;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Size = NetCoreEx.Geometry.Size;

namespace SurviveCore.Gui {
    public class GuiRenderer : IDisposable {

        private InputManager.InputState input;
        
        private readonly VertexShader vs;
        private readonly PixelShader ps;

        private readonly Buffer vertexbuffer;
        private readonly Buffer instancebuffer;
        private readonly Buffer constantbuffer;
        private readonly InputLayout layout;
        private readonly SamplerState sampler;
        private readonly BlendState blendstate;
        private readonly DepthStencilState depthstate;

        private readonly ShaderResourceView guitexture;
        private readonly Font font;

        private readonly int textoffset = 200;
        private readonly Quad[] quads;
        private int gquadnr;
        private int tquadnr;
        
        public GuiRenderer(Device device) {
            guitexture = DDSLoader.LoadDDS(device, "./Assets/Gui/Gui.dds");
            font = new Font(device, "./Assets/Gui/Fonts/Abel.fnt");
            
            CompilationResult vscode = ShaderBytecode.CompileFromFile("./Assets/Shader/Gui.hlsl", "VS", "vs_5_0");
            CompilationResult pscode = ShaderBytecode.CompileFromFile("./Assets/Shader/Gui.hlsl", "PS", "ps_5_0");
            
            if(vscode.HasErrors)
                Console.WriteLine(vscode.Message);
            if(pscode.HasErrors)
                Console.WriteLine(pscode.Message);
            
            vs = new VertexShader(device, vscode);
            ps = new PixelShader (device, pscode);
            //TODO ResourceUsage.Dynamic? + more than 500 characters
            quads = new Quad[1000];
            instancebuffer = new Buffer(device, new BufferDescription(Marshal.SizeOf<Quad>() * quads.Length, BindFlags.VertexBuffer, ResourceUsage.Default));
            vertexbuffer = Buffer.Create(device, BindFlags.VertexBuffer, new float[] {0,0,0,1,1,0,0,1,1,1,1,0});
            constantbuffer = new Buffer(device, new BufferDescription(Marshal.SizeOf<Matrix4x4>(), BindFlags.ConstantBuffer, ResourceUsage.Default));
            
            layout = new InputLayout(device, vscode, new [] {
                new InputElement("POSITION", 0, Format.R32G32_Float,        0, 0, InputClassification.PerVertexData,   0),
                new InputElement("OFFSET"  , 0, Format.R32G32B32A32_Float,  0, 1, InputClassification.PerInstanceData, 1),
                new InputElement("TEXCOORD", 0, Format.R32G32B32A32_Float, 16, 1, InputClassification.PerInstanceData, 1),
                new InputElement("COLOR"   , 0, Format.R8G8B8A8_UNorm,     32, 1, InputClassification.PerInstanceData, 1),
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
            depthstate = new DepthStencilState(device, new DepthStencilStateDescription {
                IsDepthEnabled = false,
                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.Always,
                IsStencilEnabled = false
            });
        }

        public void Begin(InputManager.InputState inputState) {
            gquadnr = 0;
            tquadnr = 0;
            input = inputState;
        }
        
        public void Text(Point p, string text, Origin origin = Origin.TopLeft, int size = 20) {
            const int DeltaWidth = -6;
            const int DeltaHeight = -17;

            int startid = tquadnr;
            int color = Color.White.ToRgba();
            float scale = (float) size / font.Size;
            int x = 0, y = 0, mx = 0;
            for (int i = 0; i < text.Length; i++) {
                if (text[i] == '\n') {
                    y += font.LineHeight + DeltaHeight;
                    mx = Math.Max(mx, x);
                    x = 0;
                    continue;
                }
                if(text[i] == '§') {
                    i++;
                    switch(text[i]) {
                        case '0':
                            color = Color.Black.ToRgba();
                            break;
                        case '1':
                            color = Color.DarkBlue.ToRgba();
                            break;
                        case '2':
                            color = Color.DarkGreen.ToRgba();
                            break;
                        case '3':
                            color = Color.DarkCyan.ToRgba();
                            break;
                        case '4':
                            color = Color.DarkRed.ToRgba();
                            break;
                        case '5':
                            color = Color.DarkMagenta.ToRgba();
                            break;
                        case '6':
                            color = Color.Gold.ToRgba();
                            break;
                        case '7':
                            color = Color.Gray.ToRgba();
                            break;
                        case '8':
                            color = Color.DarkGray.ToRgba();
                            break;
                        case '9':
                            color = Color.Blue.ToRgba();
                            break;
                        case 'a':
                            color = Color.Green.ToRgba();
                            break;
                        case 'b':
                            color = Color.Aqua.ToRgba();
                            break;
                        case 'c':
                            color = Color.Red.ToRgba();
                            break;
                        case 'd':
                            color = Color.LightPink.ToRgba();
                            break;
                        case 'e':
                            color = Color.Yellow.ToRgba();
                            break;
                        case 'f':
                            color = Color.White.ToRgba();
                            break;
                        default:
                            Console.WriteLine("Error: unknown formatig code");
                            break;
                    }
                    continue;
                }
                Font.CharInfo ci = font.GetCharInfo(text[i]);
                int kerning = 0;
                if (i > 0)
                    kerning = font.GetKerning((text[i - 1], text[i]));
                quads[textoffset + tquadnr].Color = color;
                quads[textoffset + tquadnr].Tex = new Vector4(ci.Texture.X, ci.Texture.Y, ci.Texture.Width, ci.Texture.Height);
                quads[textoffset + tquadnr].Pos = new Vector4(p.X, p.Y, 0, 0) + new Vector4(ci.Positon.X + x + kerning, ci.Positon.Y + y + DeltaHeight, ci.Positon.Width, ci.Positon.Height) * scale;
                tquadnr++;

                x += ci.Advance + kerning + DeltaWidth;
            }
            Size s = new Size((int)MathF.Round(Math.Max(mx, x) * scale), (int)MathF.Round((y + font.LineHeight + DeltaHeight) * scale));
            Vector4 offset = Vector4.Zero;
            switch(origin) {
                case Origin.TopLeft:
                    return;
                case Origin.TopRight:
                    offset.X -= s.Width;
                    break;
                case Origin.BottomLeft:
                    offset.Y -= s.Height;
                    break;
                case Origin.BottomRight:
                    offset.X -= s.Width;
                    offset.Y -= s.Height;
                    break;
                case Origin.Center:
                    offset.X -= s.Width / 2;
                    offset.Y -= s.Height / 2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
            }
            for(int i = startid; i < tquadnr; i++)
                quads[textoffset + i].Pos += offset;
        }
        
        public bool Button(Rectangle rect, string text) {
            bool hovering = !input.MouseCaptured && rect.Contains(input.RelativeMousePosition);
            int c = (hovering ? Color.LightGray : Color.White).ToRgba();
            Text(new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2), text, origin:Origin.Center, size:30);
            
            const int cs = 21;
            
            quads[gquadnr].Color = c;
            quads[gquadnr].Pos = new Vector4(rect.X, rect.Y, cs, cs);
            quads[gquadnr].Tex = new Vector4(0.0f,0.0f,0.5f,0.5f);
            gquadnr++;
            
            quads[gquadnr].Color = c;
            quads[gquadnr].Pos = new Vector4(rect.X + cs, rect.Y, rect.Width - 2 * cs, cs);
            quads[gquadnr].Tex = new Vector4(0.5f,0.0f,0.0f,0.5f);
            gquadnr++;
            
            quads[gquadnr].Color = c;
            quads[gquadnr].Pos = new Vector4(rect.X + rect.Width - cs, rect.Y, cs, cs);
            quads[gquadnr].Tex = new Vector4(0.5f,0.0f,0.5f,0.5f);
            gquadnr++;
            
            quads[gquadnr].Color = c;
            quads[gquadnr].Pos = new Vector4(rect.X, rect.Y + cs, cs, rect.Height - 2 * cs);
            quads[gquadnr].Tex = new Vector4(0.0f,0.5f,0.5f,0.0f);
            gquadnr++;
            
            quads[gquadnr].Color = c;
            quads[gquadnr].Pos = new Vector4(rect.X + cs, rect.Y + cs, rect.Width - 2 * cs, rect.Height - 2 * cs);
            quads[gquadnr].Tex = new Vector4(0.5f,0.5f,0.0f,0.0f);
            gquadnr++;
            
            quads[gquadnr].Color = c;
            quads[gquadnr].Pos = new Vector4(rect.X + rect.Width - cs, rect.Y + cs, cs, rect.Height - 2 * cs);
            quads[gquadnr].Tex = new Vector4(0.5f,0.5f,0.5f,0.0f);
            gquadnr++;
            
            quads[gquadnr].Color = c;
            quads[gquadnr].Pos = new Vector4(rect.X, rect.Y + rect.Height - cs, cs, cs);
            quads[gquadnr].Tex = new Vector4(0.0f,0.5f,0.5f,0.5f);
            gquadnr++;
            
            quads[gquadnr].Color = c;
            quads[gquadnr].Pos = new Vector4(rect.X + cs, rect.Y + rect.Height - cs, rect.Width-2 * cs, cs);
            quads[gquadnr].Tex = new Vector4(0.5f,0.5f,0.0f,0.5f);
            gquadnr++;
            
            quads[gquadnr].Color = c;
            quads[gquadnr].Pos = new Vector4(rect.X + rect.Width - cs, rect.Y + rect.Height - cs, cs, cs);
            quads[gquadnr].Tex = new Vector4(0.5f,0.5f,0.5f,0.5f);
            gquadnr++;
            return hovering && input.IsKeyDown(VirtualKey.LBUTTON);
        }
        
        public void Render(DeviceContext context, Size size) {
            input.Update();

            DepthStencilState state = context.OutputMerger.DepthStencilState;
            context.InputAssembler.InputLayout = layout;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexbuffer, 2 * sizeof(float), 0));
            context.InputAssembler.SetVertexBuffers(1, new VertexBufferBinding(instancebuffer, Marshal.SizeOf<Quad>(), 0));
            context.VertexShader.Set(vs);
            context.VertexShader.SetConstantBuffer(0, constantbuffer);
            context.PixelShader.Set(ps);
            context.PixelShader.SetShaderResource(0, guitexture);
            context.PixelShader.SetSampler(0, sampler);
            context.OutputMerger.BlendState = blendstate;
            context.OutputMerger.DepthStencilState = depthstate;
            context.UpdateSubresource(quads, instancebuffer);
            Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(0,size.Width, size.Height, 0, -1, 1);
            context.UpdateSubresource(ref mvp, constantbuffer);
            context.DrawInstanced(6,gquadnr,0,0);
            context.PixelShader.SetShaderResource(0,font.Texture);
            context.DrawInstanced(6,tquadnr,0,textoffset);
            context.OutputMerger.BlendState = null;
            context.OutputMerger.DepthStencilState = state;
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
            guitexture.Dispose();
            font.Dispose();
            depthstate.Dispose();
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Quad {
            public Vector4 Pos;
            public Vector4 Tex;
            public int Color;
        }
    }

    public enum Origin {
        TopLeft, TopRight, BottomLeft, BottomRight, Center
    }

    public static class TextFormat {
        public const string Black = "§0";
        public const string DarkBlue = "§1";
        public const string DarkGreen = "§2";
        public const string DarkCyan = "§3";
        public const string DarkRed = "§4";
        public const string DarkMagenta = "§5";
        public const string Gold = "§6";
        public const string Gray = "§7";
        public const string DarkGray = "§8";
        public const string Blue = "§9";
        public const string Green = "§a";
        public const string Aqua = "§b";
        public const string Red = "§c";
        public const string LightPink = "§d";
        public const string Yellow = "§e";
        public const string White = "§f";
    }
    
}