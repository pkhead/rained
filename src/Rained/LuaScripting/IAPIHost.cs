using Rained.Assets;
using Rained.EditorGui;
using Rained.EditorGui.Editors;
using Rained.LevelData;

namespace Rained.LuaScripting;

enum CellDirtyFlags
{
    Geometry = 1,
    Objects = 2,
    Material = 4,
    TileHead = 8
};

interface IAPIHost
{
    public bool IsGui { get; }
    public void Alert(string msg);

    public void Print(string msg);
    public void Warn(string msg);
    public void Error(string msg);

    public int DocumentCount { get; }
    public int ActiveDocument { get; set; }
    public string GetDocumentName(int index);
    public string? GetDocumentFilePath(int index);
    public void CloseDocument(int index);

    public LevelLoadResult OpenLevel(string filePath);
    public void NewLevel(int width, int height, string? filePath);
    public bool AsyncSaveActiveDocument(EditorWindow.AsyncSaveCallback? callback, string? overridePath);

    public void AddAutotile(Rained.Autotiles.Autotile autotile, string category);
    public void RemoveAutotile(Rained.Autotiles.Autotile autotile);
    public int RegisterCommand(RainEd.CommandCreationParameters cmdInit);
    public void UnregisterCommand(int cmdId);

    public void InvalidateCell(int x, int y, int layer, CellDirtyFlags flags);
    public void ResizeLevel(int newWidth, int newHeight, int anchorX, int anchorY);

    public int SelectedEffect { get; set; }
    public List<Prop> SelectedProps { get; }

    public Level Level { get; }
    public MaterialDatabase MaterialDatabase { get; }
    public TileDatabase TileDatabase { get; }
    public EffectsDatabase EffectsDatabase { get; }
    public PropDatabase PropDatabase { get; }
}

class APIGuiHost : IAPIHost
{
    public bool IsGui { get => true; }
    public void Alert(string msg)
    {
        EditorWindow.ShowNotification(msg);
    }

    public void Print(string msg)
    {
        Log.UserLogger.Information("[lua] " + msg);
    }
    public void Warn(string msg)
    {
        Log.UserLogger.Warning("[lua] " + msg);
    }
    public void Error(string msg)
    {
        Log.UserLogger.Error("[lua] " + msg);
    }

    public int DocumentCount { get => RainEd.Instance.Tabs.Count; }
    public int ActiveDocument
    {
        get
        {
            if (RainEd.Instance.CurrentTab is not null)
                return RainEd.Instance.Tabs.IndexOf(RainEd.Instance.CurrentTab);
            else
                return -1;
        }
        set
        {
            RainEd.Instance.CurrentTab = RainEd.Instance.Tabs[value];
        }
    }

    public string GetDocumentName(int index)
    {
        return RainEd.Instance.Tabs[index].Name;
    }

    public string GetDocumentFilePath(int index)
    {
        return RainEd.Instance.Tabs[index].FilePath;
    }

    public void CloseDocument(int index)
    {
        RainEd.Instance.CloseTab(RainEd.Instance.Tabs[index]);
    }

    public LevelLoadResult OpenLevel(string filePath)
    {
        return RainEd.Instance.LoadLevelThrow(filePath, showLevelLoadFailPopup: false);
    }

    public void NewLevel(int width, int height, string? filePath)
    {
        RainEd.Instance.OpenLevel(new LevelData.Level(width, height), filePath ?? "");
    }

    public bool AsyncSaveActiveDocument(EditorWindow.AsyncSaveCallback? callback, string? overridePath)
    {
        return EditorWindow.AsyncSave(callback, overridePath);
    }

    public void AddAutotile(Rained.Autotiles.Autotile autotile, string category)
    {
        RainEd.Instance.Autotiles.AddAutotile(autotile, category);
    }
    public void RemoveAutotile(Rained.Autotiles.Autotile autotile)
    {
        RainEd.Instance.Autotiles.RemoveAutotile(autotile);
    }

    public int RegisterCommand(RainEd.CommandCreationParameters cmdInit) =>
        RainEd.Instance.RegisterCommand(cmdInit);

    public void UnregisterCommand(int id) =>
        RainEd.Instance.UnregisterCommand(id);

    public void InvalidateCell(int x, int y, int layer, CellDirtyFlags flags)
    {
        // don't invalidate objects if geometry is invalidated,
        // because internally the geometry invalidator automatically
        // calls the object invalidator as appropraite
        if (flags.HasFlag(CellDirtyFlags.Geometry))
        {
            RainEd.Instance.LevelView.InvalidateGeo(x, y, layer);
        }
        else if (flags.HasFlag(CellDirtyFlags.Objects))
        {
            if (layer == 0) RainEd.Instance.CurrentTab!.NodeData.InvalidateCell(x, y);
        }

        if (flags.HasFlag(CellDirtyFlags.TileHead))
        {
            RainEd.Instance.LevelView.Renderer.InvalidateTileHead(x, y, layer);
        }
    }

    public void InvalidateCell(int x, int y, int layer)
    {
        
        if (layer == 0) RainEd.Instance.CurrentTab!.NodeData.InvalidateCell(x, y);
    }

    public void ResizeLevel(int newWidth, int newHeight, int anchorX, int anchorY)
    {
        RainEd.Instance.ResizeLevel(newWidth, newHeight, anchorX, anchorY);
    }

