namespace Rained.EditorGui.Editors;
using Raylib_cs;
using System.Numerics;
using Rained.LevelData;
using Rained.Assets;
using ImGuiNET;

abstract class TileEditorCatalog
{
    private string searchQuery = "";
    protected string SearchQuery => searchQuery;
    public Vector2? WidgetSize = null;

    abstract public void ShowGroupList();
    abstract public void ShowAssetList();
    abstract protected void ProcessSearch(string searchQuery);
    public void ProcessSearch() => ProcessSearch(SearchQuery);

    public void Draw()
    {
        var searchInputFlags = ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EscapeClearsAll;
        var widgetWidth = WidgetSize?.X ?? ImGui.GetContentRegionAvail().X;

        ImGui.SetNextItemWidth(widgetWidth);
        if (ImGui.InputTextWithHint("##Search", "Search...", ref searchQuery, 128, searchInputFlags))
        {
            ProcessSearch();
        }

        var halfWidth = widgetWidth / 2f - ImGui.GetStyle().ItemSpacing.X / 2f;
        var boxHeight = WidgetSize?.Y ?? ImGui.GetContentRegionAvail().Y;
        if (ImGui.BeginListBox("##Groups", new Vector2(halfWidth, boxHeight)))
        {
            ShowGroupList();
            ImGui.EndListBox();
        }
        
        // group listing (effects) list box
        ImGui.SameLine();
        if (ImGui.BeginListBox("##Tiles", new Vector2(halfWidth, boxHeight)))
        {
            ShowAssetList();
            ImGui.EndListBox();
        }
    }
}

abstract class TileEditorMode(TileEditor editor)
{
    public enum RectMode { Inactive, Place, Remove };
    protected RectMode rectMode = 0;
    protected CellPosition rectStart;

    protected readonly TileEditor editor = editor;
    protected bool LeftMouseDown { get; private set; } = false;
    protected bool RightMouseDown { get; private set; } = false;

    abstract public string TabName { get; }
    public RectMode CurrentRectMode => rectMode;

    public void ResetInput()
    {
        LeftMouseDown = false;
        RightMouseDown = false;
    }

    virtual public void Process()
    {
        if (EditorWindow.IsMouseClicked(ImGuiMouseButton.Left))
            LeftMouseDown = true;
        
        if (KeyShortcuts.Activated(KeyShortcut.RightMouse))
            RightMouseDown = true;
        
        if (EditorWindow.IsMouseReleased(ImGuiMouseButton.Left))
            LeftMouseDown = false;
        
        if (KeyShortcuts.Deactivated(KeyShortcut.RightMouse))
            RightMouseDown = false;
    }

    virtual public void IdleProcess() {}

    abstract public void DrawToolbar();

    public virtual void Focus()
    {
        rectMode = RectMode.Inactive;
        LeftMouseDown = false;
        RightMouseDown = false;
    }
    
    public virtual void Unfocus()
    {
        rectMode = RectMode.Inactive;
        LeftMouseDown = false;
        RightMouseDown = false;

        RainEd.Instance.LevelView.CellChangeRecorder.TryPushChange();
    }

    public virtual void UndidOrRedid() {}
}