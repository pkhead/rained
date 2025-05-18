using System.Diagnostics;
using System.Numerics;
using KeraLua;
using Rained.Assets;
using Rained.EditorGui.Editors;
using Rained.LevelData;

namespace Rained.LuaScripting.Modules;

static class PropModule
{
    private static readonly ObjectWrap<Prop> wrap = new("Prop", "PROP_REGISTRY");
    private static string[] propColorNames = null!;
    private static bool _changeRecordDirty = false;

    public static void Init(Lua lua, NLua.Lua nlua)
    {
        propColorNames = new string[LuaInterface.Host.PropDatabase.PropColors.Count];

        int i = 0;
        foreach (var col in LuaInterface.Host.PropDatabase.PropColors)
        {
            propColorNames[i++] = col.Name;
        }

        DefinePropClass(lua);

        lua.NewTable();

        lua.ModuleFunction("newProp", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var initName = lua.CheckString(1);
            if (!LuaInterface.Host.PropDatabase.TryGetPropFromName(initName, out var init))
            {
                return lua.ArgumentError(1, $"unrecognized prop '{initName}'");
            }
            Debug.Assert(init is not null);
            
            // 2 arguments overload with vertex table
            if (lua.GetTop() == 2)
            {

                Span<Vector2> pts = stackalloc Vector2[4];
                GetQuad(lua, 2, pts);
                var prop = new Prop(init, pts.ToArray());
                wrap.PushWrapper(lua, prop);
            }
            else
            {
                var center = new Vector2(
                    (float) lua.CheckNumber(2),
                    (float) lua.CheckNumber(3)
                );

                var size = new Vector2(
                    (float) lua.CheckNumber(4),
                    (float) lua.CheckNumber(5)
                );

                var prop = new Prop(init, center, size);
                wrap.PushWrapper(lua, prop);
            }

            return 1;
        });

