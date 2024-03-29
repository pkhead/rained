using System.Diagnostics;

class Program
{
    public static string AppDataPath = AppContext.BaseDirectory;

    private static void Main(string[] args)
    {
        var procStartInfo = new ProcessStartInfo()
        {
            FileName = Path.Combine(AppDataPath, "Rained.exe"),
            Arguments = string.Join(' ', args),
        };

        var proc = Process.Start(procStartInfo);
        proc!.WaitForExit();
    }
}