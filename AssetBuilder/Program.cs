using System;
using System.IO;

namespace AssetBuilder
{
    class Program
    {

        private static string srcdir;
        private static string destdir;

        static void Main(string[] args)
        {
            Console.WriteLine("Asset dest: " + Path.GetFullPath(args[0]));
            destdir = args[0];
            Console.WriteLine("Asset src : " + Path.GetFullPath(args[1]));
            srcdir = args[1];
            Console.WriteLine("Generating AO Texture");
            AOTextureGenerator.GenerateFile(Path.Combine(destdir, "Assets/Textures/AmbientOcclusion.dds"));

            Console.WriteLine("Compiling shader");
            CompileShader("Assets/Shader/Gui.hlsl"  , "vs", "ps");
            CompileShader("Assets/Shader/World.hlsl", "vs", "ps");
            
        }

        private static void CompileShader(string src, params string[] shadertypes)
        {
            foreach(string shadertype in shadertypes)
            {
                ShaderCompiler.Compile(Path.Combine(srcdir, src), shadertype.ToUpper(), shadertype.ToLower() + "_5_0", Path.Combine(destdir, Path.GetDirectoryName(src), Path.GetFileNameWithoutExtension(src) + "." + shadertype.ToLower() + ".fxo"));
            }
        }

    }
}
