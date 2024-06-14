using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;

namespace Glib;

[Serializable]
public class ShaderCompilationException : Exception
{
    public ShaderCompilationException() { }
    public ShaderCompilationException(string message) : base(message) { }
    public ShaderCompilationException(string message, System.Exception inner) : base(message, inner) { }
}

public class Shader : GLResource
{
    private const string DefaultVertexSource = @"
    #version 330 core
    layout (location = 0) in vec3 glib_aPos;
    layout (location = 1) in vec2 glib_aTexCoord;
    layout (location = 2) in vec4 glib_aColor;

    out vec2 glib_texCoord;
    out vec4 glib_color;

    uniform mat4 glib_uMatrix;
    
    void main() {
        gl_Position = glib_uMatrix * vec4(glib_aPos.xyz, 1.0);
        glib_texCoord = glib_aTexCoord;
        glib_color = glib_aColor;
    }
    ";

    private const string DefaultFragmentSource = @"
    #version 330 core

    in vec2 glib_texCoord;
    in vec4 glib_color;
    out vec4 glib_fragColor;

    uniform sampler2D glib_uTexture; 
    uniform vec4 glib_uColor;
    
    void main() {
        glib_fragColor = texture(glib_uTexture, glib_texCoord) * glib_color * glib_uColor;
    }
    ";

    /// <summary>
    /// The name of the texture uniform set by Glib.
    /// </summary>
    public const string TextureUniform = "glib_uTexture";

    /// <summary>
    /// The name of the color uniform set by Glib.
    /// </summary>
    public const string ColorUniform = "glib_uColor";

    /// <summary>
    /// The name of the matrix uniform set by Glib.
    /// </summary>
    public const string MatrixUniform = "glib_uMatrix";

    private readonly uint shaderProgram;
    private readonly GL gl;
    
    private List<uint> textureLocs = [];

    internal Shader(GL gl, string? vsSource = null, string? fsSource = null)
    {
        this.gl = gl;

        // create vertex shader
        var vShader = gl.CreateShader(GLEnum.VertexShader);
        gl.ShaderSource(vShader, vsSource ?? DefaultVertexSource);
        gl.CompileShader(vShader);

        // check for compilation error
        string infoLog = gl.GetShaderInfoLog(vShader);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            throw new ShaderCompilationException(infoLog);
        }

        // create fragment shader
        var fShader = gl.CreateShader(GLEnum.FragmentShader);
        gl.ShaderSource(fShader, fsSource ?? DefaultFragmentSource);
        gl.CompileShader(fShader);

