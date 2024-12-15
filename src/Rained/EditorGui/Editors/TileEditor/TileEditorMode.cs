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

    abstract public void ShowGroupList();
    abstract public void ShowAssetList();
    abstract protected void ProcessSearch(string searchQuery);
    public void ProcessSearch() => ProcessSearch(SearchQuery);

    public void Draw()
    {
        var searchInputFlags = ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EscapeClearsAll;
        
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.InputTextWithHint("##Search", "Search...", ref searchQuery, 128, searchInputFlags))
        {
            ProcessSearch();
        }

        var halfWidth = ImGui.GetContentRegionAvail().X / 2f - ImGui.GetStyle().ItemSpacing.X / 2f;
        var boxHeight = ImGui.GetContentRegionAvail().Y;
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
    protected enum RectMode { Inactive, Place, Remove };
    protected RectMode rectMode = 0;
    protected CellPosition rectStart;

    protected readonly TileEditor editor = editor;

    abstract public string TabName { get; }

    abstract public void Process();
    abstract public void DrawToolbar();

    public virtual void Focus()
    {
        rectMode = RectMode.Inactive;
    }
    
    public virtual void Unfocus()
    {
        rectMode = RectMode.Inactive;
    }

    public virtual void UndidOrRedid() {}
}