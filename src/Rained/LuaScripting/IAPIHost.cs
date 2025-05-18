using Rained.EditorGui;
using Rained.LevelData;

namespace Rained.LuaScripting;

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

    public Level Level { get; }
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
        return RainEd.Instance.LoadLevelThrow(filePath);
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

    public Level Level => RainEd.Instance.CurrentTab?.Level ?? throw new NoLevelException("No level document is active!");
}

class APIBatchHost : IAPIHost
{
    public bool IsGui { get => false; }

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

    class Document : IDisposable
    {
        public Level Level;
        public string Name;
        public string? FilePath;

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

    public Level Level => _documents[ActiveDocument].Level;
}