        // check for compilation error
        infoLog = gl.GetShaderInfoLog(fShader);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            throw new ShaderCompilationException(infoLog);
        }

        // combining the shaders under one program
        shaderProgram = gl.CreateProgram();
        gl.AttachShader(shaderProgram, vShader);
        gl.AttachShader(shaderProgram, fShader);
        gl.LinkProgram(shaderProgram);

        // check for link error
        gl.GetProgram(shaderProgram, GLEnum.LinkStatus, out var status);
        if (status == 0)
        {
            throw new ShaderCompilationException(gl.GetProgramInfoLog(shaderProgram));
        }

        // delete the no longer useful individual shaders
        gl.DetachShader(shaderProgram, vShader);
        gl.DetachShader(shaderProgram, fShader);
        gl.DeleteShader(vShader);
        gl.DeleteShader(fShader);

        // get uniform data
        unsafe
        {
            int uniformCount = 0;
            gl.GetProgram(shaderProgram, GLEnum.ActiveUniforms, &uniformCount);

            Span<byte> nameArr = stackalloc byte[64];

            uint j = 0;
            for (uint i = 0; i < uniformCount; i++)
            {
                gl.GetActiveUniform(shaderProgram, i, out uint len, out int size, out UniformType type, nameArr);
                /*string name = System.Text.Encoding.UTF8.GetString(nameArr[..(int)len]);
                string typeName = type switch
                {
                    UniformType.Int => "int",
                    UniformType.Float => "float",
                    UniformType.FloatVec2 => "vec2",
                    UniformType.FloatVec3 => "vec3",
                    UniformType.FloatVec4 => "vec4",
                    UniformType.FloatMat4 => "mat4",
                    UniformType.Sampler2D => "sampler2D",
                    _ => "???"
                };

                Console.WriteLine($"Shader ID: {shaderProgram}, name: {name}, type: {typeName} size: {size}");*/

                switch (type)
                {
                    case UniformType.Sampler1D:
                    case UniformType.Sampler2D:
                    case UniformType.Sampler3D:
                        textureLocs.Add(j);
                        break;
                }

                j += (uint)size;
            }
        }
    }

    protected override void FreeResources(bool disposing)
    {
        QueueFreeHandle(gl.DeleteProgram, shaderProgram);
    }

    internal void Use(GL gl) {
        gl.UseProgram(shaderProgram);
    }

    public bool HasUniform(string uName)
    {
        var loc = gl.GetUniformLocation(shaderProgram, uName);
        return loc >= 0;
    }

    private int GetUniformLocation(string uName)
    {
        var loc = gl.GetUniformLocation(shaderProgram, uName);
        if (loc < 0) throw new Exception($"Uniform '{uName}' does not exist!");
        return loc;
    }

    /// <summary>
    /// Set the value of the shader's uniform. This is only valid for the shader
    /// if it is currently active.
    /// </summary>
    public void SetUniform(string uName, float value)
    {
        gl.Uniform1(GetUniformLocation(uName), value);
    }

    /// <summary>
    /// Set the value of the shader's uniform. This is only valid for the shader
    /// if it is currently active.
    /// </summary>
    public void SetUniform(string uName, int value)
    {
        gl.Uniform1(GetUniformLocation(uName), value);
    }

    /// <summary>
    /// Set the value of the shader's uniform. This is only valid for the shader
    /// if it is currently active.
    /// </summary>
    public void SetUniform(string uName, ReadOnlySpan<int> values)
    {
        gl.Uniform1(GetUniformLocation(uName), values);
    }

    /// <summary>
    /// Set the value of the shader's uniform. This is only valid for the shader
    /// if it is currently active.
    /// </summary>
    public void SetUniform(string uName, Vector2 value)
    {
        gl.Uniform2(GetUniformLocation(uName), value);
    }

    /// <summary>
    /// Set the value of the shader's uniform. This is only valid for the shader
    /// if it is currently active.
    /// </summary>
    public unsafe void SetUniform(string uName, ReadOnlySpan<Vector2> values)
    {
        fixed (float* floats = MemoryMarshal.Cast<Vector2, float>(values))
        {
            gl.Uniform2(GetUniformLocation(uName), (uint)values.Length, floats);
        }
    }

    /// <summary>
    /// Set the value of the shader's uniform. This is only valid for the shader
    /// if it is currently active.
    /// </summary>
    public void SetUniform(string uName, Vector3 value)
    {
        gl.Uniform3(GetUniformLocation(uName), value);
    }

    /// <summary>
    /// Set the value of the shader's uniform. This is only valid for the shader
    /// if it is currently active.
    /// </summary>
    public unsafe void SetUniform(string uName, ReadOnlySpan<Vector3> values)
    {
        fixed (float* floats = MemoryMarshal.Cast<Vector3, float>(values))
        {
            gl.Uniform3(GetUniformLocation(uName), (uint)values.Length, floats);
        }
    }

    /// <summary>
    /// Set the value of the shader's uniform. This is only valid for the shader
    /// if it is currently active.
    /// </summary>
    public void SetUniform(string uName, Vector4 value)
    {
        gl.Uniform4(GetUniformLocation(uName), value);
    }

    /// <summary>
    /// Set the value of the shader's uniform. This is only valid for the shader
    /// if it is currently active.
    /// </summary>
    public unsafe void SetUniform(string uName, ReadOnlySpan<Vector4> values)
    {
        fixed (float* floats = MemoryMarshal.Cast<Vector4, float>(values))
        {
            gl.Uniform4(GetUniformLocation(uName), (uint)values.Length, floats);
        }
    }

    /// <summary>
    /// Set the value of the shader's uniform. This is only valid for the shader
    /// if it is currently active.
    /// </summary>
    public void SetUniform(string uName, Color value)
    {
        gl.Uniform4(GetUniformLocation(uName), value.R, value.G, value.B, value.A);
    }

    /// <summary>
    /// Set the value of the shader's uniform. This is only valid for the shader
    /// if it is currently active.
    /// </summary>
    public unsafe void SetUniform(string uName, ReadOnlySpan<Color> values)
    {
        fixed (float* floats = MemoryMarshal.Cast<Color, float>(values))
        {
            gl.Uniform4(GetUniformLocation(uName), (uint)values.Length, floats);
        }
    }

    /// <summary>
    /// Set the value of the shader's uniform. This is only valid for the shader
    /// if it is currently active.
    /// </summary>
    public void SetUniform(string uName, Matrix4x4 matrix)
    {
        var loc = gl.GetUniformLocation(shaderProgram, uName);
        if (loc < 0) throw new Exception($"Uniform '{uName}' does not exist!");

        Span<float> flat =
        [
            matrix.M11,
            matrix.M12,
            matrix.M13,
            matrix.M14,
            matrix.M21,
            matrix.M22,
            matrix.M23,
            matrix.M24,
            matrix.M31,
            matrix.M32,
            matrix.M33,
            matrix.M34,
            matrix.M41,
            matrix.M42,
            matrix.M43,
            matrix.M44,
        ];
        
        gl.UniformMatrix4(loc, false, flat);
    }

    /// <summary>
    /// Set the value of the shader's uniform. This is only valid for the shader
    /// if it is currently active.
    /// </summary>
    public void SetUniform(string uName, Texture texture)
    {
        var loc = gl.GetUniformLocation(shaderProgram, uName);
        if (loc < 0) throw new Exception($"Uniform '{uName}' does not exist!");

        int texUnit = textureLocs.IndexOf((uint)loc);
        if (texUnit < 0) throw new Exception("The uniform type is not a sampler");

        texture.Activate((uint)texUnit);
        gl.Uniform1(loc, texUnit);
    }
}