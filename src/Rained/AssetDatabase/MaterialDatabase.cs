using Raylib_cs;

namespace RainEd.Tiles;

record MaterialInfo
{
    public readonly int ID;
    public readonly string Name;
    public readonly Color Color;

    public MaterialInfo(int id, string name, Color color)
    {
        ID = id;
        Name = name;
        Color = color;
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
        Categories[^1].Materials.Add(CreateMaterial(     "Standard",          new Color(148,  148,    148,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Concrete",          new Color(148,  255,    255,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "RainStone",         new Color(0,    0,      255,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Bricks",            new Color(206,  148,    99,     255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "BigMetal",          new Color(255,  0,      0,      255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Tiny Signs",        new Color(255,  206,    255,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Scaffolding",       new Color(57,   57,     41,     255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Dense Pipes",       new Color(0,    0,      148,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "SuperStructure",    new Color(165,  181,    255,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "SuperStructure2",   new Color(189,  165,    0,      255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Tiled Stone",       new Color(99,   0,      255,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Chaotic Stone",     new Color(255,  0,      255,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Small Pipes",       new Color(255,  255,    0,      255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Trash",             new Color(90,   255,    0,      255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Invisible",         new Color(206,  206,    206,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "LargeTrash",        new Color(173,  24,     255,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "3DBricks",          new Color(255,  148,    0,      255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Random Machines",   new Color(74,   115,    82,     255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Dirt",              new Color(123,  74,     49,     255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Ceramic Tile",      new Color(57,   57,     99,     255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Temple Stone",      new Color(0,    123,    181,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Circuits",          new Color(0,    148,    0,      255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Ridge",             new Color(206,  8,      57,     255)     ));

        // drought materials
        Categories.Add(new MaterialCategory("Drought Materials"));
        Categories[^1].Materials.Add(CreateMaterial(     "Steel",               new Color(220,170,195,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "4Mosaic",             new Color(227, 76, 13,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Color A Ceramic",     new Color(120, 0, 90,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Color B Ceramic",     new Color(0, 175, 175,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Random Pipes",        new Color(80,0,140,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Rocks",               new Color(185,200,0,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Rough Rock",          new Color(155,170,0,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Random Metal",        new Color(180, 10, 10,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Cliff",               new Color(75, 75, 75,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Non-Slip Metal",      new Color(180, 80, 80,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Stained Glass",       new Color(180, 80, 180,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Sandy Dirt",          new Color(180, 180, 80,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "MegaTrash",           new Color(135,10,255,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Shallow Dense Pipes", new Color(13,23,110,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Sheet Metal",         new Color(145, 135, 125,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Chaotic Stone 2",     new Color(90, 90, 90,    255)     ));
        Categories[^1].Materials.Add(CreateMaterial(     "Asphalt",             new Color(115, 115, 115,    255)     ));

        // community materials
        Categories.Add(new MaterialCategory("Community Materials"));
        Categories[^1].Materials.Add(CreateMaterial(     "Shallow Circuits",    new Color(15,200,155, 255)));
        Categories[^1].Materials.Add(CreateMaterial(     "Random Machines 2",   new Color(116, 116, 80, 255)));
        Categories[^1].Materials.Add(CreateMaterial(     "Small Machines",      new Color(80, 116, 116, 255)));
        Categories[^1].Materials.Add(CreateMaterial(     "Random Metals",       new Color(255, 0, 80, 255)));
        Categories[^1].Materials.Add(CreateMaterial(     "ElectricMetal",       new Color(255,0,100, 255)));
        Categories[^1].Materials.Add(CreateMaterial(     "Grate",               new Color(190,50,190, 255)));
        Categories[^1].Materials.Add(CreateMaterial(     "CageGrate",           new Color(50,190,190, 255)));
        Categories[^1].Materials.Add(CreateMaterial(     "BulkMetal",           new Color(50,19,190, 255)));
        Categories[^1].Materials.Add(CreateMaterial(     "MassiveBulkMetal",    new Color(255,19,19, 255)));
        Categories[^1].Materials.Add(CreateMaterial(     "Dune Sand",           new Color(255, 255, 100, 255)));

        Materials = [..materialList];
    }

    private MaterialInfo CreateMaterial(string name, Color color)
    {
        var mat = new MaterialInfo(materialList.Count + 1, name, color);
        materialList.Add(mat);
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