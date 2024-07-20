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

    private readonly RlManaged.Shader layerPreviewShader;
    private readonly RlManaged.Shader layerPreviewLightShader;
    private int _updateProgress = -1; 

    private Stopwatch elapsedStopwatch = new();

    public DrizzleRenderWindow(bool onlyGeo)
    {
        try
        {
            var level = RainEd.Instance.Level;            
            var prioCam = level.PrioritizedCamera is null ? -1 : level.Cameras.IndexOf(level.PrioritizedCamera);

            drizzleRenderer = new DrizzleRender(onlyGeo, prioCam);

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
            Log.Error("Error occured when initializing render:\n{ErrorMessage}", e);
        }

        layerPreviewShader = Shaders.RenderPreviewLayerShader;
        layerPreviewLightShader = Shaders.RenderPreviewLightShader;

        previewComposite = RlManaged.RenderTexture2D.Load(
            (int)Camera.WidescreenSize.X * 20,
            (int)Camera.WidescreenSize.Y * 20
        );

        if (!onlyGeo)
        {
            var tex = previewComposite.Texture;
            using var img = Glib.Image.FromColor(tex.Width, tex.Height, Glib.Color.White, Glib.PixelFormat.RGBA);
            tex.ID!.UpdateFromImage(img);
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

                if (previewImages is not null && (needUpdateTextures || _updateProgress != -1))
                {
                    if (_updateProgress == -1)
                    {
                        Log.Debug("Update");
                        drizzleRenderer!.UpdatePreviewImages();
                        _updateProgress = 0;
                        needUpdateTextures = false;

                        for (int i = 0; i < 30; i++)
                            AllocTexture(previewImages.Layers[i], ref previewLayers![i]);
                        AllocTexture(previewImages.BlackOut1, ref previewBlackout1);
                        AllocTexture(previewImages.BlackOut2, ref previewBlackout2);
                    }

                    if (_updateProgress >= 0)
                    {
                        // update two sublayers per frame
                        for (int i = 0; i < 2; i++)
                        {
                            AllocTexture(previewImages.Layers[i], ref previewLayers![i]);
                            UpdateTexture(previewImages.Layers[_updateProgress], previewLayers![_updateProgress]!.GlibTexture);
                            _updateProgress++;

                            UpdateComposite();
                            if (_updateProgress >= 30)
                            {
                                if (previewImages.BlackOut1 is not null && previewBlackout1 is not null)
                                    UpdateTexture(previewImages.BlackOut1, previewBlackout1.GlibTexture);
                                
                                if (previewImages.BlackOut2 is not null && previewBlackout2 is not null)
                                    UpdateTexture(previewImages.BlackOut2, previewBlackout2.GlibTexture);

                                _updateProgress = -1;
                                break;
                            }
                        }
                    }
                }

                int cWidth = previewComposite.Texture.Width;
                int cHeight = previewComposite.Texture.Height;
                ImGuiExt.ImageRect(
                    previewComposite.Texture,
                    (int)(cWidth / 1.25f), (int)(cHeight / 1.25f),
                    RainEd.RenderContext.OriginBottomLeft ?
                        new Rectangle(0, cHeight, cWidth, -cHeight) :
                        new Rectangle(0f, 0f, cWidth, cHeight)
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

    private static void AllocTexture(Drizzle.RenderImage? img, ref RlManaged.Texture2D? tex)
    {
        if (img == null)
        {
            tex?.Dispose();
            tex = null;
            return;
        }

        Glib.Texture gtex;
        Glib.PixelFormat desiredPixelFormat = img.Format == Drizzle.PixelFormat.L1 ? Glib.PixelFormat.Grayscale : Glib.PixelFormat.RGBA;

        if (tex == null || tex.GlibTexture.PixelFormat != desiredPixelFormat || img.Width != tex.Width || img.Height != tex.Height)
        {
            tex?.Dispose();
            using var gimg = Glib.Image.FromColor(img.Width, img.Height, Glib.Color.White, desiredPixelFormat);
            gtex = Glib.Texture.Load(gimg);
            tex = new RlManaged.Texture2D(new Texture2D() { ID = gtex });
        }
        else
        {
            gtex = ((Texture2D)tex).ID!;
        }
    }

    private void UpdateTexture(Drizzle.RenderImage img, Glib.Texture gtex)
    {
        if (img.Pixels is not null)
        {
            // convert 1-bit-per-pixel image to an 8-bit-per-pixel image
            if (img.Format == Drizzle.PixelFormat.L1)
            {
                var srcPixels = img.Pixels!;

                int grayscalePixSize = img.Width * img.Height + 32; // add 32 bytes of padding, just in case
                if (convertedBitmap is null || convertedBitmap.Length != img.Width * img.Height + grayscalePixSize)
                    convertedBitmap = new byte[img.Width * img.Height + grayscalePixSize];
                
                ConvertBitmap(srcPixels, convertedBitmap);
                gtex.UpdateFromImage(new ReadOnlySpan<byte>(convertedBitmap, 0, img.Width * img.Height));
            }
            else
            {
                // bgra -> rgba conversion is done in the shader                
                //await Task.Run(() =>
                //{
                    gtex.UpdateFromImage(new ReadOnlySpan<byte>(img.Pixels, 0, img.Width * img.Height * 4));
                //});
            }
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

            shader.GlibShader.SetUniform("v4_renderPreviewData", new Vector4(1f, 1f, 0f, 0f));

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