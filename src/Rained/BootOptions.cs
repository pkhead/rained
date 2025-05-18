using System.Runtime.InteropServices;
namespace Rained;

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
    public readonly List<string> Files = [];

    public readonly bool NoAutoloads = false;
    public readonly List<string> Scripts = [];
    public readonly List<(string, string)> ScriptParameters = [];

    public readonly bool NoSplashScreen = false;
    public readonly bool ShowOgscule = false;
    public readonly bool LogToStdout = false;
    public readonly bool Render = false;
    public readonly int RenderThreads = int.Max(1, Environment.ProcessorCount - 1);
    public readonly string? EffectExportOutput = null;

    private static void PrintHelpMessage()
    {
        Console.WriteLine(
        $"""
        Usage:
            Rained [-v | --version]
            Rained [-h | --help]
            Rained [options...] [level paths...]
        
        --help                      Show this help screen.
        --version -v                Print out the app version.

        --log-to-stdout             Print logs to the standard output stream instead of to a file.
        --no-splash-screen          Do not show the splash screen when starting.
        --app-data <path>           Run with app data directory at <path>.
        --data <path>               Run with the Drizzle data directory at <path>.

        --render -r                 Render the given levels and exit. Does not start the GUI.
        --threads -t <count>        Optional max degree of parallelism to use when rendering.
                                    The default is the number of available cores minus one.
                                    Zero means it will be unbound.
        --no-autoloads              Don't run init.lua and other autoloading scripts on startup.
        --script <path>             Run the given Lua script, then exit. Does not start the GUI.
                                    Any level paths given in the command will be loaded first,
                                    as well as any autoload scripts. Multiple --script
                                    arguments can appear.
        --param <name=value>        Parameter to pass to all scripts.

        --export-effects <path>     Export Drizzle effect data to a .json file.
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
        static void ParseError(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("error: ");
            Console.ResetColor();
            Console.WriteLine(msg);
            Environment.ExitCode = 2;
        }

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

            if (str == "--threads" || str == "-t")
            {
                i++;
                if (int.TryParse(args[i], out int v) && v >= 0)
                {
                    RenderThreads = v;
                }
                else
                {
                    ParseError($"thread count is not a positive integer!");
                    Environment.ExitCode = 2;
                    ContinueBoot = false;
                }

                continue;
            }

            // the intrusive thoughts defeated me
            if (str == "--ogscule")
            {
                Console.WriteLine("ogscule");
                ShowOgscule = true;
                continue;
            }

            if (str == "--export-effects")
            {
                i++;
                EffectExportOutput = args[i];
                continue;
            }

            if (str == "--script")
            {
                i++;
                Scripts.Add(args[i]);
                continue;
            }

            if (str == "--param")
            {
                i++;
                var eqIdx = args[i].IndexOf('=');
                if (eqIdx == -1)
                {
                    ParseError($"invalid script param format; should be name=value.");
                    Environment.ExitCode = 2;
                    ContinueBoot = false;
                    continue;
                }
                else
                {
                    var k = args[i][..eqIdx];
                    var v = args[i][(eqIdx+1)..];
                    ScriptParameters.Add((k, v));
                }
                continue;
            }

            if (str == "--no-autoloads")
            {
                NoAutoloads = true;
                continue;
            }

            if (str[0] != '-')
            {
                Files.Add(str);
            }

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