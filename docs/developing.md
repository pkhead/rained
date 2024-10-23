# Developing
This document is for those who want to develop/modify Rained. Rained is an open-source project under the MIT license, so you are basically free to do whatever you want with it and its source as long as proper attribution is given as stated in the license. You may also contribute to Rained's development making pull requests; those are completely welcome. Some instructions here are provided in the root README.md file
of the GitHub repository, but this document further details the development setup.

## Using ANGLE
**TL;DR: If you are on Windows, copy the DLLs in `src\Glib\angle\win-x64` to `C:\Program Files\dotnet`.**

For Windows, Rained prefers to use [ANGLE](https://chromium.googlesource.com/angle/angle), an OpenGL ES implementation for various graphics APIs. On other operating systems, Rained will prefer to use desktop OpenGL 3.3. The reasoning for this is that Windows OpenGL drivers can be a bit quirky, so to speak, and depending on the user's vendor, badly optimized. Also, I kept getting reports of OpenGL errors of a mysterious origin, although that may be fixed by now.

ANGLE is provided by a set of DLLs that takes a lot of waiting to build. Fortunately, pre-built ANGLE binaries for both Windows and Linux are stored in this repository, both of which I took from an Electron project for the respective systems because I couldn't figure out how to build it myself.

However, there is a hiccup with the referencing of the DLLs for Rained. Since whatever searches for the ANGLE DLLs does not go through the C# DLL resolver, that means that the ANGLE DLLs *must* either be in PATH or, on Windows, in the same folder as the running executable. There is no problem here for release packages, since it needs to be put there anyway, but when running from a non-publish build there are two ways Rained can be launched and neither of them have the ANGLE DLLs automatically put in the directories of the executables.

If the program is being ran via `dotnet Rained.dll`, the ANGLE DLLs need to be present in the directory where `dotnet.exe` is located, otherwise the program will fail to start. If the program is being ran by running Rained.exe directly, the ANGLE DLLs need to be present in the build directory containing Rained.exe. You can alternatively copy the ANGLE DLLs to a directory that's referenced in the path for DLLs, and it should work just fine for both launch situations.

If you don't feel like doing all of this, there is a way to build Rained with desktop OpenGL that doesn't have all this DLL nonsense as a prerequisite. How to do so is explained later in this document.

## .NET CLI
The following are instructions on how to clone Rained using Git and build it using the .NET CLI:

1. Clone with Git:
```bash
git clone --recursive https://github.com/pkhead/rained
cd rained
```

2. Compile Drizzle
```bash
cd src/Drizzle
dotnet run --project Drizzle.Transpiler
```

3. Back to the root directory, build and run Rained
```bash
# only needs to be run once
dotnet tool restore

# usage of desktop GL or GLES/ANGLE is determined by OS.
dotnet cake

# alternative build command with desktop GL forced on.
dotnet cake --gles=false
```

4. Run the project!
```bash
dotnet run --no-build --project src/Rained/Rained.csproj
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

|     Name           |      Description                                           |
| ------------------ | ---------------------------------------------------------- |
| **Drizzle**        | Port of the original renderer from Lingo to C#.            |
| **Glib**           | OpenGL 3.3/OpenGL ES 2.0 and Silk.NET wrapper.             |
| **Glib.ImGui**     | ImGui.NET backend for Glib/Silk.NET.                       |
| **Glib.Tests**     | Test program for Glib visual output.                       |
| **ImGui.NET**      | Freetype-enabled version of ImGui.NET.                     |
| **Rained**         | The entire Rained application.                             |
| **Rained.Console** | C application to launch Rained from a console environment. |
| **Rained.Tests**   | A few unit tests for Rained.                               |

There is also [rainedvm](https://github.com/pkhead/rainedvm), which is a separate program that serves as a version manager utility. It is programmed in C++ and distributed separately, which is why it is a separate repository.

## The ImGui .ini file
The `config/imgui.ini` file will always be modified whenever you launch Rained, making version control want to keep track of
the unnecessary changes. However, it can't be put in the .gitignore since Rained needs to have an initial imgui.ini file.
Thus, if you don't actually want to update config/imgui.ini, I advise two practices:

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
