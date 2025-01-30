namespace Rained.LuaScripting.Modules;
using KeraLua;
using LevelData;
using Autotiles;

static class TilesModule
{
    public static void Init(Lua lua, NLua.Lua nlua)
    {
        lua.NewTable();

        //nlua.Push(new Func<string, object?, Autotile>(CreateAutotile));
        LuaHelpers.PushLuaFunction(lua, LuaCreateAutotile);
        lua.SetField(-2, "createAutotile");

        LuaHelpers.PushCsFunction(lua, new PlaceTileDelegate(PlaceTile));
        lua.SetField(-2, "placeTile");

        // function getTileAt
        nlua.Push(static (int x, int y, int layer) => {
            var level = RainEd.Instance.Level;
            if (layer < 1 || layer > 3) throw new LuaHelpers.LuaErrorException("invald layer " + layer);
            if (!level.IsInBounds(x, y)) return null;
            var tile = RainEd.Instance.Level.GetTile(level.Layers[layer-1, x, y]);
            return tile?.Name;
        });
        lua.SetField(-2, "getTileAt");

        // function hasTileHead
        nlua.Push(static (int x, int y, int layer) => {
            var level = RainEd.Instance.Level;
            if (layer < 1 || layer > 3) throw new LuaHelpers.LuaErrorException("invald layer " + layer);
            if (!level.IsInBounds(x, y)) return false;
            return level.Layers[layer-1, x, y].TileHead is not null;
        });
        lua.SetField(-2, "hasTileHead");

        // function deleteTile
        LuaHelpers.PushLuaFunction(lua, static (KeraLua.Lua lua) => {
            int x = (int) lua.CheckInteger(1);
            int y = (int) lua.CheckInteger(2);
            int layer = (int) lua.CheckInteger(3);
            bool removeGeo = false;

            if (!lua.IsNoneOrNil(4))
                removeGeo = lua.ToBoolean(4);
            
            var level = RainEd.Instance.Level;
            if (layer < 1 || layer > 3) throw new LuaHelpers.LuaErrorException("invald layer " + layer);
            if (!level.IsInBounds(x, y)) return 0;
            level.RemoveTileCell(layer - 1, x, y, removeGeo);
            return 0;
        });
        lua.SetField(-2, "deleteTile");

        // function autotilePath
        LuaHelpers.PushLuaFunction(lua, LuaStandardPathAutotile);
        lua.SetField(-2, "autotilePath");

        // function getTileInfo
        LuaHelpers.PushLuaFunction(lua, static (KeraLua.Lua lua) =>
        {
            var tileName = lua.CheckString(1);
            if (!RainEd.Instance.TileDatabase.HasTile(tileName)) return 0;
            var tileData = RainEd.Instance.TileDatabase.GetTileFromName(tileName);

            lua.NewTable();
            lua.PushString(tileData.Name); // name
            lua.SetField(-2, "name");
            lua.PushString(tileData.Category.Name); // category
            lua.SetField(-2, "category");
            lua.PushInteger(tileData.Width); // width
            lua.SetField(-2, "width");
            lua.PushInteger(tileData.Height); // height
            lua.SetField(-2, "height");
            lua.PushInteger(tileData.BfTiles); // bfTiles
            lua.SetField(-2, "bfTiles");
            lua.PushInteger(tileData.CenterX); // cx
            lua.SetField(-2, "centerX");
            lua.PushInteger(tileData.CenterY); // cy
            lua.SetField(-2, "centerY");

            // create specs table
            lua.CreateTable(tileData.Width * tileData.Height, 0);
            int i = 1;
            for (int x = 0; x < tileData.Width; x++)
            {
                for (int y = 0; y < tileData.Height; y++)
                {
                    lua.PushInteger(tileData.Requirements[x,y]);
                    lua.RawSetInteger(-2, i++);
                }
            }
            lua.SetField(-2, "specs");

            // create specs2 table
            if (tileData.HasSecondLayer)
            {
                lua.CreateTable(tileData.Width * tileData.Height, 0);
                i = 1;
                for (int x = 0; x < tileData.Width; x++)
                {
                    for (int y = 0; y < tileData.Height; y++)
                    {
                        lua.PushInteger(tileData.Requirements2[x,y]);
                        lua.RawSetInteger(-2, i++);
                    }
                }

                lua.SetField(-2, "specs2");
            }

            return 1;
        });
        lua.SetField(-2, "getTileInfo");

        // set rained.tiles
        lua.SetField(-2, "tiles");
    }

