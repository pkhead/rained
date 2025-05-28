namespace Rained.LuaScripting.Modules;
using KeraLua;
using Rained.EditorGui;
using Rained.LevelData;

static class RainedModule
{
    private static readonly Dictionary<int, int> registeredCmds = [];
    private const string CommandID = "RainedCommandID";
    
    private static readonly List<LuaCallback> updateCallbacks = [];
    private static readonly List<LuaCallback> preRenderCallbacks = [];
    private static readonly List<LuaCallback> postRenderCallbacks = [];
    private static readonly List<LuaCallback> renderFailCallbacks = [];

    private static readonly List<LuaCallback> docChangedCallbacks = [];
    private static readonly List<LuaCallback> docOpenedCallbacks = [];
    private static readonly List<LuaCallback> docClosingCallbacks = [];
    private static readonly List<LuaCallback> docSavingCallbacks = [];
    private static readonly List<LuaCallback> docSavedCallbacks = [];

    public static void Init(Lua lua, NLua.Lua nLua)
    {
        // init script parameters
        lua.NewTable();
        foreach (var (k, v) in Boot.Options.ScriptParameters)
        {
            lua.PushString(v);
            lua.SetField(-2, k);
        }
        lua.SetField(-2, "scriptParams");

        // function rained.getVersion
        lua.ModuleFunction("getVersion", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushString(RainEd.Version);
            return 1;
        });

