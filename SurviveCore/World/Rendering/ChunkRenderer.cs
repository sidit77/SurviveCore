using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace SurviveCore.World.Rendering {

    class ChunkRenderer : IDisposable {

        public static long time;
        public static int number;

        private Chunk chunk;
        private bool update;
        private int x, y, z;

        private int vao;
        private int vbo1;
        private int vbo2;
        private int size;

        private Stopwatch clock = new Stopwatch();
        [ThreadStatic]
        private static List<float> vertices1;
        [ThreadStatic]
        private static List<byte> vertices2;

        private EventHandler handler;

        public ChunkRenderer(int x, int y, int z, Chunk chunk) {
            this.handler = (object sender, EventArgs e) => {
                update = true;
            };
            this.chunk = chunk;
            this.chunk.ChunkUpdate += handler;
            update = true;
            this.x = x * Chunk.Size;
            this.y = y * Chunk.Size;
            this.z = z * Chunk.Size;
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

        public Vector3 Position => new Vector3(x, y, z);

        public int X {
            get => x / Chunk.Size;
            set => x = value * Chunk.Size;
        }

        public int Y {
            get => y / Chunk.Size;
            set => y = value * Chunk.Size;
        }

        public int Z {
            get => z / Chunk.Size;
            set => z = value * Chunk.Size;
        }

        public Chunk Chunk {
            get => chunk;
            set {
                chunk.ChunkUpdate -= handler;
                chunk = value;
                update = true;
                chunk.ChunkUpdate += handler;
            }
        }

        public void Draw() {
            if(update && t == null) {
                t = Task.Run(() => UpdateMesh());
            }
            if(t != null && t.IsCompleted) {
                Update();
            }
            GL.BindVertexArray(vao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, size);
        }

        private Task<TMesh> t;

        private void Update() {
            //clock.Restart();

            //clock.Stop();
            //ChunkRenderer.time += clock.ElapsedTicks;
            //ChunkRenderer.number++;

            TMesh mesh = t.Result;
            GL.NamedBufferData(vbo1, mesh.vertices1.Length * sizeof(float), mesh.vertices1, BufferUsageHint.DynamicDraw);
            GL.NamedBufferData(vbo2, mesh.vertices2.Length * sizeof(byte), mesh.vertices2, BufferUsageHint.DynamicDraw);
            size = mesh.vertices1.Length / 5;
            update = false;
            t = null;
        }

        private class TMesh {
            public float[] vertices1;
            public byte[] vertices2;
        }

        private TMesh UpdateMesh() {
            if(vertices1 == null)
                vertices1 = new List<float>();
            if(vertices2 == null)
                vertices2 = new List<byte>();

            BlockFace[] mask = new BlockFace[Chunk.Size * Chunk.Size];
            int u, v, d, axis;
            int n, k, l;
            bool done;
            int[] p = new int[3];
            int[] q = new int[3];
            int[] s = new int[3];

            vertices1.Clear();
            vertices2.Clear();

            for(axis = 0; axis < 6; axis++) {
                d = axis % 3;
                u = (d + 1) % 3;
                v = (d + 2) % 3;

                q[u] = 0;
                q[v] = 0;
                q[d] = axis < 3 ? 1 : -1;

                for(p[d] = 0; p[d] < Chunk.Size; p[d]++) {
                    n = 0;
                    
                    for(p[v] = 0; p[v] < Chunk.Size; ++p[v]) {
                        for(p[u] = 0; p[u] < Chunk.Size; ++p[u]) {
                            mask[n++].Visible = !chunk.GetBlockDirect(p[0], p[1], p[2]).IsUnrendered && !(chunk.GetBlock(p[0] + q[0], p[1] + q[1], p[2] + q[2]).IsSolid());
                            if(mask[n - 1].Visible) {
                                mask[n - 1].TextureID = chunk.GetBlockDirect(p[0], p[1], p[2]).GetTextureID(axis);
                                mask[n - 1].AoID = 0;
                                for(l = 0; l < 8; l++) {
                                    s[d] = 0;
                                    s[u] = AoOffsets[(l + shift[axis]) % 8 * 2 + 0] * (axis == 5 ? -1 : 1); //d == 2 ? v : u
                                    s[v] = AoOffsets[(l + shift[axis]) % 8 * 2 + 1] * (axis == 3 ? -1 : 1); //d == 2 ? u : v
                                    mask[n - 1].AoID += (chunk.GetBlock(p[0] + q[0] + s[0], p[1] + q[1] + s[1], p[2] + q[2] + s[2]).IsSolid() ? 1 : 0) << l;
                                }
                            }
                        }
                    }
                    
                    n = 0;
                    for(p[v] = 0; p[v] < Chunk.Size; ++p[v]) {
                        for(p[u] = 0; p[u] < Chunk.Size;) {
                            if(mask[n].Visible) {
                                s[d] = 1;

                                for(s[u] = 1; p[u] + s[u] < Chunk.Size && mask[n + s[u]].Visible && mask[n + s[u]].CanConnect(mask[n]); ++s[u]) ;

                                done = false;
                                for(s[v] = 1; p[v] + s[v] < Chunk.Size; ++s[v]) {
                                    for(k = 0; k < s[u]; ++k) {
                                        if(!mask[n + k + s[v] * Chunk.Size].Visible || !mask[n + k + s[v] * Chunk.Size].CanConnect(mask[n])) {
                                            done = true;
                                            break;
                                        }
                                    }
                                    if(done) break;
                                }

                                for(l = 0; l < 6; l++) {
                                    k = axis * 30 + l * 5;
                                    vertices1.Add(this.x - 0.5f + p[0] + FaceVertices[k + 0] * s[0]);
                                    vertices1.Add(this.y - 0.5f + p[1] + FaceVertices[k + 1] * s[1]);
                                    vertices1.Add(this.z - 0.5f + p[2] + FaceVertices[k + 2] * s[2]);
                                    vertices1.Add(FaceVertices[k + 3] * s[d == 2 ? u : v]);
                                    vertices1.Add(FaceVertices[k + 4] * s[d == 2 ? v : u]);
                                    vertices2.Add((byte)axis);
                                    vertices2.Add((byte)mask[n].TextureID);
                                    vertices2.Add((byte)mask[n].AoID);
                                }

                                for(l = 0; l < s[v]; ++l) {
                                    for(k = 0; k < s[u]; ++k) {
                                        mask[n + k + l * Chunk.Size].Visible = false;
                                    }
                                }


                                p[u] += s[u];
                                n += s[u];

                            } else {
                                ++p[u];
                                ++n;
                            }
                        }
                    }
                }

            }
            return new TMesh() {
                vertices1 = vertices1.ToArray(),
                vertices2 = vertices2.ToArray()
            };
        }

        private struct BlockFace {
            public bool Visible;
            public int TextureID;
            public int AoID;

            public bool CanConnect(BlockFace bf) {
                return TextureID == bf.TextureID && AoID == bf.AoID;
            }
        }

        public void Dispose() {
            GL.DeleteVertexArray(vao);
            GL.DeleteBuffer(vbo1);
            GL.DeleteBuffer(vbo2);
        }

        private static readonly int[] AoOffsets = { 
            -1,-1,
            -1, 0,
            -1, 1,
             0, 1,
             1, 1,
             1, 0,
             1,-1,
             0,-1
        };

        private static readonly int[] shift = { 4, 0, 2, 4, 0, 2 };

        private static readonly float[] FaceVertices = new float[] {
            
             1, 1, 0, 1, 0,
             1, 1, 1, 0, 0,
             1, 0, 0, 1, 1,
             1, 0, 1, 0, 1,
             1, 0, 0, 1, 1,
             1, 1, 1, 0, 0,
            
            
             0, 1, 0, 0, 0,
             0, 1, 1, 0, 1,
             1, 1, 0, 1, 0,
             1, 1, 1, 1, 1,
             1, 1, 0, 1, 0,
             0, 1, 1, 0, 1,
            
            
             1, 1, 1, 1, 0,
             0, 1, 1, 0, 0,
             1, 0, 1, 1, 1,
             0, 0, 1, 0, 1,
             1, 0, 1, 1, 1,
             0, 1, 1, 0, 0,
            
            
             0, 1, 1, 1, 0,
             0, 1, 0, 0, 0,
             0, 0, 1, 1, 1,
             0, 0, 0, 0, 1,
             0, 0, 1, 1, 1,
             0, 1, 0, 0, 0,
            
            
             1, 0, 0, 1, 0,
             1, 0, 1, 1, 1,
             0, 0, 0, 0, 0,
             0, 0, 1, 0, 1,
             0, 0, 0, 0, 0,
             1, 0, 1, 1, 1,
            
            
             0, 1, 0, 1, 0,
             1, 1, 0, 0, 0,
             0, 0, 0, 1, 1,
             1, 0, 0, 0, 1,
             0, 0, 0, 1, 1,
             1, 1, 0, 0, 0,
                
        };

    }

}

//class GreedyChunkMesher : IChunkMesher {
//    public float[] CreateChunkMesh(Chunk chunk, int meshx, int meshy, int meshz, int size) {
//        Stopwatch clock = new Stopwatch();
//        clock.Start();
//
//        List<float> vertices = new List<float>();
//
//        int l, w, h, k, u, v, n, i, j, d, id;
//        Direction op;
//        int[] x = new int[3];
//        int[] q = new int[3];
//        float[,] ve = new float[4, 5];
//        bool[] mask = new bool[WorldChunk.Size * WorldChunk.Size];
//
//        foreach(Direction dir in Enum.GetValues(typeof(Direction))) {
//            id = dir.GetID();
//            d = dir.GetAxisID();
//            op = dir.GetOpposite();
//            u = (d + 1) % 3;
//            v = (d + 2) % 3;
//
//            x[0] = 0;
//            x[1] = 0;
//            x[2] = 0;
//
//            q[0] = dir.GetX();
//            q[1] = dir.GetY();
//            q[2] = dir.GetZ();
//            
//            for(x[d] = 0; x[d] < WorldChunk.Size; x[d]++) {
//                n = 0;
//                if(x[d] + q[d] < 0 || x[d] + q[d] >= WorldChunk.Size) {
//                    for(x[v] = 0; x[v] < WorldChunk.Size; ++x[v]) {
//                        for(x[u] = 0; x[u] < WorldChunk.Size; ++x[u]) {
//                            mask[n++] = !chunk.GetBlockDirect(x[0], x[1], x[2]).IsUnrendered && !(chunk.GetBlock(x[0] + q[0], x[1] + q[1], x[2] + q[2]).GetFaceType(op) == FaceType.Solid);
//                        }
//                    }
//                }else {
//                    for(x[v] = 0; x[v] < WorldChunk.Size; ++x[v]) {
//                        for(x[u] = 0; x[u] < WorldChunk.Size; ++x[u]) {
//                            mask[n++] = !chunk.GetBlockDirect(x[0], x[1], x[2]).IsUnrendered && !(chunk.GetBlockDirect(x[0] + q[0], x[1] + q[1], x[2] + q[2]).GetFaceType(op) == FaceType.Solid);
//                        }
//                    }
//                }
//                n = 0;
//                for(j = 0; j < WorldChunk.Size; ++j) {
//                    for(i = 0; i < WorldChunk.Size;) {
//                        if(mask[n]) {
//
//                            for(w = 1; i + w < WorldChunk.Size && mask[n + w]; ++w) ;
//
//                            // Compute height (this is slightly awkward
//                            var done = false;
//                            for(h = 1; j + h < WorldChunk.Size; ++h) {
//                                for(k = 0; k < w; ++k) {
//                                    if(!mask[n + k + h * WorldChunk.Size]) {
//                                        done = true;
//                                        break;
//                                    }
//                                }
//                                if(done) break;
//                            }
//
//                            ve[0, u] = i;
//                            ve[0, v] = j;
//                            ve[0, d] = x[d] + Math.Max(q[d], 0);
//                            ve[0, 3] = 0;
//                            ve[0, 4] = 0;
//
//                            ve[1, u] = i + w;
//                            ve[1, v] = j;
//                            ve[1, d] = x[d] + Math.Max(q[d], 0);
//                            ve[1, 3] = w;
//                            ve[1, 4] = 0;
//
//                            ve[2, u] = i + w;
//                            ve[2, v] = j + h;
//                            ve[2, d] = x[d] + Math.Max(q[d], 0);
//                            ve[2, 3] = w;
//                            ve[2, 4] = h;
//
//                            ve[3, u] = i;
//                            ve[3, v] = j + h;
//                            ve[3, d] = x[d] + Math.Max(q[d], 0);
//                            ve[3, 3] = 0;
//                            ve[3, 4] = h;
//
//                            for(int c = 0; c < order2.GetLength(1); c++) {
//                                vertices.Add(meshx - 0.5f + ve[order2[Math.Max(q[d], 0), c], 0]);
//                                vertices.Add(meshy - 0.5f + ve[order2[Math.Max(q[d], 0), c], 1]);
//                                vertices.Add(meshz - 0.5f + ve[order2[Math.Max(q[d], 0), c], 2]);
//                                vertices.Add(ve[order2[Math.Max(q[d], 0), c], 3]);
//                                vertices.Add(ve[order2[Math.Max(q[d], 0), c], 4]);
//                                vertices.Add(q[0]);
//                                vertices.Add(q[1]);
//                                vertices.Add(q[2]);
//                            }
//
//                            for(l = 0; l < h; ++l) {
//                                for(k = 0; k < w; ++k) {
//                                    mask[n + k + l * WorldChunk.Size] = false;
//                                }
//                            }
//
//
//                            i += w; n += w;
//                        } else {
//                            ++i;
//                            ++n;
//                        }
//                    }
//                }
//            }
//        }
//
//        clock.Stop();
//        ChunkRenderer.time += clock.ElapsedTicks;
//        ChunkRenderer.number++;
//        return vertices.ToArray();
//    }
//
//    private readonly static int[,] order2 = new int[2, 6]{
//        { 2, 1, 3, 0, 3, 1 },
//        { 0, 1, 2, 2, 3, 0 }
//    };
//}

//class CulledChunkMesher : IChunkMesher {
//
//    public float[] CreateChunkMesh(Chunk c, int x, int y, int z, int size) {
//      List<float> vertices = new List<float>(WorldChunk.Size * WorldChunk.Size * 2 * 6 * 8);
//      for(int x2 = 0; x2 < WorldChunk.Size; x2++) {
//          for(int y2 = 0; y2 < WorldChunk.Size; y2++) {
//              for(int z2 = 0; z2 < WorldChunk.Size; z2++) {
//                  if(!c.GetBlockDirect(x2, y2, z2).IsUnrendered) {
//                      foreach(Direction d2 in Enum.GetValues(typeof(Direction))) {
//                          if(!(c.GetBlock(x2 + d2.GetX(), y2 + d2.GetY(), z2 + d2.GetZ()).GetFaceType(d2.GetOpposite()) == FaceType.Solid)) {
//                              for(int j2 = 0; j2 < order.GetLength(1); j2++) {
//                                  vertices.Add(x + x2 + faces[d2.GetID(), order[0, j2], 0]);
//                                  vertices.Add(y + y2 + faces[d2.GetID(), order[0, j2], 1]);
//                                  vertices.Add(z + z2 + faces[d2.GetID(), order[0, j2], 2]);
//                                  vertices.Add(y + y2 + faces[d2.GetID(), order[0, j2], 3]);
//                                  vertices.Add(z + z2 + faces[d2.GetID(), order[0, j2], 4]);
//                                  vertices.Add(d2.GetX());
//                                  vertices.Add(d2.GetY());
//                                  vertices.Add(d2.GetZ());
//                              }
//                          }
//                      }
//                  }
//              }
//          }
//      }
//      return vertices.ToArray();
//  }
//
//    private static readonly float[,,] faces = new float[6, 4, 5]{
//                {
//                    {  0.5f, -0.5f,  0.5f, 0, 1 },
//                    {  0.5f, -0.5f, -0.5f, 0, 0 },
//                    {  0.5f,  0.5f, -0.5f, 1, 0 },
//                    {  0.5f,  0.5f,  0.5f, 1, 1 }
//                },
//                {
//                    { -0.5f,  0.5f,  0.5f, 0, 1 },
//                    {  0.5f,  0.5f,  0.5f, 1, 1 },
//                    {  0.5f,  0.5f, -0.5f, 1, 0 },
//                    { -0.5f,  0.5f, -0.5f, 0, 0 }
//                },
//                {
//                    {  0.5f, -0.5f, -0.5f, 1, 0 },
//                    { -0.5f, -0.5f, -0.5f, 0, 0 },
//                    { -0.5f,  0.5f, -0.5f, 0, 1 },
//                    {  0.5f,  0.5f, -0.5f, 1, 1 }
//                },
//                {
//                    { -0.5f, -0.5f, -0.5f, 0, 0 },
//                    { -0.5f, -0.5f,  0.5f, 0, 1 },
//                    { -0.5f,  0.5f,  0.5f, 1, 1 },
//                    { -0.5f,  0.5f, -0.5f, 1, 0 }
//                },
//                {
//                    {  0.5f, -0.5f,  0.5f, 1, 1 },
//                    { -0.5f, -0.5f,  0.5f, 0, 1 },
//                    { -0.5f, -0.5f, -0.5f, 0, 0 },
//                    {  0.5f, -0.5f, -0.5f, 1, 0 }
//                },
//                {
//                    { -0.5f, -0.5f,  0.5f, 0, 0 },
//                    {  0.5f, -0.5f,  0.5f, 1, 0 },
//                    {  0.5f,  0.5f,  0.5f, 1, 1 },
//                    { -0.5f,  0.5f,  0.5f, 0, 1 }
//                }
//            };
//    
//    private readonly static int[,] order = new int[2, 6]{
//                { 0, 1, 2, 2, 3, 0 },
//                { 1, 2, 3, 3, 0, 1 }
//            };
//    
//
//}