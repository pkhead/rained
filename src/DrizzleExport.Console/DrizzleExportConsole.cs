using DrizzleExport;

class DrizzleExportConsole
{
    public static void Main(string[] args)
    {
        if (args[0] == "effects")
        {
            // create dummy data directory
            var tempDir = Directory.CreateTempSubdirectory().FullName;
            Directory.CreateDirectory(tempDir);
            try
            {
                Directory.CreateDirectory(Path.Combine(tempDir, "Graphics"));
                Directory.CreateDirectory(Path.Combine(tempDir, "Props"));
                Directory.CreateDirectory(Path.Combine(tempDir, "Levels"));

                File.WriteAllText(Path.Combine(tempDir, "Graphics", "Init.txt"), "");
                File.WriteAllText(Path.Combine(tempDir, "Props", "Init.txt"), "");
                File.WriteAllText(Path.Combine(tempDir, "Props", "propColors.txt"), "[\"Rainbow\", color(254, 1, 254)]\n");

                File.WriteAllText(Path.Combine(tempDir, "effectsInit.txt"), File.ReadAllText("assets/drizzle-cast/Drought_393537_baseEffectsInit.txt"));
                File.WriteAllText(Path.Combine(tempDir, "editorConfig.txt"), File.ReadAllText("assets/drizzle-cast/Drought_393536_baseConfig.txt"));
                File.WriteAllText(Path.Combine(tempDir, "largeTrashLog.txt"), "");

                DrizzleEffectExport.Export(tempDir, "assets/drizzle-cast", args[1]);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }
        else
        {
            throw new Exception("unknown export type " + args[1]);
        }
    }
}