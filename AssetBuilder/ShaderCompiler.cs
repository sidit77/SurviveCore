using SharpDX.D3DCompiler;
using System;
using System.IO;

namespace AssetBuilder{
    class ShaderCompiler
    {
        public static void Compile(string shader, string method, string version, string output)
        {
            CompilationResult code = ShaderBytecode.CompileFromFile(shader, method, version);

            if (code.HasErrors) {
                Console.WriteLine(code.Message);

            }
            else {
                using (FileStream fs = File.Create(output))
                {
                    code.Bytecode.Save(fs);
                }
            }
        }
    }
}
