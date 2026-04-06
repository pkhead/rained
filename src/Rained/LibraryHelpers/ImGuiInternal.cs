namespace ImGuiNET;
using System.Runtime.InteropServices;

static partial class ImGuiInternal
{
    [LibraryImport("cimgui")]
    public static partial void igPushItemFlag(int option, byte enabled);

    [LibraryImport("cimgui")]
    public static partial void igPopItemFlag();
}