    public Level Level => RainEd.Instance.CurrentTab?.Level ?? throw new NoLevelException("No level document is active!");
    public MaterialDatabase MaterialDatabase => RainEd.Instance.MaterialDatabase;
    public TileDatabase TileDatabase => RainEd.Instance.TileDatabase;
    public EffectsDatabase EffectsDatabase => RainEd.Instance.EffectsDatabase;
    public PropDatabase PropDatabase => RainEd.Instance.PropDatabase;
    public int SelectedEffect {
        get => RainEd.Instance.LevelView.GetEditor<EffectsEditor>().SelectedEffect;
        set => RainEd.Instance.LevelView.GetEditor<EffectsEditor>().SelectedEffect = value;
    }
    public List<Prop> SelectedProps => RainEd.Instance.LevelView.GetEditor<PropEditor>().SelectedProps;
}

class APIBatchHost : IAPIHost
{
    public bool IsGui { get => false; }

    class Document : IDisposable
    {
        public Level Level;
        public string Name;
        public string? FilePath;

        public int SelectedEffect = -1;
        public List<Prop> SelectedProps = [];

        public Document(Level level, string? filePath)
        {
            this.Level = level;

            if (filePath is not null)
            {
                Name = Path.GetFileNameWithoutExtension(filePath);
                FilePath = filePath;
            }
            else
            {
                Name = "Unnamed";
            }
        }

        public void Dispose()
        {
            Level.Dispose();
        }
    }

    private List<Document> _documents = [];
    private Stack<Document> _documentOpenStack = [];
    private int _activeDocument = -1;

    private readonly List<RainEd.Command> _commands = [];

    public int DocumentCount => _documents.Count;
    public int ActiveDocument
    {
        get => _activeDocument;
        set
        {
            if (_activeDocument != value)
            {
                _documentOpenStack.Push(_documents[_activeDocument]);
                _activeDocument = value;
            }
        }
    }

    public void Print(string msg)
    {
        Log.Information("[lua] " + msg);
        Console.WriteLine(msg);
    }

    public void Warn(string msg)
    {
        Log.Warning("[lua] " + msg);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(msg);
        Console.ResetColor();
    }
    public void Error(string msg)
    {
        Log.Error("[lua] " + msg);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(msg);
        Console.ResetColor();
    }

    public void Alert(string msg) => Print(msg);

    public string GetDocumentName(int index)
    {
        return _documents[index].Name;
    }

    public string? GetDocumentFilePath(int index)
    {
        return _documents[index].FilePath;
    }

    public void CloseDocument(int index)
    {
        _documents.RemoveAt(index);

        do
        {
            if (_documentOpenStack.Count == 0)
            {
                _activeDocument = -1;
                break;
            }
            
            _activeDocument = _documents.IndexOf(_documentOpenStack.Pop());
        } while (_activeDocument != -1);
    }

    public LevelLoadResult OpenLevel(string filePath)
    {
        var loadRes = LevelSerialization.Load(filePath);
        var tab = new Document(loadRes.Level, filePath);
        _documents.Add(tab);
        ActiveDocument = _documents.Count - 1;
        return loadRes;
    }

    public void NewLevel(int width, int height, string? filePath)
    {
        var tab = new Document(new Level(width, height), filePath);
        _documents.Add(tab);
        ActiveDocument = _documents.Count - 1;
    }

    public bool AsyncSaveActiveDocument(EditorWindow.AsyncSaveCallback? callback, string? overridePath)
    {
        var doc = _documents[ActiveDocument];
        var path = overridePath ?? doc.FilePath;
        if (path is null)
            throw new Exception("cannot save a document with no path in batch mode");
        
        LevelSerialization.SaveLevelTextFile(Level, path);
        LevelSerialization.SaveLevelLightMap(Level, path);

        if (doc.FilePath != overridePath)
        {
            doc.FilePath = overridePath;
            doc.Name = Path.GetFileNameWithoutExtension(overridePath)!;
        }

        return true;
    }

    public void AddAutotile(Rained.Autotiles.Autotile autotile, string category)
    {
        // no-op
    }
    public void RemoveAutotile(Rained.Autotiles.Autotile autotile)
    {
        // no-op
    }

    public void InvalidateGeo(int x, int y, int layer)
    {
        RainEd.Instance.LevelView.InvalidateGeo(x, y, layer);
        if (layer == 0) RainEd.Instance.CurrentTab!.NodeData.InvalidateCell(x, y);
    }

    public int RegisterCommand(RainEd.CommandCreationParameters cmdInit)
    {
        var cmd = new RainEd.Command(cmdInit);
        _commands.Add(cmd);
        return cmd.ID;
    }

    public void UnregisterCommand(int id)
    {
        var cmd = _commands.Find(x => x.ID == id);
        if (cmd is not null) _commands.Remove(cmd);
    }

    /// <summary>
    /// Invoke a command. Returns true if command was found, false if not.
    /// </summary>
    public bool InvokeCommand(string name)
    {
        var cmd = _commands.Find(x => x.Name == name);
        if (cmd is not null)
        {
            cmd.Callback(cmd.ID);
            return true;
        }
        else
        {
            return false;
        }
    }

    public void InvalidateCell(int x, int y, int layer, CellDirtyFlags flags)
    {
        // no-op
    }

    public void ResizeLevel(int newWidth, int newHeight, int anchorX, int anchorY)
    {
        Level.Resize(newWidth, newHeight, anchorX, anchorY);
    }

    public Level Level => _documents[ActiveDocument].Level;
    public MaterialDatabase MaterialDatabase => RainEd.Instance.MaterialDatabase;
    public TileDatabase TileDatabase => RainEd.Instance.TileDatabase;
    public EffectsDatabase EffectsDatabase => RainEd.Instance.EffectsDatabase;
    public PropDatabase PropDatabase => RainEd.Instance.PropDatabase;
    public int SelectedEffect
    {
        get => _documents[_activeDocument].SelectedEffect;
        set => _documents[_activeDocument].SelectedEffect = value;
    }
    public List<Prop> SelectedProps => _documents[_activeDocument].SelectedProps;
}