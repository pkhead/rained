using System.Runtime.CompilerServices;
using System.Text;
using Rained.EditorGui.Editors.CellEditing;

namespace Rained.LevelData;

/// <summary>
/// Binary serialization of a cell array, used for the clipboard.
/// </summary>
static class CellSerialization
{
    record struct SerializedLevelCell
    {
        public GeoType Geo;
        public LevelObject Objects;
        public ushort Material;

        // X position of the tile root, -1 if there is no tile here
        public int TileRootX;

        // Y position of the tile root, -1 if there is no tile here
        public int TileRootY;
        // Layer of tile root, -1 if there is no tile here
        public int TileLayer;
        public uint TileIndex;

        public SerializedLevelCell()
        {
            Geo = GeoType.Air;
            Objects = 0;
            Material = 0;
            TileRootX = -1;
            TileRootY = -1;
            TileLayer = -1;
            TileIndex = 0;
        }

        public SerializedLevelCell(ref readonly LevelCell cell)
        {
            Geo = cell.Geo;
            Objects = cell.Objects;
            Material = (ushort) cell.Material;
            TileRootX = cell.TileRootX;
            TileRootY = cell.TileRootY;
            TileLayer = cell.TileLayer;
            TileIndex = 0;
        }
    }

    public static unsafe byte[] SerializeCells(int origX, int origY, int width, int height, MaskedCell[,,] geometry)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than 0.");
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than 0.");
        if (geometry.GetLength(0) != Level.LayerCount || geometry.GetLength(1) != width || geometry.GetLength(2) != height)
            throw new ArgumentException("Invalid dimensions for geometry array.", nameof(geometry));
        ArgumentNullException.ThrowIfNull(geometry);

        // collect names of all tiles in collection
        List<string> tileNames = [];
        Dictionary<string, uint> tileIndices = [];

        for (int l = 0; l < Level.LayerCount; l++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (geometry[l,x,y].cell.TileHead is not null)
                    {
                        var tileName = geometry[l,x,y].cell.TileHead!.Name;
                        if (!tileIndices.ContainsKey(tileName))
                        {
                            tileIndices.Add(tileName, (uint)tileNames.Count);
                            tileNames.Add(tileName);
                        }
                    }
                }
            }
        }

        // serialize data
        var stream = new MemoryStream(1 + sizeof(uint) * 3);
        var writer = new BinaryWriter(stream);

        // version number, pos and size
        writer.Write((byte)0);
        writer.Write(origX);
        writer.Write(origY);
        writer.Write((uint)width);
        writer.Write((uint)height);

        // tile database
        writer.Write((uint)tileNames.Count);
        foreach (var name in tileNames)
        {
            var nameBytes = Encoding.UTF8.GetBytes(name);
            var byteCount = Math.Min(byte.MaxValue, nameBytes.Length);
            writer.Write((byte)byteCount);
            writer.Write(nameBytes, 0, byteCount);
        }
        
        // cell data
        // organized layer => x => y, just like the level save format
        for (int l = 0; l < Level.LayerCount; l++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    ref var srcCell = ref geometry[l,x,y].cell;
                    var sc = new SerializedLevelCell(ref srcCell);

                    if (srcCell.TileHead is not null)
                        sc.TileIndex = tileIndices[srcCell.TileHead.Name] + 1;
                    
                    ReadOnlySpan<byte> scDat = new(&sc, Unsafe.SizeOf<SerializedLevelCell>());
                    writer.Write(geometry[l,x,y].mask);
                    writer.Write(scDat);
                }
            }
        }

        return stream.ToArray();
    }

    public static unsafe MaskedCell[,,]? DeserializeCells(ReadOnlySpan<byte> data, out int origX, out int origY, out int width, out int height)
    {
        origX = 0;
        origY = 0;
        width = 0;
        height = 0;

        using var reader = new BinaryReader(new MemoryStream(data.ToArray()));
        
        var version = reader.ReadByte();
        if (version != 0)
        {
            Log.Error("DeserializeCells: Invalid version?");
            return null;
        }

        origX = reader.ReadInt32();
        origY = reader.ReadInt32();
        width = reader.ReadInt32();
        height = reader.ReadInt32();

        // read tile database
        var tileNames = new string[reader.ReadUInt32()];
        var tileDb = RainEd.Instance.TileDatabase;
        for (int i = 0; i < tileNames.Length; i++)
        {
            var strLen = (int) reader.ReadByte();
            tileNames[i] = Encoding.UTF8.GetString(reader.ReadBytes(strLen));
        }

        // cell data
        // organized layer => x => y, just like the level save format
        var geometry = new MaskedCell[Level.LayerCount, width, height];
        for (int l = 0; l < Level.LayerCount; l++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // get cell mask
                    bool mask = reader.ReadBoolean();

                    // get cell data
                    var sc = new SerializedLevelCell();
                    Span<byte> scDat = new(&sc, Unsafe.SizeOf<SerializedLevelCell>());
                    reader.Read(scDat);

                    // convert to actual LevelCell
                    var cell = new LevelCell
                    {
                        Geo = sc.Geo,
                        Objects = sc.Objects,
                        Material = sc.Material,
                        TileRootX = sc.TileRootX,
                        TileRootY = sc.TileRootY,
                        TileLayer = sc.TileLayer
                    };

                    if (sc.TileIndex > 0)
                        cell.TileHead = tileDb.GetTileFromName(tileNames[sc.TileIndex-1]);

                    geometry[l,x,y] = new MaskedCell(mask, cell);
                }
            }
        }

        return geometry;
    }
}