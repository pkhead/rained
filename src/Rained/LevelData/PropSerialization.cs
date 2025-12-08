namespace Rained.LevelData;

using System.Diagnostics;
using System.Numerics;
using System.Text;
using Rained.Assets;

/// <summary>
/// Binary serialization of props, used for the clipboard.
/// </summary>
static class PropSerialization
{
    public static byte[] SerializeProps(Prop[] props)
    {
        ArgumentNullException.ThrowIfNull(props);

        // collect names of all props in collection
        List<string> propNames = [];
        Dictionary<PropInit, uint> propIndices = [];

        foreach (var prop in props)
        {
            var name = prop.PropInit.Name;
            if (!propIndices.ContainsKey(prop.PropInit))
            {
                propIndices.Add(prop.PropInit, (uint)propNames.Count);
                propNames.Add(name);
            }
        }

        // write the serialized data
        using var stream = new MemoryStream(1 + sizeof(uint) * 3);
        using var writer = new BinaryWriter(stream);

        // version number
        // 0: initial version
        // 1: fez tree support
        writer.Write((byte)1);

        // write prop type table
        writer.Write((uint)propNames.Count);
        foreach (var name in propNames)
        {
            var nameBytes = Encoding.UTF8.GetBytes(name);
            var byteCount = Math.Min(byte.MaxValue, nameBytes.Length);
            writer.Write((byte)byteCount);
            writer.Write(nameBytes, 0, byteCount);
        }

        using var tmpStream = new MemoryStream(1024);
        using var tmpWriter = new BinaryWriter(tmpStream);

        // number of props
        writer.Write((uint)props.Length);
        foreach (var prop in props)
        {
            // prop type
            tmpWriter.Write(propIndices[prop.PropInit]);

            // properties
            tmpWriter.Write((byte)prop.DepthOffset);
            tmpWriter.Write((byte)prop.CustomDepth);
            tmpWriter.Write((byte)prop.CustomColor);
            tmpWriter.Write((short)prop.RenderOrder);
            tmpWriter.Write((byte)prop.Variation);
            tmpWriter.Write((ushort)prop.Seed);
            tmpWriter.Write((byte)prop.RenderTime);
            tmpWriter.Write((byte)(prop.ApplyColor ? 1 : 0));

            // transform
            var transform = prop.Transform;
            tmpWriter.Write((byte)(transform.isAffine ? 1 : 0));

            // rect
            if (transform.isAffine)
            {
                tmpWriter.Write(transform.rect.Center.X);
                tmpWriter.Write(transform.rect.Center.Y);
                tmpWriter.Write(transform.rect.Size.X);
                tmpWriter.Write(transform.rect.Size.Y);
                tmpWriter.Write(transform.rect.Rotation);
            }
            else // quad
            {
                for (int i = 0; i < 4; i++)
                {
                    tmpWriter.Write(transform.quad[i].X);
                    tmpWriter.Write(transform.quad[i].Y);
                }
            }

            // rope data?
            var rope = prop.Rope;
            var ropeModel = rope?.Model;
            tmpWriter.Write((byte)(ropeModel is not null ? 1 : 0));

            if (ropeModel is not null)
            {
                Debug.Assert(rope is not null);
                tmpWriter.Write(rope.Thickness);
                tmpWriter.Write((byte)rope.ReleaseMode);

                // ends of the rope
                tmpWriter.Write(ropeModel.PointA.X);
                tmpWriter.Write(ropeModel.PointA.Y);
                tmpWriter.Write(ropeModel.PointB.X);
                tmpWriter.Write(ropeModel.PointB.Y);

                // rope vertices
                tmpWriter.Write((uint)ropeModel.SegmentCount);
                for (int i = 0; i < ropeModel.SegmentCount; i++)
                {
                    var pos = ropeModel.GetSegmentPos(i);
                    var vel = ropeModel.GetSegmentVel(i);

                    tmpWriter.Write(pos.X);
                    tmpWriter.Write(pos.Y);
                    tmpWriter.Write(vel.X);
                    tmpWriter.Write(vel.Y);
                }
            }

            // fez tree data
            var fezTree = prop.FezTree;
            tmpWriter.Write((byte)(fezTree is not null ? 1 : 0));
            if (fezTree is not null)
            {
                tmpWriter.Write((byte)fezTree.EffectColor);
                tmpWriter.Write((float)fezTree.LeafDensity);
                tmpWriter.Write((float)fezTree.TrunkPosition.X);
                tmpWriter.Write((float)fezTree.TrunkPosition.Y);
                tmpWriter.Write((float)fezTree.TrunkAngle);
            }

            // then, write size of data + data itself to main stream
            writer.Write((uint)tmpStream.Length);
            writer.Write(tmpStream.ToArray());
            tmpStream.SetLength(0);
            tmpStream.Position = 0;
        }

        return stream.ToArray();
    }

