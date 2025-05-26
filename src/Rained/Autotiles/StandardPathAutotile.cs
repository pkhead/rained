namespace Rained.Autotiles;

using System.Numerics;
using ImGuiNET;
using LevelData;
using EditorGui;
using Rained.EditorGui.Editors;
using Rained.Assets;

class StandardPathAutotile : Autotile, ITileSelectionState
{
    public PathTileTable TileTable;
    public override bool AllowIntersections { get => TileTable.AllowJunctions; }
    public override bool AutoHistory => true;

    public StandardPathAutotile(int thickness, int length, string ld, string lu, string rd, string ru, string vert, string horiz)
    {
        Name = "My Autotile";
        Type = AutotileType.Path;
        PathThickness = 1;
        SegmentLength = 1;

        TileTable = new PathTileTable(ld, lu, rd, ru, vert, horiz);

        CheckTiles();

        catalogWidget = new TileCatalogWidget(this);
        catalogWidget.ProcessSearch();
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

        if (TileTable.AllowJunctions)
        {
            if (
                IsInvalid(TileTable.TRight, TileType.Turn) ||
                IsInvalid(TileTable.TUp, TileType.Turn) ||
                IsInvalid(TileTable.TLeft, TileType.Turn) ||
                IsInvalid(TileTable.TDown, TileType.Turn) ||
                IsInvalid(TileTable.XJunct, TileType.Turn)
            ) CanActivate = false;
        }

        if (TileTable.PlaceCaps)
        {
            if (
                IsInvalid(TileTable.CapRight, TileType.Horizontal) ||
                IsInvalid(TileTable.CapUp, TileType.Vertical) ||
                IsInvalid(TileTable.CapLeft, TileType.Horizontal) ||
                IsInvalid(TileTable.CapDown, TileType.Vertical)
            ) CanActivate = false;
        }
    }

    enum TileType
    {
        Horizontal,
        Vertical,
        Turn
    }

    private bool CheckDimensions(Assets.Tile tile, TileType tileType)
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
        
        if (ImGui.Checkbox("Allow Junctions", ref TileTable.AllowJunctions))
            CheckTiles();
        
        if (ImGui.Checkbox("Place Caps", ref TileTable.PlaceCaps))
            CheckTiles();

        if (TileTable.AllowJunctions)
        {
            ImGui.Separator();
            TileButton(ref TileTable.TRight, "T-Junction Right", TileType.Turn);
            TileButton(ref TileTable.TUp, "T-Junction Up", TileType.Turn);
            TileButton(ref TileTable.TLeft, "T-Junction Left", TileType.Turn);
            TileButton(ref TileTable.TDown, "T-Junction Down", TileType.Turn);
            TileButton(ref TileTable.XJunct, "Four-way Junction", TileType.Turn);
        }

        if (TileTable.PlaceCaps)
        {
            ImGui.Separator();
            TileButton(ref TileTable.CapRight, "Cap Right", TileType.Turn);
            TileButton(ref TileTable.CapUp, "Cap Up", TileType.Turn);
            TileButton(ref TileTable.CapLeft, "Cap Left", TileType.Turn);
            TileButton(ref TileTable.CapDown, "Cap Down", TileType.Turn);
        }

        /*
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
        ImGui.Text("Segment Length");*/

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
            ImGui.SetNextWindowPos(ImGui.GetCursorScreenPos(), ImGuiCond.Appearing);
        }

        if (invalid)
        {
            ImGui.PopStyleColor(3);
        }

        ImGui.SameLine();
        ImGui.Text(label);

        //ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
        
        if (ImGui.BeginPopup("PopupTileSelector"))
        {
            //ImGui.TextUnformatted("Select Tile");
            //ImGui.Separator();
            
            SelectedTile = null;
            catalogWidget.WidgetSize = new Vector2(
                ImGui.GetTextLineHeight() * 32f,
                ImGui.GetTextLineHeight() * 25f
            );
            catalogWidget.Draw();

            if (SelectedTile != null)
            {
                tile = SelectedTile.Name;
                CanActivate = true;
                CheckTiles();
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

    private TileCatalogWidget catalogWidget;
    private TileType activeTileType;

    // copied from TileEditorToolbar.cs
    private string searchQuery = "";
    public int SelectedTileGroup { get; set; }
    public Tile? SelectedTile { get; private set; }

    public void SelectTile(Tile tile)
    {
        if (CheckDimensions(tile, activeTileType))
        {
            SelectedTile = tile;
        }
        else
        {
            EditorWindow.ShowNotification("Tile dimensions do not match autotile dimensions");
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
            Log.Information("Append autotile {Header}", header);

            lines.Add("");
            lines.Add(header);
            //lines.Add("thickness=" + PathThickness.ToString(CultureInfo.InvariantCulture));
            //lines.Add("length=" + SegmentLength.ToString(CultureInfo.InvariantCulture));
            lines.Add("ld=" + TileTable.LeftDown);
            lines.Add("lu=" + TileTable.LeftUp);
            lines.Add("rd=" + TileTable.RightDown);
            lines.Add("ru=" + TileTable.RightUp);
            lines.Add("vertical=" + TileTable.Vertical);
            lines.Add("horizontal=" + TileTable.Horizontal);
            lines.Add("allowJunctions=" + (TileTable.AllowJunctions ? "true" : "false"));
            lines.Add("tr=" + TileTable.TRight); 
            lines.Add("tu=" + TileTable.TUp);
            lines.Add("tl=" + TileTable.TLeft);
            lines.Add("td=" + TileTable.TDown);
            lines.Add("x="  + TileTable.XJunct);
            lines.Add("placeCaps=" + (TileTable.PlaceCaps ? "true" : "false"));
            lines.Add("capRight=" + TileTable.TRight); 
            lines.Add("capUp=" + TileTable.TUp);
            lines.Add("capLeft=" + TileTable.TLeft);
            lines.Add("capDown=" + TileTable.TDown);
        }

        // was found, overwrite lines
        else
        {
            Log.Information("Overwrite autotile {Header}", header);

            //lines[location+1]  = "thickness=" + PathThickness.ToString(CultureInfo.InvariantCulture);
            //lines[location+2]  = "length=" + SegmentLength.ToString(CultureInfo.InvariantCulture);
            lines[location+1]  = "ld=" + TileTable.LeftDown;
            lines[location+2]  = "lu=" + TileTable.LeftUp;
            lines[location+3]  = "rd=" + TileTable.RightDown;
            lines[location+4]  = "ru=" + TileTable.RightUp;
            lines[location+5]  = "vertical=" + TileTable.Vertical;
            lines[location+6]  = "horizontal=" + TileTable.Horizontal;
            lines[location+7]  = "allowJunctions=" + (TileTable.AllowJunctions ? "true" : "false");
            lines[location+8] = "tr=" + TileTable.TRight;  
            lines[location+9] = "tu=" + TileTable.TUp;
            lines[location+10] = "tl=" + TileTable.TLeft;
            lines[location+11] = "td=" + TileTable.TDown;
            lines[location+12] = "x="  + TileTable.XJunct;
            lines[location+13] = "placeCaps=" + (TileTable.PlaceCaps ? "true" : "false");
            lines[location+14] = "capRight=" + TileTable.CapRight;
            lines[location+15] = "capUp=" + TileTable.CapUp;
            lines[location+16] = "capLeft=" + TileTable.CapLeft;
            lines[location+17] = "capDown=" + TileTable.CapDown;
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
            Log.Information("Delete autotile {Header}", header);

            lines.RemoveRange(location, 18);

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
            Log.Information("Rename autotile {Old} to {New}", header, lines[location]);
        }
    }
}