using System.Text;
namespace RainEd.EditorGui;

partial class FileBrowser
{
    private record FileFilter
    {
        public string FilterName;
        public string FullText;
        public string[] AllowedExtensions;
        public Func<string, bool, bool>? FilterCallback;

        public FileFilter(string name, string[] extensions, Func<string, bool, bool>? filterCallback = null)
        {
            FilterName = name;
            AllowedExtensions = extensions;
            FilterCallback = filterCallback;

            var strBuilder = new StringBuilder();
            strBuilder.Append(name);
            strBuilder.Append(" (");
            
            for (int i = 0; i < extensions.Length; i++)
            {
                var ext = extensions[i];
                if (i > 0) strBuilder.Append(", ");
                strBuilder.Append('*');
                strBuilder.Append(ext);
            }

            strBuilder.Append(')');
            FullText = strBuilder.ToString();
        }

        // the isRw parameter -- if teh SetPath function had identified it as a
        // Rain World level file
        public bool Match(string fileName, bool isRw)
        {
            foreach (var ext in AllowedExtensions)
            {
                if (ext == ".*") return true;
            }

            var pathExt = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(pathExt)) return false;
            
            foreach (var ext in AllowedExtensions)
            {
                if (pathExt == ext)
                {
                    if (FilterCallback is null) return true;
                    else if (FilterCallback(fileName, isRw))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public string Enforce(string fileName)
        {
            if (AllowedExtensions[0] != ".*" && Path.GetExtension(fileName) != AllowedExtensions[0])
                return fileName + AllowedExtensions[0];

            return fileName;
        }
    }

    public void AddFilterWithCallback(string filterName, Func<string, bool, bool>? callback = null, params string[] allowedExtensions)
    {
        fileFilters.Add(new FileFilter(filterName, allowedExtensions, callback));

        // default filter is the first filter added, else "Any"
        selectedFilter = fileFilters[1];
    }

    public void AddFilter(string filterName, params string[] allowedExtensions)
        => AddFilterWithCallback(filterName, null, allowedExtensions);

    private void RefreshFilter()
    {
        filteredEntries.Clear();

        if (mode == OpenMode.Directory)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                if (entry.Name[0] == '.') continue; // hidden files/folders
                if (entry.Type == EntryType.Directory)
                {
                    filteredEntries.Add((i, entry));
                }
            }    
        }
        else
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                if (entry.Name[0] == '.') continue; // hidden files/folders
                if (entry.Type == EntryType.Directory || selectedFilter.Match(Path.Combine(cwd, entry.Name), entry.IconIndex == 7))
                {
                    filteredEntries.Add((i, entry));
                }
            }
        }
    }
}