namespace Rained.LevelData.FileFormats;
using Rained.Assets;

record LevelLoadResult(Level Level)
{
    public Level Level = Level;
    public bool HadUnrecognizedAssets = false;
    public string[] UnrecognizedMaterials = [];
    public string[] UnrecognizedTiles = [];
    public string[] UnrecognizedProps = [];
    public string[] UnrecognizedEffects = [];
}

class LevelSerializationParams
{
    public required MaterialDatabase MaterialDatabase;
    public required TileDatabase TileDatabase;
    public required EffectsDatabase EffectsDatabase;
    public required PropDatabase PropDatabase;
    public int ActiveWorkLayer = 0;
}

struct LevelSaveResult
{
    public bool WroteLightMap;
}

class LevelGeometryData
{
    public record struct CellData(GeoType Geometry, LevelObject Objects);

    public required int Width;
    public required int Height;
    public required CellData[,,] Geometry;
}

interface ILevelFileFormat
{
    LevelLoadResult Load(string path, LevelSerializationParams? hostData = null);
    public LevelSaveResult Save(Level level, string path, LevelSerializationParams? hostData = null);

    // for the preview
    LevelGeometryData? LoadGeometry(string path, CancellationToken? cancelToken);
    void ExportForDrizzle(string path, string tmpDir);
}