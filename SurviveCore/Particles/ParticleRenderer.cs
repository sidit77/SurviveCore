using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SurviveCore.DirectX;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace SurviveCore.Particles {
    
    public class ParticleRenderer : IDisposable{
        private readonly VertexShader vs;
        private readonly PixelShader ps;
        private readonly GeometryShader gs;

        private readonly Buffer instancebuffer;
        private readonly Buffer constantbuffer;
        private readonly VertexBufferBinding vertexBufferBinding;
        private readonly InputLayout layout;
        private readonly BlendState blendstate;
        private readonly DepthStencilState depthstate;

        private readonly List<Particle> particles;

        public ParticleRenderer(Device device) {
            byte[] vscode = File.ReadAllBytes("Assets/Shader/Particle.vs.fxo");
            byte[] pscode = File.ReadAllBytes("Assets/Shader/Particle.ps.fxo");
            byte[] gscode = File.ReadAllBytes("Assets/Shader/Particle.gs.fxo");


            vs = new VertexShader(device, vscode);
            ps = new PixelShader (device, pscode);
            gs = new GeometryShader(device, gscode);
            instancebuffer = new Buffer(device, new BufferDescription(
                Marshal.SizeOf<Particle>() * 1000, 
                ResourceUsage.Dynamic,
                BindFlags.VertexBuffer, 
                CpuAccessFlags.Write,
                ResourceOptionFlags.None, 
                Marshal.SizeOf<Particle>()
            ));
            constantbuffer = new Buffer(device, new BufferDescription(
                Marshal.SizeOf<ParticleConstantBuffer>(),
                ResourceUsage.Dynamic,
                BindFlags.ConstantBuffer,
                CpuAccessFlags.Write,
                ResourceOptionFlags.None,
                Marshal.SizeOf<ParticleConstantBuffer>()
            ));
            
            layout = new InputLayout(device, vscode, new [] {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                new InputElement("RADIUS"  , 0, Format.R32_Float,      12, 0, InputClassification.PerVertexData, 0),
                new InputElement("COLOR"   , 0, Format.R8G8B8A8_UNorm, 16, 0, InputClassification.PerVertexData, 0),
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
                IsDepthEnabled = true,
                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.Less,
                IsStencilEnabled = false
            });
            
            vertexBufferBinding = new VertexBufferBinding(instancebuffer, Marshal.SizeOf<Particle>(), 0);
            
            particles = new List<Particle>();
        }

        public void AddParticle(Vector3 position, float radius, Color color)
        {
            particles.Add(new Particle(position, radius, (uint)color.ToRgba()));
        }

        public void Clear()
        {
            particles.Clear();
        }
        
        public void Render(DeviceContext context, Camera camera) {
            if(particles.Count <= 0)
                return;
            DepthStencilState state = context.OutputMerger.DepthStencilState;
            context.InputAssembler.InputLayout = layout;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
            context.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
            context.VertexShader.Set(vs);
            context.GeometryShader.SetConstantBuffer(0, constantbuffer);
            context.PixelShader.Set(ps);
            context.GeometryShader.Set(gs);
            context.OutputMerger.BlendState = blendstate;
            context.OutputMerger.DepthStencilState = depthstate;
            context.MapAndUpdate(particles.ToArray(), instancebuffer);
            ParticleConstantBuffer buffer = new ParticleConstantBuffer() {
                ViewProjection = camera.CameraMatrix,
                Right = camera.Right,
                Up = camera.Up
            };
            context.MapAndUpdate(ref buffer, constantbuffer);
            context.Draw(particles.Count,0);
            context.OutputMerger.BlendState = null;
            context.OutputMerger.DepthStencilState = state;
            context.GeometryShader.Set(null);
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        }
        
        public void Dispose() {
            vs?.Dispose();
            ps?.Dispose();
            gs?.Dispose();
            instancebuffer?.Dispose();
            constantbuffer?.Dispose();
            layout?.Dispose();
            blendstate?.Dispose();
            depthstate?.Dispose();
        }
        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 96)]
        private struct ParticleConstantBuffer {
            [FieldOffset(0)]
            public Matrix4x4 ViewProjection;
            [FieldOffset(64)]
            public Vector3 Right;
            [FieldOffset(80)]
            public Vector3 Up;
        }
        
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Particle {
        public Vector3 Position;
        public float Radius;
        public uint color;
        public Particle(Vector3 position, float radius, uint color) {
            Position = position;
            Radius = radius;
            this.color = color;
        }
    }
    
    
}