        // function rained.getApiVersion
        lua.ModuleFunction("getApiVersion", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushInteger(LuaInterface.VersionMajor);
            lua.PushInteger(LuaInterface.VersionMinor);
            lua.PushInteger(LuaInterface.VersionRevision);
            return 3;
        });

        lua.ModuleFunction("isBatchMode", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushBoolean(!LuaInterface.Host.IsGui);
            return 1;
        });
        
        lua.ModuleFunction("getAssetDirectory", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushString(Path.Combine(Boot.AppDataPath, "assets"));
            return 1;
        });

        lua.ModuleFunction("getConfigDirectory", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushString(Boot.ConfigPath);
            return 1;
        });

        lua.ModuleFunction("getDataDirectory", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushString(Rained.Assets.AssetDataPath.GetPath());
            return 1;
        });

        lua.ModuleFunction("alert", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);

            lua.PushCopy(1);
            LuaInterface.Host.Alert(lua.ToString(-1));
            lua.Pop(1);
            return 0;
        });

        lua.ModuleFunction("getLevelWidth", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushInteger(LuaInterface.Host.LevelCheck(lua).Width);
            return 1;
        });

        lua.ModuleFunction("getLevelHeight", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushInteger(LuaInterface.Host.LevelCheck(lua).Height);
            return 1;
        });

        lua.ModuleFunction("isInBounds", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var x = (int)lua.CheckNumber(1);
            var y = (int)lua.CheckNumber(2);
            lua.PushBoolean(LuaInterface.Host.LevelCheck(lua).IsInBounds(x, y));
            return 1;
        });

        lua.ModuleFunction("getDocumentCount", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushInteger(LuaInterface.Host.DocumentCount);
            return 1;
        });

        lua.ModuleFunction("getDocumentName", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var idx = (int)lua.CheckInteger(1) - 1;
            if (idx < 0 || idx > +LuaInterface.Host.DocumentCount)
            {
                lua.PushNil(); return 1;
            }

            lua.PushString(LuaInterface.Host.GetDocumentName(idx));
            return 1;
        });

        lua.ModuleFunction("getDocumentInfo", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var idx = (int)lua.CheckInteger(1) - 1;
            if (idx < 0 || idx > +LuaInterface.Host.DocumentCount)
            {
                lua.PushNil(); return 1;
            }

            var name = LuaInterface.Host.GetDocumentName(idx);
            var filePath = LuaInterface.Host.GetDocumentFilePath(idx);

            lua.NewTable();
            lua.PushString(name);
            lua.SetField(-2, "name");
            lua.PushString(filePath);
            lua.SetField(-2, "filePath");

            return 1;
        });

        lua.ModuleFunction("getActiveDocument", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            if (LuaInterface.Host.ActiveDocument != -1)
            {
                lua.PushInteger(LuaInterface.Host.ActiveDocument + 1);
            }
            else
            {
                lua.PushNil();
            }

            return 1;
        });

        lua.ModuleFunction("setActiveDocument", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);

            var idx = (int)lua.CheckInteger(1) - 1;
            if (idx < 0 || idx > +LuaInterface.Host.DocumentCount)
            {
                lua.PushBoolean(false);
                return 1;
            }

            LuaInterface.Host.ActiveDocument = idx;
            lua.PushBoolean(true);
            return 1;
        });

        lua.ModuleFunction("isDocumentOpen", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushBoolean(LuaInterface.Host.ActiveDocument != -1);
            return 1;
        });

        lua.ModuleFunction("closeDocument", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var idx = (int)lua.CheckInteger(1) - 1;

            if (idx >= 0 && idx < +LuaInterface.Host.DocumentCount)
                LuaInterface.Host.CloseDocument(idx);

            return 0;
        });

        lua.NewMetaTable(CommandID);
        LuaHelpers.PushLuaFunction(lua, static (Lua lua) =>
        {
            lua.PushString("The metatable is locked!");
            return 1;
        });
        lua.SetField(-2, "__metatable");
        lua.Pop(1);

        lua.ModuleFunction("registerCommand", static (KeraLua.Lua lua) =>
        {
            int argc = lua.GetTop();
            int cmdId;

            // (info)
            if (argc == 1)
            {
                lua.CheckType(1, LuaType.Table);

                lua.GetField(1, "name");
                lua.ArgumentCheck(lua.Type(-1) == LuaType.String, 1, "invalid command creation parameters");

                var name = lua.ToString(-1);
                lua.Pop(1);

                lua.GetField(1, "callback");
                lua.ArgumentCheck(lua.Type(-1) == LuaType.Function, 1, "invalid command creation parameters");

                int funcRef = lua.Ref(LuaRegistry.Index);

                var cmdInit = new RainEd.CommandCreationParameters(name, (id) => RunCommand(lua, id));

                // check optional fields
                lua.GetField(1, "autoHistory");
                if (!lua.IsNoneOrNil(-1))
                {
                    cmdInit.AutoHistory = lua.ToBoolean(-1);
                }
                lua.Pop(1);

                lua.GetField(1, "requiresLevel");
                if (!lua.IsNoneOrNil(-1))
                {
                    cmdInit.RequiresLevel = lua.ToBoolean(-1);
                }
                lua.Pop(1);

                cmdId = LuaInterface.Host.RegisterCommand(cmdInit);
                registeredCmds[cmdId] = funcRef;
            }

            // depcrecated (name, callback)
            else if (argc == 2)
            {
                string name = lua.CheckString(1);
                lua.CheckType(2, KeraLua.LuaType.Function);

                lua.PushCopy(2);
                int funcRef = lua.Ref(KeraLua.LuaRegistry.Index);

                var cmdInit = new RainEd.CommandCreationParameters(name, (id) => RunCommand(lua, id))
                {
                    AutoHistory = true,
                    RequiresLevel = true
                };

                cmdId = LuaInterface.Host.RegisterCommand(cmdInit);
                registeredCmds[cmdId] = funcRef;
            }
            else
            {
                return lua.ErrorWhere("invalid call to rained.registerCommand");
            }

            unsafe
            {
                var ud = (int*)lua.NewUserData(sizeof(int));
                lua.SetMetaTable(CommandID);
                *ud = cmdId;
            }

            return 1;
        });

        lua.ModuleFunction("openLevel", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var filePath = lua.CheckString(1);
            LevelLoadResult res;
            try
            {
                res = LuaInterface.Host.OpenLevel(filePath);
            }
            catch
            {
                lua.ErrorWhere("could not load level " + filePath);
                return 0;
            }
            
            if (res.HadUnrecognizedAssets)
            {
                lua.NewTable();
                lua.PushBoolean(true);
                lua.SetField(-2, "hadUnrecognizedAssets");

                // materials list
                lua.NewTable();
                for (int i = 0; i < res.UnrecognizedMaterials.Length; i++)
                {
                    lua.PushString(res.UnrecognizedMaterials[i]);
                    lua.RawSetInteger(-2, i+1);
                }
                lua.SetField(-2, "unrecognizedMaterials");

                // tiles list
                lua.NewTable();
                for (int i = 0; i < res.UnrecognizedTiles.Length; i++)
                {
                    lua.PushString(res.UnrecognizedTiles[i]);
                    lua.RawSetInteger(-2, i+1);
                }
                lua.SetField(-2, "unrecognizedTiles");

                // effects list
                lua.NewTable();
                for (int i = 0; i < res.UnrecognizedEffects.Length; i++)
                {
                    lua.PushString(res.UnrecognizedEffects[i]);
                    lua.RawSetInteger(-2, i+1);
                }
                lua.SetField(-2, "unrecognizedEffects");

                // props list
                lua.NewTable();
                for (int i = 0; i < res.UnrecognizedProps.Length; i++)
                {
                    lua.PushString(res.UnrecognizedProps[i]);
                    lua.RawSetInteger(-2, i+1);
                }
                lua.SetField(-2, "unrecognizedProps");

                return 1;
            }

            return 0;
        });

        lua.ModuleFunction("newLevel", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var w = (int)lua.CheckInteger(1);
            var h = (int)lua.CheckInteger(2);
            string? filePath;
            if (lua.IsNoneOrNil(3))
                filePath = null;
            else
                filePath = lua.CheckString(3);

            LuaInterface.Host.NewLevel(w, h, filePath);
            return 0;
        });

        lua.ModuleFunction("onUpdate", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.CheckType(1, LuaType.Function);

            lua.PushCopy(1);
            var cb = new LuaCallback(lua)
            {
                OnDisconnect = static (Lua lua, LuaCallback cb) =>
                {
                    updateCallbacks.Remove(cb);
                }
            };
            updateCallbacks.Add(cb);
            
            return 1;
        });

        lua.ModuleFunction("onPreRender", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.CheckType(1, LuaType.Function);
            lua.PushCopy(1);
            var cb = new LuaCallback(lua)
            {
                OnDisconnect = static (Lua lua, LuaCallback cb) => preRenderCallbacks.Remove(cb)
            };
            preRenderCallbacks.Add(cb);
            
            return 1;
        });

        lua.ModuleFunction("onPostRender", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.CheckType(1, LuaType.Function);
            lua.PushCopy(1);
            var cb = new LuaCallback(lua)
            {
                OnDisconnect = static (Lua lua, LuaCallback cb) => postRenderCallbacks.Remove(cb)
            };
            postRenderCallbacks.Add(cb);
            
            return 1;
        });

        lua.ModuleFunction("onRenderFailure", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.CheckType(1, LuaType.Function);
            lua.PushCopy(1);
            var cb = new LuaCallback(lua)
            {
                OnDisconnect = static (Lua lua, LuaCallback cb) => renderFailCallbacks.Remove(cb)
            };
            renderFailCallbacks.Add(cb);
            
            return 1;
        });

        lua.ModuleFunction("onDocumentChanged", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.CheckType(1, LuaType.Function);
            lua.PushCopy(1);
            var cb = new LuaCallback(lua)
            {
                OnDisconnect = static (Lua lua, LuaCallback cb) => docChangedCallbacks.Remove(cb)
            };
            docChangedCallbacks.Add(cb);
            return 1;
        });

        lua.ModuleFunction("onDocumentOpened", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.CheckType(1, LuaType.Function);
            lua.PushCopy(1);
            var cb = new LuaCallback(lua)
            {
                OnDisconnect = static (Lua lua, LuaCallback cb) => docOpenedCallbacks.Remove(cb)
            };
            docOpenedCallbacks.Add(cb);
            return 1;
        });

        lua.ModuleFunction("onDocumentClosing", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.CheckType(1, LuaType.Function);
            lua.PushCopy(1);
            var cb = new LuaCallback(lua)
            {
                OnDisconnect = static (Lua lua, LuaCallback cb) => docClosingCallbacks.Remove(cb)
            };
            docClosingCallbacks.Add(cb);
            return 1;
        });

        lua.ModuleFunction("onDocumentSaving", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.CheckType(1, LuaType.Function);
            lua.PushCopy(1);
            var cb = new LuaCallback(lua)
            {
                OnDisconnect = static (Lua lua, LuaCallback cb) => docSavingCallbacks.Remove(cb)
            };
            docSavingCallbacks.Add(cb);
            return 1;
        });

        lua.ModuleFunction("onDocumentSaved", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.CheckType(1, LuaType.Function);
            lua.PushCopy(1);
            var cb = new LuaCallback(lua)
            {
                OnDisconnect = static (Lua lua, LuaCallback cb) => docSavedCallbacks.Remove(cb)
            };
            docSavedCallbacks.Add(cb);
            return 1;
        });
    }

    public static void UpdateCallback(float dt)
    {
        if (updateCallbacks.Count > 0)
        {
            RainEd.Instance.NeedScreenRefresh();
        }
        
        foreach (var cb in updateCallbacks)
        {
            cb.LuaState.PushNumber(dt);
            cb.Invoke(1);
        }
    }

    public static void PreRenderCallback(string sourceTxt)
    {
        foreach (var cb in preRenderCallbacks)
        {
            cb.LuaState.PushString(sourceTxt);
            cb.Invoke(1);
        }
    }

    public static void PostRenderCallback(string sourceTxt, string dstTxt, params string[] dstPngs)
    {
        foreach (var cb in postRenderCallbacks)
        {
            var lua = cb.LuaState;
            lua.PushString(sourceTxt);
            lua.PushString(dstTxt);

            lua.NewTable();
            for (int i = 0; i < dstPngs.Length; i++)
            {
                lua.PushString(dstPngs[i]);
                lua.RawSetInteger(-2, i+1);
            }

            cb.Invoke(3);
        }
    }

    public static void RenderFailureCallback(string sourceTxt, string? errorReason)
    {
        foreach (var cb in renderFailCallbacks)
        {
            cb.LuaState.PushString(sourceTxt);

            if (errorReason is not null)
                cb.LuaState.PushString(errorReason);
            else
                cb.LuaState.PushNil();
            
            cb.Invoke(2);
        }
    }

    public static void DocumentChangedCallback(int index)
    {
        foreach (var cb in docChangedCallbacks)
        {
            cb.LuaState.PushInteger(index+1);
            cb.Invoke(1);
        }
    }

    public static void DocumentOpenedCallback(int index)
    {
        foreach (var cb in docOpenedCallbacks)
        {
            cb.LuaState.PushInteger(index+1);
            cb.Invoke(1);
        }
    }

    public static void DocumentClosingCallback(int index)
    {
        foreach (var cb in docClosingCallbacks)
        {
            cb.LuaState.PushInteger(index+1);
            cb.Invoke(1);
        }
    }

    public static void DocumentSavingCallback(int index)
    {
        foreach (var cb in docSavingCallbacks)
        {
            cb.LuaState.PushInteger(index+1);
            cb.Invoke(1);
        }
    }

    public static void DocumentSavedCallback(int index)
    {
        foreach (var cb in docSavedCallbacks)
        {
            cb.LuaState.PushInteger(index+1);
            cb.Invoke(1);
        }
    }

    private static void RunCommand(Lua lua, int id)
    {
        lua.RawGetInteger(LuaRegistry.Index, registeredCmds[id]);
        LuaHelpers.Call(lua, 0, 0);
        
        //lua.PushCFunction(_errHandler);
        //lua.PCall(0, 0, -2);
    }

    public static void UIUpdate()
    {
        // FileBrowser.Render(ref fileBrowser);
    }

    public static void RemoveAllCommands(Lua lua)
    {
        foreach (var (cmdId, funcRef) in registeredCmds)
        {
            LuaInterface.Host.UnregisterCommand(cmdId);
            lua.Unref(LuaRegistry.Index, funcRef);
        }

        registeredCmds.Clear();
    }
}