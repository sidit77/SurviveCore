﻿using System;
using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using SurviveCore.DirectX;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace SurviveCore.World.Rendering {

    public class ChunkRenderer : IDisposable {

        private readonly DirectXContext dx;
        
        private Chunk chunk;
        private bool active;

        private VertexBufferBinding binding;
        private int size;

        internal ChunkRenderer(DirectXContext dx) {
            this.dx = dx;

            binding = new VertexBufferBinding(null, Marshal.SizeOf<Vertex>(), 0);
            //vao = GL.GenVertexArray();
            //vbo1 = GL.GenBuffer();
            //vbo2 = GL.GenBuffer();
//
            //GL.BindVertexArray(vao);
            //GL.EnableVertexAttribArray(0);
            //GL.EnableVertexAttribArray(1);
            //GL.EnableVertexAttribArray(2);
            //GL.EnableVertexAttribArray(3);
            //GL.EnableVertexAttribArray(4);
            //GL.BindBuffer(BufferTarget.ArrayBuffer, vbo1);
            //GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0 * sizeof(float));
            //GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
            //GL.BindBuffer(BufferTarget.ArrayBuffer, vbo2);
            //GL.VertexAttribPointer(2, 1, VertexAttribPointerType.UnsignedByte, true, 3 * sizeof(byte), 0 * sizeof(byte));
            //GL.VertexAttribPointer(3, 1, VertexAttribPointerType.UnsignedByte, true, 3 * sizeof(byte), 1 * sizeof(byte));
            //GL.VertexAttribPointer(4, 1, VertexAttribPointerType.UnsignedByte, true, 3 * sizeof(byte), 2 * sizeof(byte));
//
            //GL.BindVertexArray(0);
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
        
        public void Draw(Frustum frustum) {
            if(active && size > 0 && frustum.Intersection(chunk.Location.Min, chunk.Location.Max)) {
                dx.Context.InputAssembler.SetVertexBuffers(0, binding);
                dx.Context.Draw(size, 0);
                //GL.BindVertexArray(vao);
                //GL.DrawArrays(PrimitiveType.Triangles, 0, size);
            }
        }

        public void Update(Vertex[] mesh) {
            binding.Buffer?.Dispose();
            binding.Buffer = Buffer.Create(dx.Device, BindFlags.VertexBuffer, mesh);
            //GL.NamedBufferData(vbo1, mesh.vertices1.Length * sizeof(float), mesh.vertices1, BufferUsageHint.DynamicDraw);
            //GL.NamedBufferData(vbo2, mesh.vertices2.Length * sizeof(byte), mesh.vertices2, BufferUsageHint.DynamicDraw);
            size = mesh.Length;
        }

        public void Dispose() {
            CleanUp();
            binding.Buffer?.Dispose();
            //GL.DeleteVertexArray(vao);
            //GL.DeleteBuffer(vbo1);
            //GL.DeleteBuffer(vbo2);
        }

        

    }

}

