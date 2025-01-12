namespace Rained.EditorGui.Editors;
using Raylib_cs;
using ImGuiNET;
using System.Numerics;

/// <summary>
/// Operations for copying, pasting, and moving of cells.
/// </summary>
class CellSelection
{
    public bool Active { get; private set; } = true;

    private static RlManaged.Texture2D icons = null!;
    enum IconName
    {
        SelectRect,
        MoveSelection,
        LassoSelect,
        MagicWand
    };

    enum SelectionTool
    {
        Rect,
        MoveSelection,
        Lasso,
        MagicWand
    };
    private SelectionTool curTool = SelectionTool.Rect;
    static readonly (IconName icon, string name, string tooltip)[] toolInfo = [
        (IconName.SelectRect, "Rectangle Select", "Select a rectangular area."),
        (IconName.LassoSelect, "Lasso Select", "Draw an outline of the area you want to select."),
        (IconName.MoveSelection, "Move Selection", "Move the selection area, but not the cells underneath."),
        (IconName.MagicWand, "Magic Wand", "Select connected geometry. Like Flood Fill, but for selection."),
    ];

    enum SelectionOperator
    {
        Replace,
        Add,
        Subtract,
        Intersect
    }
    private SelectionOperator curOp = SelectionOperator.Replace;
    static readonly (IconName icon, string tooltip)[] operatorInfo = [
        (IconName.SelectRect, "Replace"),
        (IconName.LassoSelect, "Add"),
        (IconName.MoveSelection, "Subtract"),
        (IconName.MagicWand, "Intersect"),
    ];

    // private int selectionMinX;
    // private int selectionMinY;
    // private int selectionMaxX;
    // private int selectionMaxY;
    // private bool[,] selectionMask;

    public CellSelection()
    {
        icons ??= RlManaged.Texture2D.Load(Path.Combine(Boot.AppDataPath, "assets", "statusbar.png"));
    }

    private static Rectangle GetIconRect(IconName icon)
    {
        return new Rectangle((int)icon * 16f, 0, 16f, 16f);
    }

    private static bool IconButton(IconName icon)
    {
        var framePadding = ImGui.GetStyle().FramePadding;
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, framePadding);
        var buttonSize = 16 * Boot.PixelIconScale;
        var desiredHeight = ImGui.GetFrameHeight();
        
        // sz + pad*2 = w
        // pad = (w - sz) / 2
        ImGui.GetStyle().FramePadding = new Vector2(
            MathF.Floor( (desiredHeight - buttonSize) / 2f ),
            MathF.Floor( (desiredHeight - buttonSize) / 2f )
        );
        bool pressed = ImGuiExt.ImageButtonRect("##test", icons, buttonSize, buttonSize, GetIconRect(icon));

        ImGui.PopStyleVar();
        return pressed;
    }

    public void DrawStatusBar()
    {
        static void ItemTooltipDesc(string tooltip, string desc)
        {
            if (ImGui.BeginItemTooltip())
            {
                ImGui.Text(tooltip);
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 24f);
                ImGui.TextDisabled(desc);
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }

        using (var group = ImGuiExt.ButtonGroup.Begin("Selection Mode", 4, 0))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 0f));
            for (int i = 0; i < toolInfo.Length; i++)
            {
                group.BeginButton(i, (int)curTool == i);

                ref var info = ref toolInfo[i];
                if (IconButton(info.icon))
                {
                    curTool = (SelectionTool)i;
                }
                ItemTooltipDesc(info.name, info.tooltip);

                group.EndButton();
            }
            ImGui.PopStyleVar();
        }

        ImGui.SameLine();
        if (ImGui.Button("Apply") || EditorWindow.IsKeyPressed(ImGuiKey.Enter))
            Active = false;
        
        ImGui.SameLine();
        if (ImGui.Button("Cancel") || EditorWindow.IsKeyPressed(ImGuiKey.Enter))
            Active = false;
    }

    public void Update()
    {
        Raylib.BeginShaderMode(Shaders.OutlineMarqueeShader);

        Shaders.OutlineMarqueeShader.GlibShader.SetUniform("time", (float)Raylib.GetTime());
        RainEd.Instance.NeedScreenRefresh();

        Raylib.DrawRectangleLines(0, 0, 400, 400, Color.White);
        
        Raylib.EndShaderMode();
    }
}