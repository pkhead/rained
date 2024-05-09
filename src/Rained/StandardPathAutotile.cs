namespace RainEd.Autotiles;
using System.Numerics;
using ImGuiNET;
using rlImGui_cs;
using System.Globalization;
using System.Text;

class StandardPathAutotile : Autotile
{
    public PathTileTable TileTable;
    public override string[] MissingTiles { get => []; }

    private static FileBrowser? fileBrowser = null;

    public StandardPathAutotile(int thickness, int length, string ld, string lu, string rd, string ru, string vert, string horiz)
    {
        Name = "My Autotile";
        Type = AutotileType.Path;
        PathThickness = thickness;
        SegmentLength = length;

        TileTable.LeftDown = ld;
        TileTable.LeftUp = lu;
        TileTable.RightDown = rd;
        TileTable.RightUp = ru;
        TileTable.Vertical = vert;
        TileTable.Horizontal = horiz;

        // placeholder values
        TileTable.TRight = "Pipe TJunct E";
        TileTable.TUp = "Pipe TJunct N";
        TileTable.TLeft = "Pipe TJunct W";
        TileTable.TDown = "Pipe TJunct S";
        TileTable.XJunct = "Pipe XJunct";

        CheckTiles();
        ProcessSearch();
    }

    private bool IsInvalid(string tileName, TileType tileType)
    {
        return
            !RainEd.Instance.TileDatabase.HasTile(tileName) ||
            !CheckDimensions(RainEd.Instance.TileDatabase.GetTileFromName(tileName), tileType);
    }

    private void CheckTiles()
    {
        CanActivate = !(
            IsInvalid(TileTable.LeftDown, TileType.Turn) ||
            IsInvalid(TileTable.LeftUp, TileType.Turn) ||
            IsInvalid(TileTable.RightDown, TileType.Turn) ||
            IsInvalid(TileTable.RightUp, TileType.Turn) ||
            IsInvalid(TileTable.Vertical, TileType.Vertical) ||
            IsInvalid(TileTable.Horizontal, TileType.Horizontal)
        );
    }

    enum TileType
    {
        Horizontal,
        Vertical,
        Turn
    }

    private bool CheckDimensions(Tiles.Tile tile, TileType tileType)
        => tileType switch
        {
            TileType.Horizontal => tile.Width == SegmentLength && tile.Height == PathThickness,
            TileType.Vertical => tile.Width == PathThickness && tile.Height == SegmentLength,
            TileType.Turn => tile.Width == tile.Height && tile.Width == PathThickness,
            _ => false
        };

