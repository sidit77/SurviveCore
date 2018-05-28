using SharpDX.D3DCompiler;
using SharpDX.DXGI;
using System;
using System.Diagnostics;
using System.IO;

namespace AssetBuilder
{
    class Program
    {

        public static string srcdir;
        public static string destdir;

        static void Main(string[] args)
        {

            Console.WriteLine("Asset dest: " + Path.GetFullPath(args[0]));
            destdir = Path.GetFullPath(args[0]);
            Console.WriteLine("Asset src : " + Path.GetFullPath(args[1]));
            srcdir = Path.GetFullPath(args[1]);
            
            AOTextureGenerator.GenerateFile("Assets/Textures/AmbientOcclusion.dds");
            
            CompileShader("Assets/Shader/Gui.hlsl"  , "vs", "ps");
            CompileShader("Assets/Shader/World.hlsl", "vs", "ps");

            CompressTexture("Assets/Textures/Gui.png", Format.R8G8B8A8_UNorm);
            CompressTextureFolder("Assets/Textures/Blocks/", Format.BC1_UNorm);
            
            Copy("Assets/Gui/Fonts/Abel.dds");
            Copy("Assets/Gui/Fonts/Abel.fnt");
        }

        private static void CompileShader(string src, params string[] shadertypes)
        {
            Console.Write("Compiling Shader " + src + ": ");
            foreach(string shadertype in shadertypes)
            {
                CompilationResult code = ShaderBytecode.CompileFromFile(Path.Combine(srcdir, src), shadertype.ToUpper(), shadertype.ToLower() + "_5_0");

                if (code.HasErrors){
                    Console.WriteLine(code.Message);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(destdir, src)));
                using (FileStream fs = File.Create(Path.Combine(destdir, Path.GetDirectoryName(src), Path.GetFileNameWithoutExtension(src) + "." + shadertype.ToLower() + ".fxo"))){
                    code.Bytecode.Save(fs);
                }
            }
            Console.WriteLine("Done");
        }

        private static void Copy(string path) {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(destdir, path)));
            File.Copy(Path.Combine(srcdir, path), Path.Combine(destdir, path), true);
        }
        
        private static void CompressTexture(string path, Format format)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(destdir, path)));
            Process
                .Start(
                    Path.Combine(srcdir, "Tools/texconv.exe"), 
                    string.Format("-f {0} -y -nologo -srgbi -srgbo -o {1} {2}", 
                    format.ToString().ToUpper(),
                    destdir, 
                    Path.Combine(srcdir, path)))
                .WaitForExit();
        }

        private static void CompressTextureFolder(string path, Format format)
        {
            
            string filename = Path.GetDirectoryName(Path.Combine(destdir, path)) + ".dds";
            Directory.CreateDirectory(Path.GetDirectoryName(filename));
            Process
                .Start(
                    Path.Combine(srcdir, "Tools/texassemble.exe"),
                    string.Format("array -y -nologo -srgbi -srgbo -o {1} {2}",
                    format.ToString().ToUpper(),
                    filename,
                    String.Join(" ", Directory.EnumerateFiles(Path.Combine(srcdir, path)))))
                .WaitForExit();
            Process
                .Start(
                    Path.Combine(srcdir, "Tools/texconv.exe"),
                    string.Format("-f {0} -y -nologo -srgbi -srgbo -o {1} {2}",
                    format.ToString().ToUpper(),
                    Path.GetDirectoryName(filename),
                    filename))
                .WaitForExit();

            using (StreamWriter fs = File.CreateText(Path.GetDirectoryName(Path.Combine(destdir, path)) + ".txt"))
            {
                foreach(string s in Directory.EnumerateFiles(Path.Combine(srcdir, path))){
                    fs.WriteLine(Path.GetFileName(s));
                }
            }

        }

    }
}
