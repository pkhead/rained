namespace Rained.LuaScripting.Modules;

using System.Diagnostics;
using System.Runtime.InteropServices;
using KeraLua;
using LevelData;
using Rained.EditorGui;

static class LevelModule
{
    public static void Init(Lua lua, NLua.Lua nlua)
    {
        lua.NewTable();

        lua.PushNumber(Level.TileSize);
        lua.SetField(-2, "cellSize");

        lua.ModuleFunction("isInBounds", static (nint luaPtr) => {
            var lua = Lua.FromIntPtr(luaPtr);
            var x = (int) lua.CheckNumber(1);
            var y = (int) lua.CheckNumber(2);
            lua.PushBoolean(RainEd.Instance.Level.IsInBounds(x, y));
            return 1;
        });

        lua.ModuleFunction("screenToCell", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var screenX = lua.CheckNumber(1);
            var screenY = lua.CheckNumber(2);

            lua.PushNumber(screenX / Level.TileSize);
            lua.PushNumber(screenY / Level.TileSize);
            return 2;
        });

        lua.ModuleFunction("cellToScreen", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var screenX = lua.CheckNumber(1);
            var screenY = lua.CheckNumber(2);

            lua.PushNumber(screenX * Level.TileSize);
            lua.PushNumber(screenY * Level.TileSize);
            return 2;
        });

        lua.ModuleFunction("resize", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.CheckType(1, LuaType.Table);

            int GetInteger(string name)
            {
                int v;
                lua.GetField(1, name);
                lua.ArgumentCheck(lua.IsInteger(-1), 1, "invalid ResizeParameters");
                v = (int)lua.ToInteger(-1);
                lua.Pop(1);

                return v;
            }

            int GetIntegerOpt(string name, int opt)
            {
                int v;
                lua.GetField(1, name);
                
                if (lua.IsNoneOrNil(-1))
                {
                    v = opt;
                }
                else
                {
                    lua.ArgumentCheck(lua.IsInteger(-1), 1, "invalid ResizeParameters");
                    v = (int)lua.ToInteger(-1);
                }

                lua.Pop(1);
                return v;
            }

            var level = RainEd.Instance.Level;
            int width = GetInteger("width");
            int height = GetInteger("height");
            int borderLeft = GetIntegerOpt("borderLeft", level.BufferTilesLeft);
            int borderTop = GetIntegerOpt("borderTop", level.BufferTilesTop);
            int borderRight = GetIntegerOpt("borderRight", level.BufferTilesRight);
            int borderBottom = GetIntegerOpt("borderBottom", level.BufferTilesBot);
            int anchorX = GetIntegerOpt("anchorX", -1);
            int anchorY = GetIntegerOpt("anchorY", -1);

            if (width < 0) lua.ArgumentError(1, "width must be greater than or equal to 0");
            if (height < 0) lua.ArgumentError(1, "width must be greater than or equal to 0");
            if (borderLeft < 0) lua.ArgumentError(1, "borderLeft must be greater than or equal to 0");
            if (borderTop < 0) lua.ArgumentError(1, "borderTop must be greater than or equal to 0");
            if (borderRight < 0) lua.ArgumentError(1, "borderRight must be greater than or equal to 0");
            if (borderBottom < 0) lua.ArgumentError(1, "borderBottom must be greater than or equal to 0");

            if (anchorX < -1 || anchorX > 1) lua.ArgumentError(1, "anchorX must be -1, 0, or 1");
            if (anchorY < -1 || anchorY > 1) lua.ArgumentError(1, "anchorY must be -1, 0, or 1");

            RainEd.Instance.ResizeLevel(width, height, anchorX, anchorY);

            level.BufferTilesLeft = borderLeft;
            level.BufferTilesTop = borderTop;
            level.BufferTilesRight = borderRight;
            level.BufferTilesBot = borderBottom;

            return 0;
        });

        lua.ModuleFunction("save", static (nint luaPtr) =>
        {
            var coro = Lua.FromIntPtr(luaPtr);
            if (!coro.IsYieldable) return coro.ErrorWhere("attempt to called rained.level.save from a non-yieldable context");

            string? overridePath = null;
            if (!coro.IsNoneOrNil(1))
            {
                overridePath = Path.GetFullPath(coro.ToString(1));
            }

            int? coroRef = null;
            string? path = null;

            bool immediate = EditorWindow.AsyncSave(
                overridePath: overridePath,
                callback: (string? p, bool immediate) =>
                {
                    path = p;
                    if (immediate) return;
                    if (coroRef is null) throw new Exception("referenced coroutine is null");

                    var lua = LuaInterface.LuaState;
                    lua.RawGetInteger(LuaRegistry.Index, coroRef.Value);
                    lua.Unref(LuaRegistry.Index, coroRef.Value);

                    lua.PushString(RainEd.Instance.CurrentFilePath);
                    LuaHelpers.ResumeCoroutine(lua.ToThread(-2), null, 1, out _);
                }
            );

            if (immediate)
            {
                coro.PushString(path!);
                return 1;
            }
            else
            {
                coro.PushThread();
                coroRef = coro.Ref(LuaRegistry.Index);

                var handle = GCHandle.Alloc(null);
                LuaKFunction kfunc = (nint luaPtr, int status, nint k) =>
                {
                    handle.Free();
                    coro.PushString(path!);
                    return 1;
                };

                handle.Target = kfunc;
                Debug.Assert(handle.IsAllocated);
                
                return coro.YieldK(0, 0, kfunc);
            }
        });

        // set metatable
        lua.NewTable();
        lua.ModuleFunction("__metatable", static (nint luaPtr) => {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushString("The metatable is locked.");
            return 1;
        });

        lua.ModuleFunction("__index", static (nint luaPtr) => {
            var lua = Lua.FromIntPtr(luaPtr);
            var idx = lua.ToString(2);

            switch (idx) {
                case "filePath":
                    if (string.IsNullOrEmpty(RainEd.Instance.CurrentFilePath)) {
                        lua.PushNil();
                    } else {
                        lua.PushString(RainEd.Instance.CurrentFilePath);
                    }
                    break;

                case "name":
                    lua.PushString(RainEd.Instance.CurrentTab!.Name);
                    break;

                case "width":
                    lua.PushInteger(RainEd.Instance.Level.Width);
                    break;

                case "height":
                    lua.PushInteger(RainEd.Instance.Level.Height);
                    break;

                case "defaultMedium":
                    lua.PushBoolean(RainEd.Instance.Level.DefaultMedium);
                    break;

                case "hasSunlight":
                    lua.PushBoolean(RainEd.Instance.Level.HasSunlight);
                    break;

                case "hasWater":
                    lua.PushBoolean(RainEd.Instance.Level.HasWater);
                    break;

                case "waterLevel":
                    lua.PushInteger(RainEd.Instance.Level.WaterLevel);
                    break;

                case "isWaterInFront":
                    lua.PushBoolean(RainEd.Instance.Level.IsWaterInFront);
                    break;
                
                case "tileSeed":
                    lua.PushInteger(RainEd.Instance.Level.TileSeed);
                    break;

                case "borderLeft":
                    lua.PushInteger(RainEd.Instance.Level.BufferTilesLeft);
                    break;

                case "borderTop":
                    lua.PushInteger(RainEd.Instance.Level.BufferTilesTop);
                    break;
                
                case "borderRight":
                    lua.PushInteger(RainEd.Instance.Level.BufferTilesRight);
                    break;

                case "borderBottom":
                    lua.PushInteger(RainEd.Instance.Level.BufferTilesBot);
                    break;

                default:
                    lua.PushNil();
                    break;
            }

            return 1;
        });

        lua.ModuleFunction("__newindex", static (nint luaPtr) => {
            var lua = Lua.FromIntPtr(luaPtr);
            var idx = lua.ToString(2);

            switch (idx) {
                case "filePath":
                    return lua.ErrorWhere("field \"filePath\" is read-only.");

                case "name":
                    return lua.ErrorWhere("field \"name\" is read-only.");

                case "width":
                    return lua.ErrorWhere("field \"width\" is read-only.");

                case "height":
                    return lua.ErrorWhere("field \"height\" is read-only.");

                case "defaultMedium":
                    RainEd.Instance.Level.DefaultMedium = lua.ToBoolean(3);
                    break;

                case "hasSunlight":
                    RainEd.Instance.Level.HasSunlight = lua.ToBoolean(3);
                    break;

                case "hasWater":
                    RainEd.Instance.Level.HasWater = lua.ToBoolean(3);
                    break;

                case "waterLevel":
                    RainEd.Instance.Level.WaterLevel = int.Max(0, (int) lua.CheckNumber(3));
                    break;

                case "isWaterInFront":
                    RainEd.Instance.Level.IsWaterInFront = lua.ToBoolean(3);
                    break;
                
                case "tileSeed":
                    RainEd.Instance.Level.TileSeed = Math.Clamp(
                        (int) lua.CheckNumber(3),
                        0, 400
                    );
                    break;

                case "borderLeft":
                    RainEd.Instance.Level.BufferTilesLeft = Math.Max( 0, (int) lua.CheckNumber(3) );
                    break;

                case "borderTop":
                    RainEd.Instance.Level.BufferTilesTop = Math.Max( 0, (int) lua.CheckNumber(3) );
                    break;
                
                case "borderRight":
                    RainEd.Instance.Level.BufferTilesRight = Math.Max( 0, (int) lua.CheckNumber(3) );
                    break;

                case "borderBottom":
                    RainEd.Instance.Level.BufferTilesBot = Math.Max( 0, (int) lua.CheckNumber(3) );
                    break;

                default:
                    return lua.ErrorWhere($"unknown field \"{idx}\"");
            }
            
            return 0;
        });
        lua.SetMetaTable(-2);

        // set rained.level
        lua.SetField(-2, "level");
    }
}