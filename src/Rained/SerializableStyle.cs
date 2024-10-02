using ImGuiNET;
using System.Numerics;
using System.Text.Json;
namespace Rained;

/// <summary>
/// A JSON-serializable ImGui style
/// </summary>
class SerializableStyle
{
    // Main
    public float[] WindowPadding { get; set; }
    public float[] FramePadding { get; set; }
    public float[] ItemSpacing { get; set; }
    public float[] ItemInnerSpacing { get; set; }
    public float[] TouchExtraPadding { get; set; }
    public float IndentSpacing { get; set; }
    public float ScrollbarSize { get; set; }
    public float GrabMinSize { get; set; }

    // Borders
    public float WindowBorderSize { get; set; }
    public float ChildBorderSize { get; set; }
    public float PopupBorderSize { get; set; }
    public float FrameBorderSize { get; set; }
    public float TabBorderSize { get; set; }
    public float TabBarBorderSize { get; set; }

    // Rounding
    public float WindowRounding { get; set; }
    public float ChildRounding { get; set; }
    public float FrameRounding { get; set; }
    public float PopupRounding { get; set; }
    public float ScrollbarRounding { get; set; }
    public float GrabRounding { get; set; }
    public float TabRounding { get; set; }

    // Tables
    public float[] CellPadding { get; set; }
    public float TableAngledHeadersAngle { get; set; }

    // Widgets
    public float[] WindowTitleAlign { get; set; }
    public string WindowMenuButtonPosition { get; set; }
    public string ColorButtonPosition { get; set; }

    public float[] ButtonTextAlign { get; set; }
    public float[] SelectableTextAlign { get; set; }
    public float SeparatorTextBorderSize { get; set; }
    public float[] SeparatorTextAlign { get; set; }
    public float[] SeparatorTextPadding { get; set; }
    public float LogSliderDeadzone { get; set; }

    public Dictionary<string, float[]> Colors { get; set; }

    private static float[] JsonVec2(Vector2 vec)
        => [vec.X, vec.Y];

    private static Vector2 JsonVec2(float[] arr)
        => new(arr[0], arr[1]);

    public SerializableStyle()
    {
        Colors = [];
        WindowMenuButtonPosition = "left";
        ColorButtonPosition = "right";
        WindowPadding = JsonVec2(Vector2.Zero);
        FramePadding = JsonVec2(Vector2.Zero);
        ItemSpacing = JsonVec2(Vector2.Zero);
        ItemInnerSpacing = JsonVec2(Vector2.Zero);
        TouchExtraPadding = JsonVec2(Vector2.Zero);
        CellPadding = JsonVec2(Vector2.Zero);
        WindowTitleAlign = JsonVec2(Vector2.Zero);
        ButtonTextAlign = JsonVec2(Vector2.Zero);
        SelectableTextAlign = JsonVec2(Vector2.Zero);
        SeparatorTextBorderSize = 0f;
        SeparatorTextAlign = JsonVec2(Vector2.Zero);
        SeparatorTextPadding = JsonVec2(Vector2.Zero);
        LogSliderDeadzone = 0f;
    }

    public SerializableStyle(ImGuiStylePtr style)
    {
        // parse colors
        Colors = [];
        for (int i = 0; i < (int)ImGuiCol.COUNT; i++)
        {
            var name = ImGui.GetStyleColorName((ImGuiCol) i);
            var col = style.Colors[i];

            Colors[name] = [col.X, col.Y, col.Z, col.W];
        }

        // parse sizes
        WindowMenuButtonPosition = style.WindowMenuButtonPosition switch
        {
            ImGuiDir.None => "none",
            ImGuiDir.Left => "left",
            ImGuiDir.Right => "right",
            _ => throw new ArgumentOutOfRangeException()
        };

        ColorButtonPosition = style.ColorButtonPosition switch
        {
            ImGuiDir.Left => "left",
            ImGuiDir.Right => "right",
            _ => throw new ArgumentOutOfRangeException()
        };
        
        WindowPadding = JsonVec2(style.WindowPadding);
        FramePadding = JsonVec2(style.FramePadding);
        ItemSpacing = JsonVec2(style.ItemSpacing);
        ItemInnerSpacing = JsonVec2(style.ItemInnerSpacing);
        TouchExtraPadding = JsonVec2(style.TouchExtraPadding);
        IndentSpacing = style.IndentSpacing;
        ScrollbarSize = style.ScrollbarSize;
        GrabMinSize = style.GrabMinSize;

        // Borders
        WindowBorderSize = style.WindowBorderSize;
        ChildBorderSize = style.ChildBorderSize;
        PopupBorderSize = style.PopupBorderSize;
        FrameBorderSize = style.FrameBorderSize;
        TabBorderSize = style.TabBorderSize;
        TabBarBorderSize = style.TabBarBorderSize;

        // Rounding
        WindowRounding = style.WindowRounding;
        ChildRounding = style.ChildRounding;
        FrameRounding = style.FrameRounding;
        PopupRounding = style.PopupRounding;
        ScrollbarRounding = style.ScrollbarRounding;
        GrabRounding = style.GrabRounding;
        TabRounding = style.TabRounding;

        // Tables
        CellPadding = JsonVec2(style.CellPadding);
        TableAngledHeadersAngle = style.TableAngledHeadersAngle / MathF.PI * 180f;

        // Widgets
        WindowTitleAlign = JsonVec2(style.WindowTitleAlign);
        ButtonTextAlign = JsonVec2(style.ButtonTextAlign);
        SelectableTextAlign = JsonVec2(style.SelectableTextAlign);
        SeparatorTextBorderSize = style.SeparatorTextBorderSize;
        SeparatorTextAlign = JsonVec2(style.SeparatorTextAlign);
        SeparatorTextPadding = JsonVec2(style.SeparatorTextPadding);
        LogSliderDeadzone = style.LogSliderDeadzone;
    }

