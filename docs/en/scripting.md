# Scripting

Rained provides a system for running Lua scripts that can provide additional
functionality and utility not present in the base version of the application.
With the scripting system, you can make rectangle autotiles; path autotiles with
specialized behavior; modify and analyze geometry, tiles, effects, cameras, and
props; create custom GUI for utility purposes; and much more, assuming you have
the required Lua programming knowledge to do so, or a script made by someone
else that does.

## Filesystem structure

The directory for Lua scripts is located in the `scripts/` directory, relative
to the Rained executable. If you use a non-portable installation of Rained,
said folder may instead be located within the configuration directory, although
GitHub releases and rainedvm only provide portable installations at the time of
writing.

Rained comes with several pre-bundled files within this folder:

- **definitions/**: Definitions for the
  [Lua Language Server](https://luals.github.io/). Also serves as the primary
  source of documentation on the scripting API (at least until I figure out how
  to generate MkDocs pages off of it).
- **helpers.lua**: Contains some Lua helper functions, though currently, and
  for the forseeable future, only contains a procedure for generating pattern
  box autotiles, and one for stringifying Lua tables.
- **autoload/detach-fix.lua**: Provides an edit command which automatically
  fixes all of the detached tile bodies in a level.
- **autoload/rendercopy.lua**: Provides a utility for automatically copying
  rendered level files to your mod folder.
- **autoload/autotiles/**: Contains all of the default Lua-based autotiles. You
  may also put your own autotiles in here as well, but that is not required.

## Entry point

The entry point of the Lua scripting runtime is `scripts/init.lua`, which in
turn (by default), recursively `require`s all Lua files located in the
`scripts/autoload` directory. Putting scripts in `autoload` is the recommended
method of getting scripts to initialize or run on boot—otherwise, you can
explicitly load custom scripts using the Lua `require` or `load` function within
`scripts/init.lua`.

## Headless mode

When launching Rained with the `--render` or `--script` command-line options,
Rained will initialize the Lua runtime, but with no GUI; the Lua runtime will be
initialized in headless mode. This can be detected from Lua by evaluating the
boolean return value of the function `rained.isBatchMode()`.

Due to a lack of a GUI, several things are different in headless mode:

- Commands, autotiles, and `rained.onUpdate` are no-ops.
- `rained.history`, `rained.gui`, and `rained.view` will be `nil`.
- `alert` will instead print messages to the standard output stream.
- Attempting to index the `imgui` module will throw an error.

!!! tip

    An easy way to make sure a script does not run in headless mode is to insert

    ```lua
    if rained.isBatchMode() then return end
    ```

    as the first line of your script.