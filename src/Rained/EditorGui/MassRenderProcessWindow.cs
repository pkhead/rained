namespace Rained.EditorGui;

using System.Numerics;
using ImGuiNET;
using Rained.Drizzle;

class MassRenderProcessWindow
{
    public const string WindowName = "Mass Render###MassRenderProc";
    private readonly Task renderTask;
    private readonly CancellationTokenSource cancelSource;

    public bool IsDone { get; private set; } = false;
    private bool cancel = false;
    private bool renderBegan = false;
    private int renderedLevels = 0;
    private int totalLevels = 1;
    private readonly Dictionary<string, float> levelProgress = [];
    private readonly List<string> problematicLevels = [];

    public MassRenderProcessWindow(DrizzleMassRender renderProcess)
    {
        cancelSource = new CancellationTokenSource();

        var prog = new Progress<MassRenderNotification>();
        prog.ProgressChanged += RenderProgressChanged;

        var ct = cancelSource.Token;
        //renderTask = null!;
        renderTask = Task.Run(() =>
        {
            renderProcess.Start(prog, ct);
        }, ct);
    }

    public void Render()
    {
        if (IsDone) return;

        if (!ImGui.IsPopupOpen(WindowName))
        {
            ImGui.OpenPopup(WindowName);

            // center popup modal
            ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
        }

        ImGui.SetNextWindowSizeConstraints(new Vector2(0f, ImGui.GetTextLineHeight() * 30.0f), Vector2.One * 9999f);
        if (ImGui.BeginPopupModal(WindowName, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
        {
            RainEd.Instance.NeedScreenRefresh();

            ImGui.BeginDisabled(cancel);
            if (ImGui.Button("Cancel"))
            {
                cancel = true;
                cancelSource.Cancel();
            }
            ImGui.EndDisabled();

            ImGui.BeginDisabled(!renderTask.IsCompleted);
            ImGui.SameLine();
            if (ImGui.Button("Close"))
            {
                ImGui.CloseCurrentPopup();
                IsDone = true;
            }
            ImGui.EndDisabled();

            ImGui.SameLine();
            if (ImGui.Button("Open Render Folder"))
            {
                RainEd.Instance.ShowPathInSystemBrowser(Path.Combine(RainEd.Instance.AssetDataPath, "Levels"), false);
            }

            {
                float progress = renderedLevels + levelProgress.Values.Sum();
                ImGui.ProgressBar(progress / totalLevels, new Vector2(-0.00001f, 0f));
            }

            // status text
            if (renderTask.IsCanceled)
            {
                ImGui.Text("Render was cancelled.");
            }
            else if (cancel)
            {
                ImGui.Text("Cancelling...");
            }
            else if (!renderBegan)
            {
                ImGui.Text("Initializing Drizzle...");
            }
            else if (renderedLevels < totalLevels)
            {
                ImGui.TextUnformatted($"{totalLevels - renderedLevels} levels remaining...");
            }
            else
            {
                ImGui.TextUnformatted("Render completed!");
            }

            // error list
            if (problematicLevels.Count > 0)
            {
                ImGui.TextUnformatted(problematicLevels.Count + " errors:");

                foreach (var name in problematicLevels)
                {
                    ImGui.BulletText(name);
                }
            }

            ImGui.EndPopup();
        }
    }

    private void RenderProgressChanged(object? sender, MassRenderNotification prog)
    {
        switch (prog)
        {
            case MassRenderBegan began:
                totalLevels = began.Total;
                renderBegan = true;
                break;

            case MassRenderLevelCompleted level:
                renderedLevels++;

                if (!level.Success)
                {
                    problematicLevels.Add(level.LevelName);
                }

                levelProgress.Remove(level.LevelName);

                break;
            
            case MassRenderLevelProgress levelProg:
                if (!levelProgress.TryAdd(levelProg.LevelName, levelProg.Progress))
                {
                    levelProgress[levelProg.LevelName] = levelProg.Progress;
                }

                break;
        }
    }
}