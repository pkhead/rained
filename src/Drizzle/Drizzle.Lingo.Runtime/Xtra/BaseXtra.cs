namespace Drizzle.Lingo.Runtime.Xtra;

public abstract class BaseXtra
{
    public abstract BaseXtra Duplicate();

    // Editor does a pattern where it does xtra().new(), work around this.
    public dynamic @new()
    {
        return Duplicate();
    }
}