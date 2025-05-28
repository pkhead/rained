namespace Rained.LuaScripting.Modules;

using System.Diagnostics;
using System.Runtime.InteropServices;
using KeraLua;
using LevelData;
using Rained.EditorGui;
using Rained.EditorGui.Editors;

static class ViewModule
{
    public static void Init(Lua lua, NLua.Lua nlua)
    {
        if (!LuaInterface.Host.IsGui) return;

        lua.NewTable();

        // set metatable
        lua.NewTable();
        lua.ModuleFunction("__metatable", static (nint luaPtr) => {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushString("The metatable is locked.");
            return 1;
        });

        lua.ModuleFunction("__index", static (nint luaPtr) => {
            var lua = Lua.FromIntPtr(luaPtr);
            LuaHelpers.LevelCheck(lua);
            var idx = lua.ToString(2);

            switch (idx) {
                case "viewX":
                    lua.PushNumber(RainEd.Instance.LevelView.ViewOffset.X);
                    break;
                
                case "viewY":
                    lua.PushNumber(RainEd.Instance.LevelView.ViewOffset.Y);
                    break;

                case "viewZoom":
                    lua.PushNumber(RainEd.Instance.LevelView.ViewZoom);
                    break;

                case "workLayer":
                    lua.PushNumber(RainEd.Instance.LevelView.WorkLayer+1);
                    break;

                case "geoLayer1":
                    lua.PushBoolean(RainEd.Instance.LevelView.GetEditor<GeometryEditor>().LayerMask[0]);
                    break;

                case "geoLayer2":
                    lua.PushBoolean(RainEd.Instance.LevelView.GetEditor<GeometryEditor>().LayerMask[1]);
                    break;

                case "geoLayer3":
                    lua.PushBoolean(RainEd.Instance.LevelView.GetEditor<GeometryEditor>().LayerMask[2]);
                    break;

                case "palettesEnabled":
                    lua.PushBoolean(RainEd.Instance.LevelView.Renderer.UsePalette);
                    break;

                case "palette1":
                    lua.PushInteger(RainEd.Instance.LevelView.Renderer.Palette.Index);
                    break;

                case "palette2":
                    lua.PushInteger(RainEd.Instance.LevelView.Renderer.Palette.FadeIndex);
                    break;

                case "paletteMix":
                    lua.PushNumber(RainEd.Instance.LevelView.Renderer.Palette.Mix);
                    break;

                default:
                    lua.PushNil();
                    break;
            }

            return 1;
        });

        lua.ModuleFunction("__newindex", static (nint luaPtr) => {
            var lua = Lua.FromIntPtr(luaPtr);
            LuaHelpers.LevelCheck(lua);
            var idx = lua.ToString(2);

            switch (idx) {
                case "viewX":
                    RainEd.Instance.LevelView.ViewOffset.X = (float)lua.CheckNumber(3);
                    break;
                
                case "viewY":
                    RainEd.Instance.LevelView.ViewOffset.Y = (float)lua.CheckNumber(3);
                    break;

                case "viewZoom":
                    RainEd.Instance.LevelView.ViewZoom = (float)lua.CheckNumber(3);
                    break;

                case "workLayer":
                {
                    var layer = Math.Clamp((int)lua.CheckInteger(3) - 1, 0, 2);
                    RainEd.Instance.LevelView.WorkLayer = layer;
                    break;
                }

                case "geoLayer1":
                    RainEd.Instance.LevelView.GetEditor<GeometryEditor>().LayerMask[0] = lua.ToBoolean(3);
                    break;

                case "geoLayer2":
                    RainEd.Instance.LevelView.GetEditor<GeometryEditor>().LayerMask[1] = lua.ToBoolean(3);
                    break;

                case "geoLayer3":
                    RainEd.Instance.LevelView.GetEditor<GeometryEditor>().LayerMask[2] = lua.ToBoolean(3);
                    break;

                case "palettesEnabled":
                    RainEd.Instance.LevelView.Renderer.UsePalette = lua.ToBoolean(3);
                    break;

                case "palette1":
                    RainEd.Instance.LevelView.Renderer.Palette.Index = (int)lua.CheckInteger(3);
                    break;

                case "palette2":
                    RainEd.Instance.LevelView.Renderer.Palette.FadeIndex = (int)lua.CheckInteger(3);
                    break;

                case "paletteMix":
                    RainEd.Instance.LevelView.Renderer.Palette.Mix = Math.Clamp((float)lua.CheckNumber(3), 0f, 1f);
                    break;

                default:
                    lua.ErrorWhere($"unknown field '{idx ?? "nil"}'", 2);
                    break;
            }

            return 0;
        });

        lua.SetMetaTable(-2);

        // set rained.view
        lua.SetField(-2, "view");
    }
}