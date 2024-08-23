using System.Diagnostics;
using System.Numerics;
using System.Text;
#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

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
#if GLES
    private const string ShaderPrelude = "#version 300 es\nprecision mediump float;\n";
#else
    private const string ShaderPrelude = "#version 330 core\n";
#endif

    private const string DefaultVertexSource = ShaderPrelude +
    """
    in vec3 a_pos;
    in vec2 a_texcoord0;
    in vec4 a_color0;

    out vec2 v_texcoord0;
    out vec4 v_color0;

    uniform mat4 u_mvp;
    
    void main() {
        gl_Position = u_mvp * vec4(a_pos, 1.0);
        v_texcoord0 = a_texcoord0;
        v_color0 = a_color0;
    }
    """;

    private const string DefaultFragmentSource = ShaderPrelude +
    """
    in vec2 v_texcoord0;
    in vec4 v_color0;

    out vec4 fragColor;

    uniform sampler2D u_texture0;
    uniform vec4 u_color;
    
    void main() {
        fragColor = texture(u_texture0, v_texcoord0) * v_color0 * u_color;
    }
    """;

    /// <summary>
    /// The name of the default texture uniform.
    /// </summary>
    public const string TextureUniform = "u_texture0";

    /// <summary>
    /// The name of the default color uniform.
    /// </summary>
    public const string ColorUniform = "u_color";

    /// <summary>
    /// The name of the model-view-projection matrix uniform.
    /// </summary>
    public const string MatrixUniform = "u_mvp";

    private readonly uint programHandle;
    internal uint Handle => programHandle;

    internal static bool _debug = false;

    private readonly Dictionary<string, (uint loc, UniformType type)> _uniformLocs = [];
    private List<string> _textureUnits = [];
    private Texture[] _boundTextures;

    private static Dictionary<string, uint> _shaderCache = [];

    private static uint? _max_attrib_index = null;

    private static void BindAttribLocation(GL gl, uint program, uint index, string name)
    {
        _max_attrib_index ??= (uint)gl.GetInteger(GetPName.MaxVertexAttribs);
        
        if (index < _max_attrib_index)
            gl.BindAttribLocation(program, index, name);        
    }

    private unsafe Shader(uint vsh, uint fsh)
    {
        var gl = RenderContext.Gl;

        programHandle = gl.CreateProgram();
        gl.AttachShader(programHandle, vsh);
        gl.AttachShader(programHandle, fsh);

        BindAttribLocation(gl, programHandle, (uint)AttributeName.Position, "a_pos");
        BindAttribLocation(gl, programHandle, (uint)AttributeName.Normal, "a_normal");
        BindAttribLocation(gl, programHandle, (uint)AttributeName.Tangent, "a_tangent");
        BindAttribLocation(gl, programHandle, (uint)AttributeName.Bitangent, "a_bitangent");
        BindAttribLocation(gl, programHandle, (uint)AttributeName.Color0, "a_color0");
        BindAttribLocation(gl, programHandle, (uint)AttributeName.Color1, "a_color1");
        BindAttribLocation(gl, programHandle, (uint)AttributeName.Color2, "a_color2");
        BindAttribLocation(gl, programHandle, (uint)AttributeName.Color3, "a_color3");
        BindAttribLocation(gl, programHandle, (uint)AttributeName.Indices, "a_indices");
        BindAttribLocation(gl, programHandle, (uint)AttributeName.Weight, "a_weight");
        BindAttribLocation(gl, programHandle, (uint)AttributeName.TexCoord0, "a_texcoord0");
        BindAttribLocation(gl, programHandle, (uint)AttributeName.TexCoord1, "a_texcoord1");
        BindAttribLocation(gl, programHandle, (uint)AttributeName.TexCoord2, "a_texcoord2");
        BindAttribLocation(gl, programHandle, (uint)AttributeName.TexCoord3, "a_texcoord3");
        BindAttribLocation(gl, programHandle, (uint)AttributeName.TexCoord4, "a_texcoord4");
        BindAttribLocation(gl, programHandle, (uint)AttributeName.TexCoord5, "a_texcoord5");
        BindAttribLocation(gl, programHandle, (uint)AttributeName.TexCoord6, "a_texcoord6");
        BindAttribLocation(gl, programHandle, (uint)AttributeName.TexCoord7, "a_texcoord7");
        gl.LinkProgram(programHandle);

        GlUtil.CheckError(gl, "Error creating program object");

        if (gl.GetProgram(programHandle, GLEnum.LinkStatus) == 0)
        {
            var infoLog = gl.GetProgramInfoLog(programHandle);
            gl.DeleteProgram(programHandle);
            throw new ShaderCompilationException("Link error: " + infoLog);
        }

        GlUtil.CheckError(gl, "Error creating program object");

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
        GlUtil.CheckError(gl, "Error parsing shader uniforms");
    }

    protected override void FreeResources(RenderContext rctx)
    {
        rctx.gl.DeleteProgram(programHandle);
    }

    /// <summary>
    /// Create a shader from source code.
    /// </summary>
    /// <param name="vsName">The source of the vertex shader, or null to use the default one.</param>
    /// <param name="fsName">The source of the fragment shader, or null to use the default one.</param>
    /// <returns>A shader.</returns>
    /// <exception cref="ShaderCreationException">Thrown if the shader could not be created.</exception>
    public static Shader Create(string? vsSource = null, string? fsSource = null)
    {
        var gl = RenderContext.Gl;

        uint vsh, fsh;
        vsh = fsh = 0;

        try
        {
            vsh = gl.CreateShader(ShaderType.VertexShader);
            gl.ShaderSource(vsh, vsSource ?? DefaultVertexSource);
            gl.CompileShader(vsh);
            if (gl.GetShader(vsh, GLEnum.CompileStatus) == 0)
            {
                var infoLog = gl.GetShaderInfoLog(vsh);
                throw new ShaderCompilationException("Vertex shader failed to compile: " + infoLog);
            }

            GlUtil.CheckError(RenderContext.Gl, "Error compiling vertex shader");

            fsh = gl.CreateShader(ShaderType.FragmentShader);
            gl.ShaderSource(fsh, fsSource ?? DefaultFragmentSource);
            gl.CompileShader(fsh);
            if (gl.GetShader(fsh, GLEnum.CompileStatus) == 0)
            {
                var infoLog = gl.GetShaderInfoLog(fsh);
                throw new ShaderCompilationException("Fragment shader failed to compile: " + infoLog);
            }

            GlUtil.CheckError(RenderContext.Gl, "Error compiling fragment shader");

            return new Shader(vsh, fsh);
        }
        finally
        {
            if (vsh != 0) gl.DeleteShader(vsh);
            if (fsh != 0) gl.DeleteShader(fsh);
        }
    }

    /// <summary>
    /// Create a shader from the name of a shader source file.
    /// </summary>
    /// <param name="vsName">The name of the vertex shader source, or null to use the default.</param>
    /// <param name="fsName">The name of the fragment shader source, or null to use the default.</param>
    /// <returns></returns>
    public static Shader Load(string? vsName, string? fsName)
    {
        var gl = RenderContext.Gl;
        var assembly = typeof(Shader).Assembly;

        string? vsSource = null;
        string? fsSource = null;
        uint vsh, fsh;

        vsName ??= "DEFAULT_VERT";
        fsName ??= "DEFAULT_FRAG";

        // get vertex source, or obtain from cache
        if (!_shaderCache.TryGetValue(vsName, out vsh))
        {
            vsh = 0;

            if (vsName == "DEFAULT_VERT")
            {
                vsSource = DefaultVertexSource;
            }
            else
            {
                using var vsStream = assembly.GetManifestResourceStream("Glib.shaders." + vsName)
                    ?? throw new ArgumentException($"Shader '{vsName}' does not exist", nameof(vsName));
                using var vsStreamReader = new StreamReader(vsStream);
                vsSource = vsStreamReader.ReadToEnd();
            }
        }

        // get fragment source, or obtain from cache
        if (!_shaderCache.TryGetValue(fsName, out fsh))
        {
            fsh = 0;

            if (fsName == "DEFAULT_FRAG")
            {
                fsSource = DefaultFragmentSource;
            }
            else
            {
                using var fsStream = assembly.GetManifestResourceStream("Glib.shaders." + fsName)
                    ?? throw new ArgumentException($"Shader '{fsName}' does not exist", nameof(fsName));
                using var fsStreamReader = new StreamReader(fsStream);
                fsSource = fsStreamReader.ReadToEnd();
            }
        }
        
        // compile vertex shader if not in cache
        if (vsh == 0)
        {
            if (vsSource is null) throw new UnreachableException();
            vsh = gl.CreateShader(ShaderType.VertexShader);
            gl.ShaderSource(vsh, vsSource);
            gl.CompileShader(vsh);
            if (gl.GetShader(vsh, GLEnum.CompileStatus) == 0)
            {
                var infoLog = gl.GetShaderInfoLog(vsh);
                gl.DeleteShader(vsh);
                throw new ShaderCompilationException("Vertex shader failed to compile: " + infoLog);
            }

            GlUtil.CheckError(RenderContext.Gl, "Error compiling vertex shader");

            _shaderCache[vsName!] = vsh;
        }

        // compile fragment shader if not cache
        if (fsh == 0)
        {
            if (fsSource is null) throw new UnreachableException();
            fsh = gl.CreateShader(ShaderType.FragmentShader);
            gl.ShaderSource(fsh, fsSource);
            gl.CompileShader(fsh);
            if (gl.GetShader(fsh, GLEnum.CompileStatus) == 0)
            {
                var infoLog = gl.GetShaderInfoLog(fsh);
                gl.DeleteShader(fsh);
                throw new ShaderCompilationException("Fragment shader failed to compile: " + infoLog);
            }

            GlUtil.CheckError(RenderContext.Gl, "Error compiling fragment shader");

            _shaderCache[fsName!] = fsh;
        }

        return new Shader(vsh, fsh);
    }

    internal static void ClearShaderCache()
    {
        var gl = RenderContext.Gl;

        foreach ((var name, var shader) in _shaderCache)
            gl.DeleteShader(shader);
        _shaderCache.Clear();
    }

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
    public void SetUniform(string uName, float value)
    {
        var gl = RenderContext.Gl;
        gl.Uniform1((int)GetUniformHandle(uName, "float", UniformType.Float), value);
        GlUtil.CheckError(RenderContext.Gl, "Could not set uniform");
    }

    /// <summary>
    /// Set the value of the shader's uniform.
    /// </summary>
    public void SetUniform(string uName, Vector2 value)
    {
        var gl = RenderContext.Gl;
        gl.Uniform2((int)GetUniformHandle(uName, "Vector2", UniformType.FloatVec2), value);
        GlUtil.CheckError(RenderContext.Gl, "Could not set uniform");
    }

    /// <summary>
    /// Set the value of the shader's uniform.
    /// Submits vec4(value.X, value.Y, value.Z, 0.0)
    /// </summary>
    public void SetUniform(string uName, Vector3 value)
    {
        var gl = RenderContext.Gl;
        gl.Uniform3((int)GetUniformHandle(uName, "Vector3", UniformType.FloatVec3), value);
        GlUtil.CheckError(RenderContext.Gl, "Could not set uniform");
    }

    /// <summary>
    /// Set the value of the shader's uniform.
    /// </summary>
    public void SetUniform(string uName, Vector4 value)
    {
        var gl = RenderContext.Gl;
        gl.Uniform4((int)GetUniformHandle(uName, "Vector4", UniformType.FloatVec4), value);
        GlUtil.CheckError(RenderContext.Gl, "Could not set uniform");
    }

    /// <summary>
    /// Set the value of the shader's uniform.
    /// </summary>
    public void SetUniform(string uName, Color value)
    {
        var gl = RenderContext.Gl;
        gl.Uniform4((int)GetUniformHandle(uName, "Vector4", UniformType.FloatVec4), new Vector4(value.R, value.G, value.B, value.A));
        GlUtil.CheckError(RenderContext.Gl, "Could not set uniform");
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
        GlUtil.CheckError(RenderContext.Gl, "Could not set uniform");
    }

    /// <summary>
    /// Set the value of the shader's uniform.
    /// </summary>
    public void SetUniform(string uName, Matrix2x2 matrix)
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
        GlUtil.CheckError(RenderContext.Gl, "Could not set uniform");
    }
    
    /// <summary>
    /// Set the value of the shader's uniform.
    /// </summary>
    public void SetUniform(string uName, Matrix3x3 matrix)
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
        GlUtil.CheckError(RenderContext.Gl, "Could not set uniform");
    }

    /// <summary>
    /// Set the value of the shader's uniform. This is only valid for the shader
    /// if it is currently active.
    /// </summary>
    public void SetUniform(string uName, Texture texture)
    {
        GetUniformHandle(uName, "Texture", UniformType.Sampler2D); // just need the type check
        var unit = _textureUnits.IndexOf(uName);
        Debug.Assert(unit >= 0);
        _boundTextures[unit] = texture;
        GlUtil.CheckError(RenderContext.Gl, "Could not set uniform");
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
            texture.Bind(gl);
            gl.Uniform1((int)handle, i);

            GlUtil.CheckError(RenderContext.Gl, "Error activating texture");
        }

        return programHandle;
    }
}