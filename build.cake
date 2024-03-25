/**
* just a script to automate the publish process (cus i also need to copy assets and data and stuff)
*/

var target = Argument("Target", "Publish");

Task("Publish")
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
    CopyDirectory("themes", "build/themes");
    CopyFile("imgui.ini", "build/imgui.ini");
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

RunTarget(target);