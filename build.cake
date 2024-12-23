/**
* just a script to automate the publish process (cus i also need to copy assets and data and stuff)
*/
#addin nuget:?package=SharpZipLib&version=1.4.2
#addin nuget:?package=Cake.Compression&version=0.3.0
using System.IO;
using System.Linq;
using Path = System.IO.Path;

var target = Argument("Target", "Build");
var os = Argument("OS", System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier);
var buildDir = "build_" + os;
var useGles = Argument<bool>("GLES", os == "win-x64");

List<string> ExecCapture(string procName, System.Collections.Generic.IEnumerable<string> args)
{
    Verbose(procName + " " + string.Join(' ', args));
    using var proc = StartAndReturnProcess(procName, new ProcessSettings()
    {
        Arguments = ProcessArgumentBuilder.FromStringsQuoted(args),
        RedirectStandardOutput = true,
        RedirectStandardError = true
    });
    proc.WaitForExit();

    var output = new List<string>();

    foreach (var str in proc.GetStandardOutput())
        output.Add(str);

    var code = proc.GetExitCode();
    if (code != 0)
        throw new Exception($"{procName} returned {code}");
    
    return output;
}

void Exec(string procName, System.Collections.Generic.IEnumerable<string> args)
{
    Verbose(procName + " " + string.Join(' ', args));
    using var proc = StartAndReturnProcess(procName, new ProcessSettings()
    {
        Arguments = ProcessArgumentBuilder.FromStringsQuoted(args)
    });
    proc.WaitForExit();

    var code = proc.GetExitCode();
    if (code != 0)
        throw new Exception($"{procName} returned {code}");    
}

enum ShaderType { Vertex, Fragment }

Task("Build Shaders")
    .Does(() =>
{
    bool hasPython3 = false;
    string pythonExec = "python3";
    try
    {
        ExecCapture("python3", ["--version"]);
        hasPython3 = true;
    }
    catch
    {
        try
        {
            pythonExec = "python";
            foreach (var line in ExecCapture("python", ["--version"]))
            {
                if (line[0..8] == "Python 3")
                {
                    hasPython3 = true;
                    break;
                }
            }
        }
        catch
        {
            hasPython3 = false;
        }
    }

    if (!hasPython3)
    {
        Information("Could not find python3!");
        Information("Shader preprocessing/validation skipped.");
        return;
    }

    string glslang = EnvironmentVariable<string>("GLSL_VALIDATOR", "glslangValidator");

    bool hasGlslang = false;
    try
    {
        ExecCapture(glslang, ["-v"]);
        hasGlslang = true;
    }
    catch
    {
        hasGlslang = false;
    }

    if (!hasGlslang)
    {
        Information("Could not find glslangValidator! Make sure it is in your PATH or the GLSL_VALIDATOR environment variable is set.");
        Information("Shader preprocessing/validation skipped.");
        return;
    }

    Exec(pythonExec, ["shader-preprocessor.py", "gles300"]);
    Exec(pythonExec, ["shader-preprocessor.py", "gl330"]);
});

Task("Build")
    .IsDependentOn("Build Shaders")
    .Does(() =>
{
    DotNetMSBuildSettings msBuildSettings = new DotNetMSBuildSettings();
    
    if (useGles)
        msBuildSettings.Properties.Add("GL", ["ES"]); // use ANGLE
    else
        msBuildSettings.Properties.Add("GL", ["Desktop"]); // use desktop GL

    var buildSettings = new DotNetBuildSettings()
    {
        MSBuildSettings = msBuildSettings
    };

    var config = Argument("Configuration", "Debug");
    if (config != "Debug") buildSettings.Configuration = config;

    DotNetBuild("src/Rained/Rained.csproj", buildSettings);
});

Task("DotNetPublish")
    .IsDependentOn("Build Shaders")
    .Does(() =>
{
    EnsureDirectoryExists(buildDir);
    CleanDirectory(buildDir);

    DotNetMSBuildSettings buildSettings = new DotNetMSBuildSettings();
    buildSettings.Properties.Add("AppDataPath", ["Assembly"]);

    if (HasArgument("full-release"))
        buildSettings.Properties.Add("FullRelease", ["true"]);

    if (useGles)
        buildSettings.Properties.Add("GL", ["ES"]); // use ANGLE
    else
        buildSettings.Properties.Add("GL", ["Desktop"]); // use desktop GL

    DotNetPublish("src/Rained/Rained.csproj", new DotNetPublishSettings
    {
        SelfContained = true,
        OutputDirectory = buildDir,
        Runtime = os,
        MSBuildSettings = buildSettings
    });

    CreateDirectory(buildDir + "/assets");
    CopyDirectory("assets", buildDir + "/assets");
    CopyDirectory("scripts", buildDir + "/scripts");
    CopyDirectory("config", buildDir + "/config");
    CopyFile("LICENSE.md", buildDir + "/LICENSE.md");
    CopyFile("CREDITS.md", buildDir + "/CREDITS.md");
    
    if (FileExists(buildDir + "/config/preferences.json"))
    {
        DeleteFile(buildDir + "/config/preferences.json");
    }

    CopyDirectory("dist", buildDir);

    // only keep console wrapper if building for Windows
    if (os != "win-x64" && FileExists(buildDir + "/Rained.Console.exe"))
    {
        DeleteFile(buildDir + "/Rained.Console.exe");
    }
});

Task("Package")
    .IsDependentOn("DotNetPublish")
    .Does(() =>
{
    if (os == "linux-x64")
        GZipCompress(buildDir, $"rained_{os}.tar.gz");
    else
        Zip(buildDir, $"rained_{os}.zip");
});

RunTarget(target);