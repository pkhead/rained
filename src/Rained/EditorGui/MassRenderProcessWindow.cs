namespace Rained.EditorGui;

using System.Numerics;
using System.Diagnostics;
using ImGuiNET;
using Rained.Drizzle;

class MassRenderProcessWindow
{
    public const string WindowName = "Mass Render###MassRenderProc";
    private readonly CancellationTokenSource cancelSource;

    public bool IsDone { get; private set; } = false;
    private bool cancel = false;
    private bool renderBegan = false;
    private int renderedLevels = 0;
    private int totalLevels = 1;
    private readonly Dictionary<string, float> levelProgress = [];
    private readonly List<string> problematicLevels = [];
    private bool renderIsDone = false;

    private readonly Stopwatch elapsedStopwatch = new();
    private bool showTime = false;
    private readonly DrizzleMassRender renderProc;

    public MassRenderProcessWindow(string[] files, int parallelismLimit)
    {
        cancelSource = new CancellationTokenSource();

        var prog = new ProgressNoSync<MassRenderNotification>();
        prog.ProgressChanged += RenderProgressChanged;

        renderProc = new DrizzleMassRender(files, parallelismLimit, prog, cancelSource.Token);

        totalLevels = files.Length;

        // startup task
        Task.Run(() =>
        {
            renderProc.StartUp(DrizzleManager.GetRuntime(RainEd.Instance.Preferences.StaticDrizzleLingoRuntime));
        }, cancelSource.Token);
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

            // update render process
            if (!renderIsDone && renderBegan)
                if (!renderProc.ProcessUpdate()) renderIsDone = true;

            ImGui.BeginDisabled(cancel);
            if (ImGui.Button("Cancel"))
            {
                cancel = true;
                cancelSource.Cancel();
            }
            ImGui.EndDisabled();

            ImGui.BeginDisabled(!renderIsDone);
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

            lock (levelProgress)
            {
                float progress = renderedLevels + levelProgress.Values.Sum();
                ImGui.ProgressBar(progress / totalLevels, new Vector2(-0.00001f, 0f));
            }

            // status text
            if (renderIsDone && problematicLevels.Count > 0)
            {
                ImGui.Text("An error occured!\nCheck the log file for more info.");
                if (elapsedStopwatch.IsRunning) elapsedStopwatch.Stop();
            }
            else if (renderIsDone && cancel)
            {
                ImGui.Text("Render was cancelled.");

                if (elapsedStopwatch.IsRunning)
                    elapsedStopwatch.Stop();
            }
            else if (cancel)
            {
                ImGui.Text("Cancelling...");

                if (elapsedStopwatch.IsRunning)
                    elapsedStopwatch.Stop();
            }
            else if (!renderBegan)
            {
                ImGui.Text("Initializing Drizzle...");
            }
            else if (renderedLevels < totalLevels)
            {
                if (!elapsedStopwatch.IsRunning)
                    elapsedStopwatch.Start();
                
                ImGui.TextUnformatted($"{totalLevels - renderedLevels} levels remaining...");
                showTime = true;
            }
            else
            {
                ImGui.TextUnformatted("Render completed!");

                if (elapsedStopwatch.IsRunning)
                    elapsedStopwatch.Stop();
            }

            if (showTime)
                ImGui.TextUnformatted(elapsedStopwatch.Elapsed.ToString(@"hh\:mm\:ss", Boot.UserCulture));

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
            case MassRenderBegin begin:
                totalLevels = begin.Total;
                renderBegan = true;
                break;

            case MassRenderBeginLevel level:
            {
                LuaScripting.Modules.RainedModule.PreRenderCallback(level.LevelFile);
                break;
            }

            case MassRenderLevelCompleted level:
            {
                var levelName = Path.GetFileNameWithoutExtension(level.LevelFile);
                var levelsPath = Path.Combine(RainEd.Instance.AssetDataPath, "Levels") + Path.DirectorySeparatorChar;
                renderedLevels++;

                if (!level.Success)
                {
                    problematicLevels.Add(levelName);
                }

                lock (levelProgress) levelProgress.Remove(level.LevelFile);

                LuaScripting.Modules.RainedModule.PostRenderCallback(
                    sourceTxt: level.LevelFile,
                    dstTxt: string.Join(null, levelsPath, levelName, ".txt"),
                    dstPngs: level.Cameras.Select(x => levelsPath + $"{levelName}_{x}.png").ToArray()
                );
                break;
            }
            
            case MassRenderLevelProgress levelProg:
                lock (levelProgress) levelProgress[levelProg.LevelFile] = levelProg.Progress;
                break;
        }
    }
}