namespace Drizzle.Lingo.Runtime.Cast;

public partial class CastMember
{
    private LingoImage? _image;

    public LingoImage? image
    {
        get
        {
            AssertType(CastMemberType.Bitmap);
            return _image;
        }
        set
        {
            AssertType(CastMemberType.Bitmap);
            _image = value;
        }
    }

    public LingoRect rect
    {
        get
        {
            AssertType(CastMemberType.Bitmap);
            return _image!.rect;
        }
    }

    public LingoNumber width
    {
        get
        {
            AssertType(CastMemberType.Bitmap);
            return image!.width;
        }
    }

    public LingoNumber height
    {
        get
        {
            AssertType(CastMemberType.Bitmap);
            return image!.height;
        }
    }

    public LingoColor getpixel(int x, int y)
    {
        AssertType(CastMemberType.Bitmap);
        return image!.getpixel(x, y);
    }

    public LingoColor getpixel(LingoNumber x, LingoNumber y) => getpixel((int)x, (int)y);

    public LingoPoint regpoint { get; set; }

    private void ImportFileImplBitmap(string path)
    {
        image = LingoImage.LoadFromPath(path).Trimmed();
    }
}
