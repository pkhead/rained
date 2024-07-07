using System.Runtime.InteropServices;
namespace RainEd;

partial class BootOptions
{
    [LibraryImport("kernel32.dll")]
    private static partial int AttachConsole(int dwProcessId);

    private static void AttachConsole()
    {
        if (!OperatingSystem.IsWindows()) return;

        if (AttachConsole(-1) != 0)
        {
            var cin = new StreamReader(Console.OpenStandardInput());
            var cerr = new StreamWriter(Console.OpenStandardError())
            {
                AutoFlush = true
            };
            var cout = new StreamWriter(Console.OpenStandardOutput())
            {
                AutoFlush = true
            };

            Console.SetOut(cout);
            Console.SetIn(cin);
            Console.SetError(cerr);
        }
    }

    public readonly bool ContinueBoot = true;
    public readonly bool ConsoleAttached = false;
    public readonly string AppDataPath = Boot.AppDataPath;
    public readonly string? DrizzleDataPath = null;
    public readonly string LevelToLoad = "";

    public readonly bool NoSplashScreen = false;
    public readonly bool ShowOgscule = false;
    public readonly bool LogToStdout = false;
    public readonly bool Render = false;

#if DEBUG
    public readonly bool GlDebug = true;
#else
    public readonly bool GlDebug = false;
#endif

    private static void PrintHelpMessage()
    {
        Console.WriteLine(
        $"""
        Usage:
            Rained [-v | --version]
            Rained [-h | --help]
            Rained [options...] [level path]
        
        --help                  Show this help screen
        --version -v            Print out version
        --render -r             Render the given level and exit
        --log-to-stdout         Print logs to the standard output stream instead of to a file
        --no-splash-screen      Do not show the splash screen when starting
        --app-data <path>       Run with app data directory at <path>
        --gl-debug              Enable OpenGL debugging
        --data <path>           Run with the Drizzle data directory at <path>
        --ogscule               the intrusive thoughts defeated me
        """
        );
    }

    public BootOptions(string[] args)
    {
        // first, scan for the --console argument
        int argCount = args.Length;

        if (OperatingSystem.IsWindows())
        {
            foreach (var arg in args)
            {
                if (arg == "--console")
                {
                    argCount--;
                    ConsoleAttached = true;
                    AttachConsole();
                    break;
                }
            }
        }


        // show help or version number
        if (argCount == 1)
        {
            var arg = args[0] == "--console" ? args[1] : args[0];
            
            if (arg == "--help" || arg == "-h")
            {
                Console.WriteLine($"Rained {RainEd.Version}");
                Console.WriteLine();

                PrintHelpMessage();
                ContinueBoot = false;
                return;
            }
            else if (arg == "--version" || arg == "-v")
            {
                Console.WriteLine($"Rained {RainEd.Version}");
                
                ContinueBoot = false;
                return;
            }
        }

        // normal command-line processing
        for (int i = 0; i < args.Length; i++)
        {
            var str = args[i];

            if (str == "--console") continue;

            // this is here because it appears SFML uses some
            // OpenGL extensions that RenderDoc doesn't support
            if (str == "--no-splash-screen")
            {
                NoSplashScreen = true;
                continue;
            }

            // runtime-configurable app data path because why not
            if (str == "--app-data")
            {
                i++;
                AppDataPath = args[i];
                continue;
            }

            if (str == "--data")
            {
                i++;
                DrizzleDataPath = args[i];
                continue;
            }

            if (str == "--log-to-stdout")
            {
                LogToStdout = true;
                continue;
            }

            if (str == "--render" || str == "-r")
            {
                Render = true;
                continue;
            }

            if (str == "--gl-debug")
            {
                GlDebug = true;
                continue;
            }

            // the intrusive thoughts defeated me
            if (str == "--ogscule")
            {
                Console.WriteLine("ogscule");
                ShowOgscule = true;
                continue;
            }

            if (string.IsNullOrEmpty(LevelToLoad) && str[0] != '-')
                LevelToLoad = str;

            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("error: ");
                Console.ResetColor();
                Console.WriteLine($"unknown option: {str}");
                Console.WriteLine();
                PrintHelpMessage();

                Environment.ExitCode = 2;
                ContinueBoot = false;
            }
        }
    }
}