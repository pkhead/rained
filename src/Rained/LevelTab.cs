using System.Numerics;
using Rained.EditorGui.Editors.CellEditing;
using Rained.LevelData;
namespace Rained;

class LevelTab : IDisposable
{
    public string FilePath;
    public string Name;
    public Level Level;
    public LevelNodeData NodeData;
    public readonly ChangeHistory.ChangeHistory ChangeHistory;
    public CellSelection? CellSelection = null;
    
    /// <summary>
    /// True if the file for the current level is non-existent or is an emergency save.
    /// </summary>
    public bool IsTemporaryFile => string.IsNullOrWhiteSpace(FilePath) || Path.GetDirectoryName(FilePath) == RainEd.EmergencySaveFolder;

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
        NodeData = new LevelNodeData(Level);
    }

    public LevelTab(Level level, string filePath = "")
    {
        FilePath = filePath;
        Name = string.IsNullOrWhiteSpace(filePath) ? "Unnamed" : Path.GetFileNameWithoutExtension(filePath);
        Level = level;
        ChangeHistory = new ChangeHistory.ChangeHistory();
        NodeData = new LevelNodeData(Level);

        if (FilePath is not null && Path.GetDirectoryName(FilePath) == RainEd.EmergencySaveFolder)
        {
            // emergency save file names are formatted as [name]-[date]
            // i only want to display [name] and the text [EMERGENCY SAVE]
            int hyphenIndex = Name.LastIndexOf('-');
            if (hyphenIndex >= 0)
            {
                Name = Name[0..hyphenIndex] + " [EMERGENCY SAVE]";
            }
            else
            {
                Name += " [EMERGENCY SAVE]";
            }
        }
    }

    public void Dispose()
    {
        Level.Dispose();
    }
}