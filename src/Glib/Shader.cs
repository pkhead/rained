using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Bgfx_cs;

namespace Glib;

[Serializable]
public class ShaderCreationException : Exception
{
    public ShaderCreationException() { }
    public ShaderCreationException(string message) : base(message) { }
    public ShaderCreationException(string message, System.Exception inner) : base(message, inner) { }
}

public class Shader : Resource
{
    private const string DefaultVertexName = "default_vs";
    private const string DefaultFragmentName = "default_fs";

    /// <summary>
    /// The name of the texture uniform set by Glib.
    /// </summary>
    public const string TextureUniform = "glib_texture";

    /// <summary>
    /// The name of the color uniform set by Glib.
    /// </summary>
    public const string ColorUniform = "glib_color";

    /// <summary>
    /// The name of the matrix uniform set by Glib.
    /// </summary>
    public const string MatrixUniform = "glib_matrix";

    private readonly Bgfx.ProgramHandle programHandle;

    internal static bool _debug = false;

    private readonly Dictionary<string, (Bgfx.UniformHandle handle, Bgfx.UniformType type)> _uniformHandles = [];

    private List<string> _textureUnits = [];
    private Texture[] _boundTextures;

    internal unsafe Shader(string? vsName = null, string? fsName = null)
    {
        var vsh = LoadShader(vsName ?? DefaultVertexName);
        var fsh = LoadShader(fsName ?? DefaultFragmentName);
        programHandle = Bgfx.create_program(vsh, fsh, true);
        if (!programHandle.Valid)
        {
            throw new ShaderCreationException("Could not create program");
        }

        var uniformHandles = stackalloc Bgfx.UniformHandle[64];
        int uniformCount;

        void ParseUniformHandles()
        {
            Bgfx.UniformInfo uniformInfo = new();
            for (int i = 0; i < uniformCount; i++)
            {
                var handle = uniformHandles[i];
                Bgfx.get_uniform_info(handle, &uniformInfo);

                var uName = Marshal.PtrToStringAnsi((nint)uniformInfo.name)!;
                if (!_uniformHandles.ContainsKey(uName))
                {
                    _uniformHandles[uName] = (handle, uniformInfo.type);

                    if (uniformInfo.type == Bgfx.UniformType.Sampler)
                    {
                        _textureUnits.Add(uName);
                    }
                }
            }
        }

        // get uniform handles
        uniformCount = Bgfx.get_shader_uniforms(vsh, uniformHandles, 64);
        ParseUniformHandles();
        uniformCount = Bgfx.get_shader_uniforms(fsh, uniformHandles, 64);
        ParseUniformHandles();

        _boundTextures = new Texture[_textureUnits.Count];
    }

    protected override void FreeResources(bool disposing)
    {
        foreach (var v in _uniformHandles.Values)
        {
            Bgfx.destroy_uniform(v.handle);
        }

        Bgfx.destroy_program(programHandle);
    }

    /// <summary>
    /// Create a shader from named resources.
    /// </summary>
    /// <param name="vsName">The name of the vertex shader, or null to use the default one.</param>
    /// <param name="fsName">The name of the fragment shader, or null to use the default one.</param>
    /// <returns>A shader.</returns>
    /// <exception cref="ShaderCreationException">Thrown if the shader could not be created.</exception>
    public static Shader Create(string? vsName = null, string? fsName = null) => new(vsName, fsName);

    private static unsafe Bgfx.ShaderHandle LoadShader(string name)
    {
        string shaderClass = Bgfx.get_renderer_type() switch
        {
            Bgfx.RendererType.Noop or Bgfx.RendererType.Direct3D11 or Bgfx.RendererType.Direct3D12 => "d3d",
            Bgfx.RendererType.OpenGL => "glsl",
            Bgfx.RendererType.Vulkan => "spirv",
            _ => throw new ShaderCreationException($"No precompiled shaders for renderer {Bgfx.get_renderer_name(Bgfx.get_renderer_type())}!")
        };

        var assembly = typeof(Shader).Assembly;
        var shaderResourceName = $"Glib.shaders.{shaderClass}.{name}";
        var stream = assembly.GetManifestResourceStream(shaderResourceName)
            ?? throw new ShaderCreationException($"Shader {shaderResourceName} does not exist");
        
        using var memStream = new MemoryStream();
        stream.CopyTo(memStream);
        var shaderSrc = memStream.ToArray() ?? throw new ShaderCreationException($"Could not read {shaderResourceName}");

        var shader = Bgfx.create_shader(BgfxUtil.Load<byte>(shaderSrc));
        Bgfx.set_shader_name(shader, name, name.Length);
        return shader;
    }

    public bool HasUniform(string uName)
    {
        return _uniformHandles.ContainsKey(uName);
    }

    private (Bgfx.UniformHandle handle, Bgfx.UniformType type) GetUniformHandle(string uName)
    {
        if (!_uniformHandles.TryGetValue(uName, out var v))
        {
            throw new ArgumentException($"Uniform '{uName}' does not exist!", nameof(uName));
        }
        
        return v;
    }

    private Bgfx.UniformHandle GetUniformHandle(string uName, string inputType, Bgfx.UniformType expectedType)
    {
        var (handle, type) = GetUniformHandle(uName);

        if (type != expectedType)
            throw new ArgumentException($"{type} is incompatible with the uniform type {inputType}");
        
        return handle;
    }

