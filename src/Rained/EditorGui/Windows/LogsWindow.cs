using ImGuiNET;
using System.Numerics;
namespace Rained.EditorGui;

static class LogsWindow
{
    public static bool IsWindowOpen = false;
    private static List<string> logs = [];
    private static bool wrap = false;
    private static Filters filters = Filters.Information | Filters.Warning | Filters.Error;

    [Flags]
    enum Filters
    {
        Information = 1,
        Warning = 2,
        Error = 4,
    }

    public static void ShowWindow()
    {
        if (!IsWindowOpen) return;
        var logs = Log.UserLog;

        if (ImGui.Begin("Logs", ref IsWindowOpen))
        {
            if (ImGui.Button("Clear"))
                logs.Clear();
            
            ImGui.SameLine();
            if (ImGui.Button("Show Full Logs"))
                Platform.OpenPath(Path.Combine(Boot.AppDataPath, "logs", "latest.log.txt"));
                
            ImGui.SameLine();
            ImGui.Checkbox("Wrap", ref wrap);
            
            {
                var filtersInt = (int)filters;
                ImGui.SameLine();
                ImGui.CheckboxFlags("Info", ref filtersInt, (int)Filters.Information);
                ImGui.SameLine();
                ImGui.CheckboxFlags("Warnings", ref filtersInt, (int)Filters.Warning);
                ImGui.SameLine();
                ImGui.CheckboxFlags("Errors", ref filtersInt, (int)Filters.Error);
                filters = (Filters)filtersInt;
            }
            
            ImGui.Separator();

            if (ImGui.BeginChild("scrolling", Vector2.Zero, ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
            {
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 5f));

                bool showInfo = filters.HasFlag(Filters.Information);
                bool showWarnings = filters.HasFlag(Filters.Warning);
                bool showErrors = filters.HasFlag(Filters.Error);

                if (wrap)
                    ImGui.PushTextWrapPos(ImGui.GetContentRegionAvail().X - 1f);
                else
                    ImGui.PushTextWrapPos(float.PositiveInfinity);
                
                foreach (var ev in logs)
                {
                    switch (ev.Level)
                    {
                        case Log.LogLevel.Error:
                            if (!showErrors) continue;
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 48f/255f, 48/255f, 1f));
                            break;

                        case Log.LogLevel.Warning:
                            if (!showWarnings) continue;
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 165f/255f, 48f/255f, 1f));
                            break;
                            
                        default:
                            if (!showInfo) continue;
                            ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Text]);
                            break;
                    }
                    
                    ImGui.TextWrapped(ev.Message);
                    
                    ImGui.PopStyleColor();
                }
                ImGui.PopStyleVar();

                ImGui.PopTextWrapPos();

                // auto-scroll
                if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
                    ImGui.SetScrollHereY(1f);                
            } ImGui.EndChild();
        } ImGui.End();
    }
}