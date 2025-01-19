using System.Runtime.CompilerServices;
using System.Text;

namespace Rained.LevelData;

/// <summary>
/// Binary serialization of a cell array.
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

    public static unsafe byte[] SerializeCells(int width, int height, (bool mask, LevelCell cell)[,,] geometry)
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

        // version number, width, height
        writer.Write((byte)0);
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
}