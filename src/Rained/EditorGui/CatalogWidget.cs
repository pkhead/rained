namespace Rained.EditorGui;

using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using Raylib_cs;

abstract class CatalogWidget
{
    private string searchQuery = "";

    protected abstract bool HasSearch { get; }

    /// <summary>
    /// True if it should show both the group and item box.
    /// If false, it only shows the item box.
    /// </summary>
    protected abstract bool Dual { get; }
    public string SearchQuery { get => searchQuery; set => searchQuery = value; }
    
    public Vector2? WidgetSize = null;

    public static bool ColoredSelectable(string label, Color color, bool isSelected)
    {
        var drawList = ImGui.GetWindowDrawList();
        float textHeight = ImGui.GetTextLineHeight();

        const string leftPadding = "  ";
        float colorWidth = ImGui.CalcTextSize(leftPadding).X - ImGui.GetStyle().ItemInnerSpacing.X;
        
        var cursor = ImGui.GetCursorScreenPos();

        drawList.AddRectFilled(
            p_min: cursor,
            p_max: cursor + new Vector2(colorWidth, textHeight),
            ImGui.ColorConvertFloat4ToU32(new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, 1f))
        );

        return ImGui.Selectable(leftPadding + label, isSelected);
    }

    abstract protected void RenderGroupList();
    abstract protected void RenderItemList();

    abstract protected void ProcessSearch(string searchQuery);
    public void ProcessSearch() => ProcessSearch(SearchQuery);

    protected bool PassesSearchQuery(string value)
    {
        return string.IsNullOrEmpty(searchQuery) || value.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase);
    }

    public void Draw()
    {
        var searchInputFlags = ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EscapeClearsAll;
        var widgetWidth = WidgetSize?.X ?? ImGui.GetContentRegionAvail().X;

        ImGui.SetNextItemWidth(widgetWidth);
        if (HasSearch)
        {
            if (ImGui.InputTextWithHint("##Search", "Search...", ref searchQuery, 128, searchInputFlags))
            {
                ProcessSearch();
            }
        }

        float boxWidth;
        var boxHeight = WidgetSize?.Y ?? ImGui.GetContentRegionAvail().Y;
        if (Dual)
        {
            boxWidth = widgetWidth / 2f - ImGui.GetStyle().ItemSpacing.X / 2f;
            if (ImGui.BeginListBox("##Groups", new Vector2(boxWidth, boxHeight)))
            {
                RenderGroupList();
                ImGui.EndListBox();
            }

            ImGui.SameLine(); // for item box
        }
        else
        {
            boxWidth = widgetWidth;
        }
        
        if (ImGui.BeginListBox("##Items", new Vector2(boxWidth, boxHeight)))
        {
            RenderItemList();
            ImGui.EndListBox();
        }
    }
}

// god, what do i name this...
abstract class CatalogWidgetExt : CatalogWidget
{
    protected readonly List<int> displayedGroups = [];
    protected int selectedGroup = 0;
    protected int selectedItem = 0;

    public int SelectedGroup {
        get => selectedGroup!;
        set
        {
            if (selectedGroup != value)
            {
                selectedGroup = value;
                ResetSelectedItem();
            }
        }
    }
    public int SelectedItem { get => selectedItem; set => selectedItem = value; }

    protected override bool HasSearch => true;
    protected override bool Dual => true;

    protected abstract string GetGroupName(int group);
    protected abstract string GetItemName(int group, int item);
    protected abstract IEnumerable<int> GetGroupList();
    protected abstract IEnumerable<int> GetItemList(int group);

    protected void ResetSelectedItem()
    {
        bool first = true;
        foreach (var i in GetItemList(selectedGroup))
        {
            if (first)
            {
                selectedItem = i;
                first = false;
            }

            if (PassesSearchQuery(GetItemName(selectedGroup, i)))
            {
                selectedItem = i;
                break;
            }
        }
    }

    protected override void ProcessSearch(string searchQuery)
    {
        displayedGroups.Clear();

        // find groups that have any entries that pass the search query
        foreach (var group in GetGroupList())
        {
            // if search query is empty, add this group to the search query
            if (string.IsNullOrEmpty(searchQuery))
            {
                displayedGroups.Add(group);
                continue;
            }

            // search is not empty, so scan the tiles in this group
            // if there is one tile that that passes the search query, the
            // group gets put in the list
            // (further searching is done in DrawViewport)
            foreach (var item in GetItemList(group))
            {
                var name = GetItemName(group, item);
                if (PassesSearchQuery(name))
                {
                    displayedGroups.Add(group);
                    break;
                }
            }
        }

        if (!displayedGroups.Contains(selectedGroup))
        {
            if (displayedGroups.Count > 0)
                selectedGroup = displayedGroups[0];

            ResetSelectedItem();
        }

        if (!PassesSearchQuery(GetItemName(selectedGroup, selectedItem)))
            ResetSelectedItem();
    }

