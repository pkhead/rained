using System.Diagnostics;
using Bgfx_cs;
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
    /// True if the texture data can be read to the CPU.
    /// </summary>
    public bool Readable = false;

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
                    Readable = false,
                },

                // depth renderbuffer
                new()
                {
                    Attachment = AttachmentPoint.Depth,
                    Useable = false,
                    Readable = false,
                }
            ]
        };
    }
}

public class Framebuffer : Resource
{
    public readonly int Width, Height;

    private Bgfx.FrameBufferHandle fbo;
    private readonly Texture[] _attachmentTexs;

    internal Bgfx.FrameBufferHandle Handle => fbo;

    internal unsafe Framebuffer(FramebufferConfiguration config)
    {
        Width = config.Width;
        Height = config.Height;

        var attachments = new Bgfx.Attachment[config.Attachments.Count];
        _attachmentTexs = new Texture[config.Attachments.Count];

        for (int i = 0; i < config.Attachments.Count; i++)
        {
            var attachConfig = config.Attachments[i];
            var canUse = attachConfig.Useable;

            var texFormat = attachConfig.Attachment switch
            {
                AttachmentPoint.Color => Bgfx.TextureFormat.RGBA8,
                AttachmentPoint.Depth => Bgfx.TextureFormat.D16,
                _ => throw new ArgumentException("Invalid AttachmentPoint enum", nameof(config))
            };

            var texFlags = canUse ? Bgfx.TextureFlags.Rt : Bgfx.TextureFlags.RtWriteOnly;
            var samplerFlags = Bgfx.SamplerFlags.UClamp | Bgfx.SamplerFlags.VClamp | Bgfx.SamplerFlags.MinAnisotropic | Bgfx.SamplerFlags.MagAnisotropic;
            
            if (!Bgfx.is_texture_valid(0, false, 1, texFormat, (ulong)texFlags | (ulong)samplerFlags))
            {
                throw new UnsupportedRendererOperationException("Could not create framebuffer attachment");
            }

            var handle = Bgfx.create_texture_2d(
                (ushort)Width, (ushort)Height,
                false, 1,
                texFormat,
                (ulong)texFlags | (ulong)samplerFlags,
                null
            );
            if (!handle.Valid)
                throw new UnsupportedRendererOperationException("Could not create framebuffer attachment");

            Bgfx.Attachment attachment = new();
            Bgfx.attachment_init(
                &attachment,
                handle,
                canUse ? Bgfx.Access.Write : Bgfx.Access.Read,
                0, 1, 0,
                (byte)Bgfx.ResolveFlags.AutoGenMips
            );

            attachments[i] = attachment;

            if (attachConfig.Attachment == AttachmentPoint.Color && attachConfig.Readable)
            {
                _attachmentTexs[i] = new ReadableTexture(
                    Width, Height,
                    texFormat,
                    handle
                );
            }
            else
            {
                _attachmentTexs[i] = new Texture(
                    Width, Height,
                    texFormat,
                    handle
                );
            }
        }

        fixed (Bgfx.Attachment* ptr = attachments)
        {
            if (!Bgfx.is_frame_buffer_valid((byte)config.Attachments.Count, ptr))
                throw new UnsupportedRendererOperationException("Framebuffer configuration is invalid");        

            fbo = Bgfx.create_frame_buffer_from_attachment((byte)config.Attachments.Count, ptr, false);
            if (!fbo.Valid)
                throw new UnsupportedRendererOperationException("Framebiffer is invalid");
        }
    }

    internal unsafe Framebuffer(Window window)
    {
        RenderContext.GetHandles(window.SilkWindow, out nint nwh, out _, out _);
        fbo = Bgfx.create_frame_buffer_from_nwh((void*)nwh, (ushort)window.PixelWidth, (ushort)window.PixelHeight, Bgfx.TextureFormat.RGBA8, Bgfx.TextureFormat.D16);
        if (!fbo.Valid)
            throw new Exception("Could not create framebuffer from window");
        
        _attachmentTexs = [];
        Width = window.PixelWidth;
        Height = window.PixelHeight;
    }

    public static Framebuffer Create(FramebufferConfiguration config) => new(config);
    public static Framebuffer Create(int width, int height) => new(FramebufferConfiguration.Standard(width, height));

    protected override void FreeResources(bool disposing)
    {
        for (int i = 0; i < _attachmentTexs.Length; i++)
        {
            _attachmentTexs[i].Dispose();
            _attachmentTexs[i] = null!;
        }

        Bgfx.destroy_frame_buffer(fbo);
    }

    /// <summary>
    /// Get the attached texture at the given slot
    /// </summary>
    /// <param name="slot"></param>
    /// <returns>The framebuffer texture attachment</returns>
    /// <exception cref="ArgumentException">Thrown if the attachment at the slot does not exist.</exception>
    public Texture GetTexture(int slot)
    {
        if (slot < 0 || slot >= _attachmentTexs.Length) throw new ArgumentException("The attachment does not exist");
        var a = _attachmentTexs[slot].Handle.idx;
        var b = Bgfx.get_texture(fbo, (byte)slot).idx;
        Debug.Assert(a == b);
        return _attachmentTexs[slot];
    }
}