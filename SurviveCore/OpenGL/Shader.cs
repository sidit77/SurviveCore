using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;

namespace SurviveCore.OpenGL {

    class ShaderProgram : IDisposable{

        private static ShaderProgram current;
        public static ShaderProgram Current {
            get => current;
            set {
                current = value;
                GL.UseProgram(current?.ID ?? 0);
            }
        }

        private readonly int id;
        private Dictionary<string, int> uniforms;

        public ShaderProgram() {
            id = GL.CreateProgram();
            uniforms = new Dictionary<string, int>();
        }

        public int ID => id;

        public ShaderProgram Link() {
            GL.LinkProgram(id);

            GL.GetProgram(id, GetProgramParameterName.LinkStatus, out int status);

            if(status != 1) {
                GL.GetProgramInfoLog(id, out string info);
                Console.WriteLine(info);
                Dispose();
            }

            return this;
        }

        public ShaderProgram AttachShader(Shader shader, bool delete = true) {
            GL.AttachShader(id, shader.ID);
            if(delete)
                shader.Dispose();
            return this;
        }

        public ShaderProgram Bind() {
            Current = this;
            return this;
        }

        public int GetUniform(string name) {
            if(!uniforms.ContainsKey(name)) {
                uniforms.Add(name, GL.GetUniformLocation(id, name));
            }
            return uniforms[name];
        }

        public int GetUniformBlock(string name) {
            if(!uniforms.ContainsKey("ub_" + name)) {
                uniforms.Add("ub_" + name, GL.GetUniformBlockIndex(id, name));
            }
            return uniforms["ub_" + name];
        }

        public void BindUniformBlock(string name, int slot) {
            BindUniformBlock(GetUniformBlock(name), slot);
        }

        public void BindUniformBlock(int location, int slot) {
            GL.UniformBlockBinding(id, location, slot);
        }

        public void Dispose() {
            GL.DeleteProgram(id);
            uniforms = null;
        }

        #region UNIFORMS_LOCATION

        public void SetUniform(int location, int value) {
            GL.ProgramUniform1(id, location, value);
        }

        public void SetUniform(int location, float value) {
            GL.ProgramUniform1(id, location, value);
        }

        public void SetUniform(int location, int xValue, int yValue) {
            GL.ProgramUniform2(id, location, xValue, yValue);
        }

        public void SetUniform(int location, float xValue, float yValue) {
            GL.ProgramUniform2(id, location, xValue, yValue);
        }

        public void SetUniform(int location, Vector2 vec) {
            GL.ProgramUniform2(id, location, vec.X, vec.Y);
        }

        public void SetUniform(int location, int xValue, int yValue, int zValue) {
            GL.ProgramUniform3(id, location, xValue, yValue, zValue);
        }

        public void SetUniform(int location, float xValue, float yValue, float zValue) {
            GL.ProgramUniform3(id, location, xValue, yValue, zValue);
        }

        public void SetUniform(int location, Vector3 vec) {
            GL.ProgramUniform3(id, location, vec.X, vec.Y, vec.Z);
        }

        public void SetUniform(int location, float xValue, float yValue, float zValue, float wValue) {
            GL.ProgramUniform4(id, location, xValue, yValue, zValue, wValue);
        }

        public void SetUniform(int location, int xValue, int yValue, int zValue, int wValue) {
            GL.ProgramUniform4(id, location, xValue, yValue, zValue, wValue);
        }

        public void SetUniform(int location, Vector4 vec) {
            GL.ProgramUniform4(id, location, vec.X, vec.Y, vec.Z, vec.W);
        }

        public void SetUniform(int location, Color4 color) {
            GL.ProgramUniform4(id, location, color.R, color.G, color.B, color.A);
        }

        public void SetUniform(int location, bool transpose, ref Matrix4x4 matrix) {
            //GL.UniformMatrix4(location, transpose, Matrix4x4.);
            unsafe{
                fixed (float* matrix_ptr = &matrix.M11) {
                    GL.ProgramUniformMatrix4(id, location, 1, transpose, matrix_ptr);
                }
            }
        }

        //public void SetUniform(int location, bool transpose, Matrix3 matrix) {
        //    GL.UniformMatrix3(location, transpose, ref matrix);
        //}
        //
        //public void SetUniform(int location, bool transpose, Matrix2 matrix) {
        //    GL.UniformMatrix2(location, transpose, ref matrix);
        //}

        #endregion

        #region UNIFORMS_NAME

        public void SetUniform(string name, int value) {
            SetUniform(GetUniform(name), value);
        }

        public void SetUniform(string name, float value) {
            SetUniform(GetUniform(name), value);
        }

        public void SetUniform(string name, int xValue, int yValue) {
            SetUniform(GetUniform(name), xValue, yValue);
        }

        public void SetUniform(string name, float xValue, float yValue) {
            SetUniform(GetUniform(name), xValue, yValue);
        }

        public void SetUniform(string name, Vector2 vec) {
            SetUniform(GetUniform(name), vec);
        }

        public void SetUniform(string name, int xValue, int yValue, int zValue) {
            SetUniform(GetUniform(name), xValue, yValue, zValue);
        }

        public void SetUniform(string name, float xValue, float yValue, float zValue) {
            SetUniform(GetUniform(name), xValue, yValue, zValue);
        }

        public void SetUniform(string name, Vector3 vec) {
            SetUniform(GetUniform(name), vec);
        }

        public void SetUniform(string name, float xValue, float yValue, float zValue, float wValue) {
            SetUniform(GetUniform(name), xValue, yValue, zValue, wValue);
        }

        public void SetUniform(string name, int xValue, int yValue, int zValue, int wValue) {
            SetUniform(GetUniform(name), xValue, yValue, zValue, wValue);
        }

        public void SetUniform(string name, Vector4 vec) {
            SetUniform(GetUniform(name), vec);
        }

        public void SetUniform(string name, bool transpose, ref Matrix4x4 matrix) {
            SetUniform(GetUniform(name), transpose, ref matrix);
        }

        //public void SetUniform(string name, bool transpose, ref Matrix3 matrix) {
        //    SetUniform(GetUniform(name), transpose, matrix);
        //}
        //
        //public void SetUniform(string name, bool transpose, ref Matrix2 matrix) {
        //    SetUniform(GetUniform(name), transpose, matrix);
        //}

        public void SetUniform(string name, Color4 color) {
            SetUniform(GetUniform(name), color);
        }

        #endregion

    }

    class Shader : IDisposable {

        private readonly int id;

        public Shader(ShaderType type, string source) {
            id = GL.CreateShader(type);

            GL.ShaderSource(id, source);
            GL.CompileShader(id);

            GL.GetShader(id, ShaderParameter.CompileStatus, out int status);

            if(status != 1) {
                GL.GetShaderInfoLog(id, out string info);
                Console.WriteLine(info);
                Dispose();
            }
        }

        public int ID => id;

        public void Dispose() {
            GL.DeleteShader(id);
        }
        
        public static Shader FromFile(string file, ShaderType type) {
            string line;
            using(StreamReader sr = File.OpenText(file)) {
                line = sr.ReadToEnd();
            }
            return new Shader(type, line);
        }

    }

}
