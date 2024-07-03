using System.Runtime.InteropServices;
using Silk.NET.Windowing;
using Microsoft.Win32;
namespace Glib;

static partial class PlatformSpecific
{
    [LibraryImport("dwmapi.dll")]
    private static unsafe partial uint DwmSetWindowAttribute(nint hwnd, uint dwAttribute, nint pvAttribute, uint cbAttribute);

    private enum DwmWindowAttribute : uint
    {
        UseImmersiveDarkMode = 20
    };

    /// <summary>
    /// Attempt to set a window to use the user's preferred light/dark theme. Windows-specific
    /// </summary>
    /// <param name="window">The Silk IWindow</param>
    /// <param name="windowTheme">The resulting window theme</param>
    /// <returns>True if the operation was successful, false if not.</returns>
    public unsafe static bool TryWindowTheme(IWindow window, out WindowTheme windowTheme)
    {
        windowTheme = WindowTheme.Default;
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763)) return false;

        try
        {
            var hwnd = window.Native!.Win32!.Value.Hwnd;

            // read registry key which determines the user's theme
            // this seems to be the only reliable way to do this
            var regValue = Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "AppsUseLightTheme", -1);
            if (regValue is not null && regValue is int appsUseLightTheme && appsUseLightTheme != -1)
            {
                // 0 == dark mode, 1 == light mode
                if (appsUseLightTheme == 0 || appsUseLightTheme == 1)
                {
                    windowTheme = appsUseLightTheme == 0 ? WindowTheme.Dark : WindowTheme.Light;
                    uint value = windowTheme == WindowTheme.Dark ? (uint)1 : (uint)0;
                    if (DwmSetWindowAttribute(hwnd, (uint)DwmWindowAttribute.UseImmersiveDarkMode, (nint)(&value), sizeof(uint)) == 0)
                    {
                        return true; // success!
                    }
                }
            }
        }
        catch (Exception)
        {}
        
        return false;
    }

    // what is this bs...
    // Silk.NET does not have a binding to glfwGetWindowContentScale
    [LibraryImport("glfw3", EntryPoint = "glfwGetWindowContentScale")]
    public static unsafe partial void GlfwGetWindowContentScale(Silk.NET.GLFW.WindowHandle* window, out float xScale, out float yScale);

    [LibraryImport("glfw3", EntryPoint = "glfwSetWindowContentScaleCallback")]
    public static unsafe partial void GlfwSetWindowContentScaleCallback(Silk.NET.GLFW.WindowHandle* window, nint callback);

}