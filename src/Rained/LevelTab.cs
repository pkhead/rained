using System.Numerics;
namespace RainEd;

class LevelTab
{
    public string FilePath;
    public string Name;
    public Level Level;
    public readonly ChangeHistory.ChangeHistory ChangeHistory;

    public Vector2 ViewOffset = new();
    public float ViewZoom = 1f;
    public float ZoomSteps = 0;
    public int WorkLayer = 0;

    public LevelTab()
    {
        FilePath = string.Empty;
        Name = "Unnamed";
        Level = Level.NewDefaultLevel();
        ChangeHistory = new ChangeHistory.ChangeHistory();
    }

    public LevelTab(Level level, string filePath = "")
    {
        FilePath = filePath;
        Name = string.IsNullOrWhiteSpace(filePath) ? "Unnamed" : Path.GetFileNameWithoutExtension(filePath);
        Level = level;
        ChangeHistory = new ChangeHistory.ChangeHistory();

        if (FilePath is not null && Path.GetDirectoryName(FilePath) == RainEd.EmergencySaveFolder)
        {
            Name += " [EMERGEMCY SAVE]";
            /*int hyphenIndex = levelName.LastIndexOf('-');
            if (hyphenIndex >= 0)
            {
                levelName = levelName[0..hyphenIndex] + " [EMERGENCY SAVE]";
            }
            else
            {
                levelName += " [EMERGENCY SAVE]";
            }*/
        }
    }
}