    /// <summary>
    /// Set the value of the shader's uniform.
    /// Submits vec4(value, 0.0, 0.0, 0.0)
    /// </summary>
    public unsafe void SetUniform(string uName, float value)
    {
        Vector4 v = new(value, 0f, 0f, 0f);
        Bgfx.set_uniform(GetUniformHandle(uName, "float", Bgfx.UniformType.Vec4), &v, 1);
    }

    /// <summary>
    /// Set the value of the shader's uniform.
    /// Submits vec4(value.X, value.Y, 0.0, 0.0)
    /// </summary>
    public unsafe void SetUniform(string uName, Vector2 value)
    {
        Vector4 v = new(value.X, value.Y, 0f, 0f);
        Bgfx.set_uniform(GetUniformHandle(uName, "Vector2", Bgfx.UniformType.Vec4), &v, 1);
    }

    /// <summary>
    /// Set the value of the shader's uniform.
    /// Submits vec4(value.X, value.Y, value.Z, 0.0)
    /// </summary>
    public unsafe void SetUniform(string uName, Vector3 value)
    {
        Vector4 v = new(value.X, value.Y, value.Z, 0f);
        Bgfx.set_uniform(GetUniformHandle(uName, "Vector3", Bgfx.UniformType.Vec4), &v, 1);
    }

    /// <summary>
    /// Set the value of the shader's uniform.
    /// </summary>
    public unsafe void SetUniform(string uName, Vector4 value)
    {
        Bgfx.set_uniform(GetUniformHandle(uName, "Vector4", Bgfx.UniformType.Vec4), &value, 1);
    }

    /// <summary>
    /// Set the value of the shader's uniform.
    /// </summary>
    public unsafe void SetUniform(string uName, Color value)
    {
        Bgfx.set_uniform(GetUniformHandle(uName, "Color", Bgfx.UniformType.Vec4), &value, 1);
    }

    /// <summary>
    /// Set the value of the shader's uniform.
    /// </summary>
    public unsafe void SetUniform(string uName, Matrix4x4 matrix)
    {
        var handle = GetUniformHandle(uName, "Matrix4x4", Bgfx.UniformType.Mat4);

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
        
        fixed (float* data = flat)
        {
            Bgfx.set_uniform(handle, data, 16);
        }
    }

    /// <summary>
    /// Set the value of the shader's uniform.
    /// </summary>
    public unsafe void SetUniform(string uName, Matrix2x2 matrix)
    {
        var (handle, type) = GetUniformHandle(uName);

        if (type == Bgfx.UniformType.Mat3)
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

            fixed (float* data = flat)
            {
                Bgfx.set_uniform(handle, data, 1);
            }
        }
        else if (type == Bgfx.UniformType.Mat4)
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

            fixed (float* data = flat)
            {
                Bgfx.set_uniform(handle, data, 1);
            }
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
        var (handle, type) = GetUniformHandle(uName);

        if (type == Bgfx.UniformType.Mat3)
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

            fixed (float* data = flat)
            {
                Bgfx.set_uniform(handle, data, 1);
            }
        }
        else if (type == Bgfx.UniformType.Mat4)
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

            fixed (float* data = flat)
            {
                Bgfx.set_uniform(handle, data, 1);
            }
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
        var _ = GetUniformHandle(uName, "Texture", Bgfx.UniformType.Sampler); // just need the type check
        var unit = _textureUnits.IndexOf(uName);
        Debug.Assert(unit >= 0);
        _boundTextures[unit] = texture;
    }

    internal Bgfx.ProgramHandle Activate(Texture placeholderTexture)
    {
        for (int i = 0; i < _textureUnits.Count; i++)
        {
            var handle = GetUniformHandle(_textureUnits[i], "Texture", Bgfx.UniformType.Sampler);
            var texture = _boundTextures[i] ?? placeholderTexture;
            var texHandle = texture.Handle;
            if (!texHandle.Valid)
            {
                RenderContext.LogError("Texture handle was invalid!");
                texture = placeholderTexture;
            }

            var texFlags = Bgfx.SamplerFlags.None;

            if (texture.MinFilterMode == TextureFilterMode.Linear) texFlags |= Bgfx.SamplerFlags.MinAnisotropic;
            if (texture.MinFilterMode == TextureFilterMode.Nearest) texFlags |= Bgfx.SamplerFlags.MinPoint;

            if (texture.MagFilterMode == TextureFilterMode.Linear) texFlags |= Bgfx.SamplerFlags.MagAnisotropic;
            if (texture.MagFilterMode == TextureFilterMode.Nearest) texFlags |= Bgfx.SamplerFlags.MagPoint;

            if (texture.WrapModeU == TextureWrapMode.Clamp) texFlags |= Bgfx.SamplerFlags.UClamp;
            if (texture.WrapModeU == TextureWrapMode.Mirror) texFlags |= Bgfx.SamplerFlags.UMirror;

            if (texture.WrapModeV == TextureWrapMode.Clamp) texFlags |= Bgfx.SamplerFlags.VClamp;
            if (texture.WrapModeV == TextureWrapMode.Mirror) texFlags |= Bgfx.SamplerFlags.VMirror;

            Bgfx.set_texture((byte)i, handle, texHandle, (ushort)texFlags);
        }

        return programHandle;
    }
}