namespace Rained.EditorGui.Editors;
using Raylib_cs;
using ImGuiNET;
using System.Numerics;
using Rained.LevelData;

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
        MagicWand,
        OpReplace,
        OpAdd,
        OpSubtract,
        OpIntersect
    };

    enum SelectionTool
    {
        Rect,
        MoveSelection,
        Lasso,
        MagicWand
    };
    private SelectionTool curTool = SelectionTool.Rect;
    static readonly (IconName icon, string name)[] toolInfo = [
        (IconName.SelectRect, "Rectangle Select"),
        (IconName.LassoSelect, "Lasso Select"),
        (IconName.MoveSelection, "Move Selection"),
        (IconName.MagicWand, "Magic Wand"),
    ];

    enum SelectionOperator
    {
        Replace,
        Add,
        Subtract,
        Intersect
    }
    private SelectionOperator curOp = SelectionOperator.Replace;
    static readonly (IconName icon, string name)[] operatorInfo = [
        (IconName.OpReplace, "Replace"),
        (IconName.OpAdd, "Add"),
        (IconName.OpSubtract, "Subtract"),
        (IconName.OpIntersect, "Intersect"),
    ];

    private int selectionMinX = 0;
    private int selectionMinY = 0;
    private int selectionMaxX = 0;
    private int selectionMaxY = 0;
    private bool selectionActive = false;

    // used for mouse drag
    private bool mouseWasDragging = false;
    private int selectionStartX = -1;
    private int selectionStartY = -1;
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

        var textColorVec4 = ImGui.GetStyle().Colors[(int)ImGuiCol.Text] * 255f;
        bool pressed = ImGuiExt.ImageButtonRect(
            "##test",
            icons,
            buttonSize, buttonSize,
            GetIconRect(icon),
            new Color((int)textColorVec4.X, (int)textColorVec4.Y, (int)textColorVec4.Z, (int)textColorVec4.W)
        );

        ImGui.PopStyleVar();
        return pressed;
    }

    public void DrawStatusBar()
    {
        // selection mode options
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
                ImGui.SetItemTooltip(info.name);

                group.EndButton();
            }
            ImGui.PopStyleVar();
        }

        // operator mode options
        ImGui.SameLine();
        using (var group = ImGuiExt.ButtonGroup.Begin("Operator Mode", 4, 0))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 0f));
            for (int i = 0; i < operatorInfo.Length; i++)
            {
                group.BeginButton(i, (int)curOp == i);

                ref var info = ref operatorInfo[i];
                if (IconButton(info.icon))
                {
                    curOp = (SelectionOperator)i;
                }
                ImGui.SetItemTooltip(info.name);

                group.EndButton();
            }
            ImGui.PopStyleVar();
        }

        ImGui.SameLine();
        if (ImGui.Button("Copy") || EditorWindow.IsKeyPressed(ImGuiKey.Enter))
            Active = false;
        
        ImGui.SameLine();
        if (ImGui.Button("Cancel") || EditorWindow.IsKeyPressed(ImGuiKey.Escape))
            Active = false;
    }

    public void Update()
    {
        // TODO: crosshair cursor
        
        // update
        var view = RainEd.Instance.LevelView;
        if (view.IsViewportHovered)
        {
            if (EditorWindow.IsMouseDragging(ImGuiMouseButton.Left))
            {
                if (!mouseWasDragging)
                {
                    selectionStartX = view.MouseCx;
                    selectionStartY = view.MouseCy;
                    selectionActive = true;
                }

                var endX = view.MouseCx;
                var endY = view.MouseCy;

                selectionMinX = Math.Min(selectionStartX, endX);
                selectionMaxX = Math.Max(selectionStartX, endX);
                selectionMinY = Math.Min(selectionStartY, endY);
                selectionMaxY = Math.Max(selectionStartY, endY);
            }
        }

        mouseWasDragging = EditorWindow.IsMouseDragging(ImGuiMouseButton.Left);

        // draw
        Raylib.BeginShaderMode(Shaders.OutlineMarqueeShader);

        Shaders.OutlineMarqueeShader.GlibShader.SetUniform("time", (float)Raylib.GetTime());
        RainEd.Instance.NeedScreenRefresh();

        // draw selection outline
        if (selectionActive)
        {
            var w = selectionMaxX - selectionMinX + 1;
            var h = selectionMaxY - selectionMinY + 1;
            
            Raylib.DrawRectangleLines(
                selectionMinX * Level.TileSize,
                selectionMinY * Level.TileSize,
                w * Level.TileSize,
                h * Level.TileSize,
                Color.White
            );
        }

        Raylib.EndShaderMode();
    }
}