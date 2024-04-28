/**
* just a script to automate the publish process (cus i also need to copy assets and data and stuff)
*/

var target = Argument("Target", "Package");

Task("DotNetPublish")
    .Does(() =>
{
    EnsureDirectoryExists("build");
    CleanDirectory("build");

    DotNetMSBuildSettings buildSettings = new DotNetMSBuildSettings();
    buildSettings.Properties.Add("AppDataPath", ["Assembly"]);

    DotNetPublish("rained.sln", new DotNetPublishSettings
    {
        SelfContained = true,
        OutputDirectory = "build",
        MSBuildSettings = buildSettings
    });

    CreateDirectory("build/assets");
    CopyDirectory("assets", "build/assets");
    CopyDirectory("scripts", "build/scripts");
    CopyDirectory("config", "build/config");
    DeleteFile("build/config/preferences.json");
    CopyFile("LICENSE.md", "build/LICENSE.md");

    CopyDirectory("dist", "build");

    /*
    if (!DirectoryExists("build/Data"))
    {
        CreateDirectory("build/Data");
        CopyDirectory("Data", "build/Data");
    }
    */
});

Task("ConsoleWrapper")
    .Does(() =>
{
    if (!IsRunningOnWindows()) return;

    EnsureDirectoryExists("build");

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
        StartProcess(cxxCompiler, "-static -Os src/Rained.Console/console-launch.cpp -o build/Rained.Console.exe");
    }
});

Task("Package")
    .IsDependentOn("DotNetPublish")
    .IsDependentOn("ConsoleWrapper")
    .Does(() =>
{
    Zip("build", "rained_X.X.X-win-x64.zip");
});

RunTarget(target);