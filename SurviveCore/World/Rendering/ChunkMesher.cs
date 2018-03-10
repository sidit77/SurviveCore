using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SurviveCore.World.Rendering {
    
    public class ChunkMesher {
        
        private readonly List<Vertex> vertices;

        private readonly  BlockFace[] mask;
        private readonly  int[] p = new int[3];
        private readonly  int[] q = new int[3];
        private readonly  int[] s = new int[3];
        private int u, v, d, axis;
        private int n, k, l;
        private bool done;
        
        public ChunkMesher() {
            vertices = new List<Vertex>();
            mask = new BlockFace[Chunk.Size * Chunk.Size];
        }

        public Vertex[] GenerateMesh(Chunk chunk) {
            if(chunk.isEmpty())
                return null;
            
            int x = chunk.Location.WX;
            int y = chunk.Location.WY;
            int z = chunk.Location.WZ;
            
            vertices.Clear();

            for (axis = 0; axis < 6; axis++) {
                d = axis % 3;
                u = (d + 1) % 3;
                v = (d + 2) % 3;

                q[u] = 0;
                q[v] = 0;
                q[d] = axis < 3 ? 1 : -1;

                int f1 = !chunk.IsFull() ? 0          : (axis >= 3 ? 0 : Chunk.Size - 1);
                int f2 = !chunk.IsFull() ? Chunk.Size : (axis >= 3 ? 1 : Chunk.Size);
                
                for(p[d] = f1; p[d] < f2; p[d]++) {
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
                                    vertices.Add(new Vertex(
                                        x - 0.5f + p[0] + FaceVertices[k + 0] * s[0],
                                        y - 0.5f + p[1] + FaceVertices[k + 1] * s[1],
                                        z - 0.5f + p[2] + FaceVertices[k + 2] * s[2],
                                        FaceVertices[k + 3] * s[d == 2 ? u : v],
                                        FaceVertices[k + 4] * s[d == 2 ? v : u],
                                        (byte)mask[n].AoID));
                                    //vertices2.Add((byte)axis);
                                    //vertices2.Add((byte)mask[n].TextureID);
                                    //vertices2.Add((byte)mask[n].AoID);
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

            return vertices.Count <= 0 ? null : vertices.ToArray();
        }

        private struct BlockFace {
            public bool Visible;
            public int TextureID;
            public int AoID;

            public bool CanConnect(BlockFace bf) {
                return TextureID == bf.TextureID && AoID == bf.AoID;
            }
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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vertex {
        public Vector3 Position;
        public Vector2 Texcoord;
        public byte AmbientOcclusion;

        public Vertex(Vector3 position, Vector2 texcoord, byte ao) {
            Position = position;
            Texcoord = texcoord;
            AmbientOcclusion = ao;
        }

        public Vertex(float x, float y, float z, float s, float t, byte ao) {
            Position = new Vector3(x,y,z);
            Texcoord = new Vector2(s, t);
            AmbientOcclusion = ao;
        }
        
    }
    
}