    private static int LuaCreateAutotile(Lua lua)
    {
        var name = lua.CheckString(1);
        var category = "Misc";

        // the optional second argument is the category name
        if (!lua.IsNoneOrNil(2))
        {
            category = lua.CheckString(2);
        }

        var autotile = new LuaAutotileInterface()
        {
            Name = name
        };

        RainEd.Instance.Autotiles.AddAutotile(autotile.autotile, category);
        // bleh
        LuaInterface.NLuaState.Push(autotile);
        return 1;
    }

    private static int LuaStandardPathAutotile(KeraLua.Lua lua)
    {
        var state = lua;

        // arg 1: tile table
        state.CheckType(1, KeraLua.LuaType.Table);
        // arg 2: layer
        int layer = (int) state.CheckInteger(2) - 1;
        // arg 3: segment list
        state.CheckType(3, KeraLua.LuaType.Table);
        
        // arg 4: modifier string
        string modifierStr = "";
        if (!state.IsNoneOrNil(4))
            modifierStr = state.CheckString(4);

        int startIndex = 0;
        int endIndex = (int) state.Length(3);

        // arg 5: optional start index
        if (!state.IsNoneOrNil(5))
            startIndex = (int) state.CheckInteger(5) - 1;
        
        // arg 6: optional end index
        if (!state.IsNoneOrNil(6))
            endIndex = (int) state.CheckInteger(6);
        
        // verify layer argument
        if (layer < 0 || layer > 2) return 0;
        
        var tileTable = new PathTileTable();

        // parse tiling options
        if (state.GetField(1, "placeJunctions") != KeraLua.LuaType.Nil)
        {
            if (!state.IsBoolean(-1)) return state.Error("invalid tile table");
            tileTable.AllowJunctions = state.ToBoolean(-1);
        }

        if (state.GetField(1, "placeCaps") != KeraLua.LuaType.Nil)
        {
            if (!state.IsBoolean(-1)) return state.Error("invaild tile table");
            tileTable.PlaceCaps = state.ToBoolean(-1);
        }

        state.Pop(2);

        // parse the tile table
        if (state.GetField(1, "ld") != KeraLua.LuaType.String) return state.Error("invalid tile table");
        tileTable.LeftDown = state.ToString(-1);
        if (state.GetField(1, "lu") != KeraLua.LuaType.String) return state.Error("invalid tile table");
        tileTable.LeftUp = state.ToString(-1);
        if (state.GetField(1, "rd") != KeraLua.LuaType.String) return state.Error("invalid tile table");
        tileTable.RightDown = state.ToString(-1);
        if (state.GetField(1, "ru") != KeraLua.LuaType.String) return state.Error("invalid tile table");
        tileTable.RightUp = state.ToString(-1);
        if (state.GetField(1, "vertical") != KeraLua.LuaType.String) return state.Error("invalid tile table");
        tileTable.Vertical = state.ToString(-1);
        if (state.GetField(1, "horizontal") != KeraLua.LuaType.String) return state.Error("invalid tile table");
        tileTable.Horizontal = state.ToString(-1);

        state.Pop(6);

        if (tileTable.AllowJunctions)
        {
            if (state.GetField(1, "tr") != KeraLua.LuaType.String) return state.Error("invalid tile table");
            tileTable.TRight = state.ToString(-1);
            if (state.GetField(1, "tu") != KeraLua.LuaType.String) return state.Error("invalid tile table");
            tileTable.TUp = state.ToString(-1);
            if (state.GetField(1, "tl") != KeraLua.LuaType.String) return state.Error("invalid tile table");
            tileTable.TLeft = state.ToString(-1);
            if (state.GetField(1, "td") != KeraLua.LuaType.String) return state.Error("invalid tile table");
            tileTable.TDown = state.ToString(-1);
            if (state.GetField(1, "x") != KeraLua.LuaType.String) return state.Error("invalid tile table");
            tileTable.XJunct = state.ToString(-1);

            state.Pop(5);
        }

        if (tileTable.PlaceCaps)
        {
            if (state.GetField(1, "capRight") != KeraLua.LuaType.String) return state.Error("invalid tile table");
            tileTable.CapRight = state.ToString(-1);
            if (state.GetField(1, "capUp") != KeraLua.LuaType.String) return state.Error("invalid tile table");
            tileTable.CapUp = state.ToString(-1);
            if (state.GetField(1, "capLeft") != KeraLua.LuaType.String) return state.Error("invalid tile table");
            tileTable.CapLeft = state.ToString(-1);
            if (state.GetField(1, "capDown") != KeraLua.LuaType.String) return state.Error("invalid tile table");
            tileTable.CapDown = state.ToString(-1);

            state.Pop(4);
        }

        // parse path segment table
        var pathSegments = new List<PathSegment>();

        state.PushCopy(3); // push segment table onto stack
        
        // begin looping through table
        state.PushNil();
        while (state.Next(-2))
        {
            // the value is at the top of the stack
            if (!state.IsTable(-1)) return state.Error("invalid segment table");
            int tableIndex = state.GetTop();
            var segment = new PathSegment();

            if (state.GetField(tableIndex, "right") != KeraLua.LuaType.Boolean) return state.Error("invalid segment table");
            segment.Right = state.ToBoolean(-1);
            if (state.GetField(tableIndex, "up") != KeraLua.LuaType.Boolean) return state.Error("invalid segment table");
            segment.Up = state.ToBoolean(-1);
            if (state.GetField(tableIndex, "left") != KeraLua.LuaType.Boolean) return state.Error("invalid segment table");
            segment.Left = state.ToBoolean(-1);
            if (state.GetField(tableIndex, "down") != KeraLua.LuaType.Boolean) return state.Error("invalid segment table");
            segment.Down = state.ToBoolean(-1);
            if (state.GetField(tableIndex, "x") != KeraLua.LuaType.Number) return state.Error("invalid segment table");
            segment.X = (int) state.ToNumber(-1);
            if (state.GetField(tableIndex, "y") != KeraLua.LuaType.Number) return state.Error("invalid segment table");
            segment.Y = (int) state.ToNumber(-1);

            pathSegments.Add(segment);

            state.Pop(6); // pop retrieved values of table
            state.Pop(1); // pop value
        }

        // pop the segment table
        state.Pop(1);

        var modifier = modifierStr switch
        {
            "geometry" => TilePlacementMode.Geometry,
            "force" => TilePlacementMode.Force,
            _ => TilePlacementMode.Normal
        };

        // *inhale* then finally, run the autotiler
        Autotile.StandardTilePath(tileTable, layer, [..pathSegments], modifier, startIndex, endIndex);

        return 0;
    }

