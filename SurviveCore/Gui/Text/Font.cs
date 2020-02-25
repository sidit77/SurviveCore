using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using SharpDX.Direct3D11;
using SurviveCore.DirectX;

namespace SurviveCore.Gui.Text{
    public class Font : IDisposable{
   
        private readonly ShaderResourceView texture;
        private readonly Dictionary<char, CharInfo> charinfo;
        private readonly Dictionary<(char,char), int> kerning;
        private readonly int lineheight;
        private readonly int size;

        public int Size => size;
        public int LineHeight => lineheight;
        public ShaderResourceView Texture => texture;
        
        public Font(Device device, string path) {
            charinfo = new Dictionary<char, CharInfo>();
            kerning = new Dictionary<(char, char), int>();
            string pagefile = "";
            Size pagesize = new Size();
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
                                size = int.Parse(keyvalue[1]);
                                break;
                            case "scaleW":
                                pagesize.Width = int.Parse(keyvalue[1]);
                                break;
                            case "scaleH":
                                pagesize.Height = int.Parse(keyvalue[1]);
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
                    charinfo.Add(c, new CharInfo(x,y,w,h,xo,yo,xa,pagesize));
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
        }

        public CharInfo GetCharInfo(char c) {
            if (charinfo.TryGetValue(c, out CharInfo ci))
                return ci;
            return charinfo['?'];
        }
        
        
        
        public int GetKerning((char,char) c) {
            return kerning.GetValueOrDefault(c,0);
        }
        
        public void Dispose() {
            texture.Dispose();
        }
        
        public struct CharInfo {
            public readonly Rectangle Positon;
            public readonly RectangleF Texture;
            public readonly int Advance;
            
            public CharInfo(int x, int y, int w, int h, int xOffset, int yOffset, int advance, Size pagesize) {
                Positon = new Rectangle(xOffset, yOffset, w, h);
                Texture = new RectangleF((float)x / pagesize.Width, (float)y / pagesize.Height, (float)w / pagesize.Width, (float)h / pagesize.Height);
                Advance = advance;
            }
        }
        
   }

    
    
}
