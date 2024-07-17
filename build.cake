/**
* just a script to automate the publish process (cus i also need to copy assets and data and stuff)
*/
#addin nuget:?package=SharpZipLib
#addin nuget:?package=Cake.Compression
using System.IO;
using Path = System.IO.Path;

var target = Argument("Target", "Package");
var os = Argument("OS", System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier);

var buildDir = "build_" + os;

bool IsUpToDate(string dstFile, params string[] deps)
{
    var dstWriteTime = System.IO.File.GetLastWriteTime(dstFile);

    foreach (var dep in deps)
    {
        if (System.IO.File.GetLastWriteTime(dep) > dstWriteTime)
            return false;
    }

    return true;
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
    string shaderc = EnvironmentVariable<string>("BGFX_SHADERC", "shaderc");

    bool hasShaderC = false;
    try
    {
        Exec(shaderc, ["-v"]);
        hasShaderC = true;
    }
    catch
    {
        hasShaderC = false;
    }

    if (!hasShaderC)
    {
        Information("Could not find shaderc! Make sure it is in your PATH or your BGFX_SHADERC environment variable is set.");
        Information("Shader compilation skipped.");
        return;
    }
    
    string platform = "windows";
    if (os == "win-x64" || os == "win-x86" || os == "win-arm64") platform = "windows";
    else if (os == "linux-x64" || os == "linux-x86" || os == "linux-arm64") platform = "linux";
    else if (os == "osx" || os == "osx-arm64" || os == "os-x64") platform = "osx";
    else
    {
        Error($"Unknown runtime {os} when building shaders. Fall back to windows.");
    }

    // shaderc --varyingdef shadersrc/varying.def.sc -i shadersrc -f shadersrc/vs.sc -o shaders/vs.glsl --type vertex --platform windows -p 150
    var shaderBuildDir = Path.Combine("shaders","build",os);
    EnsureDirectoryExists(shaderBuildDir);
    EnsureDirectoryExists(Path.Combine(shaderBuildDir,"glsl"));
    EnsureDirectoryExists(Path.Combine(shaderBuildDir,"d3d"));
    EnsureDirectoryExists(Path.Combine(shaderBuildDir,"spirv"));

    void CompileShader(string srcFile, string dstFile, string shaderTypeStr, string shaderTarget)
    {
        if (!IsUpToDate(dstFile, srcFile, "shaders/bgfx_shader.sh", "shaders/varying.def.sc"))
        {
            Information($"Compile shader '{srcFile}' to '{dstFile}'");
            Exec(shaderc, [
                "--varyingdef", "shaders/varying.def.sc",
                "-i shaders",
                "-f", srcFile,
                "-o", dstFile,
                "--type", shaderTypeStr,
                "--platform", platform,
                "-p", shaderTarget
            ]);
        }
    }

    void ShaderSource(string fileName, ShaderType shaderType)
    {
        string shaderTypeStr = shaderType switch
        {
            ShaderType.Vertex => "vertex",
            ShaderType.Fragment => "fragment",
            _ => throw new ArgumentOutOfRangeException(nameof(shaderType))
        };

        string name = Path.GetFileNameWithoutExtension(fileName);
        string srcFile = Path.Combine("shaders", fileName);

        CompileShader(srcFile, Path.Combine(shaderBuildDir, "glsl", name + ".bin"), shaderTypeStr, "150");
        CompileShader(srcFile, Path.Combine(shaderBuildDir, "d3d", name + ".bin"), shaderTypeStr, "s_5_0");
        CompileShader(srcFile, Path.Combine(shaderBuildDir, "spirv", name + ".bin"), shaderTypeStr, "spirv");
    }

    foreach (var fileName in System.IO.Directory.EnumerateFiles("shaders"))
    {
        if (Path.GetExtension(fileName) != ".sc") continue;
        var name = Path.GetFileNameWithoutExtension(fileName);
        if (name == "varying.def") continue;

        string suffix = "[UNKNOWN]";
        if (name.Length < 3 || ((suffix = name[^3..]) != "_vs" && suffix != "_fs"))
            throw new Exception($"Shader source file {name}.sc must end in '_vs.sc' or '_fs.sc'.");
        
        if      (suffix == "_vs") ShaderSource(name + ".sc", ShaderType.Vertex);
        else if (suffix == "_fs") ShaderSource(name + ".sc", ShaderType.Fragment);
        else throw new Exception("Unreachable code");
    }
});

Task("DotNetPublish")
    .IsDependentOn("Build Shaders")
    .Does(() =>
{
    EnsureDirectoryExists(buildDir);
    CleanDirectory(buildDir);

    DotNetMSBuildSettings buildSettings = new DotNetMSBuildSettings();
    buildSettings.Properties.Add("AppDataPath", ["Assembly"]);

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