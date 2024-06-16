using System.Numerics;
namespace Raylib_cs;

static class RlExt
{
    // Not actually a function in Raylib. I just added this because
    // drawing the contents of a framebuffer will be upside down.
    public static void DrawRenderTextureV(RenderTexture2D target, Vector2 pos, Color tint)
    {
        var tex = target.Texture;
        Raylib.DrawTexturePro(
            texture: target.Texture,
            source: new Rectangle(0f, tex.Height, tex.Width, -tex.Height),
            dest: new Rectangle(pos, tex.Width, tex.Height),
            origin: Vector2.Zero,
            rotation: 0f,
            tint: tint
        );
    }
    
    public static void DrawRenderTexture(RenderTexture2D target, int posX, int posY, Color tint)
        => DrawRenderTextureV(target, new Vector2(posX, posY), tint);
}