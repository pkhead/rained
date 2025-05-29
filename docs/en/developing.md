# Developing
This document is for those who want to develop/modify Rained. Rained is an open-source project under the MIT license, so you are basically free to do whatever you want with it and its source as long as proper attribution is given as stated in the license. You may also contribute to Rained's development making pull requests; those are completely welcome. Some instructions here are provided in the root README.md file
of the GitHub repository, but this document further details the development setup.

## Using ANGLE

!!! info

    TL;DR: If you are on Windows, copy the DLLs in `src\Glib\angle\win-x64` to `C:\Program Files\dotnet`.

For Windows, Rained prefers to use [ANGLE](https://chromium.googlesource.com/angle/angle), an OpenGL ES implementation for various graphics APIs. On other operating systems, Rained will prefer to use desktop OpenGL 3.3. The reasoning for this is that Windows OpenGL drivers can be a bit quirky, so to speak, and depending on the user's vendor, badly optimized. Also, I kept getting reports of OpenGL errors of a mysterious origin, although that bug has been fixed by now.

ANGLE is provided by a set of DLLs that takes a lot of waiting to build. Fortunately, pre-built ANGLE binaries for both Windows and Linux are stored in this repository, both of which I took from an Electron project for the respective systems because I couldn't figure out how to build it myself.

However, there is a hiccup with the referencing of the DLLs for Rained. Since whatever searches for the ANGLE DLLs does not go through the C# DLL resolver, that means that the ANGLE DLLs *must* either be in the system's library directories, LD_LIBRARY_PATH (on Unix), or the same folder as the running executable (on Windows). There is no problem here for release packages, since it needs to be put there anyway, but when running from a non-publish build there are two ways Rained can be launched and neither of them have the ANGLE DLLs automatically put in the directories of the executables.

If the program is being ran via `dotnet Rained.dll`, the ANGLE DLLs need to be present in the directory where `dotnet.exe` is located, otherwise the program will fail to start. If the program is being ran by running Rained.exe directly, the ANGLE DLLs need to be present in the build directory containing Rained.exe. You can alternatively copy the ANGLE DLLs to a directory that's referenced in the DLL search path, and it should work just fine for both launch situations.

If you don't feel like doing all of this, there is a way to build Rained with desktop OpenGL that doesn't have all this DLL nonsense as a prerequisite. How to do so is explained later in this document.

## Building
Prerequisities:

 - .NET Core toolchain
 - Python 3
 - *(optional)* OpenGL ES driver or [ANGLE libraries](src/Glib/angle) in the DLL search path.
 - *(optional)* [glslang](https://github.com/KhronosGroup/glslang) CLI

### Setup
1. Clone repository with Git:
```bash
git clone --recursive https://github.com/pkhead/rained
cd rained
```

2. Compile Drizzle and run exporter (rerun this whenever you update Drizzle)
```bash
cd src/Drizzle
dotnet run --project Drizzle.Transpiler
cd ../..
dotnet run --project src/DrizzleExport.Console effects src/Rained/Assets/effects.json
```

3. Generate Lua API (rerun this whenever you update ImGui)
```bash
python3 lua-imgui-gen.py
```

### Building the app
#### .NET CLI and Cake
```bash
# only needs to be run once
dotnet tool restore

# usage of desktop GL or GLES/ANGLE is determined by OS.
dotnet cake

# alternative build command with desktop GL forced on.
dotnet cake --gles=false
```

Run the project!
```bash
dotnet run --no-build --project src/Rained/Rained.csproj
```

#### .NET CLI alone
This is a translation of the Cake build script:
```bash
# validate/compile updated shader source files
# if you don't have glslangValidator, just skip these steps.
python3 shader-preprocessor.py gl330
python3 shader-preprocessor.py gles300

# you have three options here:
dotnet build src/Rained/Rained.csproj /p:GL=ES      # you can build with ES/ANGLE
dotnet build src/Rained/Rained.csproj /p:GL=Desktop # or you can build with normal OpenGL
dotnet build src/Rained/Rained.csproj               # this will auto-select based on OS. windows = GLES/ANGLE, linux = OpenGL
```

I unfortunately have no steps for setting up the build process in IDEs such as Visual Studio or the JetBrains one, because I
don't use those. But hopefully, you will be able to extrapolate the information from these instructions to work with your IDE of choice.

## Shaders
If you ever want to create new shaders or modify existing ones, you will need to run them through the shader preprocessor. The shader preprocessor exists for shader sources to include other shader files---something which OpenGL shader compilation doesn't support out of the box---and to handle differences between normal GLSL and ES GLSL.

In order to use the shader preprocessor, you will need Python 3 and [glslang](https://github.com/KhronosGroup/glslang) installed on your system. I don't believe glslang has an installer, but you need to install it in a way such that typing `glslangValidator` from any terminal will run the correct executable, which you do by modifying your system or user PATH.

Once you have both installed, the shader preprocessor will automatically run when calling `dotnet cake`. Although without the required software installed, the preprocessing step will simply be skipped.

## Documentation
The documentation is built using [Material for MkDocs](https://squidfunk.github.io/mkdocs-material/). You'll need python and pip to build it.

```bash
# install material for mkdocs
pip install mkdocs-material

# serve docs on http://localhost:8000
mkdocs serve

# build doc site
mkdocs build
```

## Subprojects
Rained has multiple projects in the C# solution. Here is a list of their brief descriptions:

|     Name                  |      Description                                           |
| ------------------------- | ---------------------------------------------------------- |
| **Drizzle**               | Port of the original renderer from Lingo to C#.            |
| **Glib**                  | OpenGL 3.3/OpenGL ES 2.0 and Silk.NET wrapper.             |
| **Glib.ImGui**            | ImGui.NET backend for Glib/Silk.NET.                       |
| **Glib.Tests**            | Test program for Glib visual output.                       |
| **ImGui.NET**             | Freetype-enabled version of ImGui.NET.                     |
| **Rained**                | The entire Rained application.                             |
| **Rained.Console**        | C application to launch Rained from a console environment. |
| **Rained.Tests**          | A few unit tests for Rained's Lingo parser                 |
| **DrizzleExport**         | Library to export Drizzle effect data to a .json file      |
| **DrizzleExport.Console** | Basic console interface to DrizzleExport.           |

There is also [rainedvm](https://github.com/pkhead/rainedvm), which is a separate program that serves as a version manager utility. It is programmed in C++ and distributed separately, which is why it is a separate repository.

## The ImGui .ini file
Previously the `config/imgui.ini` file was kept track in version control because an initial imgui.ini file is required for correct window positioning. It was a bit problematic, however, since not only would it always reflect window changes made by the developer on their latest run, it seems to change on a Rained launch even if you don't mess with any windows. Obviously, it's not preferable to have the imgui.ini file be included in every commit

However, as of [commit ddfafe2](https://github.com/pkhead/rained/commit/ddfafe2c6d468a49cc2dee6d937a8ac367fe037c), it is no longer necessary to keep it in version control since Rained will now generate an imgui.ini file from a programmer-given base file if it doesn't exist, and it thusly has been removed from version control.

Still, below are practices for how to deal with the imgui.ini file if it is in version control:

1. Run `git update-index --assume-unchanged config/imgui.ini`. This will make Git ignore any changes to the file, though
   it may cause problems when switching branches. You can use `git stash` in this situation. If you want to undo this,
   run `git update-index --no-assume-unchanged config/imgui.ini`.
2. Remember to manually unstage config/imgui.ini before every commit, or manually stage every file but that one.

## The "nightly" tag
The "nightly" tag really only exists so that I'm able to create nightly GitHub releases. It's a bit annoying. I wouldn't recommend interacting with it.

Since the action deletes and re-creates the "nightly" tag on every release, in order to update the tag
on your clone (not that you would want to, I suppose), you would have to run the following Git commands:
```bash
git tag -d nightly # delete the nightly tag on your clone
git fetch origin tag nightly # fetch the nightly tag from origin
# running `git fetch` or `git pull` itself after deleting the tag should also work.
```
