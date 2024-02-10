using System.IO;
using SixLabors.ImageSharp;

namespace Drizzle.Lingo.Runtime.Xtra;

public sealed class ImgXtra : BaseXtra
{
    public override BaseXtra Duplicate()
    {
        return new ImgXtra();
    }

    public int ix_saveimage(LingoPropertyList props)
    {
        var img = (LingoImage)props["image"]!;
        var fileName = (string)props["filename"]!;

        using var file = File.Create(fileName);
        img.SaveAsPng(file);

        return 1;
    }
}
