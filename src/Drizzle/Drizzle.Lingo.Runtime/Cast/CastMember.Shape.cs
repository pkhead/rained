namespace Drizzle.Lingo.Runtime.Cast;

public sealed partial class CastMember
{
    public LingoNumber _lineDirection;

    public LingoNumber linedirection
    {
        get
        {
            AssertType(CastMemberType.Shape);
            return _lineDirection;
        }
        set
        {
            AssertType(CastMemberType.Shape);
            _lineDirection = value;
        }
    }
}