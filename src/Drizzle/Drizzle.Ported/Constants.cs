namespace Drizzle.Ported;

public enum TileGeometry
{
    Air = 0,
    SolidWall = 1,
    SlopeBL = 2,
    SlopeBR = 3,
    SlopeTL = 4,
    SlopeTR = 5,
    Floor = 6,
    // I found reference to these two in a random cast member but they don't seem used anywhere.
    //Shortcut = 7,
    //Nn = 8,
    /// <summary>
    /// Invisible wall.
    /// </summary>
    Glass = 9
}

public enum TileFeature
{
    BeamHorizontal = 1,
    BeamVertical = 2,
    Hive = 3,
    ShortcutEntrance = 4,
    Shortcut = 5,
    Entrance = 6,
    DragonDen = 7,
    Rock = 9,
    Spear = 10,
    Crack = 11,
    ForbidFlyChains = 12,
    GarbageWormHole = 13,
    Waterfall = 18,
    WhackAMoleHole = 19,
    WormGrass = 20,
    ScavengerHole = 21,
}