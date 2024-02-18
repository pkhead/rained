using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
using RenderState = RainEd.LevelDrizzleRender.RenderState;
namespace RainEd;

class DrizzleRenderWindow : IDisposable
{
    public readonly LevelDrizzleRender drizzleRenderer;
    private bool isOpen = false;
    private readonly RlManaged.Texture2D[] previewTextures;
    private readonly RlManaged.RenderTexture2D previewComposite;
    private bool needUpdateTextures = false;

    private const string LayerPreviewShaderSource = @"
        #version 330

        in vec2 fragTexCoord;
        in vec4 fragColor;

        uniform sampler2D texture0;
        uniform vec4 colDiffuse;

        out vec4 finalColor;

        void main()
        {
            vec4 texelColor = texture(texture0, fragTexCoord);
            bool isWhite = texelColor.r == 1.0 && texelColor.g == 1.0 && texelColor.b == 1.0;
            vec3 correctColor = texelColor.bgr;
            
            finalColor = vec4(
                mix(correctColor, vec3(1.0, 1.0, 1.0), fragColor.r * 0.8),
                1.0 - float(isWhite)
            ) * colDiffuse;
        }    
    ";

    private RlManaged.Shader layerPreviewShader;

    public DrizzleRenderWindow()
    {
        drizzleRenderer = new LevelDrizzleRender();
        
        previewTextures = new RlManaged.Texture2D[30];

        for (int i = 0; i < 30; i++)
        {
            previewTextures[i] = RlManaged.Texture2D.LoadFromImage(drizzleRenderer.RenderLayerPreviews[i]);
        }

        previewComposite = RlManaged.RenderTexture2D.Load(
            (int)Camera.WidescreenSize.X * 20,
            (int)Camera.WidescreenSize.Y * 20
        );

        layerPreviewShader = RlManaged.Shader.LoadFromMemory(null, LayerPreviewShaderSource);

        drizzleRenderer.PreviewUpdated += () =>
        {
            needUpdateTextures = true;
        };

        Raylib.BeginTextureMode(previewComposite);
        Raylib.ClearBackground(Color.White);
        Raylib.EndTextureMode();
    }

    public void Dispose()
    {
        drizzleRenderer.Dispose();
        previewComposite.Dispose();

        for (int i = 0; i < previewTextures.Length; i++)
        {
            previewTextures[i].Dispose();
        }

        layerPreviewShader.Dispose();
    }

    public bool DrawWindow()
    {
        if (!isOpen)
        {
            ImGui.OpenPopup("Render");
            isOpen = false;
        }

        var doClose = false;

        drizzleRenderer.Update();

        if (ImGui.BeginPopupModal("Render"))
        {
            var cancel = drizzleRenderer.State == RenderState.Cancelling || drizzleRenderer.IsDone;

            // cancel button (disabled if cancelling/canceled)
            if (cancel)
                ImGui.BeginDisabled();
            
            if (ImGui.Button("Cancel"))
                drizzleRenderer.Cancel();
            
            if (cancel)
                ImGui.EndDisabled();

            // close button (disabled if render process is not done)
            if (!drizzleRenderer.IsDone)
                ImGui.BeginDisabled();
            
            ImGui.SameLine();
            if (ImGui.Button("Close"))
            {
                doClose = true;
                ImGui.CloseCurrentPopup();
            }
            
            if (!drizzleRenderer.IsDone)
                ImGui.EndDisabled();
            
            ImGui.SameLine();
            ImGui.ProgressBar(drizzleRenderer.RenderProgress, new Vector2(-1.0f, 0.0f));

            // status sidebar
            if (ImGui.BeginChild("##status", new Vector2(ImGui.GetTextLineHeight() * 20.0f, ImGui.GetContentRegionAvail().Y)))
            {
                if (drizzleRenderer.State == RenderState.Cancelling)
                {
                    ImGui.Text("Cancelling...");
                }
                else if (drizzleRenderer.State == RenderState.Canceled)
                {
                    ImGui.Text("Canceled");
                }
                else
                {
                    if (drizzleRenderer.State == RenderState.Finished)
                    {
                        ImGui.Text("Done!");
                    }
                    else if (drizzleRenderer.State == RenderState.Initializing)
                    {
                        ImGui.Text("Initializing Zygote runtime...");
                    }
                    else
                    {
                        ImGui.Text($"Rendering {drizzleRenderer.CamerasDone+1} of {drizzleRenderer.CameraCount} cameras...");
                    }

                    ImGui.TextUnformatted(drizzleRenderer.DisplayString);
                }
            } ImGui.EndChild();

            // preview image
            ImGui.SameLine();

            if (needUpdateTextures)
            {
                needUpdateTextures = false;

                for (int i = 0; i < 30; i++)
                    drizzleRenderer.RenderLayerPreviews[i].UpdateTexture(previewTextures[i]);
                UpdateComposite();
            }

            int cWidth = previewComposite.Texture.Width;
            int cHeight = previewComposite.Texture.Height;
            rlImGui.ImageRect(
                previewComposite.Texture,
                (int)(cWidth / 1.25f), (int)(cHeight / 1.25f),
                new Rectangle(0, cHeight, cWidth, -cHeight)
            );
            
            ImGui.EndPopup();
        }

        return doClose;
    }

    private void UpdateComposite()
    {
        Raylib.BeginTextureMode(previewComposite);
        Raylib.ClearBackground(Color.White);

        Raylib.BeginShaderMode(layerPreviewShader);

        for (int i = 29; i >= 0; i--)
        {
            float fadeValue = i / 30f;
            Raylib.DrawTexture(previewTextures[i], -300, -200, new Color((int)(fadeValue * 255f), 0, 0, 255));
        }
        Raylib.EndShaderMode();

        Raylib.EndTextureMode();
    }
}