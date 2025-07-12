namespace Rained.EditorGui.AssetPreviews;
using System.Numerics;
using Raylib_cs;
using Rained.Assets;

class PropPreview
{
    private RlManaged.RenderTexture2D? _previewTexture;
    public RlManaged.RenderTexture2D? Texture => _previewTexture;
    private PropInit? _curPropPreview = null;

    public void UpdatePreview(PropInit prop)
    {
        var texWidth = (int)(prop.Width * 20f);
        var texHeight = (int)(prop.Height * 20f);

        if (_previewTexture is null || _curPropPreview != prop)
        {
            _curPropPreview = prop;

            _previewTexture?.Dispose();
            _previewTexture = RlManaged.RenderTexture2D.Load(texWidth, texHeight);   
        }

        Raylib.BeginTextureMode(_previewTexture);
        Raylib.ClearBackground(Color.Blank);
        Raylib.BeginShaderMode(Shaders.PropShader);
        {
            var propTexture = RainEd.Instance.AssetGraphics.GetPropTexture(prop);
            for (int depth = prop.LayerCount - 1; depth >= 0; depth--)
            {
                float whiteFade = Math.Clamp(depth / 16f, 0f, 1f);
                Rectangle srcRect, dstRec;

                if (propTexture is not null)
                {
                    srcRect = prop.GetPreviewRectangle(0, depth);
                    dstRec = new Rectangle(Vector2.Zero, srcRect.Size);
                }
                else
                {
                    srcRect = new Rectangle(Vector2.Zero, 2.0f * Vector2.One);
                    dstRec = new Rectangle(Vector2.Zero, prop.Width * 20f, prop.Height * 20f);
                }

                var drawColor = new Color(255, (int)(whiteFade * 255f), 0, 0);

                if (propTexture is not null)
                {
                    propTexture.DrawRectangle(srcRect, dstRec, drawColor);
                }
                else
                {
                    Raylib.DrawTexturePro(
                        RainEd.Instance.PlaceholderTexture,
                        srcRect, dstRec,
                        Vector2.Zero, 0f,
                        drawColor   
                    );
                }
            }
        }
        Raylib.EndShaderMode();
        Raylib.EndTextureMode();
    }
}