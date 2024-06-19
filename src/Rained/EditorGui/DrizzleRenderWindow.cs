using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using System.Diagnostics;
using DrizzleRender = RainEd.Drizzle.DrizzleRender;
using RenderState = RainEd.Drizzle.DrizzleRender.RenderState;
using System.Runtime.CompilerServices;
using System.Globalization;

namespace RainEd;

class DrizzleRenderWindow : IDisposable
{
    public readonly DrizzleRender? drizzleRenderer;
    private bool isOpen = false;
    private readonly RlManaged.Texture2D?[]? previewLayers = null;
    private RlManaged.Texture2D? previewBlackout1 = null;
    private RlManaged.Texture2D? previewBlackout2 = null;
    private readonly RlManaged.RenderTexture2D previewComposite;
    private bool needUpdateTextures = false;

    private const string LayerPreviewShaderSource = @"
        #version 330

        in vec2 glib_texCoord;
        in vec4 glib_color;

        uniform sampler2D glib_uTexture;
        uniform vec4 glib_uColor;

        out vec4 glib_fragColor;

        void main()
        {
            vec4 texelColor = texture(glib_uTexture, glib_texCoord);
            bool isWhite = texelColor.r == 1.0 && texelColor.g == 1.0 && texelColor.b == 1.0;
            vec3 correctColor = texelColor.bgr;
            
            glib_fragColor = vec4(
                mix(correctColor, vec3(1.0), glib_color.r * 0.8),
                1.0 - float(isWhite)
            ) * glib_uColor;
        }    
    ";

    private const string LayerPreviewLightShaderSource = @"
        #version 330

        in vec2 glib_texCoord;
        in vec4 glib_color;

        uniform sampler2D glib_uTexture;
        uniform vec4 glib_uColor;

        out vec4 glib_fragColor;

        void main()
        {
            vec4 texelColor = texture(glib_uTexture, glib_texCoord);
            bool isWhite = texelColor.r == 1.0;
            vec3 correctColor = texelColor.bgr;
            
            glib_fragColor = vec4(
                vec3(1.0, 0.0, 0.0),
                1.0 - float(isWhite)
            ) * glib_uColor;
        }    
    ";

    private readonly RlManaged.Shader layerPreviewShader;
    private readonly RlManaged.Shader layerPreviewLightShader;

    private Stopwatch elapsedStopwatch = new();

    public DrizzleRenderWindow(bool onlyGeo)
    {
        try
        {
            drizzleRenderer = new DrizzleRender(onlyGeo);

            if (!onlyGeo)
            {
                drizzleRenderer.PreviewUpdated += () =>
                {
                    needUpdateTextures = true;
                };
                previewLayers = new RlManaged.Texture2D[30];

            }
        }
        catch (Exception e)
        {
            RainEd.Logger.Error("Error occured when initializing render:\n{ErrorMessage}", e);
        }

        layerPreviewShader = RlManaged.Shader.LoadFromMemory(null, LayerPreviewShaderSource);
        layerPreviewLightShader = RlManaged.Shader.LoadFromMemory(null, LayerPreviewLightShaderSource);

        previewComposite = RlManaged.RenderTexture2D.Load(
            (int)Camera.WidescreenSize.X * 20,
            (int)Camera.WidescreenSize.Y * 20
        );

        if (!onlyGeo)
        {
            Raylib.BeginTextureMode(previewComposite);
            Raylib.ClearBackground(Color.White);
            Raylib.EndTextureMode();
        }
    }

    public void Dispose()
    {
        drizzleRenderer?.Dispose();
        previewComposite.Dispose();

        if (previewLayers is not null)
        {
            for (int i = 0; i < previewLayers.Length; i++)
            {
                previewLayers[i]?.Dispose();
            }
        }

        previewBlackout1?.Dispose();
        previewBlackout2?.Dispose();

        layerPreviewShader.Dispose();
    }

    private void ShowControlButtons(ref bool doClose)
    {
        bool cancelDisabled = true;
        bool closeDisabled = false;
        bool revealDisabled = true;

        if (drizzleRenderer is not null)
        {
            // yes i know this code is a bit iffy
            cancelDisabled =
                drizzleRenderer.State == RenderState.Cancelling ||
                drizzleRenderer.State == RenderState.Errored || drizzleRenderer.IsDone;
            
            closeDisabled = !drizzleRenderer.IsDone && drizzleRenderer.State != RenderState.Errored;
            revealDisabled = !drizzleRenderer.IsDone || drizzleRenderer.State == RenderState.Canceled;
        }

        // cancel button (disabled if cancelling/canceled)
        if (cancelDisabled)
            ImGui.BeginDisabled();
        
        // render preview lags a lot, so make it so the user doesn't have to let go of
        // mouse button in order to activate the cancel button
        ImGui.Button("Cancel");
        if (ImGui.IsItemClicked())
            drizzleRenderer?.Cancel();
        
        if (cancelDisabled)
            ImGui.EndDisabled();

        // close button (disabled if render process is not done)
        if (closeDisabled)
            ImGui.BeginDisabled();
        
        ImGui.SameLine();
        if (ImGui.Button("Close"))
        {
            doClose = true;
            ImGui.CloseCurrentPopup();
        }

        if (closeDisabled)
            ImGui.EndDisabled();

        // show in file browser button
        if (revealDisabled)
            ImGui.BeginDisabled();
        
        ImGui.SameLine();
        if (ImGui.Button("Show In File Browser"))
            RainEd.Instance.ShowPathInSystemBrowser(Path.Combine(
                RainEd.Instance.AssetDataPath,
                "Levels",
                Path.GetFileNameWithoutExtension(RainEd.Instance.CurrentFilePath) + ".txt"
            ), true);
        
        if (revealDisabled)
            ImGui.EndDisabled();
    }

