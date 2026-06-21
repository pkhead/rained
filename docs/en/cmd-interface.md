# Command-line interface

Rained provides a command-line interface. On Windows, Rained can be ran as a
command by invoking the `Rained.Console.exe` executable, which is a console
application that runs `Rained.exe` as a child process in a matter suitable for
use as a console command. On Unix-based platforms, there is no separation in
execution between console and GUI applications, so you simply invoke the
`Rained` executable to run it as a command.

Below is the output of the `Rained --help` command for version 2.5.1:
```
Usage:
    Rained [-v | --version]
    Rained [-h | --help]
    Rained [options...] [level paths...]

--help                      Show this help screen.
--version -v                Print out the app version.

--log-to-stdout             Print logs to the standard output stream.
--no-splash-screen          Do not show the splash screen when launching.
--app-data <path>           Run with app data directory at <path>.
--data <path>               Run with the Drizzle data directory at <path>.
--new-instance -n           Always open a new instance of Rained, instead of potentially
                            reusing a pre-existing one.

--render -r                 Render the given levels and exit. Does not start the GUI.
--threads -t <count>        Optional max degree of parallelism to use when rendering.
                            The default is the number of available cores minus one.
                            Zero means it will be unbound.
--no-autoloads              Don't run init.lua and other autoloading scripts on startup.
--script <path>             Run the given Lua script, then exit. Does not start the GUI.
                            Pass - to read from the standard input stream. Any level
                            paths given in the command will be loaded first, as well as
                            any autoload scripts. Multiple --script arguments can appear.
--param <name=value>        Parameter to pass to all scripts.

--export-effects <path>     Export Drizzle effect data to a .json file and exit.
                            Does not start the GUI or run scripts.
```

<!-- not much to explain here, since it's all already done in the help command ... -->