    public override void ConfigGui()
    {
        ImGui.PushItemWidth(ImGui.GetTextLineHeight() * 10f);
        
        TileButton(ref TileTable.LeftDown, "Left-Down", TileType.Turn);
        TileButton(ref TileTable.LeftUp, "Left-Up", TileType.Turn);
        TileButton(ref TileTable.RightDown, "Right-Down", TileType.Turn);
        TileButton(ref TileTable.RightUp, "Right-Up", TileType.Turn);
        TileButton(ref TileTable.Vertical, "Vertical", TileType.Vertical);
        TileButton(ref TileTable.Horizontal, "Horizontal", TileType.Horizontal);
        ImGui.Checkbox("Intersections", ref TileTable.Intersections);
        if (TileTable.Intersections)
        {
            TileButton(ref TileTable.TRight, "T-Junction Right", TileType.Turn);
            TileButton(ref TileTable.TUp, "T-Junction Up", TileType.Turn);
            TileButton(ref TileTable.TLeft, "T-Junction Left", TileType.Turn);
            TileButton(ref TileTable.TDown, "T-Junction Down", TileType.Turn);
            TileButton(ref TileTable.XJunct, "Four-way Junction", TileType.Turn);
        }

        // separator between TileButtons and number options
        ImGui.Separator();

        if (ImGui.InputInt("##Thickness", ref PathThickness))
        {
            PathThickness = Math.Clamp(PathThickness, 1, 10);
            CheckTiles();
        }

        ImGui.SameLine(); // WHY DOESN'T THE TEXT ALIGN!!!
        ImGui.Text("Thickness");

        if (ImGui.InputInt("##Length", ref SegmentLength))
        {
            SegmentLength = Math.Clamp(SegmentLength, 1, 10);
            CheckTiles();
        }

        ImGui.SameLine(); // WHY DOESN'T THE TEXT ALIGN!!!
        ImGui.Text("Segment Length");

        ImGui.PopItemWidth();

        ImGui.SeparatorText("Options");
        if (ImGui.Button("Delete"))
        {
            ImGui.OpenPopup("Delete?");
        }
        ImGui.SameLine();

        // show deletion confirmation prompt
        ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
        if (ImGuiExt.BeginPopupModal("Delete?", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
        {
            ImGui.TextUnformatted($"Are you sure you want to delete '{Name}'?");
            
            ImGui.Separator();
            if (StandardPopupButtons.Show(PopupButtonList.YesNo, out int btn))
            {
                if (btn == 0) // yes
                {
                    RainEd.Instance.Autotiles.DeleteStandard(this);
                }

                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        if (ImGui.Button("Rename"))
        {
            RainEd.Instance.Autotiles.OpenRenamePopup(this);
        }
        ImGui.SameLine();

        RainEd.Instance.Autotiles.RenderRenamePopup();

        if (ImGui.Button("Convert to Plugin"))
        {
            fileBrowser = new FileBrowser(FileBrowser.OpenMode.Write, ConvertToPlugin, Path.Combine(Boot.AppDataPath, "scripts", "autoload", "autotiles"));
            fileBrowser.AddFilter("Lua script", ".lua");
        }

        FileBrowser.Render(ref fileBrowser);
    }

    private static string StringLiteral(string str)
    {
        str = str.Replace("\"", "\\\"");
        return "\"" + str + "\"";
    }

    private const string luaScriptTemplate =
        """
        local helpers = require("helpers") -- load the helpers.lua module

        -- setup autotile data
        local autotile = rained.createAutotile({0})
        autotile.type = "{1}"
        autotile.pathThickness = {2}
        autotile.segmentLength = {3}

        -- Rained will not allow the user to use this autotile
        -- if any of the tiles in this table are not installed
        autotile.requiredTiles = {{
            {8},
            {9},
            {4},
            {5},
            {6},
            {7}
        }}

        -- table of tiles to use for the standard autotiler function
        -- which is helpers.autotilePath
        local tileTable = {{
            ld = {4},
            lu = {5},
            rd = {6},
            ru = {7},
            vertical = {8},
            horizontal = {9}
        }}

        -- this is the callback function that Rained invokes when the user
        -- wants to autotile a given path
        ---@param layer integer The layer to run the autotiler on
        ---@param segments PathSegment[] The list of path segments
        ---@param forceModifier ForceModifier Force-placement mode, as a string. Can be nil, "force", or "geometry".
        function autotile:tilePath(layer, segments, forceModifier)
            helpers.autotilePath(tileTable, layer, segments, forceModifier)
        end
        """;

    private void ConvertToPlugin(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;
        
        // check if save location is in the autoload folder
        var relativePath = Path.GetRelativePath(Path.Combine(Boot.AppDataPath, "scripts", "autoload"), filePath);
        if (relativePath[0] == '.')
        {
            RainEd.Instance.ShowNotification("The save location should be in the autoload folder!");
            return;
        }

        var catName = RainEd.Instance.Autotiles.GetCategoryNameOf(this);

        var luaScript = string.Format(luaScriptTemplate,
            // autotile name[, category]
            catName == "Misc" ? StringLiteral(Name) : StringLiteral(Name) + ", " + StringLiteral(catName),

            // autotile type
            "path",

            // path thickness & segment length
            PathThickness, SegmentLength,

            // tile table
            StringLiteral(TileTable.LeftDown),
            StringLiteral(TileTable.LeftUp),
            StringLiteral(TileTable.RightDown),
            StringLiteral(TileTable.RightUp),
            StringLiteral(TileTable.Vertical),
            StringLiteral(TileTable.Horizontal)
        );

        File.WriteAllText(filePath, luaScript);
        RainEd.Instance.Autotiles.DeleteStandard(this);

        // run the lua file using require
        // (also, cut off the .lua file extension at the end so that
        // it works properly)
        var L = LuaInterface.NLuaState.State;
        var requirePath = "autoload." + relativePath[..^4].Replace(Path.DirectorySeparatorChar, '.');
        
        // first, unload the "package"
        L.GetGlobal("package");
        L.GetField(-1, "loaded");
        L.PushString(requirePath);
        L.PushNil();
        L.SetTable(-3);
        L.Pop(2); // pop package and package.loaded

        // then, load it again.
        L.GetGlobal("require");
        L.PushString(requirePath);
        L.Call(1, 0);
    }

    private void TileButton(ref string tile, string label, TileType tileType)
    {
        bool invalid = IsInvalid(tile, tileType);

        // if invalid, make button red
        if (invalid)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.95f, 0.32f, 0.32f, 0.40f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.95f, 0.32f, 0.32f, 0.52f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.95f, 0.32f, 0.32f, 1.00f));
        }

        ImGui.PushID(label);
        if (ImGui.Button(tile, new Vector2(ImGui.GetTextLineHeight() * 10f, 0f)))
        {
            ImGui.OpenPopup("PopupTileSelector");
        }

        if (invalid)
        {
            ImGui.PopStyleColor(3);
        }

        ImGui.SameLine();
        ImGui.Text(label);

        ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
        if (ImGui.BeginPopup("PopupTileSelector"))
        {
            ImGui.TextUnformatted("Select Tile");
            ImGui.Separator();

            TileSelectorGui();

            if (selectedTile != null)
            {
                if (CheckDimensions(selectedTile, tileType))
                {
                    tile = selectedTile.Name;

                    CanActivate = true;
                    CheckTiles();
                }
                else
                {
                    RainEd.Instance.ShowNotification("Tile dimensions do not match autotile dimensions");
                }

                selectedTile = null;
                ImGui.CloseCurrentPopup();
            }

            if (EditorWindow.IsKeyPressed(ImGuiKey.Escape))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        ImGui.PopID();
    }

    public override void TilePath(int layer, PathSegment[] pathSegments, bool force, bool geometry)
    {
        var modifier = TilePlacementMode.Normal;
        if (geometry)   modifier = TilePlacementMode.Geometry;
        else if (force) modifier = TilePlacementMode.Force;

        StandardTilePath(TileTable, layer, pathSegments, modifier);
    }

    // copied from TileEditorToolbar.cs
    private string searchQuery = "";
    private int selectedTileGroup = 0;
    private Tiles.Tile? selectedTile = null;

    // available groups (available = passes search)
    private readonly List<int> matSearchResults = [];
    private readonly List<int> tileSearchResults = [];

    private void ProcessSearch()
    {
        var tileDb = RainEd.Instance.TileDatabase;
        var matDb = RainEd.Instance.MaterialDatabase;

        tileSearchResults.Clear();
        matSearchResults.Clear();

        // find material groups that have any entires that pass the searchq uery
        for (int i = 0; i < matDb.Categories.Count; i++)
        {
            // if search query is empty, add this group to the results
            if (searchQuery == "")
            {
                matSearchResults.Add(i);
                continue;
            }

            // search is not empty, so scan the materials in this group
            // if there is one material that that passes the search query, the
            // group gets put in the list
            // (further searching is done in DrawViewport)
            for (int j = 0; j < matDb.Categories[i].Materials.Count; j++)
            {
                // this material passes the search, so add this group to the search results
                if (matDb.Categories[i].Materials[j].Name.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
                {
                    matSearchResults.Add(i);
                    break;
                }
            }
        }

        // find groups that have any entries that pass the search query
        for (int i = 0; i < tileDb.Categories.Count; i++)
        {
            // if search query is empty, add this group to the search query
            if (searchQuery == "")
            {
                tileSearchResults.Add(i);
                continue;
            }

            // search is not empty, so scan the tiles in this group
            // if there is one tile that that passes the search query, the
            // group gets put in the list
            // (further searching is done in DrawViewport)
            for (int j = 0; j < tileDb.Categories[i].Tiles.Count; j++)
            {
                // this tile passes the search, so add this group to the search results
                if (tileDb.Categories[i].Tiles[j].Name.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
                {
                    tileSearchResults.Add(i);
                    break;
                }
            }
        }
    }

    private void TileSelectorGui()
    {
        var tileDb = RainEd.Instance.TileDatabase;

        var searchInputFlags = ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EscapeClearsAll;

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.InputTextWithHint("##Search", "Search...", ref searchQuery, 128, searchInputFlags))
        {
            ProcessSearch();
        }

        var halfWidth = ImGui.GetTextLineHeight() * 16f;
        var boxHeight = ImGui.GetTextLineHeight() * 25f;
        if (ImGui.BeginListBox("##Groups", new Vector2(halfWidth, boxHeight)))
        {
            var drawList = ImGui.GetWindowDrawList();
            float textHeight = ImGui.GetTextLineHeight();

            foreach (var i in tileSearchResults)
            {
                var group = tileDb.Categories[i];
                var cursor = ImGui.GetCursorScreenPos();

                if (ImGui.Selectable("  " + group.Name, selectedTileGroup == i) || tileSearchResults.Count == 1)
                    selectedTileGroup = i;
                
                drawList.AddRectFilled(
                    p_min: cursor,
                    p_max: cursor + new Vector2(10f, textHeight),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(group.Color.R / 255f, group.Color.G / 255f, group.Color.B / 255f, 1f))
                );
            }
            
            ImGui.EndListBox();
        }
        
        // group listing (effects) list box
        ImGui.SameLine();
        if (ImGui.BeginListBox("##Tiles", new Vector2(halfWidth, boxHeight)))
        {
            var tileList = tileDb.Categories[selectedTileGroup].Tiles;

            for (int i = 0; i < tileList.Count; i++)
            {
                var tile = tileList[i];

                // don't show this prop if it doesn't pass search test
                if (!tile.Name.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
                    continue;
                
                if (ImGui.Selectable(tile.Name))
                {
                    selectedTile = tile;
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();

                    var previewTexture = RainEd.Instance.AssetGraphics.GetTilePreviewTexture(tile);
                    if (previewTexture is not null)
                        rlImGui.Image(previewTexture, tile.Category.Color);
                    else
                        rlImGui.ImageSize(RainEd.Instance.PlaceholderTexture, 16, 16);

                    ImGui.EndTooltip();
                }
            }
            
            ImGui.EndListBox();
        }
    }

    public void Save(List<string> lines)
    {
        var catName = RainEd.Instance.Autotiles.GetCategoryNameOf(this);
        var header = $"[{Name}:{catName}]";

        // find the location of the autotile in the line list    
        var location = lines.IndexOf(header);

        // a tile in the Misc category may have its category name omitted, so we search for that too.
        if (location == -1 && catName == "Misc")
        {
            header = $"[{Name}]";
            location = lines.IndexOf(header);
        }
    
        // not found, append lines
        if (location == -1)
        {
            RainEd.Logger.Information("Append autotile {Header}", header);

            lines.Add("");
            lines.Add(header);
            lines.Add("thickness=" + PathThickness.ToString(CultureInfo.InvariantCulture));
            lines.Add("length=" + SegmentLength.ToString(CultureInfo.InvariantCulture));
            lines.Add("ld=" + TileTable.LeftDown);
            lines.Add("lu=" + TileTable.LeftUp);
            lines.Add("rd=" + TileTable.RightDown);
            lines.Add("ru=" + TileTable.RightUp);
            lines.Add("vertical=" + TileTable.Vertical);
            lines.Add("horizontal=" + TileTable.Horizontal);
        }

        // was found, overwrite lines
        else
        {
            RainEd.Logger.Information("Overwrite autotile {Header}", header);

            lines[location+1] = "thickness=" + PathThickness.ToString(CultureInfo.InvariantCulture);
            lines[location+2] = "length=" + SegmentLength.ToString(CultureInfo.InvariantCulture);
            lines[location+3] = "ld=" + TileTable.LeftDown;
            lines[location+4] = "lu=" + TileTable.LeftUp;
            lines[location+5] = "rd=" + TileTable.RightDown;
            lines[location+6] = "ru=" + TileTable.RightUp;
            lines[location+7] = "vertical=" + TileTable.Vertical;
            lines[location+8] = "horizontal=" + TileTable.Horizontal;
        }
    }

    public void Delete(List<string> lines)
    {
        var catName = RainEd.Instance.Autotiles.GetCategoryNameOf(this);
        var header = $"[{Name}:{catName}]";

        // find the location of the autotile in the line list    
        var location = lines.IndexOf(header);

        // a tile in the Misc category may have its category name omitted, so we search for that too.
        if (location == -1 && catName == "Misc")
        {
            header = $"[{Name}]";
            location = lines.IndexOf(header);
        }
    
        // delete if found
        if (location >= 0)
        {
            RainEd.Logger.Information("Delete autotile {Header}", header);

            lines.RemoveRange(location, 9);

            // remove newline before autotile definition
            if (location > 0 && string.IsNullOrWhiteSpace(lines[location-1]))
            {
                lines.RemoveAt(location-1);
            }
        }
    }

    public void Rename(List<string> lines, string newName, string newCategory)
    {
        var catName = RainEd.Instance.Autotiles.GetCategoryNameOf(this);
        var header = $"[{Name}:{catName}]";

        // find the location of the autotile in the line list    
        var location = lines.IndexOf(header);

        // a tile in the Misc category may have its category name omitted, so we search for that too.
        if (location == -1 && catName == "Misc")
        {
            header = $"[{Name}]";
            location = lines.IndexOf(header);
        }
    
        // delete if found
        if (location >= 0)
        {
            lines[location] = newCategory == "Misc" ? $"[{newName}]" : $"[{newName}:{newCategory}]";
            RainEd.Logger.Information("Rename autotile {Old} to {New}", header, lines[location]);
        }
    }
}