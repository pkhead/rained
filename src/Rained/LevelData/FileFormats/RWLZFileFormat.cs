using System.IO.Compression;
using Raylib_cs;

namespace Rained.LevelData.FileFormats;

class RWLZFileFormat : ILevelFileFormat
{
    private static void ScanArchive(ZipArchive archive, out ZipArchiveEntry? dataEntry, out ZipArchiveEntry? lightEntry)
    {
        dataEntry = null;
        lightEntry = null;

        foreach (var entry in archive.Entries)
        {
            // skip if entry isn't top-level
            if (!string.IsNullOrEmpty(Path.GetDirectoryName(entry.FullName))) continue;

            if (Path.GetExtension(entry.FullName) == ".txt" && dataEntry is null)
                dataEntry = entry;
            
            else if (Path.GetExtension(entry.FullName) == ".png" && lightEntry is null)
                lightEntry = entry;

            if (dataEntry is not null && lightEntry is not null)
                break;
        }
    }

    public LevelLoadResult Load(string path, LevelSerializationParams? hostData = null)
    {
        using var file = File.OpenRead(path);
        using var archive = new ZipArchive(file, ZipArchiveMode.Read);
        ScanArchive(archive, out var dataEntry, out var lightEntry);

        if (dataEntry is null)
            throw new InvalidDataException($"{path} does not have a valid .rwlz structure.");

        // read level data line-by-line
        List<string> levelData = [];
        {
            using var stream = dataEntry.Open();
            using var reader = new StreamReader(stream);

            while (true)
            {
                var line = reader.ReadLine();
                if (line is null) break;
                levelData.Add(line);
            }
        }

        // read lightmap image
        Image? image = null;
        if (lightEntry is not null)
        {
            using var stream = lightEntry.Open();
            var gimage = new Glib.Image(stream);

            image = new Image()
            {
                image = gimage
            };
        }

        try
        {
            return VanillaFileFormat.LoadRaw([.. levelData], image, hostData);
        }
        finally
        {
            if (image.HasValue)
                Raylib.UnloadImage(image.Value);
        }
    }

    public LevelSaveResult Save(Level level, string path, LevelSerializationParams? hostData = null)
    {
        using var file = File.OpenWrite(path);
        using var archive = new ZipArchive(file, ZipArchiveMode.Create);
        bool wroteLightMap = false;

        ZipArchiveEntry dataEntry = archive.CreateEntry("level.txt");

        // write level data
        {
            using var stream = dataEntry.Open();
            VanillaFileFormat.SaveLevelTextFile(level, stream, hostData);
        }

        // write light map data
        if (level.LightMap.IsLoaded)
        {
            ZipArchiveEntry lightEntry = archive.CreateEntry("level.png");
            using var stream = lightEntry.Open();

            wroteLightMap = VanillaFileFormat.SaveLevelLightMap(level, stream);
        }

        return new LevelSaveResult
        {
            WroteLightMap = wroteLightMap
        };
    }

    public LevelGeometryData? LoadGeometry(string path, CancellationToken? cancelToken)
    {
        using var file = File.OpenRead(path);
        using var archive = new ZipArchive(file, ZipArchiveMode.Read);
        ScanArchive(archive, out var dataEntry, out var lightEntry);

        if (dataEntry is null)
            throw new InvalidDataException($"{path} does not have a valid .rwlz structure.");

        // read level data line-by-line
        using var stream = dataEntry.Open();
        return VanillaFileFormat.LoadGeometryFromStream(stream, cancelToken);
    }

    public void ExportForDrizzle(string path, string tmpDir)
    {
        var levelName = Path.GetFileNameWithoutExtension(path);

        using var file = File.OpenRead(path);
        using var archive = new ZipArchive(file, ZipArchiveMode.Read);
        ScanArchive(archive, out var dataEntry, out var lightEntry);

        if (dataEntry is null)
            throw new InvalidDataException($"{path} does not have a valid .rwlz structure.");

        dataEntry.ExtractToFile(Path.Combine(tmpDir, levelName + ".txt"));
        lightEntry?.ExtractToFile(Path.Combine(tmpDir, levelName + ".png"));
    }
}