    public int PreviousGroup(int group)
    {
        if (displayedGroups.Count == 0) return default;
        var idx = displayedGroups.IndexOf(group);
        if (idx == -1) return displayedGroups[0];

        return displayedGroups[Util.Mod(idx - 1, displayedGroups.Count)];
    }

    public int PreviousItem(int group, int item)
    {
        var list = GetItemList(group)
            .Where(x => PassesSearchQuery(GetItemName(group, x)))
            .ToArray();
        
        if (list.Length == 0) return item;

        var idx = Array.IndexOf(list, item);
        if (idx == -1) return list[0];
        
        if (idx == 0) return list[^1];
        else          return list[idx-1];
    }

    public int NextGroup(int group)
    {
        if (displayedGroups.Count == 0) return group;
        var idx = displayedGroups.IndexOf(group);
        if (idx == -1) return displayedGroups[0];

        return displayedGroups[(idx + 1) % displayedGroups.Count];
    }

    public int NextItem(int group, int item)
    {
        var list = GetItemList(group)
            .Where(x => PassesSearchQuery(GetItemName(group, x)))
            .ToArray();
        
        if (list.Length == 0) return item;

        var idx = Array.IndexOf(list, item);
        if (idx == -1) return list[0];
        
        return list[(idx + 1) % list.Length];
    }
}

class GenericDualCatalogWidget : CatalogWidgetExt
{
    public bool ShowGroupColors = false;
    public bool ShowItemColors = false;

    public delegate (string name, Color color) GetGroupInfoDelegate(int group);
    public delegate (string name, Color color) GetItemInfoDelegate(int group, int item);
    public delegate bool RenderItemDelegate(int item, bool selected);
    public delegate void ItemPostRenderDelegate(int item, bool selected);
    public delegate IEnumerable<int> GetGroupsDelegate();
    public delegate IEnumerable<int> GetItemsInGroupDelegate(int group);

    /// <summary>
    /// Callback to return group data for rendering.
    /// </summary>
    public required GetGroupInfoDelegate GetGroupInfo;
    public required GetItemInfoDelegate GetItemInfo;
    public RenderItemDelegate? RenderItem = null;
    public ItemPostRenderDelegate? ItemPostRender = null;

    /// <summary>
    /// Callback that returns list of groups to render.
    /// </summary>
    public required GetGroupsDelegate GetGroups;

    /// <summary>
    /// Callback that returns the list of item IDs from a group ID.
    /// </summary>
    public required GetItemsInGroupDelegate GetItemsInGroup;

    protected override string GetGroupName(int group) => GetGroupInfo(group).name;
    protected override string GetItemName(int group, int item) => GetItemInfo(group, item).name;
    protected override IEnumerable<int> GetGroupList() => GetGroups();
    protected override IEnumerable<int> GetItemList(int group) => GetItemsInGroup(group);

    override protected void RenderGroupList()
    {
        if ((selectedGroup == -1 || !displayedGroups.Contains(selectedGroup)) && displayedGroups.Count > 0)
            selectedGroup = displayedGroups[0];

        foreach (var group in displayedGroups)
        {
            var (name, color) = GetGroupInfo(group);
            bool isSelected = selectedGroup!.Equals(group);

            bool pressed = ShowGroupColors ? ColoredSelectable(name, color, isSelected) : ImGui.Selectable(name, isSelected);
            if (pressed || displayedGroups.Count == 1)
            {
                if (!selectedGroup.Equals(group))
                {
                    selectedGroup = group;
                    selectedItem = GetItemsInGroup(selectedGroup).First();
                }
            }
        }
    }

    override protected void RenderItemList()
    {
        Debug.Assert(selectedGroup != -1);
        if (selectedGroup == -1 || displayedGroups.Count == 0) return;

        foreach (var item in GetItemsInGroup(selectedGroup))
        {
            var (name, color) = GetItemInfo(selectedGroup, item);
            if (!PassesSearchQuery(name))
                continue;
            
            bool isSelected = item!.Equals(selectedItem);
            bool pressed;

            if (RenderItem is not null)
            {
                pressed = RenderItem(item, isSelected);
            }
            else
            {
                pressed = ShowItemColors ? ColoredSelectable(name, color, isSelected) : ImGui.Selectable(name, isSelected);
            }

            if (pressed)
            {
                selectedItem = item;
            }

            ItemPostRender?.Invoke(item, isSelected);
        }
    }
}