using DrizzleExport;

class DrizzleExportConsole
{
    public static void Main(string[] args)
    {
        var dataPath = args[0];

        if (args[1] == "effects")
        {
            DrizzleEffectExport.Export(dataPath, "assets/drizzle-cast", args[2]);
        }
        else
        {
            throw new Exception("unknown export type " + args[2]);
        }
    }
}