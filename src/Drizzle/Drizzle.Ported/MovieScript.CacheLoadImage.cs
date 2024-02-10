using Drizzle.Lingo.Runtime;

namespace Drizzle.Ported;

public sealed partial class MovieScript
{
    private readonly LruCache<string, LingoImage> _imageCache = new(64);

    public LingoImage cacheloadimage(string fileName)
    {
        return _imageCache.Get(fileName, this, static (state, fileName) => state.CacheLoadImageLoad(fileName));
    }

    private LingoImage CacheLoadImageLoad(string fileName)
    {
        var member = _global.member("previewImprt")!;
        member.importfileinto(fileName);
        member.name = "previewImprt";
        return member.image!;
    }

    public void ImageCacheClear() => _imageCache.Clear();
}
