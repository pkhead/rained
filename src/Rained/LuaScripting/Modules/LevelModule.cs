namespace Rained.LuaScripting.Modules;
using KeraLua;
using LevelData;

static class LevelModule
{
    public static void Init(Lua lua, NLua.Lua nlua)
    {
        lua.NewTable();

        lua.ModuleFunction("isInBounds", static (nint luaPtr) => {
            var lua = Lua.FromIntPtr(luaPtr);
            var x = (int) lua.CheckNumber(1);
            var y = (int) lua.CheckNumber(2);
            lua.PushBoolean(RainEd.Instance.Level.IsInBounds(x, y));
            return 1;
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

                case "borderLeft":
                    RainEd.Instance.Level.BufferTilesLeft = (int) lua.CheckNumber(3);
                    break;

                case "borderTop":
                    RainEd.Instance.Level.BufferTilesTop = (int) lua.CheckNumber(3);
                    break;
                
                case "borderRight":
                    RainEd.Instance.Level.BufferTilesRight = (int) lua.CheckNumber(3);
                    break;

                case "borderBottom":
                    RainEd.Instance.Level.BufferTilesBot = (int) lua.CheckNumber(3);
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