using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using SurviveCore.DirectX;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace SurviveCore.Gui.Text{
    public class Font : IDisposable{
   
        private readonly ShaderResourceView texture;
        private readonly ShaderResourceView chardata;
        private readonly Dictionary<char, CharInfo> charinfo;
        private readonly Dictionary<(char,char), int> kerning;
        private float lineheight;
        private readonly Vector2 pagesize;

        public Vector2 Size => pagesize;
        public ShaderResourceView Texture => texture;
        public ShaderResourceView CharData => chardata;
        
        public Font(Device device, string path) {
            List<CharRenderData> charbufferdata = new List<CharRenderData>();
            charinfo = new Dictionary<char, CharInfo>();
            kerning = new Dictionary<(char, char), int>();
            string pagefile = "";
            foreach (string line in File.ReadAllLines(path)) {
                string[] tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length <= 0)
                    continue;
                if (tokens[0].Equals("common")) {
                    foreach(string subline in tokens) {
                        string[] keyvalue = subline.Split('=', StringSplitOptions.RemoveEmptyEntries);
                        if(keyvalue.Length != 2)
                            continue;
                        switch (keyvalue[0]) {
                            case "lineHeight":
                                lineheight = int.Parse(keyvalue[1]);
                                break;
                            case "base":
                                break;
                            case "scaleW":
                                pagesize.X = int.Parse(keyvalue[1]);
                                break;
                            case "scaleH":
                                pagesize.Y = int.Parse(keyvalue[1]);
                                break;
                            case "pages":
                                if (Int32.Parse(keyvalue[1]) != 1)
                                    throw new ArgumentException("only fonts with exactly 1 page are supported!");
                                break;
                            case "packed":
                                break;
                        }
                    }
                }
                if (tokens[0].Equals("page")) {
                    foreach(string subline in tokens) {
                        string[] keyvalue = subline.Split('=', StringSplitOptions.RemoveEmptyEntries);
                        if(keyvalue.Length != 2)
                            continue;
                        switch (keyvalue[0]) {
                            case "id":
                                break;
                            case "file":
                                pagefile = Path.Combine(Path.GetDirectoryName(path), keyvalue[1].Substring(1, keyvalue[1].Length - 2));
                                break;
                        }
                    }
                }
                if (tokens[0].Equals("char")) {
 
                    char c = ' ';
                    int x = 0, y = 0, w = 0, h = 0, xo = 0, yo = 0, xa = 0;
                    
                    foreach(string subline in tokens) {
                        string[] keyvalue = subline.Split('=', StringSplitOptions.RemoveEmptyEntries);
                        if(keyvalue.Length != 2)
                            continue;
                        switch (keyvalue[0]) {
                            case "id":
                                c = (char) int.Parse(keyvalue[1]);
                                break;
                            case "x":
                                x = int.Parse(keyvalue[1]);
                                break;
                            case "y":
                                y = int.Parse(keyvalue[1]);
                                break;
                            case "width":
                                w = int.Parse(keyvalue[1]);
                                break;
                            case "height":
                                h = int.Parse(keyvalue[1]);
                                break;
                            case "xoffset":
                                xo = int.Parse(keyvalue[1]);
                                break;
                            case "yoffset":
                                yo = int.Parse(keyvalue[1]);
                                break;
                            case "xadvance":
                                xa = int.Parse(keyvalue[1]);
                                break;
                            case "page":
                                break;
                            case "chnl":
                                break;
                        }
                    }
                    charinfo.Add(c, new CharInfo(charbufferdata.Count, xo, yo, xa));
                    charbufferdata.Add(new CharRenderData(x,y,w,h,pagesize));
                }
                if (tokens[0].Equals("kerning")) {
                    char c1 = ' ', c2 = ' ';
                    int a = 0;
                    foreach(string subline in tokens) {
                        string[] keyvalue = subline.Split('=', StringSplitOptions.RemoveEmptyEntries);
                        if(keyvalue.Length != 2)
                            continue;
                        switch (keyvalue[0]) {
                            case "first":
                                c1 = (char) int.Parse(keyvalue[1]);
                                break;
                            case "second":
                                c2 = (char) int.Parse(keyvalue[1]);
                                break;
                            case "amount":
                                a = int.Parse(keyvalue[1]);
                                break;
                        }
                    }
                    kerning.Add((c1,c2),a);
                }
            }
            texture = DDSLoader.LoadDDS(device, pagefile);
            Buffer b = Buffer.Create(device, charbufferdata.ToArray(), new BufferDescription(
                charbufferdata.Count * Marshal.SizeOf<CharRenderData>(), 
                ResourceUsage.Immutable, BindFlags.ShaderResource, 
                CpuAccessFlags.None, ResourceOptionFlags.BufferStructured, 
                Marshal.SizeOf<CharRenderData>()));
            chardata = new ShaderResourceView(device, b);
            b.Dispose();
        }

        public CharInfo GetCharInfo(char c) {
            return charinfo[c];
        }
        
        public int GetKerning((char,char) c) {
            return kerning.GetValueOrDefault(c,0);
        }
        
        public void Dispose() {
            texture.Dispose();
            chardata.Dispose();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct CharRenderData {
            private Vector2 TexMin;
            private Vector2 TexSize;
            private Vector2 Size;
            private readonly Vector2 Unused;
            
            public CharRenderData(float x, float y, float w, float h, Vector2 pagesize) : this() {
                TexMin = new Vector2(x, y) / pagesize;
                TexSize = new Vector2(w, h) / pagesize;
                Size = new Vector2(w, h);
            }
        }
        
   }

    public struct CharInfo {
        public readonly int RenderId;
        public readonly Vector2 Pos;
        public readonly int Advance;
        
        public CharInfo(int id, float xOffset, float yOffset, int advance) {
            RenderId = id;
            Pos = new Vector2(xOffset, yOffset);
            Advance = advance;
        }
    }
    
}
