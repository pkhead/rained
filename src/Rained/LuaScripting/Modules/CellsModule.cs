namespace Rained.LuaScripting.Modules;
using Rained.EditorGui;
using KeraLua;
using LevelData;

static class CellsModule
{
    public static void Init(Lua lua, NLua.Lua nlua)
    {
        lua.NewTable();

        // function getGeo
        nlua.Push(static (int x, int y, int layer) =>
        {
            layer--;
            if (!LuaInterface.Host.LevelCheck().IsInBounds(x, y)) return 0;
            if (layer < 0 || layer > 2) throw new LuaHelpers.LuaErrorException("invald layer " + (layer+1));
            return (int) LuaInterface.Host.LevelCheck().Layers[layer, x, y].Geo;
        });
        lua.SetField(-2, "getGeo");

        // function setGeo
        nlua.Push(static (int x, int y, int layer, int geoType) =>
        {
            layer--;
            if (!LuaInterface.Host.LevelCheck().IsInBounds(x, y)) return;
            if (layer < 0 || layer > 2) throw new Exception("invald layer " + (layer+1));
            if (geoType < 0 || geoType == 8 || geoType > 9) throw new Exception("invalid geo type " + geoType);
            LuaInterface.Host.LevelCheck().Layers[layer, x, y].Geo = (GeoType) geoType;
            LuaInterface.Host.InvalidateCell(x, y, layer, CellDirtyFlags.Geometry);
        });
        lua.SetField(-2, "setGeo");

        // function getMaterial
        nlua.Push(static (int x, int y, int layer) =>
        {
            layer--;
            if (!LuaInterface.Host.LevelCheck().IsInBounds(x, y)) return null;
            if (layer < 0 || layer > 2) throw new LuaHelpers.LuaErrorException("invald layer " + (layer+1));

            var idx = LuaInterface.Host.LevelCheck().Layers[layer, x, y].Material;
            if (idx == 0) return null;
            return LuaInterface.Host.MaterialDatabase.GetMaterial(idx)?.Name;
        });
        lua.SetField(-2, "getMaterial");

        // function setMaterial
        LuaHelpers.PushLuaFunction(lua, static (KeraLua.Lua lua) =>
        {
            var x = (int) lua.CheckNumber(1);
            var y = (int) lua.CheckNumber(2);
            var layer = (int) lua.CheckNumber(3) - 1;
            string? matName = lua.IsNil(4) ? null : lua.CheckString(4);

            int matId;
            if (matName is not null)
            {
                var mat = LuaInterface.Host.MaterialDatabase.GetMaterial(matName);
                if (mat is null)
                    return lua.ArgumentError(4, $"'{matName}' is not a recognized material");
                matId = mat.ID;
            }
            else
            {
                matId = 0;
            }

            if (!LuaInterface.Host.LevelCheck(lua).IsInBounds(x, y)) return 0;
            if (layer < 0 || layer > 2) throw new LuaHelpers.LuaErrorException("invald layer " + (layer+1));
            LuaInterface.Host.LevelCheck(lua).Layers[layer, x, y].Material = matId;
            LuaInterface.Host.InvalidateCell(x, y, layer, CellDirtyFlags.Material);
            return 0;
        });
        lua.SetField(-2, "setMaterial");

        // function setMaterialId
        LuaHelpers.PushLuaFunction(lua, static (KeraLua.Lua lua) =>
        {
            var x = (int) lua.CheckNumber(1);
            var y = (int) lua.CheckNumber(2);
            var layer = (int) lua.CheckNumber(3) - 1;
            var matId = (int) lua.CheckNumber(4);
            
            if (!LuaInterface.Host.LevelCheck(lua).IsInBounds(x, y)) return 0;
            if (layer < 0 || layer > 2) throw new LuaHelpers.LuaErrorException("invald layer " + (layer+1));
            LuaInterface.Host.LevelCheck(lua).Layers[layer, x, y].Material = matId;
            return 0;
        });
        lua.SetField(-2, "setMaterialId");

        // function getMaterial
        nlua.Push(static (int x, int y, int layer) =>
        {
            layer--;
            if (!LuaInterface.Host.LevelCheck().IsInBounds(x, y)) return 0;
            if (layer < 0 || layer > 2) throw new LuaHelpers.LuaErrorException("invald layer " + (layer+1));

            return LuaInterface.Host.LevelCheck().Layers[layer, x, y].Material;
        });
        lua.SetField(-2, "getMaterialId");

        // function getObjects
        LuaHelpers.PushLuaFunction(lua, static (KeraLua.Lua lua) =>
        {
            int x = (int) lua.CheckNumber(1);
            int y = (int) lua.CheckNumber(2);
            int layer = (int) lua.CheckInteger(3) - 1;

            lua.NewTable();
            if (!LuaInterface.Host.LevelCheck(lua).IsInBounds(x, y)) return 1;
            if (layer < 0 || layer > 2) throw new LuaHelpers.LuaErrorException("invald layer " + (layer+1));

            ref var cell = ref LuaInterface.Host.LevelCheck(lua).Layers[layer, x, y];
            for (int i = 1; i < 32; i++)
            {
                if (cell.Has((LevelObject)(1 << (i-1))))
                {
                    lua.PushInteger(i);
                    lua.SetInteger(-2, lua.Length(-2) + 1);
                }
            }

            return 1;
        });
        lua.SetField(-2, "getObjects");

        // function setObjects
        LuaHelpers.PushLuaFunction(lua, static (KeraLua.Lua lua) =>
        {
            int x = (int) lua.CheckNumber(1);
            int y = (int) lua.CheckNumber(2);
            int layer = (int) lua.CheckInteger(3) - 1;
            lua.CheckType(4, KeraLua.LuaType.Table);
            
            if (!LuaInterface.Host.LevelCheck(lua).IsInBounds(x, y)) return 0;
            if (layer < 0 || layer > 2) throw new LuaHelpers.LuaErrorException("invald layer " + (layer+1));

            LevelObject objects = 0;

            for (int i = 1; i <= lua.Length(4); i++)
            {
                int k = (int) lua.GetInteger(4, i);
                if (k < 1 || k >= 32) throw new LuaHelpers.LuaErrorException("invalid geometry object");
                objects |= (LevelObject)(1 << (k-1));
            }

            LuaInterface.Host.LevelCheck(lua).Layers[layer, x, y].Objects = objects;
            LuaInterface.Host.InvalidateCell(x, y, layer, CellDirtyFlags.Objects);

            return 0;
        });
        lua.SetField(-2, "setObjects");

        // function hasObject
        LuaHelpers.PushLuaFunction(lua, static (Lua lua) =>
        {
            int nargs = lua.GetTop();
            int x = (int) lua.CheckNumber(1);
            int y = (int) lua.CheckNumber(2);
            int layer = (int) lua.CheckInteger(3) - 1;

            if (!LuaInterface.Host.LevelCheck(lua).IsInBounds(x, y)) return 0;
            if (layer < 0 || layer > 2) throw new LuaHelpers.LuaErrorException("invald layer " + (layer+1));

            LevelObject objs = 0;
            for (int i = 4; i <= nargs; i++)
            {
                int v = (int)lua.CheckInteger(i);
                if (v > 0)
                    objs |= (LevelObject)(1 << (v - 1));
            }

            ref var cell = ref LuaInterface.Host.LevelCheck(lua).Layers[layer, x, y];
            lua.PushBoolean((int)(cell.Objects | ~objs) == ~0);
            return 1;
        });
        lua.SetField(-2, "hasObject");

        // function addObject
        LuaHelpers.PushLuaFunction(lua, static (Lua lua) =>
        {
            int nargs = lua.GetTop();
            int x = (int) lua.CheckNumber(1);
            int y = (int) lua.CheckNumber(2);
            int layer = (int) lua.CheckInteger(3) - 1;

            if (!LuaInterface.Host.LevelCheck(lua).IsInBounds(x, y)) return 0;
            if (layer < 0 || layer > 2) throw new LuaHelpers.LuaErrorException("invald layer " + (layer+1));

            LevelObject objs = 0;
            for (int i = 4; i <= nargs; i++)
            {
                int v = (int)lua.CheckInteger(i);
                if (v > 0)
                    objs |= (LevelObject)(1 << (v - 1));
            }

            ref var cell = ref LuaInterface.Host.LevelCheck(lua).Layers[layer, x, y];
            cell.Objects |= objs;
            LuaInterface.Host.InvalidateCell(x, y, layer, CellDirtyFlags.Objects);
            return 0;
        });
        lua.SetField(-2, "addObject");

        // function removeObject
        LuaHelpers.PushLuaFunction(lua, static (Lua lua) =>
        {
            int nargs = lua.GetTop();
            int x = (int) lua.CheckNumber(1);
            int y = (int) lua.CheckNumber(2);
            int layer = (int) lua.CheckInteger(3) - 1;

            if (!LuaInterface.Host.LevelCheck(lua).IsInBounds(x, y)) return 0;
            if (layer < 0 || layer > 2) throw new LuaHelpers.LuaErrorException("invald layer " + (layer+1));

            LevelObject objs = 0;
            for (int i = 4; i <= nargs; i++)
            {
                int v = (int)lua.CheckInteger(i);
                if (v > 0)
                    objs |= (LevelObject)(1 << (v - 1));
            }

            ref var cell = ref LuaInterface.Host.LevelCheck(lua).Layers[layer, x, y];
            cell.Objects &= ~objs;
            LuaInterface.Host.InvalidateCell(x, y, layer, CellDirtyFlags.Objects);
            return 0;
        });
        lua.SetField(-2, "removeObject");

        // function getTileData
        LuaHelpers.PushLuaFunction(lua, static (KeraLua.Lua lua) =>
        {
            int x = (int) lua.CheckNumber(1);
            int y = (int) lua.CheckNumber(2);
            int layer = (int) lua.CheckInteger(3) - 1;

            if (!LuaInterface.Host.LevelCheck(lua).IsInBounds(x, y)) return 0;
            if (layer < 0 || layer > 2) throw new LuaHelpers.LuaErrorException("invald layer " + (layer+1));

            ref var cell = ref LuaInterface.Host.LevelCheck(lua).Layers[layer, x, y];

            if (cell.TileHead is not null)
                lua.PushString(cell.TileHead.Name);
            else
                lua.PushNil();
            
            if (cell.TileRootX >= 0)
                lua.PushInteger(cell.TileRootX);
            else
                lua.PushNil();
            
            if (cell.TileRootY >= 0)
                lua.PushInteger(cell.TileRootY);
            else
                lua.PushNil();
            
            if (cell.TileLayer >= 0)
                lua.PushInteger(cell.TileLayer);
            else
                lua.PushNil();
            
            return 4;
        });
        lua.SetField(-2, "getTileData");

        // function setTileHead
        LuaHelpers.PushLuaFunction(lua, static (KeraLua.Lua lua) =>
        {
            int x = (int) lua.CheckNumber(1);
            int y = (int) lua.CheckNumber(2);
            int layer = (int) lua.CheckInteger(3) - 1;
            string? tileName = null;

            if (!lua.IsNoneOrNil(4))
                tileName = lua.CheckString(4);

            if (!LuaInterface.Host.LevelCheck(lua).IsInBounds(x, y)) return 0;
            if (layer < 0 || layer > 2) throw new LuaHelpers.LuaErrorException("invald layer " + (layer+1));

            Assets.Tile? tile = null;

            if (tileName is not null)
            {
                if (!LuaInterface.Host.TileDatabase.HasTile(tileName))
                    throw new LuaHelpers.LuaErrorException($"tile '{tileName}' does not exist");
                
                tile = LuaInterface.Host.TileDatabase.GetTileFromName(tileName);
            }

            LuaInterface.Host.LevelCheck(lua).SetTileHead(layer, x, y, tile);
            LuaInterface.Host.InvalidateCell(x, y, layer, CellDirtyFlags.TileHead);

            return 0;
        });
        lua.SetField(-2, "setTileHead");

        // function setTileRoot
        LuaHelpers.PushLuaFunction(lua, static (KeraLua.Lua lua) =>
        {
            int x = (int) lua.CheckNumber(1);
            int y = (int) lua.CheckNumber(2);
            int layer = (int) lua.CheckInteger(3) - 1;

            int tileRootX = (int) lua.CheckNumber(4);
            int tileRootY = (int) lua.CheckNumber(5);
            int tileLayer = (int) lua.CheckInteger(6) - 1;

            if (!LuaInterface.Host.LevelCheck(lua).IsInBounds(x, y)) return 0;
            if (layer < 0 || layer > 2) throw new LuaHelpers.LuaErrorException("invald layer " + (layer+1));

            if (!LuaInterface.Host.LevelCheck(lua).IsInBounds(tileRootX, tileRootY) || tileLayer < 0 || tileLayer > 2)
                throw new LuaHelpers.LuaErrorException("target tile root is out of bounds");
            
            // invalidate old tile head
            var cell = LuaInterface.Host.LevelCheck(lua).Layers[layer, x, y];
            if (cell.TileRootX != -1)
            {
                LuaInterface.Host.InvalidateCell(cell.TileRootX, cell.TileRootY, cell.TileLayer, CellDirtyFlags.TileHead);
            }

            LuaInterface.Host.LevelCheck(lua).SetTileRoot(layer, x, y, tileRootX, tileRootY, tileLayer);
            LuaInterface.Host.InvalidateCell(tileRootX, tileRootY, tileLayer, CellDirtyFlags.TileHead);

            return 0;
        });
        lua.SetField(-2, "setTileRoot");

        // function clearTileRoot
        LuaHelpers.PushLuaFunction(lua, static (KeraLua.Lua lua) =>
        {
            int x = (int) lua.CheckNumber(1);
            int y = (int) lua.CheckNumber(2);
            int layer = (int) lua.CheckInteger(3) - 1;

            if (!LuaInterface.Host.LevelCheck(lua).IsInBounds(x, y)) return 0;
            if (layer < 0 || layer > 2) throw new LuaHelpers.LuaErrorException("invald layer " + (layer+1));

            LuaInterface.Host.LevelCheck(lua).ClearTileRoot(layer, x, y);
            LuaInterface.Host.InvalidateCell(x, y, layer, CellDirtyFlags.TileHead);
            
            return 0;
        });
        lua.SetField(-2, "clearTileRoot");

        // set rained.cells
        lua.SetField(-2, "cells");
    }
}