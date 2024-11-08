using System.Diagnostics;
#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif
namespace Glib;

public enum AttachmentPoint
{
    Color, Depth
}

/// <summary>
/// Configuration for a framebuffer attachment
/// </summary>
public struct AttachmentConfig
{
    public AttachmentPoint Attachment = AttachmentPoint.Color;

    /// <summary>
    /// True if shaders can read from this texture.
    /// </summary>
    public bool Useable = true;

    /// <summary>
    /// Color attachment index.
    /// </summary>
    public uint Index = 0;

    public AttachmentConfig() {}
}

public struct FramebufferConfiguration
{
    public int Width;
    public int Height;
    public List<AttachmentConfig> Attachments;

    public FramebufferConfiguration()
    {
        Width = 800;
        Height = 600;
        Attachments = [];
    }

    public FramebufferConfiguration(int width, int height)
    {
        Attachments = [];
        Width = width;
        Height = height;
    }

    public readonly FramebufferConfiguration AddAttachment(AttachmentConfig config)
    {
        Attachments.Add(config);
        return this;
    }

    public readonly Framebuffer Create() => Framebuffer.Create(this);

    public static FramebufferConfiguration Standard(int width, int height)
    {
        return new FramebufferConfiguration()
        {
            Width = width,
            Height = height,
            Attachments = [
                // color texture
                new()
                {
                    Attachment = AttachmentPoint.Color,
                    Useable = true,
                },

                // depth renderbuffer
                new()
                {
                    Attachment = AttachmentPoint.Depth,
                    Useable = false,
                }
            ]
        };
    }
}

public class Framebuffer : Resource
{
    public readonly int Width, Height;

    private readonly uint fbo;
    private readonly Texture?[] _attachmentTexs;
    private readonly List<uint> _renderBufHandles = [];

    internal uint Handle => fbo;

    internal unsafe Framebuffer(FramebufferConfiguration config)
    {
        Width = config.Width;
        Height = config.Height;

        if (Width <= 0 || Height <= 0)
            throw new InvalidOperationException("Width and height must be integers greater than 0");
        
        if (Width >= Texture.MaxSize || Height >= Texture.MaxSize)
            throw new UnsupportedOperationException("Width and height exceeds supported dimensions.");
        
        var gl = RenderContext.Gl;

        var oldDrawFb = (uint)gl.GetInteger(GetPName.DrawFramebufferBinding);
        var oldReadFb = (uint)gl.GetInteger(GetPName.ReadFramebufferBinding);

        //var attachments = new Bgfx.Attachment[config.Attachments.Count];
        _attachmentTexs = new Texture[config.Attachments.Count];

        fbo = gl.GenFramebuffer();
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
        GlUtil.CheckError(gl, "Error when binding framebuffer");

        for (int i = 0; i < config.Attachments.Count; i++)
        {
            var attachConfig = config.Attachments[i];

            GLEnum texFormat, attachPoint;
            switch (attachConfig.Attachment)
            {
                case AttachmentPoint.Color:
                    if (attachConfig.Index >= 16)
                        throw new ArgumentException("Color attachment index is out of range", nameof(config));
                    texFormat = GLEnum.Rgba;
                    attachPoint = (GLEnum)((int)GLEnum.ColorAttachment0 + attachConfig.Index);
                    break;
                
                case AttachmentPoint.Depth:
                    texFormat = GLEnum.DepthComponent16;
                    attachPoint = GLEnum.DepthAttachment;
                    break;
                
                default:
                    throw new ArgumentException("Invalid AttachmentPoint enum", nameof(config));
            }
            
            if (attachConfig.Useable)
            {
                var handle = gl.GenTexture();
                gl.BindTexture(GLEnum.Texture2D, handle);
                gl.TexImage2D(GLEnum.Texture2D, 0, (int)texFormat, (uint)Width, (uint)Height, 0, texFormat, GLEnum.UnsignedByte, null);
                GlUtil.CheckError(gl, "Could not create framebuffer texture attachment");
                
                var wrapMode = (int)GLEnum.ClampToEdge;
                var filterMode = (int)GLEnum.Linear;
                gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, ref wrapMode);
                gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, ref wrapMode);
                gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, ref filterMode);
                gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, ref filterMode);

                gl.FramebufferTexture2D(GLEnum.Framebuffer, attachPoint, GLEnum.Texture2D, handle, 0);
                GlUtil.CheckError(gl, "Could not create framebuffer texture attachment");

                if (attachConfig.Attachment == AttachmentPoint.Color)
                {
                    _attachmentTexs[i] = new ReadableTexture(Width, Height, texFormat, handle, this, attachConfig.Index);
                }
                else
                {
                    _attachmentTexs[i] = new Texture(Width, Height, texFormat, handle);
                }
            }
            else
            {
                var handle = gl.GenRenderbuffer();
                gl.BindRenderbuffer(GLEnum.Renderbuffer, handle);
                gl.RenderbufferStorage(GLEnum.Renderbuffer, texFormat, (uint)Width, (uint)Height);
                GlUtil.CheckError(gl, "Could not create framebuffer renderbuffer attachment");

                gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, attachPoint, GLEnum.Renderbuffer, handle);
                gl.BindRenderbuffer(GLEnum.Renderbuffer, 0);
                GlUtil.CheckError(gl, "Could not create framebuffer renderbuffer attachment");

                _renderBufHandles.Add(handle);
            }
        }

        var fbStatus = gl.CheckFramebufferStatus(GLEnum.Framebuffer);
        if (fbStatus != GLEnum.FramebufferComplete)
        {
            throw new UnsupportedOperationException($"Could not create framebuffer: {fbStatus}");
        }

        gl.BindFramebuffer(GLEnum.DrawFramebuffer, oldDrawFb);
        gl.BindFramebuffer(GLEnum.ReadFramebuffer, oldReadFb);
        GlUtil.CheckError(gl, "Error when unbinding framebuffer");
    }

    public static Framebuffer Create(FramebufferConfiguration config) => new(config);
    public static Framebuffer Create(int width, int height) => new(FramebufferConfiguration.Standard(width, height));

    protected override void FreeResources(RenderContext rctx)
    {
        for (int i = 0; i < _attachmentTexs.Length; i++)
        {
            _attachmentTexs[i]?.Dispose();
            _attachmentTexs[i] = null!;
        }

        foreach (var handle in _renderBufHandles)
        {
            rctx.gl.DeleteRenderbuffer(handle);
        }

        rctx.gl.DeleteFramebuffer(fbo);
    }

    /// <summary>
    /// Get the attached texture at the given slot
    /// </summary>
    /// <param name="slot"></param>
    /// <returns>The framebuffer texture attachment</returns>
    /// <exception cref="ArgumentException">Thrown if the attachment at the slot does not exist or is a renderbuffer.</exception>
    public Texture GetTexture(int slot)
    {
        if (slot < 0 || slot >= _attachmentTexs.Length) throw new ArgumentException("The attachment does not exist");
        var tex = _attachmentTexs[slot] ?? throw new ArgumentException("The attachment is a renderbuffer");
        return tex;
    }
}