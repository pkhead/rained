using NLua;
namespace RainEd;

class LuaInterface
{
    private Lua luaState;
        
    public LuaInterface()
    {
        luaState = new Lua();
    
        // disable NLua import function and debug library
        luaState.DoString("import = nil debug = nil");

        // configure package.path
        var package = (LuaTable) luaState["package"];
        package["path"] = Path.Combine(Boot.AppDataPath, "scripts", "?.lua") + ";" + Path.Combine(Boot.AppDataPath, "scripts", "?", "init.lua");

        luaState.DoString("Rained = {}");
        var luaRained = (LuaTable) luaState["Rained"];
        luaRained["testFunction"] = new Action(TestFunction);
    }

    public void Initialize()
    {
        luaState.DoFile(Path.Combine(Boot.AppDataPath, "scripts", "init.lua"));
    }
    
    public static void TestFunction()
    {
        Console.WriteLine("Hello!!");
    }
}