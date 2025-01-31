using KeraLua;
using Rained.ChangeHistory;
using Rained.EditorGui.Editors;

namespace Rained.LuaScripting.Modules;

static class HistoryModule
{
    public static UniversalChangeRecorder ChangeRecorder => changeRecorder;
    private static readonly UniversalChangeRecorder changeRecorder = new();
    private static readonly string[] changeTypeEnumStrings = ["properties", "cells", "cameras", "effects", "props", "all"];

    public static void Init(Lua lua, NLua.Lua nlua)
    {
        lua.NewTable();

        lua.ModuleFunction("beginChange", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var argc = lua.GetTop();

            if (changeRecorder.Active)
                return lua.ErrorWhere("active change already exists");

            // no arguments
            LevelComponents componentMask = LevelComponents.None;
            if (argc == 0)
            {
                componentMask = LevelComponents.All;
            }
            else
            {
                for (int i = 1; i <= argc; i++)
                {
                    int opt = lua.CheckOption(i, null, changeTypeEnumStrings);
                    if (opt == 5) // ALL
                    {
                        componentMask = LevelComponents.All;
                        break;
                    }
                    else
                    {
                        componentMask |= (LevelComponents)(1 << opt);
                    }
                }
            }

            changeRecorder.BeginChange(componentMask);
            return 0;
        });

        lua.ModuleFunction("endChange", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);

            if (!changeRecorder.Active)
                return lua.ErrorWhere("active change does not exist");
            
            changeRecorder.TryPushChange();
            return 0;
        });

        lua.ModuleFunction("isChangeActive", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushBoolean(changeRecorder.Active);
            return 1;
        });

        lua.ModuleFunction("undo", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushBoolean( RainEd.Instance.ChangeHistory.Undo() );
            return 1;
        });

        lua.ModuleFunction("redo", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushBoolean( RainEd.Instance.ChangeHistory.Redo() );
            return 1;
        });

        // set rained.history
        lua.SetField(-2, "history");
    }
}