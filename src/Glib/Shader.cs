using System.Diagnostics;
using System.Numerics;
using System.Text;
using Silk.NET.OpenGLES;

namespace Glib;

[Serializable]
public class ShaderCompilationException : Exception
{
    public ShaderCompilationException() { }
    public ShaderCompilationException(string message) : base(message) { }
    public ShaderCompilationException(string message, System.Exception inner) : base(message, inner) { }
}

public class Shader : Resource
{
    private const string DefaultVertexSource = @"#version 300 es
    precision mediump float;

    layout(location=0) in vec3 glib_aPos;
    layout(location=1) in vec2 glib_aTexCoord;
    layout(location=2) in vec4 glib_aColor;

    out vec2 glib_texCoord;
    out vec4 glib_color;

    uniform mat4 glib_uMvp;
    
    void main() {
        gl_Position = glib_uMvp * vec4(glib_aPos.xyz, 1.0);
        glib_texCoord = glib_aTexCoord;
        glib_color = glib_aColor;
    }
    ";

    private const string DefaultFragmentSource = @"#version 300 es
    precision mediump float;

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
    public const string MatrixUniform = "glib_uMvp";

    private readonly uint programHandle;
    internal uint Handle => programHandle;

    internal static bool _debug = false;

    private readonly Dictionary<string, (uint loc, UniformType type)> _uniformLocs = [];
    private List<string> _textureUnits = [];
    private Texture[] _boundTextures;

    internal unsafe Shader(string? vsSource = null, string? fsSource = null)
    {
        var gl = RenderContext.Gl;
        
        var vsh = gl.CreateShader(ShaderType.VertexShader);
        gl.ShaderSource(vsh, vsSource ?? DefaultVertexSource);
        gl.CompileShader(vsh);
        if (gl.GetShader(vsh, GLEnum.CompileStatus) == 0)
        {
            var infoLog = gl.GetShaderInfoLog(vsh);
            gl.DeleteShader(vsh);
            throw new ShaderCompilationException("Vertex shader failed to compile: " + infoLog);
        }

        var fsh = gl.CreateShader(ShaderType.FragmentShader);
        gl.ShaderSource(fsh, fsSource ?? DefaultFragmentSource);
        gl.CompileShader(fsh);
        if (gl.GetShader(fsh, GLEnum.CompileStatus) == 0)
        {
            var infoLog = gl.GetShaderInfoLog(fsh);
            gl.DeleteShader(vsh);
            gl.DeleteShader(fsh);
            throw new ShaderCompilationException("Fragment shader failed to compile: " + infoLog);
        }

        programHandle = gl.CreateProgram();
        gl.AttachShader(programHandle, vsh);
        gl.AttachShader(programHandle, fsh);
        gl.LinkProgram(programHandle);

        if (gl.GetProgram(programHandle, GLEnum.LinkStatus) == 0)
        {
            var infoLog = gl.GetProgramInfoLog(programHandle);
            gl.DeleteShader(vsh);
            gl.DeleteShader(fsh);
            gl.DeleteProgram(programHandle);
            throw new ShaderCompilationException("Link error: " + infoLog);
        }

        gl.DeleteShader(vsh);
        gl.DeleteShader(fsh);

        // get list of uniforms
        int uniformCount = gl.GetProgram(programHandle, GLEnum.ActiveUniforms);
        var maxNameLen = gl.GetProgram(programHandle, GLEnum.ActiveUniformMaxLength);
        Span<byte> nameArr = stackalloc byte[maxNameLen];

        for (uint i = 0; i < uniformCount; i++)
        {
            gl.GetActiveUniform(programHandle, i, out uint len, out int size, out UniformType type, nameArr);
            var uniformLoc = (uint) gl.GetUniformLocation(programHandle, nameArr);
            var uName = Encoding.UTF8.GetString(nameArr[..(int)len]);
            _uniformLocs[uName] = (uniformLoc, type);

            switch (type)
            {
                case UniformType.Sampler1D:
                case UniformType.Sampler2D:
                case UniformType.Sampler3D:
                    _textureUnits.Add(uName);
                    break;
            }
        }

        _boundTextures = new Texture[_textureUnits.Count];
    }

    protected override void FreeResources(bool disposing)
    {
        RenderContext.Gl.DeleteProgram(programHandle);
    }

    /// <summary>
    /// Create a shader from source strings.
    /// </summary>
    /// <param name="vsName">The source of the vertex shader, or null to use the default one.</param>
    /// <param name="fsName">The source of the fragment shader, or null to use the default one.</param>
    /// <returns>A shader.</returns>
    /// <exception cref="ShaderCreationException">Thrown if the shader could not be created.</exception>
    public static Shader Create(string? vsSource = null, string? fsSource = null) => new(vsSource, fsSource);

    public bool HasUniform(string uName)
    {
        return _uniformLocs.ContainsKey(uName);
    }

    private (uint loc, UniformType type) GetUniformHandle(string uName)
    {
        if (!_uniformLocs.TryGetValue(uName, out var v))
            throw new ArgumentException($"Uniform '{uName}' does not exist!", nameof(uName));
        
        return v;
    }

    private uint GetUniformHandle(string uName, string inputType, UniformType expectedType)
    {
        var (handle, type) = GetUniformHandle(uName);

        if (type != expectedType)
            throw new ArgumentException($"{type} is incompatible with the uniform type {inputType}");
        
        return handle;
    }

