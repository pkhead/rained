namespace RainEd;
using Raylib_cs;

class AssetGraphicsProvider
{
    private readonly Dictionary<string, RlManaged.Texture2D?> tileTexCache = [];
    private readonly Dictionary<string, RlManaged.Texture2D?> propTexCache = [];

    // tile previews are separate images...
    private readonly Dictionary<string, RlManaged.Texture2D?> previewTexCache = [];

    // Does Path.Combine(directory, query)
    // On Linux, it does extra processing to account for the fact that
    // it uses a case-sensitive filesystem.
    private static string GetFilePath(string directory, string fileName)
    {
        var combined = Path.Combine(directory, fileName);

        if ((OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD()) && !File.Exists(combined))
        {
            foreach (var filePath in Directory.GetFiles(directory))
            {
                if (string.Equals(fileName, Path.GetFileName(filePath), StringComparison.InvariantCultureIgnoreCase))
                {
                    combined = filePath;
                    break;
                }
            }
        }

        return combined;
    }

    /// <summary>
    /// Obtain the texture of a prop asset. May be cached.
    /// </summary>
    /// <param name="assetName">The name of the asset.</param>
    /// <returns>The prop texture, or null if the graphics file was invalid or not found.</returns>
    public RlManaged.Texture2D? GetPropTexture(string assetName)
    {
        if (propTexCache.TryGetValue(assetName, out RlManaged.Texture2D? texture))
            return texture;

        // find prop path
        // for some reason, previews for drought props are in cast data instead of in the Props folder
        // kind of annoying. so i just put those images in assets/internal
        string texturePath = GetFilePath(Path.Combine(RainEd.Instance.AssetDataPath, "Props"), assetName + ".png");
        if (!File.Exists(texturePath) && DrizzleCastMap.TryGetValue(assetName, out string? castPath))
        {
            texturePath = Path.Combine(Boot.AppDataPath, "assets", "internal", castPath!);
        }

        texture = null;

        using var srcImage = RlManaged.Image.Load(texturePath);
        if (Raylib.IsImageReady(srcImage))
        {
            CropImage(srcImage);
            texture = RlManaged.Texture2D.LoadFromImage(srcImage);
        }
        else
        {
            RainEd.Logger.Warning($"Image {texturePath} is invalid or missing!");
        }

        propTexCache.Add(assetName, texture);
        return texture;
    }

    /// <summary>
    /// Obtain the texture of a prop init. Works for normal props and tiles as props.
    /// May be cached
    /// </summary>
    /// <param name="propInit">The prop init whose data is used to obtain the texture.</param>
    /// <returns>The prop/tile texture, or null if the graphics file was invalid or not found.</returns>
    public RlManaged.Texture2D? GetPropTexture(Props.PropInit propInit)
    {
        if (propInit.PropFlags.HasFlag(Props.PropFlags.Tile))
            return GetTileTexture(propInit.Name);
        else
            return GetPropTexture(propInit.Name);
    }

    /// <summary>
    /// Obtain the texture of a tile asset. May be cached.
    /// </summary>
    /// <param name="assetName">The name of the tile asset.</param>
    /// <returns>The tile texture, or null if the graphics file was invalid or not found.</returns>
    public RlManaged.Texture2D? GetTileTexture(string assetName)
    {
        if (tileTexCache.TryGetValue(assetName, out RlManaged.Texture2D? texture))
            return texture;

        // find tile path
        // for some reason, previews for drought props are in cast data instead of in the Props folder
        // kind of annoying. so i just put those images in assets/internal
        string texturePath = GetFilePath(Path.Combine(RainEd.Instance.AssetDataPath, "Graphics"), assetName + ".png");
        if (!File.Exists(texturePath) && DrizzleCastMap.TryGetValue(assetName, out string? castPath))
        {
            texturePath = Path.Combine(Boot.AppDataPath, "assets", "internal", castPath!);
        }

        using var srcImage = RlManaged.Image.Load(texturePath);

        if (Raylib.IsImageReady(srcImage))
        {
            CropImage(srcImage);
            texture = RlManaged.Texture2D.LoadFromImage(srcImage);
        }
        else
        {
            RainEd.Logger.Warning($"Image {texturePath} is invalid or missing!");
        }

        tileTexCache.Add(assetName, texture);
        return texture;
    }

