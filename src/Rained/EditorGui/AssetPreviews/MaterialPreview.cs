namespace Rained.EditorGui.AssetPreviews;
using System.Numerics;
using Raylib_cs;
using ImGuiNET;
using Rained.Assets;
using Rained.Rendering;

class MaterialPreview : IDisposable
{
    private RlManaged.Texture2D? _loadedMatPreview;
    private string? _activeMatPreview = null;

    public void Dispose()
    {
        _loadedMatPreview?.Dispose();
        _loadedMatPreview = null;
    }

    public void RenderPreviewTooltip(string materialName)
    {
        if (_activeMatPreview != materialName)
        {
            _activeMatPreview = materialName;
            _loadedMatPreview?.Dispose();
            _loadedMatPreview = RlManaged.Texture2D.Load(Path.Combine(Boot.AppDataPath, "assets", "mat-previews", materialName + ".png"));
        }

        if (_loadedMatPreview is not null && Raylib_cs.Raylib.IsTextureReady(_loadedMatPreview))
        {
            ImGui.BeginTooltip();
            ImGuiExt.ImageSize(_loadedMatPreview, _loadedMatPreview.Width * Boot.PixelIconScale, _loadedMatPreview.Height * Boot.PixelIconScale);
            ImGui.EndTooltip();
        }
    }
}