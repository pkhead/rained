/**
* just a script to automate the publish process (cus i also need to copy assets and data and stuff)
*/
#addin nuget:?package=SharpZipLib
#addin nuget:?package=Cake.Compression

var target = Argument("Target", "Package");
var os = Argument("OS", "win-x64");

var buildDir = "build_" + os;

Task("DotNetPublish")
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
    DeleteFile(buildDir + "/config/preferences.json");
    CopyFile("LICENSE.md", buildDir + "/LICENSE.md");

    CopyDirectory("dist", buildDir);
});

Task("ConsoleWrapper")
    .Does(() =>
{
    if (os != "win-x64") return;

    EnsureDirectoryExists(buildDir);

    bool clangExists = false;
    string cxxCompiler = EnvironmentVariable<string>("CXX_COMPILER", "c++");
    try
    {
        StartProcess(cxxCompiler, "-v");
        clangExists = true;
    }
    catch
    {}
    
    if (!clangExists)
    {
        Information("A C++ compiler was not found! Rained will still build normally, just without the console wrapper app.");
    }
    else
    {
        StartProcess(cxxCompiler, $"-static -Os src/Rained.Console/console-launch.cpp -o {buildDir}/Rained.Console.exe");
    }
});

Task("Package")
    .IsDependentOn("DotNetPublish")
    .IsDependentOn("ConsoleWrapper")
    .Does(() =>
{
    if (os == "linux-x64")
        GZipCompress(buildDir, $"rained_X.X.X_{os}.tar.gz");
    else
        Zip(buildDir, $"rained_X.X.X_{os}.zip");
});

RunTarget(target);