    public void Apply(ImGuiStylePtr style)
    {
        // colors
        for (int i = 0; i < (int)ImGuiCol.COUNT; i++)
        {
            var name = ImGui.GetStyleColorName((ImGuiCol) i);
            
            if (Colors.TryGetValue(name, out float[]? col))
            {
                if (col is not null && col.Length == 4)
                {
                    style.Colors[i] = new Vector4(col[0], col[1], col[2], col[3]);
                }
            }
        }

        // sizes
        style.WindowMenuButtonPosition = WindowMenuButtonPosition switch
        {
            "none" => ImGuiDir.None,
            "left" => ImGuiDir.Left,
            "right" => ImGuiDir.Right,
            _ => ImGuiDir.Left
        };

        style.ColorButtonPosition = ColorButtonPosition switch
        {
            "left" => ImGuiDir.Left,
            "right" => ImGuiDir.Right,
            _ => ImGuiDir.Right
        };
        
        style.WindowPadding = JsonVec2(WindowPadding);
        style.FramePadding = JsonVec2(FramePadding);
        style.ItemSpacing = JsonVec2(ItemSpacing);
        style.ItemInnerSpacing = JsonVec2(ItemInnerSpacing);
        style.TouchExtraPadding = JsonVec2(TouchExtraPadding);
        style.IndentSpacing = IndentSpacing;
        style.ScrollbarSize = ScrollbarSize;
        style.GrabMinSize = GrabMinSize;

        // Borders
        style.WindowBorderSize = WindowBorderSize;
        style.ChildBorderSize = ChildBorderSize;
        style.PopupBorderSize = PopupBorderSize;
        style.FrameBorderSize = FrameBorderSize;
        style.TabBorderSize = TabBorderSize;
        style.TabBarBorderSize = TabBarBorderSize;

        // Rounding
        style.WindowRounding = WindowRounding;
        style.ChildRounding = ChildRounding;
        style.FrameRounding = FrameRounding;
        style.PopupRounding = PopupRounding;
        style.ScrollbarRounding = ScrollbarRounding;
        style.GrabRounding = GrabRounding;
        style.TabRounding = TabRounding;

        // Tables
        style.CellPadding = JsonVec2(CellPadding);
        style.TableAngledHeadersAngle = TableAngledHeadersAngle / 180f * MathF.PI;

        // Widgets
        style.WindowTitleAlign = JsonVec2(WindowTitleAlign);
        style.ButtonTextAlign = JsonVec2(ButtonTextAlign);
        style.SelectableTextAlign = JsonVec2(SelectableTextAlign);
        style.SeparatorTextBorderSize = SeparatorTextBorderSize;
        style.SeparatorTextAlign = JsonVec2(SeparatorTextAlign);
        style.SeparatorTextPadding = JsonVec2(SeparatorTextPadding);
        style.LogSliderDeadzone = LogSliderDeadzone;
    }

    private static readonly JsonSerializerOptions serializerOptions = new()
    {
        // fine, c#... you win... with your stupid pascal case...
        
        //PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip
        //DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
    };

    public void WriteToFile(string path)
    {
        var json = JsonSerializer.Serialize(this, serializerOptions);
        File.WriteAllText(path, json);
    }

    public static SerializableStyle? FromFile(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SerializableStyle>(json, serializerOptions);
    }
}