using System.Reflection;
using KeraLua;

namespace RainEd;

static class LuaHelpers
{
    private const string MetatableName = "luahelpers_delegate";

    private static int nextID = 1;
    private static Dictionary<int, Delegate> allocatedObjects = new();
    
    private static LuaFunction gcDelegate = new LuaFunction(GCDelegate);
    private static LuaFunction callDelegate = new LuaFunction(CallDelegate);
    private static LuaFunction mtDelegate = new LuaFunction(MetatableDelegate);

    public static void Init(Lua lua)
    {
        lua.NewMetaTable(MetatableName);
        lua.PushCFunction(gcDelegate);
        lua.SetField(-2, "__gc");

        lua.PushCFunction(MetatableDelegate);
        lua.SetField(-2, "__metatable");

        lua.Pop(1);
    }

    private static int MetatableDelegate(nint luaPtr)
    {
        Lua lua = Lua.FromIntPtr(luaPtr)!;
        lua.PushString("the metatable is locked");
        return 1;
    }

    private static unsafe int GCDelegate(nint luaPtr)
    {
        Lua lua = Lua.FromIntPtr(luaPtr)!;
        int id = *((int*)lua.CheckUserData(1, MetatableName));

        allocatedObjects.Remove(id);
        return 0;
    }

    private static unsafe int CallDelegate(nint luaPtr)
    {
        Lua lua = Lua.FromIntPtr(luaPtr)!;
        var luaNumArgs = lua.GetTop();
        
        int* userData = (int*) lua.CheckUserData(Lua.UpValueIndex(1), MetatableName);
        int id = *userData;
        var func = allocatedObjects[id];

        var paramInfo = func.Method.GetParameters();
        var parameters = new object?[paramInfo.Length];
        int luaParamIndex = 1;
        for (int i = 0; i < paramInfo.Length; i++)
        {
            var param = paramInfo[i];
            if (param.IsOut) continue;

            var type = param.ParameterType;
            bool isNullable = Nullable.GetUnderlyingType(type) != null;

            if ((!type.IsValueType || isNullable) && (luaParamIndex > luaNumArgs || lua.IsNoneOrNil(luaParamIndex)))
            {
                parameters[i] = null;
            }
            else
            {
                if (type == typeof(int))
                {
                    parameters[i] = (int)lua.CheckInteger(luaParamIndex);
                }
                else if (type == typeof(long))
                {
                    parameters[i] = lua.CheckInteger(luaParamIndex);
                }
                else if (type == typeof(float))
                {
                    parameters[i] = (float) lua.CheckNumber(luaParamIndex);
                }
                else if (type == typeof(double))
                {
                    parameters[i] = lua.CheckNumber(luaParamIndex);
                }
                else if (type == typeof(string))
                {
                    parameters[i] = lua.CheckString(luaParamIndex);
                }
                else if (type == typeof(bool))
                {
                    parameters[i] = lua.ToBoolean(luaParamIndex);
                }
                else
                {
                    throw new Exception("Unsupported parameter type");
                }
            }

            luaParamIndex++;
        }

        int returnCount = 0;
        object? ret;
        try
        {
            ret = func.DynamicInvoke(parameters);
        }
        catch (TargetInvocationException e)
        {
            if (e.InnerException is not null)
                return lua.Error(e.InnerException.Message);
            else
                throw;
        }

        if (ret is not null)
        {
            PushValue(lua, ret);
            returnCount++;
        }

        // process out arguments
        for (int i = 0; i < paramInfo.Length; i++)
        {
            var param = paramInfo[i];
            if (param.IsOut)
            {
                PushValue(lua, parameters[i]);
                returnCount++;
            }
        }

        return returnCount;
    }

    private static void PushValue(Lua lua, object? v)
    {
        if (v is null)
        {
            lua.PushNil();
        }
        else if (v is int v1)
        {
            lua.PushInteger(v1);
        }
        else if (v is long v2)
        {
            lua.PushInteger(v2);
        }
        else if (v is float v3)
        {
            lua.PushNumber(v3);
        }
        else if (v is double v4)
        {
            lua.PushNumber(v4);
        }
        else if (v is string v5)
        {
            lua.PushString(v5);
        }
        else if (v is bool v6)
        {
            lua.PushBoolean(v6);
        }
        else
        {
            throw new Exception("Unsupported value type");
        }
    }

    public static unsafe void PushDelegate(Lua lua, Delegate func)
    {
        int* userData = (int*) lua.NewUserData(sizeof(int));
        *userData = nextID;
        
        lua.GetMetaTable(MetatableName);
        lua.SetMetaTable(-2);
        allocatedObjects[nextID++] = func;

        lua.PushCClosure(callDelegate, 1);
    }
}