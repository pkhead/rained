using System.Globalization;
using System.Numerics;
using System.Reflection;
namespace Rained.WorldData;

[AttributeUsage(AttributeTargets.Field)]
class TagNameAttribute(string name) : Attribute
{
    public string Name = name;
}

enum RoomTag : byte
{
    None,
    
    [TagName("SHELTER")]
    Shelter,
    [TagName("ANCIENTSHELTER")]
    AncientShelter,
    [TagName("GATE")]
    Gate,
    [TagName("SWARMROOM")]
    SwarmRoom,
    [TagName("PERF_HEAVY")]
    PerfHeavy,
    [TagName("SCAVOUTPOST")]
    ScavOutpost,
    [TagName("NOTRACKERS")]
    NoTrackers,
    [TagName("ARENA")]
    Arena
}

class WorldRoom(Room room, IEnumerable<string?> connections, RoomTag tag = RoomTag.None)
{
    public Room Room = room;
    public Vector2 Position = Vector2.Zero;
    public List<string?> Connections = connections.ToList();
    public RoomTag Tag = tag;
}

class World
{
    public readonly string RegionName;
    public readonly List<WorldRoom> Rooms;
    public readonly string WorldDirectory;
    public readonly string RoomsDirectory;

    public World(string worldsDir, string regionName)
    {
        Rooms = [];
        RegionName = regionName.ToLower(CultureInfo.InvariantCulture);
        WorldDirectory = Path.Combine(worldsDir, regionName) + Path.DirectorySeparatorChar;
        RoomsDirectory = Path.Combine(worldsDir, regionName + "-rooms") + Path.DirectorySeparatorChar;

        ReadWorldFile();
    }

    private void ReadWorldFile()
    {
        var gatesDir = Path.Combine(WorldDirectory, "..", "gates");
        var worldFilePath = Path.Combine(WorldDirectory, "world_" + RegionName + ".txt");

        // read world file, ignoring comments or empty lines
        List<string> worldFileLines = [];
        foreach (var l in File.ReadLines(worldFilePath))
        {
            if (!string.IsNullOrWhiteSpace(l) && (l.Length < 2 || l[..2] != "//"))
                worldFileLines.Add(l);
        }

        // go to ROOMS section
        int lineIdx = 0;
        while (true)
        {
            if (worldFileLines[lineIdx++] == "ROOMS")
                break;
        }

        // read rooms
        while (worldFileLines[lineIdx] != "END ROOMS")
        {
            var roomData = worldFileLines[lineIdx].Split(" : ");

            var roomName = roomData[0];
            var roomConnections = roomData[1].Split(", ");

            // read room tag
            RoomTag roomTag = RoomTag.None;
            if (roomData.Length > 2)
            {
                foreach (var field in typeof(RoomTag).GetFields())
                {
                    if (field.GetCustomAttribute<TagNameAttribute>() is TagNameAttribute attr && attr.Name == roomData[2])
                    {
                        roomTag = (RoomTag) field.GetValue(null)!;
                        break;
                    }
                }

                if (roomTag == RoomTag.None)
                {
                    Log.UserLogger.Error("Invalid tag {Tag}", roomData[2]);
                }
            }

            var room = new Room(Path.Combine(roomTag == RoomTag.Gate ? gatesDir : RoomsDirectory, roomName + ".txt"));
            Rooms.Add(new WorldRoom(
                room: room,
                connections: roomConnections.Select(x => x == "DISCONNECTED" ? null : x),
                tag: roomTag
            ));

            lineIdx++;
        }

        Log.Debug("Done loading world");
    }
}