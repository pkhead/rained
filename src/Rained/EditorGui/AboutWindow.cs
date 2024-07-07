using ImGuiNET;

using System.Numerics;
namespace RainEd;

static class AboutWindow
{
    private const string WindowName = "About Rained";
    public static bool IsWindowOpen = false;

    private static RlManaged.Texture2D? rainedLogo = null;

    public static void ShowWindow()
    {
        if (!ImGui.IsPopupOpen(WindowName) && IsWindowOpen)
        {
            ImGui.OpenPopup(WindowName);

            // center popup modal
            ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
        }

        if (ImGui.BeginPopupModal(WindowName, ref IsWindowOpen, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
        {
            rainedLogo ??= RlManaged.Texture2D.Load(Path.Combine(Boot.AppDataPath,"assets","rained-logo.png"));

            // TODO: os/runtime information
            ImGui.SameLine(Math.Max(0f, (ImGui.GetWindowWidth() - rainedLogo.Width) / 2.0f));
            ImGuiExt.Image(rainedLogo);
            
            ImGui.Text("A Rain World level editor - " + RainEd.Version);
            ImGui.NewLine();
            ImGui.Text("(c) 2024 pkhead - MIT License");
            ImGui.Text("Rain World - Videocult/Adult Swim Games/Akapura Games");
            ImGuiExt.LinkText("GitHub", "https://github.com/pkhead/rained");

            // notify user of a new version
            if (RainEd.Instance.LatestVersionInfo is not null && RainEd.Instance.LatestVersionInfo.VersionName != RainEd.Version)
            {
                ImGui.NewLine();
                ImGui.Text("New version available!");
                ImGui.SameLine();
                ImGuiExt.LinkText(RainEd.Instance.LatestVersionInfo.VersionName, RainEd.Instance.LatestVersionInfo.GitHubReleaseUrl);
            }
            
            ImGui.SeparatorText("Libraries");
            
            ImGui.Bullet();
            ImGuiExt.LinkText("Drizzle", "https://github.com/pkhead/Drizzle");
            ImGui.SameLine();
            ImGui.Text("(c) 2021 Pieter-Jan Briers - MIT License");

            ImGui.Bullet();
            ImGuiExt.LinkText("ImGui.NET", "https://github.com/ImGuiNET/ImGui.NET");
            ImGui.SameLine();
            ImGui.Text("(c) 2017 Eric Mellino and ImGui.NET contributors - MIT License");

            ImGui.Bullet();
            ImGuiExt.LinkText("Dear ImGui", "https://github.com/ocornut/imgui");
            ImGui.SameLine();
            ImGui.Text("(c) 2014-2024 Omar Cornut - MIT License");

            ImGui.Bullet();
            ImGuiExt.LinkText("Silk.NET", "https://dotnet.github.io/Silk.NET");
            ImGui.SameLine();
            ImGui.Text("(c) 2021- .NET Foundation and Contributors - MIT License");
        }
    }
}