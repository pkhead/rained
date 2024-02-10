using Drizzle.Ported;

namespace Drizzle.Logic.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[TestOf(typeof(LruCache<,>))]
public sealed class LruCacheTest
{
    [Test]
    public void Test()
    {
        var cache = new LruCache<int, string>(2);
        cache.Get(1, Load);
        cache.Get(2, Load);
        cache.Get(1, Load);
        cache.Get(3, Load);

        static string Load(int key) => key.ToString();
    }
}
