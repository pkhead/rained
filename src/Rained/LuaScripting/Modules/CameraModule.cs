namespace Rained.LuaScripting.Modules;

using System.Numerics;
using KeraLua;
using LevelData;

static class CameraModule
{
    private static readonly ObjectWrap<Camera> wrap = new("Camera", "CAMERA_REGISTRY");

    public static void Init(Lua lua, NLua.Lua nlua)
    {
        DefineCamera(lua);

        lua.NewTable();

        lua.ModuleFunction("getFullSize", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushNumber(Camera.Size.X);
            lua.PushNumber(Camera.Size.Y);
            return 2;
        });

        lua.ModuleFunction("getWidescreenSize", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushNumber(Camera.WidescreenSize.X);
            lua.PushNumber(Camera.WidescreenSize.Y);
            return 2;
        });

        lua.ModuleFunction("getFullscreenSize", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushNumber(Camera.StandardSize.X);
            lua.PushNumber(Camera.StandardSize.Y);
            return 2;
        });

        lua.ModuleFunction("getCount", static (nint luaPtr) => {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushInteger(RainEd.Instance.Level.Cameras.Count);
            return 1;
        });

        lua.ModuleFunction("getCamera", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var idx = (int) lua.CheckNumber(1) - 1;
            if (idx < 0 || idx >= RainEd.Instance.Level.Cameras.Count)
                lua.PushNil();
            else
            {
                var cam = RainEd.Instance.Level.Cameras[idx];
                wrap.PushWrapper(lua, cam);
            }

            return 1;
        });

        lua.ModuleFunction("addCamera", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var cam = wrap.GetRef(lua, 1);
            var level = RainEd.Instance.Level;
            
            if (lua.IsNoneOrNil(2))
            {
                level.Cameras.Remove(cam);
                level.Cameras.Add(cam);
            }
            else
            {
                var idx = (int)lua.ToInteger(2) - 1;
                if (idx <= 0 || idx > level.Cameras.Count)
                    lua.ArgumentError(2, "out of range");
                
                level.Cameras.Insert(idx, cam);
            }

            return 0;
        });

        lua.ModuleFunction("removeCamera", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var cam = wrap.GetRef(lua, 1);
            var level = RainEd.Instance.Level;

            lua.PushBoolean( level.Cameras.Remove(cam) );
            return 1;
        });

        lua.ModuleFunction("newCamera", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var initX = (float) lua.OptNumber(1, 1.0);
            var initY = (float) lua.OptNumber(2, 1.0);

            var cam = new Camera(new Vector2(initX, initY));
            wrap.PushWrapper(lua, cam);
            return 1;
        });

        lua.ModuleFunction("getPriority", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var cam = RainEd.Instance.Level.PrioritizedCamera;

            if (cam is null)
                lua.PushNil();
            else
                wrap.PushWrapper(lua, cam);
            
            return 1;
        });

        lua.ModuleFunction("setPriority", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            if (lua.IsNoneOrNil(1))
                RainEd.Instance.Level.PrioritizedCamera = null;
            else
            {
                var cam = wrap.GetRef(lua, 1);
                RainEd.Instance.Level.PrioritizedCamera = cam;
            }

            return 0;
        });

        lua.ModuleFunction("getCameras", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var level = RainEd.Instance.Level;

            lua.NewTable();
            for (int i = 0; i < level.Cameras.Count; i++)
            {
                wrap.PushWrapper(lua, level.Cameras[i]);
                lua.RawSetInteger(-2, i + 1);
            }
            return 1;
        });

        // set rained.cells
        lua.SetField(-2, "cameras");
    }

    public static void DefineCamera(Lua lua)
    {
        wrap.InitMetatable(lua);

        lua.ModuleFunction("__index", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var cam = wrap.GetRef(lua, 1);
            var k = lua.CheckString(2);

            switch (k)
            {
                case "x":
                    lua.PushNumber(cam.Position.X);
                    break;

                case "y":
                    lua.PushNumber(cam.Position.Y);
                    break;

                case "index":
                {
                    var idx = RainEd.Instance.Level.Cameras.IndexOf(cam);
                    if (idx == -1) lua.PushNil();
                    else lua.PushInteger(idx + 1);
                    break;
                }

                case "getCornerOffset":
                    lua.PushCFunction(static (nint luaPtr) =>
                    {
                        var lua = Lua.FromIntPtr(luaPtr);
                        var cam = wrap.GetRef(lua, 1);
                        var idx = (int) lua.CheckNumber(2);
                        if (idx < 1 || idx > 4) return lua.ErrorWhere("corner index is out of bounds");

                        var offset = cam.GetCornerOffset(idx);
                        lua.PushNumber(offset.X);
                        lua.PushNumber(offset.Y);
                        return 2;
                    });
                    break;

                case "setCornerOffset":
                    lua.PushCFunction(static (nint luaPtr) =>
                    {
                        var lua = Lua.FromIntPtr(luaPtr);
                        var cam = wrap.GetRef(lua, 1);
                        var idx = (int) lua.CheckNumber(2);
                        if (idx < 1 || idx > 4) return lua.ErrorWhere("corner index is out of bounds");

                        var dx = (float) lua.CheckNumber(3);
                        var dy = (float) lua.CheckNumber(4);

                        var angle = MathF.Atan2(dx, -dy);
                        var offset = MathF.Sqrt(dx * dx + dy * dy);
                        cam.CornerAngles[idx] = angle;
                        cam.CornerOffsets[idx] = offset / 4f;
                        return 0;
                    });
                    break;

                case "getCornerAngle":
                    lua.PushCFunction(static (nint luaPtr) =>
                    {
                        var lua = Lua.FromIntPtr(luaPtr);
                        var cam = wrap.GetRef(lua, 1);
                        var idx = (int) lua.CheckNumber(2);
                        if (idx < 1 || idx > 4) return lua.ErrorWhere("corner index is out of bounds");

                        lua.PushNumber(cam.CornerAngles[idx]);
                        lua.PushNumber(cam.CornerOffsets[idx]);
                        return 2;
                    });
                    break;

                case "setCornerAngle":
                    lua.PushCFunction(static (nint luaPtr) =>
                    {
                        var lua = Lua.FromIntPtr(luaPtr);
                        var cam = wrap.GetRef(lua, 1);
                        var idx = (int) lua.CheckNumber(2);
                        if (idx < 1 || idx > 4) return lua.ErrorWhere("corner index is out of bounds");

                        var angle = (float) lua.CheckNumber(3);
                        var offset = (float) lua.CheckNumber(4);

                        cam.CornerAngles[idx] = angle;
                        cam.CornerOffsets[idx] = offset;
                        return 0;
                    });
                    break;
                
                case "clone":
                    lua.PushCFunction(static (nint luaPtr) =>
                    {
                        var lua = Lua.FromIntPtr(luaPtr);
                        var cam = wrap.GetRef(lua, 1);

                        var newCam = new Camera(cam.Position);
                        for (int i = 0; i < 4; i++)
                        {
                            newCam.CornerAngles[i] = cam.CornerAngles[i];
                            newCam.CornerOffsets[i] = cam.CornerOffsets[i];
                        }
                        
                        wrap.PushWrapper(lua, newCam);
                        return 1;
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

            switch (k)
            {
                case "x":
                    cam.Position.X = (float) lua.CheckNumber(3);
                    break;

                case "y":
                    cam.Position.Y = (float) lua.CheckNumber(3);
                    break;
                    
                default:
                    return lua.ErrorWhere($"unknown field \"{k}\"");
            }

            return 0;
        });

        lua.Pop(1);
    }
}