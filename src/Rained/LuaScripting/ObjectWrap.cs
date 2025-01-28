namespace Rained.LuaScripting;

using System.Diagnostics;
using KeraLua;
using Rained;

class ObjectWrap<T> where T : notnull
{
    private uint _nextId = 1;
    private readonly string TypeMt;
    private readonly string RegistryIndex;

    private static readonly Dictionary<uint, T> _refs = [];
    private static readonly Dictionary<T, uint> _objIds = [];

    public ObjectWrap(string typeName, string registryIndex)
    {
        TypeMt = typeName;
        RegistryIndex = registryIndex;
    }

    public unsafe uint GetID(Lua lua, int i)
    {
        var ud = (uint*) lua.CheckUserData(1, TypeMt);
        lua.ArgumentCheck(ud != null, 1, $"{TypeMt} expected");
        return *ud;
    }

    public unsafe T GetRef(Lua lua, int i)
    {
        var ud = (uint*) lua.CheckUserData(1, TypeMt);
        lua.ArgumentCheck(ud != null, 1, $"{TypeMt} expected");
        return _refs[*ud];
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

        lua.ModuleFunction("__gc", (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var id = GetID(lua, 1);

            Log.Debug("{Type} id {Id} gc", TypeMt, id);

            _objIds.Remove(_refs[id]);
            _refs.Remove(id);
            return 0;
        });
    }

    public unsafe void PushWrapper(Lua lua, T obj)
    {
        if (lua.GetField((int)LuaRegistry.Index, RegistryIndex) != LuaType.Table)
        {
            throw new Exception("Problem obtaining camera from registry.");
        }
        var camTable = lua.GetTop();

        uint id;
        if (_objIds.TryGetValue(obj, out id))
        {
            var type = lua.RawGetInteger(camTable, id);
            Debug.Assert(type == LuaType.UserData);
            lua.Remove(camTable); // remove table
        }
        else
        {
            id = _nextId++;

            var ud = (uint*) lua.NewIndexedUserData(sizeof(uint), 0);
            *ud = id;
            lua.GetMetaTable(TypeMt);
            lua.SetMetaTable(-2);
            lua.PushCopy(-1);
            lua.RawSetInteger(camTable, id);
            lua.Remove(camTable);

            _objIds.Add(obj, id);
            _refs.Add(id, obj);
        }
    }
}