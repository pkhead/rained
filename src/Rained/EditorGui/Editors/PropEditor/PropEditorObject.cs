using Rained.LevelData;
using System.Diagnostics;
using System.Numerics;
namespace Rained.EditorGui.Editors;

enum PropEditorObjectType
{
    Prop,
    FezTreeTrunk,
}

readonly record struct PropEditorObject(PropEditorObjectType Type, Prop Prop)
{
    public int DepthOffset => Prop.DepthOffset;
    public bool IsMovable => Type == PropEditorObjectType.Prop ? Prop.IsMovable : true;
    public bool CanRemove => Type == PropEditorObjectType.Prop;
    public string DisplayName
    {
        get
        {
            switch (Type)
            {
                case PropEditorObjectType.Prop:
                    return Prop.PropInit.Name;
                
                case PropEditorObjectType.FezTreeTrunk:
                    return Prop.PropInit.Name + " trunk";

                default: throw new UnreachableException();
            }
        }
    }

    public ref Vector2 FezTrunkPosition { get => ref Prop.FezTree!.TrunkPosition; }
    public ref float FezTrunkAngle { get => ref Prop.FezTree!.TrunkAngle; }

    private static bool IsPointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
    {
        static float sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
        }

        float d1 = sign(pt, v1, v2);
        float d2 = sign(pt, v2, v3);
        float d3 = sign(pt, v3, v1);

        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        return !(hasNeg && hasPos);
    }

    public bool PointOverlaps(Vector2 point)
    {
        switch (Type)
        {
            case PropEditorObjectType.Prop:
            {
                var pts = Prop.QuadPoints;
                return (
                    IsPointInTriangle(point, pts[0], pts[1], pts[2]) ||
                    IsPointInTriangle(point, pts[2], pts[3], pts[0])    
                );
            }

            case PropEditorObjectType.FezTreeTrunk:
            {
                return Vector2.DistanceSquared(FezTrunkPosition, point) < 0.8 * 0.8;
            }

            default: throw new UnreachableException();
        }
    }

    public static PropEditorObject CreateProp(Prop prop)
    {
        return new PropEditorObject(PropEditorObjectType.Prop, prop);
    }

    public static PropEditorObject CreateFezTreeTrunk(Prop prop)
    {
        return new PropEditorObject(PropEditorObjectType.FezTreeTrunk, prop);
    }

    public static IEnumerable<Prop> SelectPropsWithType(IEnumerable<PropEditorObject> objects, PropEditorObjectType typ)
    {
        return objects
            .Where(x => x.Type == typ)
            .Select(x => x.Prop);
    }
}