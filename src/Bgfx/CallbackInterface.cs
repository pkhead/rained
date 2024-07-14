using System.Runtime.InteropServices;

namespace Bgfx_cs;

static partial class BgfxInterop
{
    const string DllName = "bgfx_interop";

    [LibraryImport(DllName, EntryPoint = "create_bgfx_interface")]
    public unsafe static partial void* CreateBgfxInterface(delegate* unmanaged<byte*, void> logFunc, delegate* unmanaged<byte*, ushort, int, byte*, void> fatalFunc); 

    [LibraryImport(DllName, EntryPoint = "destroy_bgfx_interface")]
    public unsafe static partial void DestroyBgfxInterface(void* callbackInterface);
}

public class CallbackInterface : IDisposable
{
    private unsafe void* cbInterface;

    public delegate void LogCallback(string msg);
    public delegate void FatalCallback(string filePath, int line, Bgfx.Fatal code, string msg);

    private unsafe delegate void RawLogCallback(byte* str);
    private unsafe delegate void RawFatalCallback(byte* filePath, ushort line, int code, byte* str);

    private unsafe RawLogCallback _logFunc;
    private unsafe RawFatalCallback _fatalFunc;

    public LogCallback? Log;
    public FatalCallback? Fatal;
    unsafe public nint Pointer => (nint) cbInterface;

    public unsafe CallbackInterface()
    {
        _logFunc = (byte* str) =>
        {
            if (str != null)
            {
                var msg = Marshal.PtrToStringAnsi((nint)str)!.TrimEnd(); // trim the \n
                Log?.Invoke(msg);
            }
        };

        _fatalFunc = (byte* _filePath, ushort _line, int _code, byte* _str) =>
        {
            var filePath = Marshal.PtrToStringAnsi((nint)_filePath);
            int line = _line;
            Bgfx.Fatal code = (Bgfx.Fatal) _code;
            var msg = Marshal.PtrToStringAnsi((nint)_str);

            Fatal?.Invoke(filePath!, line, code, msg!.TrimEnd());
        };
        
        cbInterface = BgfxInterop.CreateBgfxInterface(
            logFunc: (delegate* unmanaged<byte*, void>) Marshal.GetFunctionPointerForDelegate(_logFunc),
            fatalFunc: (delegate* unmanaged<byte*, ushort, int, byte*, void>) Marshal.GetFunctionPointerForDelegate(_fatalFunc)
        );
    }

    unsafe ~CallbackInterface()
    {
        BgfxInterop.DestroyBgfxInterface(cbInterface);
    }

    public unsafe void Dispose()
    {
        BgfxInterop.DestroyBgfxInterface(cbInterface);
        cbInterface = null;
        GC.SuppressFinalize(this);
    }
}