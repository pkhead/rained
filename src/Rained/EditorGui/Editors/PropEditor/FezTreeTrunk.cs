using Rained.LevelData;
using System.Numerics;
namespace Rained.EditorGui.Editors;

readonly struct FezTreeTrunk
{
    public readonly Prop OriginProp;
    
    public ref Vector2 Position { get => ref OriginProp.FezTree!.TrunkPosition; }
    public ref float Angle { get => ref OriginProp.FezTree!.TrunkAngle; }

    public FezTreeTrunk(Prop prop)
    {
        OriginProp = prop;
    }
}