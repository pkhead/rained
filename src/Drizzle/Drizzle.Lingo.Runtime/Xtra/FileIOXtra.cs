using System;
using System.IO;

namespace Drizzle.Lingo.Runtime.Xtra;

public sealed class FileIOXtra : BaseXtra
{
    private FileStream? _file;

    public void openfile(string filePath) => openfile(filePath, 0);

    public void openfile(string filePath, LingoNumber mode)
    {
        var access = mode.IntValue switch
        {
            0 => FileAccess.ReadWrite,
            1 => FileAccess.Read,
            2 => FileAccess.Write,
            _ => throw new ArgumentException("Invalid file mode!")
        };

        _file = File.Open(filePath, FileMode.Open, access);
    }

    public void closefile()
    {
        _file?.Dispose();
        _file = null;
    }

    public void delete()
    {
        if (_file == null)
            throw new InvalidOperationException("File not open!");

        var name = _file.Name;
        closefile();

        File.Delete(name);
    }

    public void createfile(string path)
    {
        File.Create(path).Dispose();
    }

    public void writestring(string str)
    {
        if (_file == null)
            throw new InvalidOperationException("File not open!");

        using var writer = new StreamWriter(_file, leaveOpen: true);
        writer.Write(str);
    }

    public void writereturn(LingoSymbol type)
    {
        if (_file == null)
            throw new InvalidOperationException("File not open!");

        using var writer = new StreamWriter(_file, leaveOpen: true);
        writer.Write("\r\n");
    }

    public string readfile()
    {
        if (_file == null)
            throw new InvalidOperationException("File not open!");

        var reader = new StreamReader(_file, leaveOpen: true);
        return reader.ReadToEnd();
    }

    public override BaseXtra Duplicate()
    {
        return new FileIOXtra();
    }
}