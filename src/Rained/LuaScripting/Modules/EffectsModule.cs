using System.Diagnostics;
using KeraLua;
using Rained.Assets;
using Rained.EditorGui.Editors;
using Rained.LevelData;

namespace Rained.LuaScripting.Modules;

static class EffectsModule
{
    private static readonly ObjectWrap<Effect> wrap = new("Effect", "EFFECT_REGISTRY");

    private static readonly string[] layerEnum = ["All", "1", "2", "3", "1+2", "2+3"];
    private static readonly string[] colorNames = ["Color1", "Color2", "Dead"];

    public static void Init(Lua lua, NLua.Lua nlua)
    {
        DefineEffectType(lua);

        lua.NewTable();

        lua.ModuleFunction("getCount", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushInteger(RainEd.Instance.Level.Effects.Count);
            return 1;
        });

        lua.ModuleFunction("getEffect", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var effects = RainEd.Instance.Level.Effects;

            var idx = (int) lua.CheckInteger(1) - 1;
            if (idx < 0 || idx >= effects.Count) return lua.ArgumentError(1, "index is out of range");

            wrap.PushWrapper(lua, effects[idx]);
            return 1;
        });
        
        lua.ModuleFunction("getEffectName", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var effects = RainEd.Instance.Level.Effects;

            var idx = (int) lua.CheckInteger(1) - 1;
            if (idx < 0 || idx >= effects.Count) return lua.ArgumentError(1, "index is out of range");

            lua.PushString(effects[idx].Data.name);
            return 1;
        });

        lua.ModuleFunction("removeEffect", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var effects = RainEd.Instance.Level.Effects;
            
            if (lua.IsUserData(1))
            {
                var eff = wrap.GetRef(lua, 1);
                lua.PushBoolean( effects.Remove(eff) );
                return 1;
            }
            else
            {
                var idx = (int) lua.CheckInteger(1) - 1;
                if (idx < 0 || idx >= effects.Count) return lua.ArgumentError(1, "index is out of range");

                effects.RemoveAt(idx);
                return 0;
            }
        });

        lua.ModuleFunction("addEffect", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var effects = RainEd.Instance.Level.Effects;
            var eff = wrap.GetRef(lua, 1);
            var idx = (int) lua.OptInteger(2, effects.Count + 1) - 1;
            if (idx < 0 || idx > effects.Count) return lua.ArgumentError(2, "index is out of range");
            
            if (effects.Remove(eff) && idx == effects.Count + 1)
            {
                idx--;
            }
            effects.Insert(idx, eff);
            return 0;
        });

        lua.ModuleFunction("newEffect", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var effectName = lua.CheckString(1);

            if (!RainEd.Instance.EffectsDatabase.TryGetEffectFromName(effectName, out var init))
                return lua.ArgumentError(1, $"effect {effectName} does not exist");
            
            var eff = new Effect(RainEd.Instance.Level, init);
            wrap.PushWrapper(lua, eff);
            return 1;
        });

        lua.ModuleFunction("getSelectedEffect", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var editor = RainEd.Instance.LevelView.GetEditor<EffectsEditor>();
            if (editor.SelectedEffect < 0) lua.PushNil();
            else
            {
                lua.PushInteger(editor.SelectedEffect + 1);
            }
            return 1;
        });

        lua.ModuleFunction("hasOption", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var effectName = lua.CheckString(1);

            if (!RainEd.Instance.EffectsDatabase.TryGetEffectFromName(effectName, out var init))
                return lua.ArgumentError(1, $"effect {effectName} does not exist");
            
            var name = lua.CheckString(2);

            switch (name)
            {
                case "Layers":
                    if (init.useLayers)
                    {
                        lua.PushBoolean(true); return 1;
                    }
                    break;

                case "3D":
                    if (init.use3D)
                    {
                        lua.PushBoolean(true); return 1;
                    }
                    break;

                case "Color":
                    if (init.usePlantColors)
                    {
                        lua.PushBoolean(true); return 1;
                    }
                    break;

                case "Affect Gradients and Decals":
                    if (init.useDecalAffect)
                    {
                        lua.PushBoolean(true); return 1;
                    }
                    break;

                case "Seed":
                    lua.PushBoolean(true); return 1;

                default:
                {
                    var configIndex = init.GetCustomConfigIndex(name);
                    lua.PushBoolean(configIndex != -1);
                    return 1;
                }
            }

            throw new UnreachableException();
        });

        lua.ModuleFunction("getOptions", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var effectName = lua.CheckString(1);

            if (!RainEd.Instance.EffectsDatabase.TryGetEffectFromName(effectName, out var init))
                return lua.ArgumentError(1, $"effect {effectName} does not exist");

            int i = 1;
            lua.NewTable();

            lua.PushString("Seed");
            lua.RawSetInteger(-2, i++);

            if (init.useLayers)
            {
                lua.PushString("Layers");
                lua.RawSetInteger(-2, i++);
            }

            if (init.use3D)
            {
                lua.PushString("3D");
                lua.RawSetInteger(-2, i++);
            }

            if (init.usePlantColors)
            {
                lua.PushString("Color");
                lua.RawSetInteger(-2, i++);
            }

            if (init.useDecalAffect)
            {
                lua.PushString("Affect Gradients and Decals");
                lua.RawSetInteger(-2, i++);
            }

            foreach (var opt in init.customConfigs)
            {
                lua.PushString(opt.Name);
                lua.RawSetInteger(-2, i++);
            }

            return 1;
        });

        lua.ModuleFunction("getOptionType", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var effectName = lua.CheckString(1);

            if (!RainEd.Instance.EffectsDatabase.TryGetEffectFromName(effectName, out var init))
                return lua.ArgumentError(1, $"effect {effectName} does not exist");
            
            var optionName = lua.CheckString(2);
            switch (optionName)
            {
                case "Layers":
                    if (init.useLayers)
                    {
                        lua.PushString("enum");
                        lua.CreateTable(layerEnum.Length, 0);
                        int ti = 1;
                        for (int i = 0; i < layerEnum.Length; i++)
                        {
                            lua.PushString(layerEnum[i]);
                            lua.RawSetInteger(-2, ti++);
                        }
                        return 2;
                    }
                    break;
                
                case "Color":
                    if (init.usePlantColors)
                    {
                        lua.PushString("enum");
                        lua.CreateTable(colorNames.Length, 0);
                        int ti = 1;
                        for (int i = 0; i < colorNames.Length; i++)
                        {
                            lua.PushString(colorNames[i]);
                            lua.RawSetInteger(-2, ti++);
                        }
                        return 2;
                    }
                    break;

                case "3D":
                    if (init.use3D)
                    {
                        lua.PushString("boolean");
                        return 1;
                    }
                    break;
                
                case "Affect Gradients and Decals":
                    if (init.useDecalAffect)
                    {
                        lua.PushString("boolean");
                        return 1;
                    }
                    break;
            }

            // a custom option...
            var idx = init.GetCustomConfigIndex(optionName);
            if (idx == -1)
            {
                lua.PushNil();
                return 1;
            }

            if (init.customConfigs[idx] is CustomEffectInteger intConfig)
            {
                lua.PushString("integer");
                return 1;
            }
            else if (init.customConfigs[idx] is CustomEffectString strConfig)
            {
                lua.PushString("enum");
                lua.CreateTable(strConfig.Options.Length, 0);
                int ti = 1;
                foreach (var opt in strConfig.Options)
                {
                    lua.PushString(opt);
                    lua.RawSetInteger(-2, ti++);
                }
                return 2;
            }
            else throw new UnreachableException();
        });

        lua.ModuleFunction("getOptionDefaultValue", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var effectName = lua.CheckString(1);

            if (!RainEd.Instance.EffectsDatabase.TryGetEffectFromName(effectName, out var init))
                return lua.ArgumentError(1, $"effect {effectName} does not exist");
            
            var optionName = lua.CheckString(2);
            switch (optionName)
            {
                case "Layers":
                    if (init.useLayers)
                    {
                        lua.PushString(layerEnum[(int)init.defaultLayer]);
                        return 1;
                    }
                    break;
                
                case "Color":
                    if (init.usePlantColors)
                    {
                        lua.PushString(layerEnum[init.defaultPlantColor]);
                        return 1;
                    }
                    break;

                case "3D":
                    if (init.use3D)
                    {
                        lua.PushBoolean(false);
                        return 1;
                    }
                    break;
                
                case "Affect Gradients and Decals":
                    if (init.useDecalAffect)
                    {
                        lua.PushBoolean(init.decalAffectDefault);
                        return 1;
                    }
                    break;
            }

            // a custom option...
            var idx = init.GetCustomConfigIndex(optionName);
            if (idx == -1)
            {
                lua.PushNil();
                return 1;
            }

            if (init.customConfigs[idx] is CustomEffectInteger intConfig)
            {
                lua.PushNil();
                return 1;
            }
            else if (init.customConfigs[idx] is CustomEffectString strConfig)
            {
                lua.PushString(strConfig.Default);
                return 1;
            }
            else throw new UnreachableException();
        });

        lua.ModuleFunction("isInstalled", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var effectName = lua.CheckString(1);
            lua.PushBoolean(RainEd.Instance.EffectsDatabase.TryGetEffectFromName(effectName, out _));
            return 1;
        });

        lua.ModuleFunction("getEffectCatalog", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);

            lua.NewTable();
            int i = 1;
            foreach (var group in RainEd.Instance.EffectsDatabase.Groups)
            {
                foreach (var effect in group.effects)
                {
                    lua.PushString(effect.name);
                    lua.RawSetInteger(-2, i++);
                }
            }
            return 1;
        });

        lua.ModuleFunction("getEffectCategories", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);

            lua.NewTable();
            int i = 1;
            foreach (var group in RainEd.Instance.EffectsDatabase.Groups)
            {
                lua.PushString(group.name);
                lua.RawSetInteger(-2, i++);
            }
            return 1;
        });

        lua.ModuleFunction("getEffectsInCategory", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var nameArg = lua.CheckString(1);

            foreach (var group in RainEd.Instance.EffectsDatabase.Groups)
            {
                if (group.name == nameArg)
                {
                    lua.NewTable();
                    int i = 1;
                    foreach (var eff in group.effects)
                    {
                        lua.PushString(eff.name);
                        lua.RawSetInteger(-2, i++);
                    }
                    return 1;
                }
            }

            lua.PushNil();
            return 1;
        });

        lua.SetField(-2, "effects");
    }

    private static void ConfigChange(Effect eff)
    {
        HistoryModule.ChangeRecorder.EffectRecorder.ChangeConfig(eff);
    }

    public static void DefineEffectType(Lua lua)
    {
        wrap.InitMetatable(lua);

        lua.ModuleFunction("__index", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var eff = wrap.GetRef(lua, 1);
            var k = lua.CheckString(2);

            switch (k)
            {
                case "name":
                    lua.PushString(eff.Data.name);
                    break;

                case "index":
                {
                    var idx = RainEd.Instance.Level.Effects.IndexOf(eff);
                    if (idx == -1) lua.PushNil();
                    else lua.PushInteger(idx + 1);
                    break;
                }

                case "clone":
                    lua.PushCFunction(static (nint luaPtr) =>
                    {
                        var lua = Lua.FromIntPtr(luaPtr);
                        var eff = wrap.GetRef(lua, 1);

                        var clone = new Effect(RainEd.Instance.Level, eff.Data)
                        {
                            AffectGradientsAndDecals = eff.AffectGradientsAndDecals,
                            Is3D = eff.Is3D,
                            Layer = eff.Layer,
                            Matrix = eff.Matrix,
                            PlantColor = eff.PlantColor,
                            Seed = eff.Seed,
                            CustomValues = (int[])eff.CustomValues.Clone()
                        };

                        for (int x = 0; x < eff.Width; x++)
                        {
                            for (int y = 0; y < eff.Height; y++)
                            {
                                clone.Matrix[x,y] = eff.Matrix[x,y];
                            }
                        }

                        wrap.PushWrapper(lua, clone);
                        return 1;
                    });
                    break;

                case "getOption":
                    lua.PushCFunction(static (nint luaPtr) =>
                    {
                        var lua = Lua.FromIntPtr(luaPtr);
                        var eff = wrap.GetRef(lua, 1);
                        var name = lua.CheckString(2);
                        bool valid = true;

                        switch (name)
                        {
                            case "Layers":
                                if (eff.Data.useLayers)
                                {
                                    lua.PushString( layerEnum[(int)eff.Layer] );
                                }
                                else valid = false;
                                break;

                            case "3D":
                                if (eff.Data.use3D)
                                {
                                    lua.PushBoolean(eff.Is3D);
                                }
                                else valid = false;
                                break;

                            case "Color":
                                if (eff.Data.usePlantColors)
                                {
                                    lua.PushString( colorNames[eff.PlantColor] );
                                }
                                else valid = false;
                                break;

                            case "Affect Gradients and Decals":
                                if (eff.Data.useDecalAffect)
                                {
                                    lua.PushBoolean(eff.AffectGradientsAndDecals);
                                }
                                else valid = false;
                                break;

                            case "Seed":
                                lua.PushInteger(eff.Seed);
                                break;

                            default:
                                valid = false;
                                break;
                        }

                        if (!valid)
                        {
                            var configIndex = eff.Data.GetCustomConfigIndex(name);
                            if (configIndex == -1)
                                return lua.ErrorWhere($"effect '{eff.Data.name}' does not have option '{name}'");

                            if (eff.Data.customConfigs[configIndex] is CustomEffectString strConfig)
                            {
                                lua.PushString( strConfig.Options[eff.CustomValues[configIndex]] );
                            }
                            else if (eff.Data.customConfigs[configIndex] is CustomEffectInteger intConfig)
                            {
                                lua.PushInteger( eff.CustomValues[configIndex] );
                            }
                            else throw new UnreachableException();
                        }

                        return 1;
                    });
                    break;

                case "setOption":
                    lua.PushCFunction(static (nint luaPtr) =>
                    {
                        var lua = Lua.FromIntPtr(luaPtr);
                        var eff = wrap.GetRef(lua, 1);
                        var name = lua.CheckString(2);
                        const int ValueIndex = 3;
                        bool valid = true;

                        switch (name)
                        {
                            case "Layers":
                                if (eff.Data.useLayers)
                                {
                                    if (lua.IsInteger(ValueIndex))
                                    {
                                        switch (lua.ToInteger(ValueIndex))
                                        {
                                            case 1:
                                                ConfigChange(eff);
                                                eff.Layer = Effect.LayerMode.First;
                                                break;

                                            case 2:
                                                ConfigChange(eff);
                                                eff.Layer = Effect.LayerMode.Second;
                                                break;

                                            case 3:
                                                ConfigChange(eff);
                                                eff.Layer = Effect.LayerMode.Third;
                                                break;

                                            default:
                                                return lua.ArgumentError(ValueIndex, "invalid layer: " + lua.ToInteger(ValueIndex));
                                        }
                                    }
                                    else if (lua.IsString(ValueIndex) && lua.ToString(ValueIndex) == "all")
                                    {
                                        ConfigChange(eff);
                                        eff.Layer = Effect.LayerMode.All;
                                    }
                                    else
                                    {
                                        ConfigChange(eff);
                                        eff.Layer = (Effect.LayerMode) lua.CheckOption(ValueIndex, null, layerEnum);
                                    }
                                }
                                break;
                            
                            case "3D":
                                if (eff.Data.use3D)
                                {
                                    ConfigChange(eff);
                                    eff.Is3D = lua.ToBoolean(ValueIndex);
                                }
                                else valid = false;
                                break;

                            case "Color":
                                if (eff.Data.usePlantColors)
                                {
                                    ConfigChange(eff);
                                    eff.PlantColor = lua.CheckOption(ValueIndex, null, colorNames);
                                }
                                else valid = false;
                                break;

                            case "Affect Gradients and Decals":
                                if (eff.Data.useDecalAffect)
                                {
                                    ConfigChange(eff);
                                    eff.AffectGradientsAndDecals = lua.ToBoolean(ValueIndex);
                                }
                                else valid = false;
                                break;

                            case "Seed":
                                ConfigChange(eff);
                                eff.Seed = (int) lua.ToNumber(ValueIndex);
                                break;

                            default:
                                valid = false;
                                break;
                        }

                        if (!valid)
                        {
                            var configIndex = eff.Data.GetCustomConfigIndex(name);
                            if (configIndex == -1)
                                return lua.ErrorWhere($"effect '{eff.Data.name}' does not have option '{name}'");

                            ConfigChange(eff);
                            if (eff.Data.customConfigs[configIndex] is CustomEffectString strConfig)
                            {
                                eff.CustomValues[configIndex] = lua.CheckOption(ValueIndex, null, strConfig.Options);
                            }
                            else if (eff.Data.customConfigs[configIndex] is CustomEffectInteger intConfig)
                            {
                                eff.CustomValues[configIndex] = (int) lua.CheckInteger(ValueIndex);
                            }
                            else throw new UnreachableException();
                        }

                        return 0;
                    });
                    break;

                case "setMatrixValue":
                    lua.PushCFunction(static (nint luaPtr) =>
                    {
                        var lua = Lua.FromIntPtr(luaPtr);
                        var eff = wrap.GetRef(lua, 1);
                        var x = (int) lua.CheckInteger(2);
                        var y = (int) lua.CheckInteger(3);
                        var v = (float) lua.CheckNumber(4);
                        v = float.Clamp(v, 0f, 100f);

                        if (!RainEd.Instance.Level.IsInBounds(x, y))
                            return 0;

                        HistoryModule.ChangeRecorder.EffectRecorder.ChangeMatrix(eff);
                        eff.Matrix[x, y] = v;
                        if (eff.Data.binary) eff.Matrix[x,y] = eff.Matrix[x,y] >= 50f ? 100f : 0f;
                        return 0;
                    });
                    break;

                case "getMatrixValue":
                    lua.PushCFunction(static (nint luaPtr) =>
                    {
                        var lua = Lua.FromIntPtr(luaPtr);
                        var eff = wrap.GetRef(lua, 1);
                        var x = (int) lua.CheckInteger(2);
                        var y = (int) lua.CheckInteger(3);

                        if (!RainEd.Instance.Level.IsInBounds(x, y))
                        {
                            lua.PushNil();
                            return 1;
                        }

                        lua.PushNumber(eff.Matrix[x, y]);
                        return 1;
                    });
                    break;

                case "getMatrix":
                    lua.PushCFunction(static (nint luaPtr) =>
                    {
                        var lua = Lua.FromIntPtr(luaPtr);
                        var eff = wrap.GetRef(lua, 1);

                        lua.CreateTable(eff.Width, 0);
                        for (int x = 0; x < eff.Width; x++)
                        {
                            lua.CreateTable(eff.Height, 0);
                            for (int y = 0; y < eff.Height; y++)
                            {
                                lua.PushNumber(eff.Matrix[x, y]);
                                lua.RawSetInteger(-2, y + 1);
                            }
                            lua.RawSetInteger(-2, x + 1);
                        }
                        return 1;
                    });
                    break;

                case "setMatrix":
                    lua.PushCFunction(static (nint luaPtr) =>
                    {
                        var lua = Lua.FromIntPtr(luaPtr);
                        var level = RainEd.Instance.Level;
                        var eff = wrap.GetRef(lua, 1);

                        // matrix validation
                        lua.CheckType(2, LuaType.Table);
                        lua.PushCopy(2);

                        var twidth = lua.Length(-1);
                        if (twidth != level.Width)
                            return lua.ArgumentError(2, "mismatched array dimensions");

                        for (int i = 1; i <= twidth; i++)
                        {
                            lua.GetInteger(-1, i);
                            if (!lua.IsTable(-1)) return lua.ArgumentError(2, "invalid array");

                            var theight = lua.Length(-1);
                            if (theight != level.Height)
                                return lua.ArgumentError(2, "mismatched array dimensions");
                            
                            for (int j = 1; j <= theight; j++)
                            {
                                lua.GetInteger(-1, j);
                                if (!lua.IsNumber(-1)) return lua.ArgumentError(2, "invalid array elements");
                                lua.Pop(1);
                            }
                            lua.Pop(1);
                        }

                        // read matrix
                        HistoryModule.ChangeRecorder.EffectRecorder.ChangeMatrix(eff);
                        for (int x = 0; x < level.Width; x++)
                        {
                            lua.GetInteger(-1, x + 1);
                            for (int y = 0; y < level.Height; y++)
                            {
                                lua.GetInteger(-1, y + 1);
                                eff.Matrix[x,y] = (float) double.Clamp(lua.ToNumber(-1), 0.0, 100.0);
                                if (eff.Data.binary) eff.Matrix[x,y] = eff.Matrix[x,y] >= 50f ? 100f : 0f;
                                lua.Pop(1);
                            }
                            lua.Pop(1);
                        }

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
            var cam = wrap.GetRef(lua, 1);
            var k = lua.CheckString(2);
            
            return lua.ErrorWhere($"unknown field \"{k}\"");
        });

        lua.Pop(1);
    }
}