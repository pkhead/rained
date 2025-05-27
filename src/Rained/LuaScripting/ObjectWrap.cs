namespace Rained.LuaScripting;

using System.Diagnostics;
using KeraLua;
using Rained;

static class ObjectWrapDatabase
{
    private static uint _nextId = 1;
    private static readonly Dictionary<uint, object> refs = [];
    private static readonly Dictionary<object, uint> objIds = [];

    public static uint NextID()
    {
        return _nextId++;
    }

    public static void Associate(uint id, object obj)
    {
        refs.Add(id, obj);
        objIds.Add(obj, id);
    }

    public static void Remove(uint id)
    {
        objIds.Remove(refs[id]);
        refs.Remove(id);
    }

    public static bool TryGetID(object obj, out uint id)
    {
        return objIds.TryGetValue(obj, out id);
    }

    public static object GetObject(uint id)
    {
        return refs[id];
    }
}

class ObjectWrap<T>(string typeName, string registryIndex) where T : notnull
{
    private readonly string TypeMt = typeName;
    private readonly string RegistryIndex = registryIndex;
    private static LuaFunction? gcDelegate = null;

    public unsafe uint GetID(Lua lua, int i)
    {
        var ud = (uint*) lua.CheckUserData(i, TypeMt);
        lua.ArgumentCheck(ud != null, i, $"{TypeMt} expected");
        return *ud;
    }

    public unsafe T GetRef(Lua lua, int i)
    {
        var ud = (uint*) lua.CheckUserData(i, TypeMt);
        lua.ArgumentCheck(ud != null, i, $"{TypeMt} expected");
        if (*ud == 0) lua.ArgumentError(i, $"{TypeMt} instance was disposed");
        return (T) ObjectWrapDatabase.GetObject(*ud);
    }

    /// <summary>
    /// Initialize the registry and metatable for the type and push
    /// the metatable on the stack.
    /// </summary>
    /// <param name="lua"></param>
    public void InitMetatable(Lua lua)
    {
        lua.NewTable();
        lua.NewTable();
        lua.PushString("v");
        lua.SetField(-2, "__mode");
        lua.SetMetaTable(-2);
        lua.SetField((int)LuaRegistry.Index, RegistryIndex);

        if (!lua.NewMetaTable(TypeMt)) return;

        lua.ModuleFunction("__metatable", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushString("The metatable is locked.");
            return 1;
        });

        gcDelegate ??= (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var id = GetID(lua, 1);

            Log.Debug("{Type} id {Id} gc", TypeMt, id);

            ObjectWrapDatabase.Remove(id);
            return 0;
        };

        lua.ModuleFunction("__gc", gcDelegate);
    }

    public unsafe void PushWrapper(Lua lua, T obj)
    {
        if (lua.GetField((int)LuaRegistry.Index, RegistryIndex) != LuaType.Table)
        {
            throw new Exception($"Problem obtaining {TypeMt} from registry.");
        }
        var camTable = lua.GetTop();

        uint id;
        if (ObjectWrapDatabase.TryGetID(obj, out id))
        {
            var type = lua.RawGetInteger(camTable, id);
            Debug.Assert(type == LuaType.UserData);
            lua.Remove(camTable); // remove table
        }
        else
        {
            id = ObjectWrapDatabase.NextID();

            var ud = (uint*) lua.NewIndexedUserData(sizeof(uint), 0);
            *ud = id;
            lua.GetMetaTable(TypeMt);
            lua.SetMetaTable(-2);
            lua.PushCopy(-1);
            lua.RawSetInteger(camTable, id);
            lua.Remove(camTable);

            ObjectWrapDatabase.Associate(id, obj);
        }
    }
}