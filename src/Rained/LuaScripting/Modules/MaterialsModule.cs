using KeraLua;
using Rained.ChangeHistory;
using Rained.EditorGui.Editors;

namespace Rained.LuaScripting.Modules;

static class MaterialsModule
{
    public static void Init(Lua lua, NLua.Lua nlua)
    {
        lua.NewTable();
        
        lua.ModuleFunction("getDefaultMaterial", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var matId = LuaInterface.Host.LevelCheck(lua).DefaultMaterial;
            var matName = LuaInterface.Host.MaterialDatabase.GetMaterial(matId).Name;
            lua.PushString(matName);
            return 1;
        });

        lua.ModuleFunction("getDefaultMaterialId", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var matId = LuaInterface.Host.LevelCheck(lua).DefaultMaterial;
            lua.PushInteger(matId);
            return 1;
        });

        lua.ModuleFunction("setDefaultMaterial", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var matDb = LuaInterface.Host.MaterialDatabase;
            
            if (lua.IsInteger(1))
            {
                var matId = (int) lua.CheckInteger(1);
                if (matId >= matDb.Materials.Length) return lua.ErrorWhere("unknown material " + matId);

                LuaInterface.Host.LevelCheck(lua).DefaultMaterial = matId;
            }
            else
            {
                var matName = lua.CheckString(1);
                var matInfo = matDb.GetMaterial(matName);
                if (matInfo is null) return lua.ErrorWhere($"unknown material '{matName}'");

                LuaInterface.Host.LevelCheck(lua).DefaultMaterial = matInfo.ID;
            }

            return 0;
        });

        lua.ModuleFunction("isInstalled", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var matDb = LuaInterface.Host.MaterialDatabase;
            var matName = lua.CheckString(1);

            var matInfo = matDb.GetMaterial(matName);
            lua.PushBoolean(matInfo is not null);
            return 1;
        });

        lua.ModuleFunction("getMaterialCatalog", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var matDb = LuaInterface.Host.MaterialDatabase;

            lua.NewTable();
            int i = 1;
            foreach (var mat in matDb.Materials)
            {
                lua.PushString(mat.Name);
                lua.RawSetInteger(-2, i++);
            }

            return 1;
        });

        lua.ModuleFunction("getMaterialCategories", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var matDb = LuaInterface.Host.MaterialDatabase;

            lua.NewTable();
            int i = 1;
            foreach (var cat in matDb.Categories)
            {
                lua.PushString(cat.Name);
                lua.RawSetInteger(-2, i++);
            }

            return 1;
        });

        lua.ModuleFunction("getMaterialsInCategory", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var matDb = LuaInterface.Host.MaterialDatabase;
            var catName = lua.CheckString(1);

            foreach (var cat in matDb.Categories)
            {
                if (cat.Name == catName)
                {
                    lua.NewTable();
                    int i = 1;
                    foreach (var mat in cat.Materials)
                    {
                        lua.PushString(mat.Name);
                        lua.RawSetInteger(-2, i++);
                    }

                    return 1;
                }
            }

            lua.PushNil();
            return 1;
        });

        lua.ModuleFunction("getMaterialId", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var matDb = LuaInterface.Host.MaterialDatabase;
            var matName = lua.CheckString(1);

            var matInfo = matDb.GetMaterial(matName);
            if (matInfo is null) lua.PushNil();
            else lua.PushInteger(matInfo.ID);

            return 1;
        });

        lua.ModuleFunction("getMaterialName", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var matDb = LuaInterface.Host.MaterialDatabase;
            var matId = (int) lua.CheckInteger(1);

            var matInfo = matDb.GetMaterial(matId);
            if (matInfo is null) lua.PushNil();
            else lua.PushString(matInfo.Name);

            return 1;
        });

        // set rained.materials
        lua.SetField(-2, "materials");
    }
}