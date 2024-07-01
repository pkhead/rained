using ImGuiNET;

using System.Diagnostics;
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

            // TODO: version number, build date, os/runtime information, library licenses
            ImGuiExt.Image(rainedLogo);
            ImGui.Text("A Rain World level editor - " + RainEd.Version);
            ImGui.NewLine();
            ImGui.Text("(c) 2024 pkhead - MIT License");
            ImGui.Text("Rain World by Videocult/Adult Swim Games/Akapura Games");
            LinkText("GitHub", "https://github.com/pkhead/rained");

            // notify user of a new version
            if (RainEd.Instance.LatestVersionInfo is not null && RainEd.Instance.LatestVersionInfo.VersionName != RainEd.Version)
            {
                ImGui.NewLine();
                ImGui.Text("New version available!");
                ImGui.SameLine();
                LinkText(RainEd.Instance.LatestVersionInfo.VersionName, RainEd.Instance.LatestVersionInfo.GitHubReleaseUrl);
            }
            
            ImGui.SeparatorText("Libraries");
            
            ImGui.Bullet();
            LinkText("Drizzle", "https://github.com/pkhead/Drizzle");
            ImGui.SameLine();
            ImGui.Text("(c) 2021 Pieter-Jan Briers - MIT License");

            ImGui.Bullet();
            LinkText("ImGui.NET", "https://github.com/ImGuiNET/ImGui.NET");
            ImGui.SameLine();
            ImGui.Text("(c) 2017 Eric Mellino and ImGui.NET contributors - MIT License");

            ImGui.Bullet();
            LinkText("Dear ImGui", "https://github.com/ocornut/imgui");
            ImGui.SameLine();
            ImGui.Text("(c) 2014-2024 Omar Cornut - MIT License");

            ImGui.Bullet();
            LinkText("rlImGui-cs", "https://github.com/raylib-extras/rlImGui-cs");
            ImGui.SameLine();
            ImGui.Text("(c) 2020-2021 Jeffery Myers - MIT License");

            ImGui.Bullet();
            LinkText("Raylib-cs", "https://github.com/ChrisDill/Raylib-cs");
            ImGui.SameLine();
            ImGui.Text("(c) 2018-2024 ChrisDill - Zlib License");
            
            ImGui.Bullet();
            LinkText("SFML.Net", "https://github.com/SFML/SFML.Net");
            ImGui.SameLine();
            ImGui.Text("(C) 2007-2023 Laurent Gomila - laurent@sfml-dev.org - Zlib License");

            ImGui.Bullet();
            LinkText("CSFML", "https://github.com/SFML/CSFML");
            ImGui.SameLine();
            ImGui.Text("(C) 2007-2023 Laurent Gomila - laurent@sfml-dev.org - Zlib License");
        }
    }

    private static void LinkText(string id, string link)
    {
        string display;
        if (id[0] == '#')
        {
            display = link;
        }
        else
        {
            display = id;
        }
        
        var cursorPos = ImGui.GetCursorPos();
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var textSize = ImGui.CalcTextSize(display);

        // link interactive
        if (ImGui.InvisibleButton(id, textSize))
        {
            if (!Platform.OpenURL(link))
            {
                RainEd.Logger.Error("Could not open URL on user platform.");
            }
        }

        // draw link text
        ImGui.SetCursorPos(cursorPos);
        Vector4 textColor;
        if (ImGui.IsItemHovered() || ImGui.IsItemActive())
        {
            textColor = ImGui.GetStyle().Colors[(int) ImGuiCol.ButtonActive];
        }
        else
        {
            textColor = ImGui.GetStyle().Colors[(int) ImGuiCol.ButtonHovered];
        }
        
        ImGui.TextColored(textColor, display);

        // underline
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddLine(cursorScreenPos + textSize * new Vector2(0f, 1f), cursorScreenPos + textSize, ImGui.ColorConvertFloat4ToU32(textColor));
    }
}