    /// <summary>
    /// Obtain the texture of a tile's preview texture. May be cached.
    /// </summary>
    /// <param name="tile">The tile whose data is used to obtain the preview texture.</param>
    /// <returns>The tile preview texture, or null if the graphics file was invalid or not found.</returns>
    public RlManaged.Texture2D? GetTilePreviewTexture(Tiles.Tile tile)
    {
        // if texture already exists in cache,
        // return that instead of processing it again
        if (previewTexCache.TryGetValue(tile.Name, out RlManaged.Texture2D? outTexture))
        {
            return outTexture;
        }

        var graphicsPath = GetFilePath(Path.Combine(RainEd.Instance.AssetDataPath, "Graphics"), tile.Name + ".png");
        if (!File.Exists(graphicsPath) && DrizzleCastMap.TryGetValue(tile.Name, out string? castPath))
        {
            graphicsPath = Path.Combine(Boot.AppDataPath, "assets", "internal", castPath!);
        }

        using var fullImage = RlManaged.Image.Load(graphicsPath);
        if (Raylib.IsImageReady(fullImage))
        {
            CropImage(fullImage);

            var previewRect = new Rectangle(
                0,
                tile.ImageRowCount * 20 + tile.ImageYOffset,
                tile.Width * 16,
                tile.Height * 16
            );

            /*if (previewRect.X < 0 || previewRect.Y < 0 ||
                previewRect.X >= fullImage.Width || previewRect.Y >= fullImage.Height ||
                previewRect.X + previewRect.Width > fullImage.Width ||
                previewRect.Y + previewRect.Height > fullImage.Height
            )
            {
                RainEd.Logger.Warning($"Tile '{tile.Name}' preview image is out of bounds");
            }*/

            using var previewImage = RlManaged.Image.GenColor(tile.Width * 16, tile.Height * 16, Color.White);
            previewImage.Format(PixelFormat.UncompressedR8G8B8A8);

            Raylib.ImageDraw(
                ref previewImage.Ref(),
                fullImage,
                previewRect,
                new Rectangle(0, 0, previewRect.Width, previewRect.Height),
                Color.White
            );

            // convert black-and-white image to white-and-transparent, respectively
            for (int x = 0; x < previewImage.Width; x++)
            {
                for (int y = 0; y < previewImage.Height; y++)
                {
                    if (Raylib.GetImageColor(previewImage, x, y).Equals(new Color(255, 255, 255, 255)))
                    {
                        previewImage.DrawPixel(x, y, new Color(255, 25, 255, 0));
                    }
                    else
                    {
                        previewImage.DrawPixel(x, y, new Color(255, 255, 255, 255));
                    }
                }
            }

            outTexture = RlManaged.Texture2D.LoadFromImage(previewImage);
        }
        else
        {
            // tile graphics could not be loaded
            RainEd.Logger.Warning($"Preview image {graphicsPath} is invalid or missing!");
            outTexture = null;
        }

        previewTexCache.Add(tile.Name, outTexture);
        return outTexture;
    }

    /// <summary>
    /// Crop out an image's bordering white pixels.
    /// Adobe Director auto-crops images when loading, which
    /// is why this is necessary.
    /// </summary>
    /// <param name="sourceImage">The image to crop.</param>
    /// <returns>True if the image was cropped, false if not.</returns>
    public static bool CropImage(RlManaged.Image sourceImage)
    {
        int imgMinX = -1;
        int imgMinY = -1;
        int imgMaxX = -1;
        int imgMaxY = -1;

        // find imgMinY
        for (int y = 0; y < sourceImage.Height; y++)
        {
            for (int x = 0; x < sourceImage.Width; x++)
            {
                var color = Raylib.GetImageColor(sourceImage, x, y);
                if (color.R != 255 || color.G != 255 || color.B != 255)
                {
                    imgMinY = y;
                    goto exitTopSearch;
                }
            }
        }
        exitTopSearch:;

        // find imgMinX
        for (int x = 0; x < sourceImage.Width; x++)
        {
            for (int y = 0; y < sourceImage.Height; y++)
            {
                var color = Raylib.GetImageColor(sourceImage, x, y);
                if (color.R != 255 || color.G != 255 || color.B != 255)
                {
                    imgMinX = x;
                    goto exitLeftSearch;
                }
            }
        }
        exitLeftSearch:;

        // find imgMaxY
        for (int y = sourceImage.Height - 1; y >= 0; y--)
        {
            for (int x = sourceImage.Width - 1; x >= 0; x--)
            {
                var color = Raylib.GetImageColor(sourceImage, x, y);
                if (color.R != 255 || color.G != 255 || color.B != 255)
                {
                    imgMaxY = y;
                    goto exitBottomSearch;
                }
            }
        }
        exitBottomSearch:;

        // find imgMaxX
        for (int x = sourceImage.Width - 1; x >= 0; x--)
        {
            for (int y = sourceImage.Height - 1; y >= 0; y--)
            {
                var color = Raylib.GetImageColor(sourceImage, x, y);
                if (color.R != 255 || color.G != 255 || color.B != 255)
                {
                    imgMaxX = x;
                    goto exitRightSearch;
                }
            }
        }
        exitRightSearch:;

        int width = imgMaxX - imgMinX + 1;
        int height = imgMaxY - imgMinY + 1;

        if (width == sourceImage.Width && height == sourceImage.Height)
        {
            return false;
        }
        else
        {
            Raylib.ImageCrop(ref sourceImage.Ref(), new Rectangle(imgMinX, imgMinY, width, height));
            return true;
        }
    }
}