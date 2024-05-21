using System.Numerics;
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
    layout (location = 0) in vec3 aPos;
    layout (location = 1) in vec2 aTexCoord;
    layout (location = 2) in vec4 aColor;

    out vec2 TexCoord;
    out vec4 VertexColor;

    uniform mat4 uTransformMatrix;
    
    void main() {
        gl_Position = uTransformMatrix * vec4(aPos.xyz, 1.0);
        TexCoord = aTexCoord;
        VertexColor = aColor;
    }
    ";

    private const string DefaultFragmentSource = @"
    #version 330 core

    in vec4 VertexColor;
    in vec2 TexCoord;

    out vec4 FragColor;
    
    void main() {
        FragColor = VertexColor;
    }
    ";

    private readonly uint shaderProgram;
    private readonly GL gl;

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

    public void SetUniform(string uName, float value)
    {
        var loc = gl.GetUniformLocation(shaderProgram, uName);
        if (loc < 0) throw new Exception($"Uniform '{uName}' does not exist!");
        gl.Uniform1(loc, value);
    }

    public void SetUniform(string uName, int value)
    {
        var loc = gl.GetUniformLocation(shaderProgram, uName);
        if (loc < 0) throw new Exception($"Uniform '{uName}' does not exist!");
        gl.Uniform1(loc, value);
    }

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
}