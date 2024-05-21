using Silk.NET.OpenGL;

namespace Glib;

public enum AttachmentPoint
{
    Color, Depth, Stencil,
    DepthAndStencil
}

/// <summary>
/// Configuration for a framebuffer attachment
/// </summary>
public struct AttachmentConfig
{
    public AttachmentPoint Attachment = AttachmentPoint.Color;

    /// <summary>
    /// True if this attachment can be readable
    /// as a texture.
    /// </summary>
    public bool CanRead = true;

    /// <summary>
    /// The attachment point index. Is a number in the range
    /// [0, 32). Used only for AttachmentPoint.Color.
    /// </summary>
    public uint Index = 0;

    public AttachmentConfig() {}
}

public struct FramebufferConfiguration
{
    public int Width;
    public int Height;
    public List<AttachmentConfig> Attachments = [];

    public FramebufferConfiguration(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public void AddAttachment(AttachmentConfig config)
    {
        Attachments.Add(config);
    }
}

public class Framebuffer : GLResource
{
    private readonly GL gl;
    private readonly uint fbo;
    private readonly Texture[] textures;
    private readonly int[] textureIndices;
    private readonly uint[] renderBuffers;

    //private readonly AttachmentConfig[] attachmentConfig;

    internal uint Handle => fbo;

    internal unsafe Framebuffer(GL gl, FramebufferConfiguration config)
    {
        this.gl = gl;

        fbo = gl.GenFramebuffer();
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

        //attachmentConfig = [..config.Attachments];

        textureIndices = new int[config.Attachments.Count];

        int numTextures = 0;
        int numRenderBuffers = 0;
        int j = 0;
        for (int i = 0; i < config.Attachments.Count; i++)
        {
            if (config.Attachments[i].CanRead)
            {
                numTextures++;
                textureIndices[i] = j++;
            }
            else
            {
                numRenderBuffers++;
                textureIndices[i] = -1;
            }
        }

        textures = new Texture[numTextures];
        renderBuffers = new uint[numRenderBuffers];

        int textureIndex = 0;
        int rbIndex = 0;
        for (int i = 0; i < textureIndices.Length; i++)
        {
            var attachConfig = config.Attachments[i];
            if (attachConfig.Index < 0 || attachConfig.Index >= 32)
            {
                throw new ArgumentOutOfRangeException(nameof(config));
            }

            GLEnum attachEnum, format;
            switch (attachConfig.Attachment)
            {
                case AttachmentPoint.Color:
                    attachEnum = (GLEnum)((int)GLEnum.ColorAttachment0 + attachConfig.Index);
                    format = GLEnum.Rgba;
                    break;
                
                case AttachmentPoint.Depth:
                    attachEnum = GLEnum.DepthAttachment;
                    format = GLEnum.DepthComponent;
                    break;

                case AttachmentPoint.Stencil:
                    attachEnum = GLEnum.StencilAttachment;
                    format = GLEnum.StencilIndex;
                    break;

                case AttachmentPoint.DepthAndStencil:
                    attachEnum = GLEnum.DepthStencilAttachment;
                    format = GLEnum.Depth24Stencil8;
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(config));
            }

            if (attachConfig.CanRead)
            {
                var tex = new Texture(gl, config.Width, config.Height, format);
                gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachEnum, GLEnum.Texture2D, tex.TextureHandle, 0);
                textures[textureIndex++] = tex;
            }
            else
            {
                var rbo = gl.GenRenderbuffer();
                gl.BindRenderbuffer(GLEnum.Renderbuffer, rbo);
                gl.RenderbufferStorage(GLEnum.Renderbuffer, format, (uint)config.Width, (uint)config.Height);
                gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, attachEnum, GLEnum.Renderbuffer, rbo);
                renderBuffers[rbIndex++] = rbo;
            }
        }

        if (gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
        {
            throw new Exception("Framebuffer is not complete!");
        }

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    protected override void FreeResources(bool disposing)
    {
        if (disposing)
        {
            gl.DeleteFramebuffer(fbo);
            
            foreach (var tex in textures)
                tex.Dispose();

            gl.DeleteRenderbuffers(renderBuffers);
        }
        else
        {
            QueueFreeHandle(gl.DeleteFramebuffer, fbo);
            foreach (var buf in renderBuffers)
                QueueFreeHandle(gl.DeleteRenderbuffer, buf);
        }
    }

    /// <summary>
    /// Get the attached texture at the given slot
    /// </summary>
    /// <param name="slot"></param>
    /// <returns>The framebuffer texture attachment</returns>
    /// <exception cref="Exception">Thrown if the attachment at the slot is a render buffer.</exception>
    public Texture GetTexture(int slot)
    {
        int idx = textureIndices[slot];
        if (idx == -1) throw new Exception($"The attachment at slot {slot} is a render buffer");
        return textures[idx];
    }
}