    private void ShowStatusText()
    {
        bool showTime = true;

        // status sidebar
        if (drizzleRenderer is null || drizzleRenderer.State == RenderState.Errored)
        {
            ImGui.Text("An error occured!\nCheck the logs for more info.");
            if (elapsedStopwatch.IsRunning) elapsedStopwatch.Stop();
        }
        else if (drizzleRenderer.State == RenderState.Cancelling)
        {
            ImGui.Text("Cancelling...");
            if (elapsedStopwatch.IsRunning) elapsedStopwatch.Stop();
        }
        else if (drizzleRenderer.State == RenderState.Canceled)
        {
            ImGui.Text("Canceled");
            if (elapsedStopwatch.IsRunning) elapsedStopwatch.Stop();
        }
        else
        {
            if (drizzleRenderer.State == RenderState.Finished)
            {
                ImGui.Text("Done!");
                if (elapsedStopwatch.IsRunning) elapsedStopwatch.Stop();
            }
            else if (drizzleRenderer.State == RenderState.Initializing)
            {
                ImGui.Text("Initializing Zygote runtime...");
                showTime = false;
            }
            else if (drizzleRenderer.State == RenderState.Loading)
            {
                ImGui.Text("Loading level...");
                showTime = false;
            }
            else if (drizzleRenderer.State == RenderState.GeometryExport)
            {
                ImGui.Text($"Exporting geometry...");
                showTime = false;
            }
            else
            {
                ImGui.Text($"Rendering {drizzleRenderer.CamerasDone+1} of {drizzleRenderer.CameraCount} cameras...");
                
                if (!elapsedStopwatch.IsRunning)
                {
                    elapsedStopwatch.Start();
                }
            }

            if (showTime)
                ImGui.TextUnformatted(elapsedStopwatch.Elapsed.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture));
            
            ImGui.NewLine();
            ImGui.TextUnformatted(drizzleRenderer.DisplayString);
        }
    }

    public bool DrawWindow()
    {
        if (!isOpen)
        {
            ImGui.OpenPopup("Render");
            isOpen = false;
        }

        var doClose = false;

        drizzleRenderer?.Update();

        ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
        ImGui.SetNextWindowSizeConstraints(new Vector2(0f, ImGui.GetTextLineHeight() * 30.0f), Vector2.One * 9999f);
        if (ImGuiExt.BeginPopupModal("Render", ImGuiWindowFlags.AlwaysAutoResize))
        {
            bool isPreviewEnabled = drizzleRenderer!.PreviewImages is not null && !drizzleRenderer.OnlyGeometry;
            float renderProgress = 0f;

            if (drizzleRenderer is not null)
                renderProgress = drizzleRenderer.RenderProgress;

            // if preview is enabled, show the progress bar above the preview image
            // otherwise show it below the button line and above the status text
            if (isPreviewEnabled)
            {
                ImGui.BeginGroup();
                ShowControlButtons(ref doClose);
                ShowStatusText();
                ImGui.EndGroup();

                ImGui.SameLine();
                ImGui.BeginGroup();
                ImGui.ProgressBar(renderProgress, new Vector2(-0.000001f, 0.0f));
                
                // update the preview texture
                var previewImages = drizzleRenderer!.PreviewImages;
                if (needUpdateTextures && previewImages is not null)
                {
                    if (previewLayers is null)
                        throw new NullReferenceException("previewLayers is null");

                    RainEd.Logger.Information("update preview");
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    needUpdateTextures = false;

                    drizzleRenderer.UpdatePreviewImages();

                    stopwatch.Stop();
                    RainEd.Logger.Debug("Fetch preview images in {Time} ms", stopwatch.Elapsed.TotalMilliseconds);
                    stopwatch.Restart();

                    // update preview images
                    for (int i = 0; i < 30; i++)
                    {
                        var img = previewImages.Layers[i];
                        UpdateTexture(img, ref previewLayers[i]);
                    }
                    UpdateTexture(previewImages.BlackOut1, ref previewBlackout1);
                    UpdateTexture(previewImages.BlackOut2, ref previewBlackout2);
                    UpdateComposite();

                    stopwatch.Stop();
                    RainEd.Logger.Debug("Update preview texture in {Time} ms", stopwatch.Elapsed.TotalMilliseconds);
                }

                int cWidth = previewComposite.Texture.Width;
                int cHeight = previewComposite.Texture.Height;
                ImGuiExt.ImageRect(
                    previewComposite.Texture,
                    (int)(cWidth / 1.25f), (int)(cHeight / 1.25f),
                    new Rectangle(0, cHeight, cWidth, -cHeight)
                );
                ImGui.EndGroup();
            }
            else
            {
                ShowControlButtons(ref doClose);

                ImGui.ProgressBar(renderProgress, new Vector2(ImGui.GetContentRegionAvail().X, 0.0f));
                ShowStatusText();
            }
            
            ImGui.EndPopup();
        }

        return doClose;
    }

    // 1-bit-per-pixel images need to be converted into a 8-bit-per-pixel image...
    private byte[]? convertedBitmap = null;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void ConvertBitmap(byte[] pixels, byte[] convertedBitmap)
    {
        int j = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            convertedBitmap[j++] = (byte)(255 * (pixels[i] & 1));
            convertedBitmap[j++] = (byte)(255 * ((pixels[i] >> 1) & 1));
            convertedBitmap[j++] = (byte)(255 * ((pixels[i] >> 2) & 1));
            convertedBitmap[j++] = (byte)(255 * ((pixels[i] >> 3) & 1));
            convertedBitmap[j++] = (byte)(255 * ((pixels[i] >> 4) & 1));
            convertedBitmap[j++] = (byte)(255 * ((pixels[i] >> 5) & 1));
            convertedBitmap[j++] = (byte)(255 * ((pixels[i] >> 6) & 1));
            convertedBitmap[j++] = (byte)(255 * ((pixels[i] >> 7) & 1));
        }
    }

    private void UpdateTexture(Drizzle.RenderImage? img, ref RlManaged.Texture2D? tex)
    {
        if (img == null)
        {
            tex?.Dispose();
            tex = null;
            return;
        }

        Glib.Texture gtex;

        if (tex == null || img.Width != tex.Width || img.Height != tex.Height)
        {
            tex?.Dispose();
            gtex = RainEd.RenderContext.CreateTexture(img.Width, img.Height, Glib.PixelFormat.RGBA);
            tex = new RlManaged.Texture2D(new Texture2D() { ID = gtex });
        }
        else
        {
            gtex = ((Texture2D)tex).ID!;
        }

        // convert 1-bit-per-pixel image to an 8-bit-per-pixel image
        if (img.Format == Drizzle.PixelFormat.L1)
        {
            if (convertedBitmap is null || convertedBitmap.Length != img.Width * img.Height)
                convertedBitmap = new byte[img.Width * img.Height];
            
            ConvertBitmap(img.Pixels, convertedBitmap);
            gtex.UpdateFromImage(convertedBitmap, Glib.PixelFormat.Grayscale);
        }
        else
        {
            // bgra -> rgba conversion is done in the shader
            gtex.UpdateFromImage(img.Pixels, Glib.PixelFormat.RGBA);
        }
    }

    private void UpdateComposite()
    {
        Raylib.BeginTextureMode(previewComposite);
        Raylib.ClearBackground(Color.White);

        var previewStatus = drizzleRenderer!.PreviewImages!;
        var renderStage = previewStatus.Stage;

        if (renderStage != Drizzle.RenderPreviewStage.Setup)
        {
            var shader = layerPreviewShader;
            if (renderStage == Drizzle.RenderPreviewStage.Lights)
            {
                shader = layerPreviewLightShader;
                Raylib.ClearBackground(Color.Black);
            }

            Raylib.BeginShaderMode(shader);

            for (int i = 29; i >= 0; i--)
            {
                var tex = previewLayers![i];

                if (tex is not null)
                {
                    var ox = (previewComposite.Texture.Width - tex.Width) / 2f;
                    var oy = (previewComposite.Texture.Height - tex.Height) / 2f;

                    float fadeValue = i / 30f;
                    Raylib.DrawTextureV(tex, new Vector2(ox - i, oy - i), new Color((int)(fadeValue * 255f), 0, 0, 255));
                }
            }

            if (previewBlackout1 is not null && previewStatus.RenderBlackOut1)
            {
                var ox = (previewComposite.Texture.Width - previewBlackout1.Width) / 2f;
                var oy = (previewComposite.Texture.Height - previewBlackout1.Height) / 2f;
                Raylib.DrawTexture(previewBlackout1, (int)ox, (int)oy, new Color(0, 0, 0, 255));
            }

            if (previewBlackout2 is not null && previewStatus.RenderBlackOut2)
            {
                var ox = (previewComposite.Texture.Width - previewBlackout2.Width) / 2f;
                var oy = (previewComposite.Texture.Height - previewBlackout2.Height) / 2f;
                Raylib.DrawTexture(previewBlackout2, (int)ox, (int)oy, new Color(0, 0, 0, 255));
            }

            Raylib.EndShaderMode();
        }

        Raylib.EndTextureMode();
    }
}