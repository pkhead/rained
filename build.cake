/**
* just a script to automate the publish process (cus i also need to copy assets and data and stuff)
*/
#addin nuget:?package=SharpZipLib
#addin nuget:?package=Cake.Compression

var target = Argument("Target", "Package");
var os = Argument("OS", System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier);

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