    public static Prop[]? DeserializeProps(ReadOnlySpan<byte> data)
    {
        using var stream = new MemoryStream(data.ToArray());
        using var reader = new BinaryReader(stream);
        var propDb = RainEd.Instance.PropDatabase;

        var version = reader.ReadByte();
        if (version < 0 || version > 1)
        {
            Log.Error("DeserializeProps: Invalid version?");
            return null;
        }

        // read prop type table
        var numPropTypes = reader.ReadInt32();
        PropInit?[] propInits = new PropInit[numPropTypes];
        for (int i = 0; i < numPropTypes; i++)
        {            
            var strLen = (int)reader.ReadByte();
            var name = Encoding.UTF8.GetString(reader.ReadBytes(strLen));

            if (propDb.TryGetPropFromName(name, out var init))
            {
                propInits[i] = init;
            }
            else
            {
                propInits[i] = null!;
                Log.UserLogger.Error("Unrecognized prop " + name);
            }
        }

        // read props
        var numProps = reader.ReadInt32();
        List<Prop> props = new List<Prop>(numProps);

        for (int i = 0; i < numProps; i++)
        {
            var dataLen = reader.ReadInt32();
            var propTypeId = reader.ReadInt32();
            var propInit = propInits[propTypeId];

            // prop init not recognized - could not load prop. skip it.
            if (propInit is null)
            {
                stream.Seek(dataLen - 4, SeekOrigin.Current);
                continue;
            }

            var prop = new Prop(propInit, Vector2.Zero, Vector2.One);
            props.Add(prop);

            // load properties
            prop.DepthOffset = reader.ReadByte();
            prop.CustomDepth = reader.ReadByte();
            prop.CustomColor = reader.ReadByte();
            prop.RenderOrder = reader.ReadInt16();
            prop.Variation = reader.ReadByte();
            prop.Seed = reader.ReadInt16();
            prop.RenderTime = (PropRenderTime) reader.ReadByte();
            prop.ApplyColor = reader.ReadByte() != 0;

            // read transform
            prop.Transform.isAffine = reader.ReadByte() != 0;
            if (prop.Transform.isAffine)
            {
                ref var rect = ref prop.Transform.rect;
                rect.Center.X = reader.ReadSingle();
                rect.Center.Y = reader.ReadSingle();
                rect.Size.X = reader.ReadSingle();
                rect.Size.Y = reader.ReadSingle();
                rect.Rotation = reader.ReadSingle();
            }
            else
            {
                for (int j = 0; j < 4; j++)
                {
                    prop.Transform.quad[j].X = reader.ReadSingle();
                    prop.Transform.quad[j].Y = reader.ReadSingle();
                }
            }

            // rope data?
            var hasRope = reader.ReadByte() != 0;
            if (hasRope)
            {
                Debug.Assert(prop.Rope is not null);
                prop.Rope.Thickness = reader.ReadSingle();
                prop.Rope.ReleaseMode = (RopeReleaseMode) reader.ReadByte();

                // ends of the rope
                prop.Rope.PointA.X = reader.ReadSingle();
                prop.Rope.PointA.Y = reader.ReadSingle();
                prop.Rope.PointB.X = reader.ReadSingle();
                prop.Rope.PointB.Y = reader.ReadSingle();

                // TODO: LoadPoints cannot take velocity values...
                var segCount = reader.ReadInt32();
                Vector2[] segments = new Vector2[segCount];
                for (int j = 0; j < segCount; j++)
                {
                    var px = reader.ReadSingle();
                    var py = reader.ReadSingle();
                    var vx = reader.ReadSingle();
                    var vy = reader.ReadSingle();

                    segments[j] = new Vector2(px, py);
                }

                prop.Rope.LoadPoints(segments);
            }

            // fez tree data?
            if (version >= 1)
            {
                var hasFezTree = reader.ReadByte() != 0;
                if (hasFezTree)
                {
                    Debug.Assert(prop.FezTree is not null);

                    prop.FezTree.EffectColor = (PropFezTreeEffectColor) reader.ReadByte();
                    prop.FezTree.LeafDensity = reader.ReadSingle();
                    prop.FezTree.TrunkPosition.X = reader.ReadSingle();
                    prop.FezTree.TrunkPosition.Y = reader.ReadSingle();
                    prop.FezTree.TrunkAngle = reader.ReadSingle();
                }
            }
        }

        return [.. props];
    }
}