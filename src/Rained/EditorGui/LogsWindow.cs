using ImGuiNET;
using System.Numerics;
namespace RainEd;

static class LogsWindow
{
    public static bool IsWindowOpen = false;
    private static List<string> logs = [];

    public static void ShowWindow()
    {
        if (!IsWindowOpen) return;
        var logs = Log.UserLog;

        if (ImGui.Begin("Logs", ref IsWindowOpen))
        {
            if (ImGui.Button("Clear"))
                logs.Clear();
            
            ImGui.Separator();

            if (ImGui.BeginChild("scrolling", Vector2.Zero, ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
            {
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 5f));
                
                foreach (var ev in logs)
                {
                    switch (ev.Level)
                    {
                        case Log.LogLevel.Error:
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 48f/255f, 48/255f, 1f));
                            break;

                        case Log.LogLevel.Warning:
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 165f/255f, 48f/255f, 1f));
                            break;
                            
                        default:
                            ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Text]);
                            break;
                    }
                    
                    ImGui.TextUnformatted(ev.Message);
                    ImGui.PopStyleColor();
                }
                ImGui.PopStyleVar();

                // auto-scroll
                if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
                    ImGui.SetScrollHereY(1f);
                
                ImGui.EndChild();
            }
        } ImGui.End();
    }
}