    delegate bool PlaceTileDelegate(out string? result, string tileName, int x, int y, int layer, string? modifier);
    public static bool PlaceTile(out string? result, string tileName, int x, int y, int layer, string? modifier)
    {
        result = null;

        var level = RainEd.Instance.Level;
        var placeMode = TilePlacementMode.Normal;
        
        // validate arguments
        if (modifier is not null)
        {
            if (modifier != "geometry" && modifier != "force")
            {
                throw new Exception($"expected 'geometry' or 'force' for argument 5, got '{modifier}'");
            }

            if (modifier == "geometry")   placeMode = TilePlacementMode.Geometry;
            else if (modifier == "force") placeMode = TilePlacementMode.Force;
        }
        if (layer < 1 || layer > 3)
            throw new Exception($"invalid layer {layer}");
        if (!RainEd.Instance.TileDatabase.HasTile(tileName))
            throw new Exception($"tile '{tileName}' is not recognized");
        layer--; // layer is 1-based in the lua code
        
        // begin placement
        var tile = RainEd.Instance.TileDatabase.GetTileFromName(tileName);

        var validationStatus = level.SafePlaceTile(tile, layer, x, y, placeMode);
        switch (validationStatus)
        {
            case TilePlacementStatus.OutOfBounds:
                result = "out of bounds";
                return false;
            case TilePlacementStatus.Overlap:
                result = "overlap";
                return false;
            case TilePlacementStatus.Geometry:
                result = "geometry";
                return false;
            case TilePlacementStatus.Success:
                return true;
        }

        return true;
    }
}