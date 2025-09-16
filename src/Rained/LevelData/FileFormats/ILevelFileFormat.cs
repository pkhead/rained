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

interface ILevelFileFormat
{
    LevelLoadResult Load(string path, LevelSerializationParams? hostData = null);
    public LevelSaveResult Save(Level level, string path, LevelSerializationParams? hostData = null);
}