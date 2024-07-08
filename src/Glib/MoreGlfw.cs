using System.Reflection;
using System.Runtime.InteropServices;

namespace Glib;

// damn silk.net doesn't read dpi on glfw backend
// nor does it have bindings to glfw's getWindowContentScale
// i have previously written a curse-filled comment on this matter,
// but i deleted this file, forgetting that i wrote one, and this is a recreation of it.
internal static partial class MoreGlfw
{
    [LibraryImport("glfw3", EntryPoint = "glfwGetWindowContentScale")]
    public static unsafe partial void GlfwGetWindowContentScale(Silk.NET.GLFW.WindowHandle* window, out float xScale, out float yScale);

    [LibraryImport("glfw3", EntryPoint = "glfwSetWindowContentScaleCallback")]
    public static unsafe partial void GlfwSetWindowContentScaleCallback(Silk.NET.GLFW.WindowHandle* window, nint callback);

    public static bool init = false;

    public static void InitializeDllImportResolver()
    {
        if (init) return;
        init = true;
        NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), DllImportResolver);
    }

    private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName == "glfw3")
        {
            string dllName;

            if (OperatingSystem.IsWindows())
            {
                dllName = "glfw3.dll";
            }
            else if (OperatingSystem.IsMacOS())
            {
                dllName = "libglfw.3.dylib";
            }
            else // assume linux
            {
                dllName = "libglfw.so.3.3";
            }

            return NativeLibrary.Load(dllName, assembly, searchPath);
        }

        return IntPtr.Zero;
    }
}