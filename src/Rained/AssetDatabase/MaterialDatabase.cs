using Raylib_cs;

namespace RainEd.Tiles;

record MaterialInfo
{
    public readonly int ID;
    public readonly string Name;
    public readonly Color Color;
    public readonly MaterialCategory Category;

    public MaterialInfo(int id, string name, Color color, MaterialCategory category)
    {
        ID = id;
        Name = name;
        Color = color;
        Category = category;
    }
}

record MaterialCategory
{
    public string Name;
    public List<MaterialInfo> Materials = [];

    public MaterialCategory(string name)
    {
        Name = name;
    }
}

class MaterialDatabase
{
    private readonly List<MaterialInfo> materialList;
    public readonly MaterialInfo[] Materials;
    public readonly List<MaterialCategory> Categories;

    public MaterialDatabase()
    {
        materialList = [];
        Categories = [];

        // vanilla materials
        Categories.Add(new MaterialCategory("Materials"));
        CreateMaterial(     "Standard",          new Color(148,  148,    148,    255)     );
        CreateMaterial(     "Concrete",          new Color(148,  255,    255,    255)     );
        CreateMaterial(     "RainStone",         new Color(0,    0,      255,    255)     );
        CreateMaterial(     "Bricks",            new Color(206,  148,    99,     255)     );
        CreateMaterial(     "BigMetal",          new Color(255,  0,      0,      255)     );
        CreateMaterial(     "Tiny Signs",        new Color(255,  206,    255,    255)     );
        CreateMaterial(     "Scaffolding",       new Color(57,   57,     41,     255)     );
        CreateMaterial(     "Dense Pipes",       new Color(0,    0,      148,    255)     );
        CreateMaterial(     "SuperStructure",    new Color(165,  181,    255,    255)     );
        CreateMaterial(     "SuperStructure2",   new Color(189,  165,    0,      255)     );
        CreateMaterial(     "Tiled Stone",       new Color(99,   0,      255,    255)     );
        CreateMaterial(     "Chaotic Stone",     new Color(255,  0,      255,    255)     );
        CreateMaterial(     "Small Pipes",       new Color(255,  255,    0,      255)     );
        CreateMaterial(     "Trash",             new Color(90,   255,    0,      255)     );
        CreateMaterial(     "Invisible",         new Color(206,  206,    206,    255)     );
        CreateMaterial(     "LargeTrash",        new Color(173,  24,     255,    255)     );
        CreateMaterial(     "3DBricks",          new Color(255,  148,    0,      255)     );
        CreateMaterial(     "Random Machines",   new Color(74,   115,    82,     255)     );
        CreateMaterial(     "Dirt",              new Color(123,  74,     49,     255)     );
        CreateMaterial(     "Ceramic Tile",      new Color(57,   57,     99,     255)     );
        CreateMaterial(     "Temple Stone",      new Color(0,    123,    181,    255)     );
        CreateMaterial(     "Circuits",          new Color(0,    148,    0,      255)     );
        CreateMaterial(     "Ridge",             new Color(206,  8,      57,     255)     );

        // drought materials
        Categories.Add(new MaterialCategory("Drought Materials"));
        CreateMaterial(     "Steel",               new Color(220,170,195,    255)     );
        CreateMaterial(     "4Mosaic",             new Color(227, 76, 13,    255)     );
        CreateMaterial(     "Color A Ceramic",     new Color(120, 0, 90,    255)     );
        CreateMaterial(     "Color B Ceramic",     new Color(0, 175, 175,    255)     );
        CreateMaterial(     "Random Pipes",        new Color(80,0,140,    255)     );
        CreateMaterial(     "Rocks",               new Color(185,200,0,    255)     );
        CreateMaterial(     "Rough Rock",          new Color(155,170,0,    255)     );
        CreateMaterial(     "Random Metal",        new Color(180, 10, 10,    255)     );
        CreateMaterial(     "Cliff",               new Color(75, 75, 75,    255)     );
        CreateMaterial(     "Non-Slip Metal",      new Color(180, 80, 80,    255)     );
        CreateMaterial(     "Stained Glass",       new Color(180, 80, 180,    255)     );
        CreateMaterial(     "Sandy Dirt",          new Color(180, 180, 80,    255)     );
        CreateMaterial(     "MegaTrash",           new Color(135,10,255,    255)     );
        CreateMaterial(     "Shallow Dense Pipes", new Color(13,23,110,    255)     );
        CreateMaterial(     "Sheet Metal",         new Color(145, 135, 125,    255)     );
        CreateMaterial(     "Chaotic Stone 2",     new Color(90, 90, 90,    255)     );
        CreateMaterial(     "Asphalt",             new Color(115, 115, 115,    255)     );

        // community materials
        Categories.Add(new MaterialCategory("Community Materials"));
        CreateMaterial(     "Shallow Circuits",    new Color(15,200,155, 255));
        CreateMaterial(     "Random Machines 2",   new Color(116, 116, 80, 255));
        CreateMaterial(     "Small Machines",      new Color(80, 116, 116, 255));
        CreateMaterial(     "Random Metals",       new Color(255, 0, 80, 255));
        CreateMaterial(     "ElectricMetal",       new Color(255,0,100, 255));
        CreateMaterial(     "Grate",               new Color(190,50,190, 255));
        CreateMaterial(     "CageGrate",           new Color(50,190,190, 255));
        CreateMaterial(     "BulkMetal",           new Color(50,19,190, 255));
        CreateMaterial(     "MassiveBulkMetal",    new Color(255,19,19, 255));
        CreateMaterial(     "Dune Sand",           new Color(255, 255, 100, 255));

        // read from init.txt
        RegisterCustomMaterials();

        Materials = [..materialList];
    }

    private void RegisterCustomMaterials()
    {
        var parser = new Lingo.LingoParser();
        var initFile = Path.Combine(RainEd.Instance.AssetDataPath, "Materials", "Init.txt");
        if (!File.Exists(initFile))
        {
            Log.Error("Materials/Init.txt not found!");
            return;
        }
        
        foreach (var line in File.ReadLines(initFile))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line[0] == '-')
            {
                Categories.Add(new MaterialCategory(line[1..]));
            }
            else
            {
                var data = parser.Read(line) as Lingo.List ?? throw new Exception("Malformed material init");
                
                var name = (string) data.fields["nm"];
                var color = (Lingo.Color) data.fields["color"];

                CreateMaterial(name, new Color(color.R, color.G, color.B, 255));
            }
        }
    }

    private MaterialInfo CreateMaterial(string name, Color color)
    {
        if (Categories.Count == 0) throw new Exception("The first category header is missing");
        
        var mat = new MaterialInfo(materialList.Count + 1, name, color, Categories[^1]);
        materialList.Add(mat);
        Categories[^1].Materials.Add(mat);
        return mat;
    }

    public MaterialInfo GetMaterial(int id)
    {
        return materialList[id - 1];
    }

    public MaterialInfo? GetMaterial(string name)
    {
        for (int i = 0; i < materialList.Count; i++)
        {
            if (materialList[i].Name == name)
                return materialList[i];
        }

        return null;
    }
}