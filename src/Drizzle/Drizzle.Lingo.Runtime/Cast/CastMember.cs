using System;
using System.Diagnostics;
using System.IO;

namespace Drizzle.Lingo.Runtime.Cast;

public sealed partial class CastMember
{
    private readonly LingoCastLib _castLib;
    private string? _name;
    public LingoRuntime Runtime { get; }
    public int Number { get; }

    public CastMemberType Type { get; set; }

    public string? name
    {
        get => _name;
        set
        {
            _name = value;
            _castLib.NameIndexDirty();
        }
    }

    public string Cast { get; }

    public CastMember(LingoRuntime runtime, LingoCastLib castLib, int number, string cast)
    {
        _castLib = castLib;
        Runtime = runtime;
        Number = number;
        Cast = cast;
    }

    public LingoSymbol type => Type switch
    {
        CastMemberType.Empty => default,
        CastMemberType.Bitmap => new LingoSymbol("bitmap"),
        CastMemberType.Script => new LingoSymbol("script"),
        CastMemberType.Text => new LingoSymbol("text"),
        CastMemberType.Shape => new LingoSymbol("shape"),
        _ => throw new ArgumentOutOfRangeException()
    };

    public void erase()
    {
        Type = CastMemberType.Empty;
        _text = "";
        _image = null;
    }

    public void importfileinto(string path, LingoPropertyList? propList = null)
    {
        var fullPath = Runtime.GetFilePath(path);
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path)[1..];
        ImportFile(fullPath, ext, name);
    }

    public void ImportFile(string fullPath, string ext, string? name)
    {
        erase();

        var type = ext switch
        {
            "png" or "bmp" => CastMemberType.Bitmap,
            "lingo" => CastMemberType.Script,
            "txt" => CastMemberType.Text,
            _ => CastMemberType.Empty
        };

        Type = type;

        if (name != null)
            this.name = name;

        switch (Type)
        {
            case CastMemberType.Bitmap:
                ImportFileImplBitmap(fullPath);
                break;
            case CastMemberType.Script:
                break;
            case CastMemberType.Shape:
                break;
            case CastMemberType.Text:
                ImportFileImplText(fullPath);
                break;
        }
    }

    private void AssertType(CastMemberType type)
    {
        Debug.Assert(Type == type, "Wrong cast member type!");
    }

    public void CloneFrom(CastMember other)
    {
        Type = other.Type;
        name = other.name;

        switch (Type)
        {
            case CastMemberType.Bitmap:
                _image = other._image!.DuplicateShared();
                break;
            case CastMemberType.Script:
                break;
            case CastMemberType.Shape:
                break;
            case CastMemberType.Text:
                _text = other._text;
                break;
        }
    }
}

public enum CastMemberType
{
    Empty,
    Bitmap,
    Script,
    Text,
    Shape
}