        lua.ModuleFunction("addProp", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var prop = wrap.GetRef(lua, 1);
            var level = LuaInterface.Host.LevelCheck(lua);

            if (!level.Props.Contains(prop))
            {
                level.Props.Add(prop);
                _changeRecordDirty = true;
            }

            return 0;
        });

        lua.ModuleFunction("removeProp", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var prop = wrap.GetRef(lua, 1);

            LuaInterface.Host.LevelCheck(lua).Props.Remove(prop);
            if (LuaInterface.Host.SelectedProps.Remove(prop))
            {
                _changeRecordDirty = true;
                lua.PushBoolean(true);
            }
            else
            {
                lua.PushBoolean(false);
            }

            return 1;
        });

        lua.ModuleFunction("getProps", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.NewTable();

            int i = 1;
            foreach (var prop in LuaInterface.Host.LevelCheck(lua).Props)
            {
                wrap.PushWrapper(lua, prop);
                lua.RawSetInteger(-2, i);
                i++; 
            }

            return 1;
        });

        lua.ModuleFunction("getSelection", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.NewTable();

            int i = 1;
            foreach (var prop in LuaInterface.Host.SelectedProps)
            {
                wrap.PushWrapper(lua, prop);
                lua.RawSetInteger(-2, i);
                i++;
            }

            return 1;
        });

        lua.ModuleFunction("clearSelection", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            LuaInterface.Host.SelectedProps.Clear();
            return 0;
        });

        lua.ModuleFunction("addToSelection", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var prop = wrap.GetRef(lua, 1);

            if (!LuaInterface.Host.SelectedProps.Contains(prop))
                LuaInterface.Host.SelectedProps.Add(prop);
            
            return 0;
        });

        lua.ModuleFunction("removeFromSelection", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var prop = wrap.GetRef(lua, 1);
            
            lua.PushBoolean( LuaInterface.Host.SelectedProps.Remove(prop) );
            return 1;
        });

        lua.ModuleFunction("getPropCatalog", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var propDb = LuaInterface.Host.PropDatabase;

            lua.NewTable();
            int i = 1;
            foreach (var cat in propDb.Categories)
            {
                if (cat.IsTileCategory) continue;
                foreach (var init in cat.Props)
                {
                    lua.PushString(init.Name);
                    lua.RawSetInteger(-2, i++);
                }
            }

            return 1;
        });

        lua.ModuleFunction("getPropCategories", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var propDb = LuaInterface.Host.PropDatabase;

            lua.NewTable();
            int i = 1;
            foreach (var cat in propDb.Categories)
            {
                if (cat.IsTileCategory) continue;
                
                lua.PushString(cat.Name);
                lua.RawSetInteger(-2, i++);
            }

            return 1;
        });

        lua.ModuleFunction("getTileAsPropCategories", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var propDb = LuaInterface.Host.PropDatabase;

            lua.NewTable();
            int i = 1;
            foreach (var cat in propDb.TileCategories)
            {   
                lua.PushString(cat.Name);
                lua.RawSetInteger(-2, i++);
            }

            return 1;
        });

        lua.ModuleFunction("getPropsInCategory", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var propDb = LuaInterface.Host.PropDatabase;
            var catName = lua.CheckString(1);

            foreach (var cat in propDb.Categories)
            {   
                if (cat.IsTileCategory) continue;
                if (cat.Name == catName)
                {
                    lua.NewTable();
                    int i = 1;

                    foreach (var init in cat.Props)
                    {
                        lua.PushString(init.Name);
                        lua.RawSetInteger(-2, i++);
                    }

                    return 1;
                }
            }

            lua.PushNil();
            return 1;
        });

        lua.ModuleFunction("getPropsInTileCategory", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var propDb = LuaInterface.Host.PropDatabase;
            var catName = lua.CheckString(1);

            foreach (var cat in propDb.TileCategories)
            {   
                if (cat.Name == catName)
                {
                    lua.NewTable();
                    int i = 1;

                    foreach (var init in cat.Props)
                    {
                        lua.PushString(init.Name);
                        lua.RawSetInteger(-2, i++);
                    }

                    return 1;
                }
            }

            lua.PushNil();
            return 1;
        });

        lua.ModuleFunction("getCustomColors", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var propDb = LuaInterface.Host.PropDatabase;
            
            lua.NewTable();
            int i = 1;
            foreach (var propColor in propDb.PropColors)
            {
                lua.PushString(propColor.Name);
                lua.RawSetInteger(-2, i++);
            }

            return 1;
        });

        lua.ModuleFunction("getPropInfo", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            if (!LuaInterface.Host.PropDatabase.TryGetPropFromName(lua.CheckString(1), out PropInit? init))
            {
                lua.PushNil();
                return 1;
            }

            lua.NewTable();

            lua.PushString(init.Name);
            lua.SetField(-2, "name");

            lua.PushString(init.Category.Name);
            lua.SetField(-2, "category");

            lua.PushString(init.Type switch
            {
                PropType.Standard => "standard",
                PropType.VariedStandard => "variedStandard",
                PropType.Soft => "soft",
                PropType.ColoredSoft => "coloredSoft",
                PropType.VariedSoft => "variedSoft",
                PropType.SimpleDecal => "simpleDecal",
                PropType.VariedDecal => "variedDecal",
                PropType.Antimatter => "antimatter",
                PropType.Rope => "rope",
                PropType.Long => "long",
                _ => ""
            });
            lua.SetField(-2, "type");

            if (init.ColorTreatment == PropColorTreatment.Unspecified)
                lua.PushNil();
            else
                lua.PushString(init.ColorTreatment switch
                {
                    PropColorTreatment.Standard => "standard",
                    PropColorTreatment.Bevel => "bevel",
                    _ => ""
                });
            lua.SetField(-2, "colorTreatment");

            lua.PushInteger(init.VariationCount);
            lua.SetField(-2, "variationCount");

            lua.PushBoolean(init.PropFlags.HasFlag(PropFlags.Tile));
            lua.SetField(-2, "tileAsProp");

            return 1;
        });

        // set rained.props
        lua.SetField(-2, "props");
    }

    private static void GetQuad(Lua lua, int arg, Span<Vector2> pts)
    {
        for (int i = 0; i < 4; i++)
        {
            lua.GetInteger(arg, i + 1);
            lua.ArgumentCheck(lua.IsTable(-1), arg, "invalid quad struct");

            lua.GetField(-1, "x");
            lua.ArgumentCheck(lua.IsNumber(-1), arg, "invalid quad struct");
            var x = (float) lua.ToNumber(-1);
            lua.Pop(1);

            lua.GetField(-1, "y");
            lua.ArgumentCheck(lua.IsNumber(-1), arg, "invalid quad struct");
            var y = (float) lua.ToNumber(-1);
            lua.Pop(1);

            pts[i] = new Vector2(x, y);

            lua.Pop(1); // pop vertex table
        }
    }

    public static void DefinePropClass(Lua lua)
    {
        wrap.InitMetatable(lua);

        lua.ModuleFunction("__index", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var prop = wrap.GetRef(lua, 1);
            var k = lua.CheckString(2);

            switch (k)
            {
                case "name":
                    lua.PushString(prop.PropInit.Name);
                    break;

                case "renderOrder":
                    lua.PushInteger(prop.RenderOrder);
                    break;

                case "depthOffset":
                    lua.PushInteger(prop.DepthOffset);
                    break;

                case "seed":
                    lua.PushInteger(prop.Seed);
                    break;

                case "renderTime":
                    lua.PushString(prop.RenderTime switch
                    {
                        PropRenderTime.PreEffects => "preEffects",
                        PropRenderTime.PostEffects => "postEffects",
                        _ => ""
                    });
                    break;

                case "variation":
                    lua.PushInteger(prop.Variation + 1);
                    break;

                case "applyColor":
                    lua.PushBoolean(prop.ApplyColor);
                    break;

                case "customDepth":
                    lua.PushInteger(prop.CustomDepth);
                    break;

                case "customColor":
                    lua.PushString(LuaInterface.Host.PropDatabase.PropColors[prop.CustomColor].Name);
                    break;
                    
                case "clone":
                    lua.PushCFunction(static (nint luaPtr) =>
                    {
                        var lua = Lua.FromIntPtr(luaPtr);
                        var prop = wrap.GetRef(lua, 1);
                        wrap.PushWrapper(lua, prop.Clone());
                        return 1;
                    });
                    break;

                case "getRect":
                    lua.PushCFunction(static (nint luaPtr) =>
                    {
                        var lua = Lua.FromIntPtr(luaPtr);
                        var prop = wrap.GetRef(lua, 1);

                        if (!prop.IsAffine)
                        {
                            lua.PushNil();
                            return 1;
                        }

                        var rect = prop.Rect;
                        lua.NewTable();
                        lua.PushNumber(rect.Center.X);
                        lua.SetField(-2, "x");
                        lua.PushNumber(rect.Center.Y);
                        lua.SetField(-2, "y");
                        lua.PushNumber(rect.Size.X);
                        lua.SetField(-2, "width");
                        lua.PushNumber(rect.Size.Y);
                        lua.SetField(-2, "height");
                        lua.PushNumber(rect.Rotation);
                        lua.SetField(-2, "rotation");

                        return 1;
                    });
                    break;

                case "setRect":
                    lua.PushCFunction(static (nint luaPtr) =>
                    {
                        var lua = Lua.FromIntPtr(luaPtr);
                        var prop = wrap.GetRef(lua, 1);
                        lua.CheckType(2, LuaType.Table);

                        lua.GetField(2, "x");
                        if (!lua.IsNumber(-1)) return lua.ArgumentError(2, "invalid rect struct");
                        var x = (float) lua.ToNumber(-1);
                        lua.Pop(1);

                        lua.GetField(2, "y");
                        if (!lua.IsNumber(-1)) return lua.ArgumentError(2, "invalid rect struct");
                        var y = (float) lua.ToNumber(-1);
                        lua.Pop(1);

                        lua.GetField(2, "width");
                        if (!lua.IsNumber(-1)) return lua.ArgumentError(2, "invalid rect struct");
                        var width = (float) lua.ToNumber(-1);
                        lua.Pop(1);

                        lua.GetField(2, "height");
                        if (!lua.IsNumber(-1)) return lua.ArgumentError(2, "invalid rect struct");
                        var height = (float) lua.ToNumber(-1);
                        lua.Pop(1);

                        lua.GetField(2, "rotation");
                        if (!lua.IsNumber(-1)) return lua.ArgumentError(2, "invalid rect struct");
                        var rotation = (float) lua.ToNumber(-1);
                        lua.Pop(1);

                        TransformChange(prop);
                        prop.Transform.isAffine = true;
                        prop.Transform.rect = new RotatedRect()
                        {
                            Center = new Vector2(x, y),
                            Size = new Vector2(width, height),
                            Rotation = rotation
                        };
                        

                        return 0;
                    });
                    break;

                case "getQuad":
                    lua.PushCFunction(static (nint luaPtr) =>
                    {
                        var lua = Lua.FromIntPtr(luaPtr);
                        var prop = wrap.GetRef(lua, 1);

                        var quad = prop.QuadPoints;
                        lua.CreateTable(4, 0);

                        for (int i = 0; i < 4; i++)
                        {
                            lua.NewTable();
                            lua.PushNumber(quad[i].X);
                            lua.SetField(-2, "x");
                            lua.PushNumber(quad[i].Y);
                            lua.SetField(-2, "y");
                            lua.RawSetInteger(-2, i+1);
                        }

                        return 1;
                    });
                    break;

                case "setQuad":
                    lua.PushCFunction(static (nint luaPtr) =>
                    {
                        var lua = Lua.FromIntPtr(luaPtr);
                        var prop = wrap.GetRef(lua, 1);
                        lua.CheckType(2, LuaType.Table);

                        Span<Vector2> pts = stackalloc Vector2[4];

                        for (int i = 0; i < 4; i++)
                        {
                            lua.GetInteger(2, i + 1);
                            lua.ArgumentCheck(lua.IsTable(-1), 2, "invalid quad struct");

                            lua.GetField(-1, "x");
                            lua.ArgumentCheck(lua.IsNumber(-1), 2, "invalid quad struct");
                            var x = (float) lua.ToNumber(-1);
                            lua.Pop(1);

                            lua.GetField(-1, "y");
                            lua.ArgumentCheck(lua.IsNumber(-1), 2, "invalid quad struct");
                            var y = (float) lua.ToNumber(-1);
                            lua.Pop(1);

                            pts[i] = new Vector2(x, y);

                            lua.Pop(1); // pop vertex table
                        }

                        TransformChange(prop);
                        prop.Transform.isAffine = false;
                        for (int i = 0; i < 4; i++)
                        {
                            prop.Transform.quad[i] = pts[i];
                        }

                        prop.TryConvertToAffine();

                        return 0;
                    });
                    break;

                case "resetTransform":
                    lua.PushCFunction(static (nint luaPtr) =>
                    {
                        var lua = Lua.FromIntPtr(luaPtr);
                        var prop = wrap.GetRef(lua, 1);

                        TransformChange(prop);
                        prop.ResetTransform();

                        return 0;
                    });
                    break;

                case "flipX":
                    lua.PushCFunction(static (nint luaPtr) =>
                    {
                        var lua = Lua.FromIntPtr(luaPtr);
                        var prop = wrap.GetRef(lua, 1);

                        TransformChange(prop);
                        prop.FlipX();

                        return 0;
                    });
                    break;

                case "flipY":
                    lua.PushCFunction(static (nint luaPtr) =>
                    {
                        var lua = Lua.FromIntPtr(luaPtr);
                        var prop = wrap.GetRef(lua, 1);

                        TransformChange(prop);
                        prop.FlipY();

                        return 0;
                    });
                    break;
                
                default:
                    lua.PushNil();
                    break;
            }

            return 1;
        });

        lua.ModuleFunction("__newindex", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var prop = wrap.GetRef(lua, 1);
            var k = lua.CheckString(2);

            switch (k)
            {
                case "renderOrder":
                    SettingsChange(prop);
                    prop.RenderOrder = (int) lua.CheckInteger(3);
                    break;

                case "depthOffset":
                    SettingsChange(prop);
                    prop.DepthOffset = (int) lua.CheckInteger(3);
                    prop.DepthOffset = Math.Clamp( prop.DepthOffset, 0, 29 );
                    break;

                case "seed":
                    SettingsChange(prop);
                    prop.Seed = (int) lua.CheckInteger(3);
                    prop.Seed = Util.Mod( prop.Seed, 1000 );
                    break;

                case "renderTime":
                    var e = lua.CheckOption(3, null, ["preEffects", "postEffects"]);
                    SettingsChange(prop);
                    prop.RenderTime = (PropRenderTime) e;
                    break;

                case "variation":
                    SettingsChange(prop);
                    prop.Variation = (int) lua.CheckInteger(3);
                    prop.Variation = Math.Clamp(prop.Variation, 1, prop.PropInit.VariationCount) - 1;
                    break;

                case "applyColor":
                    SettingsChange(prop);
                    prop.ApplyColor = lua.ToBoolean(3);
                    break;

                case "customDepth":
                    SettingsChange(prop);
                    prop.CustomDepth = (int) lua.CheckInteger(3);
                    prop.CustomDepth = Math.Max( 0, prop.CustomDepth  );
                    break;

                case "customColor":
                    SettingsChange(prop);
                    prop.CustomColor = lua.CheckOption(3, null, propColorNames);
                    break;
                    
                default:
                    return lua.ErrorWhere($"unknown field \"{k}\"");
            }

            return 0;
        });

        lua.Pop(1);
    }

    public static void UpdateSettingsSnapshot()
    {
        if (_changeRecordDirty)
        {
            var changeRecorder = RainEd.Instance.LevelView.GetEditor<PropEditor>().ChangeRecorder;
            changeRecorder.TakeSettingsSnapshot();
            _changeRecordDirty = false;
        }
    }

    private static void SettingsChange(Prop prop)
    {
        HistoryModule.ChangeRecorder.PropRecorder.ChangeSettings(prop);
        _changeRecordDirty = true;
    }

    private static void TransformChange(Prop prop)
    {
        HistoryModule.ChangeRecorder.PropRecorder.ChangeTransform(prop);
    }
}