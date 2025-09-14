using System.Text;

namespace Rained.EditorGui;

/// <summary>
/// Cache of the subdirectories and files located within a directory. Used by
/// DirectoryTreeView so that I/O operations and sorting doesn't need to be
/// called per-frame.
/// </summary>
class DirectoryTreeCache
{
    public readonly string BaseDirectory;
    public string FileFilter;

    private readonly Dictionary<string, DirectoryListing> directoryItems = [];

    public DirectoryTreeCache(string dirPath, string? fileFilter)
    {
        BaseDirectory = dirPath;
        FileFilter = fileFilter ?? "*";

        Refresh();
    }

    public void Refresh()
    {
        directoryItems.Clear();
        RefreshDirectoryList("/", true);
    }

    public void MoveFile(string oldPath, string newPath)
    {
        oldPath = NormalizePath(oldPath);
        newPath = NormalizePath(newPath);
        if (oldPath == newPath) return;

        File.Move(ConvertToRealPath(oldPath), ConvertToRealPath(newPath));
        var oldParent = NormalizePath(Join(oldPath, ".."));
        var newParent = NormalizePath(Join(newPath, ".."));

        RefreshDirectoryList(oldParent, false);

        if (oldParent != newParent)
            RefreshDirectoryList(newParent, false);
    }

    public void MoveDirectory(string oldPath, string newPath)
    {
        oldPath = NormalizePath(oldPath);
        newPath = NormalizePath(newPath);
        if (oldPath == newPath) return;

        Directory.Move(ConvertToRealPath(oldPath), ConvertToRealPath(newPath));
        var oldParent = NormalizePath(Join(oldPath, ".."));
        var newParent = NormalizePath(Join(newPath, ".."));

        RemoveDirectoryFromIndex(oldPath);
        RefreshDirectoryList(newPath, true);

        RefreshDirectoryList(oldParent, false);

        if (oldParent != newParent)
            RefreshDirectoryList(newParent, false);
    }

    public void DeleteFile(string path)
    {
        Platform.TrashFile(ConvertToRealPath(path));
        var parent = NormalizePath(Join(path, ".."));
        RefreshDirectoryList(parent, false);
    }

    public void DeleteDirectory(string path)
    {
        Platform.TrashDirectory(ConvertToRealPath(path));
        var parent = NormalizePath(Join(path, ".."));
        RefreshDirectoryList(parent, false);
        RemoveDirectoryFromIndex(NormalizePath(path));
    }

    // assumes path is already normalized
    private void RemoveDirectoryFromIndex(string path)
    {
        foreach (var name in GetDirectories(path))
        {
            RemoveDirectoryFromIndex(Join(path, name));
        }

        directoryItems.Remove(path);
    }

    public void RefreshDirectoryList(string dirPath, bool recursive)
    {
        var realPath = ConvertToRealPath(dirPath);

        List<string> directories = [];
        List<string> files = [];

        foreach (var path in Directory.EnumerateDirectories(realPath))
        {
            directories.Add(Path.GetFileName(path));
            if (recursive)
                RefreshDirectoryList(ConvertToVirtualPath(path)!, true);
        }

        foreach (var path in Directory.EnumerateFiles(realPath, FileFilter))
        {
            files.Add(Path.GetFileName(path));
        }

        directories.Sort();
        files.Sort();

        directoryItems[dirPath] = new DirectoryListing(
            directories: [.. directories],
            files: [.. files]
        );
    }

    public string[] GetFiles(string path)
    {
        path = NormalizePath(path);
        if (!directoryItems.TryGetValue(path, out var list))
            throw new ArgumentException($"Virtual directory \"{path}\" does not exist", nameof(path));

        return list.Files;
    }

    public string[] GetDirectories(string path)
    {
        path = NormalizePath(path);
        if (!directoryItems.TryGetValue(path, out var list))
            throw new ArgumentException($"Virtual directory \"{path}\" does not exist", nameof(path));

        return list.Directories;
    }

    public static string Join(params string[] paths)
    {
        if (paths.Length == 0) return "";

        if (paths[0] == "/")
        {
            return "/" + string.Join('/', paths, 1, paths.Length - 1);
        }
        else
        {
            return string.Join('/', paths);
        }
    }

    public static string NormalizePath(string virtualPath)
    {
        List<string> stack = [];
        foreach (var v in virtualPath.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (v == ".")
            {
                continue;
            }
            else if (v == "..")
            {
                if (stack.Count > 0)
                    stack.RemoveAt(stack.Count - 1);
            }
            else
            {
                stack.Add(v);
            }
        }

        return "/" + string.Join('/', [.. stack]);
    }

    public string ConvertToRealPath(string virtualPath)
    {
        return Path.Combine([BaseDirectory, .. virtualPath.Split('/', StringSplitOptions.RemoveEmptyEntries)]);
    }

    public string? ConvertToVirtualPath(string realPath)
    {
        realPath = Path.GetFullPath(realPath);
        string relPath = Path.GetRelativePath(BaseDirectory, realPath);
        if (relPath == realPath)
            return null;

        if (relPath == ".")
            return "/";

        return "/" + relPath.Replace(Path.DirectorySeparatorChar, '/');
    }

    class DirectoryListing(string[] directories, string[] files)
    {
        public string[] Directories = directories;
        public string[] Files = files;
    }
}