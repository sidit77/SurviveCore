using System;
using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace SurviveCore.World.Rendering {

    public class ChunkRenderer : IDisposable {

        private readonly Device device;
        
        private Chunk chunk;
        private bool active;

        private VertexBufferBinding binding;
        private int size;

        internal ChunkRenderer(Device dx) {
            device = dx;
            binding = new VertexBufferBinding(null, Marshal.SizeOf<Vertex>(), 0);
        }

        public ChunkRenderer SetUp(Chunk c) {
            chunk = c;
            active = true;
            return this;
        }

        public void CleanUp() {
            chunk = null;
            active = false;
        }

        public void SetActive(bool active) {
            this.active = active;
        }
        
        public bool Draw(DeviceContext context, Frustum frustum) {
            if(active && size > 0 && frustum.Intersection(chunk.Location.Min, chunk.Location.Max)) {
                context.InputAssembler.SetVertexBuffers(0, binding);
                context.Draw(size, 0);
                return true;
            }
            return false;
        }

        public void Update(Vertex[] mesh) {
            binding.Buffer?.Dispose();
            binding.Buffer = Buffer.Create(device, BindFlags.VertexBuffer, mesh);
            size = mesh.Length;
        }

        public void Dispose() {
            CleanUp();
            binding.Buffer?.Dispose();
        }

        

    }

}

