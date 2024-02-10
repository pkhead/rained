using System.Globalization;
using System.Threading;

namespace Drizzle.Lingo.Runtime.Utils;

public static class CultureFix
{
    public static void FixCulture()
    {
        var enUs = CultureInfo.GetCultureInfoByIetfLanguageTag("en-US");
        CultureInfo.DefaultThreadCurrentCulture = enUs;
        CultureInfo.DefaultThreadCurrentUICulture = enUs;
    }
}
