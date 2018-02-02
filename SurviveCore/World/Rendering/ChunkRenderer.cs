using System;
using OpenTK.Graphics.OpenGL4;
using SurviveCore.OpenGL.Helper;

namespace SurviveCore.World.Rendering {

    class ChunkRenderer : IDisposable {

        private Chunk chunk;
        private bool active;

        private readonly int vao;
        private readonly int vbo1;
        private readonly int vbo2;
        private int size;

        public ChunkRenderer() {
            vao = GL.GenVertexArray();
            vbo1 = GL.GenBuffer();
            vbo2 = GL.GenBuffer();

            GL.BindVertexArray(vao);
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);
            GL.EnableVertexAttribArray(3);
            GL.EnableVertexAttribArray(4);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo1);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0 * sizeof(float));
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo2);
            GL.VertexAttribPointer(2, 1, VertexAttribPointerType.UnsignedByte, true, 3 * sizeof(byte), 0 * sizeof(byte));
            GL.VertexAttribPointer(3, 1, VertexAttribPointerType.UnsignedByte, true, 3 * sizeof(byte), 1 * sizeof(byte));
            GL.VertexAttribPointer(4, 1, VertexAttribPointerType.UnsignedByte, true, 3 * sizeof(byte), 2 * sizeof(byte));

            GL.BindVertexArray(0);
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
            if(active && frustum.Intersection(chunk.Location.Min, chunk.Location.Max)) {
                GL.BindVertexArray(vao);
                GL.DrawArrays(PrimitiveType.Triangles, 0, size);
            }
        }

        public void Update(Mesh mesh) {
            GL.NamedBufferData(vbo1, mesh.vertices1.Length * sizeof(float), mesh.vertices1, BufferUsageHint.DynamicDraw);
            GL.NamedBufferData(vbo2, mesh.vertices2.Length * sizeof(byte), mesh.vertices2, BufferUsageHint.DynamicDraw);
            size = mesh.vertices1.Length / 5;
        }

        public void Dispose() {
            CleanUp();
            GL.DeleteVertexArray(vao);
            GL.DeleteBuffer(vbo1);
            GL.DeleteBuffer(vbo2);
        }

        

    }

}