    /// <summary>
    /// Set the value of the shader's uniform.
    /// </summary>
    public unsafe void SetUniform(string uName, float value)
    {
        var gl = RenderContext.Gl;
        gl.Uniform1((int)GetUniformHandle(uName, "float", UniformType.Float), value);
    }

    /// <summary>
    /// Set the value of the shader's uniform.
    /// </summary>
    public unsafe void SetUniform(string uName, Vector2 value)
    {
        var gl = RenderContext.Gl;
        gl.Uniform2((int)GetUniformHandle(uName, "Vector2", UniformType.FloatVec2), value);
    }

    /// <summary>
    /// Set the value of the shader's uniform.
    /// Submits vec4(value.X, value.Y, value.Z, 0.0)
    /// </summary>
    public unsafe void SetUniform(string uName, Vector3 value)
    {
        var gl = RenderContext.Gl;
        gl.Uniform3((int)GetUniformHandle(uName, "Vector3", UniformType.FloatVec3), value);
    }

    /// <summary>
    /// Set the value of the shader's uniform.
    /// </summary>
    public unsafe void SetUniform(string uName, Vector4 value)
    {
        var gl = RenderContext.Gl;
        gl.Uniform4((int)GetUniformHandle(uName, "Vector4", UniformType.FloatVec4), value);
    }

    /// <summary>
    /// Set the value of the shader's uniform.
    /// </summary>
    public unsafe void SetUniform(string uName, Color value)
    {
        var gl = RenderContext.Gl;
        gl.Uniform4((int)GetUniformHandle(uName, "Vector4", UniformType.FloatVec4), new Vector4(value.R, value.G, value.B, value.A));
    }

    /// <summary>
    /// Set the value of the shader's uniform.
    /// </summary>
    public void SetUniform(string uName, Matrix4x4 matrix)
    {
        var gl = RenderContext.Gl;
        var handle = (int)GetUniformHandle(uName, "Matrix4x4", UniformType.FloatMat4);

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
        
        gl.UniformMatrix4(handle, false, flat);
    }

    /// <summary>
    /// Set the value of the shader's uniform.
    /// </summary>
    public unsafe void SetUniform(string uName, Matrix2x2 matrix)
    {
        var gl = RenderContext.Gl;
        var (handle, type) = GetUniformHandle(uName);

        if (type == UniformType.FloatMat3)
        {
            Span<float> flat = [
                matrix.M11,
                matrix.M12,
                0f,

                matrix.M21,
                matrix.M22,
                0f,

                0f, 0f, 1f
            ];

            gl.UniformMatrix3((int)handle, false, flat);
        }
        else if (type == UniformType.FloatMat4)
        {
            Span<float> flat = [
                matrix.M11,
                matrix.M12,
                0f, 0f,

                matrix.M21,
                matrix.M22,
                0f, 0f,

                0f, 0f, 1f, 0f,
                0f, 0f, 0f, 1f
            ];

            gl.UniformMatrix4((int)handle, false, flat);
        }
        else
        {
            throw new ArgumentException($"Matrix2x2 is incompatible with the uniform type {type}");
        }
    }
    
    /// <summary>
    /// Set the value of the shader's uniform.
    /// </summary>
    public unsafe void SetUniform(string uName, Matrix3x3 matrix)
    {
        var gl = RenderContext.Gl;
        var (handle, type) = GetUniformHandle(uName);

        if (type == UniformType.FloatMat3)
        {
            Span<float> flat = [
                matrix.M11,
                matrix.M12,
                matrix.M13,

                matrix.M21,
                matrix.M22,
                matrix.M23,

                matrix.M31,
                matrix.M32,
                matrix.M33
            ];

            gl.UniformMatrix3((int)handle, false, flat);
        }
        else if (type == UniformType.FloatMat4)
        {
            Span<float> flat = [
                matrix.M11,
                matrix.M12,
                matrix.M13,
                0f,

                matrix.M21,
                matrix.M22,
                matrix.M23,
                0f,

                matrix.M31,
                matrix.M32,
                matrix.M33,
                0f,

                0f, 0f, 0f, 1f
            ];
            
            gl.UniformMatrix4((int)handle, false, flat);
        }
        else
        {
            throw new ArgumentException($"Matrix3x3 is incompatible with the uniform type {type}");
        }
    }

    /// <summary>
    /// Set the value of the shader's uniform. This is only valid for the shader
    /// if it is currently active.
    /// </summary>
    public unsafe void SetUniform(string uName, Texture texture)
    {
        GetUniformHandle(uName, "Texture", UniformType.Sampler2D); // just need the type check
        var unit = _textureUnits.IndexOf(uName);
        Debug.Assert(unit >= 0);
        _boundTextures[unit] = texture;
    }

    internal uint ActivateTextures(Texture placeholderTexture)
    {
        var gl = RenderContext.Gl;

        for (int i = 0; i < _textureUnits.Count; i++)
        {
            var handle = GetUniformHandle(_textureUnits[i], "Texture", UniformType.Sampler2D);
            var texture = _boundTextures[i] ?? placeholderTexture;
            var texHandle = texture.Handle;
            /*if (!texHandle.Valid)
            {
                RenderContext.LogError("Texture handle was invalid!");
                texture = placeholderTexture;
            }*/

            gl.ActiveTexture((GLEnum)((int)GLEnum.Texture0 + i));
            gl.BindTexture(GLEnum.Texture2D, texture.Handle);
            gl.Uniform1((int)handle, i);
        }

        return programHandle;
    }
}