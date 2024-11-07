---@meta
-- Type annotations for Lua analzyers and linters, such as sumneko-lua

---automatically require all Lua files in a require path
---@param path string The path to require
---@param recurse boolean? If the function should recurse through all sub-paths. Defaults to false.
function autorequire(path, recurse) end

rained = {}
rained.cells = {}
rained.tiles = {}

GEO_TYPE = {
    AIR = 0,
    SOLID = 1,
    SLOPE_RIGHT_UP = 2,
    SLOPE_LEFT_UP = 3,
    SLOPE_RIGHT_DOWN = 4,
    SLOPE_LEFT_DOWN = 5,
    FLOOR = 6,
    SHORTCUT_ENTRANCE = 7,
    GLASS = 9
}

OBJECT_TYPE = {
    NONE = 0,
    HORIZONTAL_BEAM = 1,
    VERTICAL_BEAM = 2,
    HIVE = 3,
    SHORTCUT = 5,
    ENTRANCE = 6,
    CREATURE_DEN = 7,
    ROCK = 9,
    SPEAR = 10,
    CRACK = 11,
    FORBID_FLY_CHAIN = 12,
    GARBAGE_WORM = 13,
    WATERFALL = 18,
    WHACK_A_MOLE_HOLE = 19,
    WORM_GRASS = 20,
    SCAVENGER_HOLE = 21
}

---Get the current version number.
---@return string version The version number as a string 
function rained.getVersion() end

---Get the current API version.
---@return integer major, integer minor
function rained.getApiVersion() end

---Show a notification to the user.
---@param msg string The message to show
function rained.alert(msg) end

---Register a command invokable by the user.
---@param name string The display name of the command.
---@param callback function The action to run on command.
function rained.registerCommand(name, callback) end

---Get the width of the level.
---@return integer
function rained.getLevelWidth() end

---Get the height of the level.
---@return integer
function rained.getLevelHeight() end

---Check if a given coordinate is in the bounds of the level.
---@param x integer
---@param y integer
---@return boolean
function rained.isInBounds(x, y) end

---Get the geometry type of a given cell.
---@param x integer
---@param y integer
---@param layer integer The given layer, in the range [1, 3].
---@return integer
function rained.cells.getGeo(x, y, layer) end

---Set the geometry type of a given cell.
---@param x integer
---@param y integer
---@param layer integer The given layer, in the range [1, 3].
---@param geo integer The geometry type of a cell.
function rained.cells.setGeo(x, y, layer, geo) end

---Set the material of a given cell.
---@param x integer
---@param y integer
---@param layer integer The given layer, in the range [1, 3].
---@param material string The name of the desired material. Nil to clear.
function rained.cells.setMaterial(x, y, layer, material) end

---Get the name of a cell's material.
---@param x integer
---@param y integer
---@param layer integer The given layer, in the range [1, 3].
---@return string name The name of the material, or nil if there was no material there.
function rained.cells.getMaterial(x, y, layer) end

---Set the material ID of a given cell.
---@param x integer
---@param y integer
---@param layer integer The given layer, in the range [1, 3].
---@param material integer The ID of the material. Zero to clear.
function rained.cells.setMaterialId(x, y, layer, material) end

---Get the name of a cell's material ID.
---@param x integer
---@param y integer
---@param layer integer The given layer, in the range [1, 3].
---@return integer id The ID of the material, or 0 if there was no material there.
function rained.cells.getMaterialId(x, y, layer) end

---Get the geometry objects of a given cell.
---@param x integer
---@param y integer
---@param layer integer
---@return integer[]
function rained.cells.getObjects(x, y, layer) end

---Set the geometry objects of a given cell.
---@param x integer
---@param y integer
---@param layer integer
---@param objects integer[] The list of objects.
function rained.cells.setObjects(x, y, layer, objects) end

---Get the tile data of a cell
---@param x integer
---@param y integer
---@param layer integer The given layer, in the range [1, 3].
---@return string? tileName, integer? tileRootX, integer? tileRootY, integer? tileRootL
function rained.cells.getTileData(x, y, layer) end

---Set the tile head of the given cell.
---@param x integer
---@param y integer
---@param layer integer The given layer, in the range [1, 3].
---@param tileName string? The name of the tile, or nil.
function rained.cells.setTileHead(x, y, layer, tileName) end

---Set the tile head pointer of a given cell.
---@param x integer The X position of the cell to modify.
---@param y integer The Y position of the cell to modify.
---@param layer integer The layer of the cell to modify.
---@param rootX integer The X position of the pointed tile head.
---@param rootY integer The Y position of the pointed tile head.
---@param rootL integer The layer of the pointed tile head.
function rained.cells.setTileRoot(x, y, layer, rootX, rootY, rootL) end

---Clear the tile head pointer of a given cell.
---@param x integer
---@param y integer
---@param layer integer
function rained.cells.clearTileRoot(x, y, layer) end

---Create an autotile
---@param name string The name of the autotile.
---@param category string? The category to place the autotile in. Defaults to Misc.
---@return Autotile autotile The new autotile.
---@overload fun(name: string)
function rained.tiles.createAutotile(name, category) end

---@class TileData
---@field name string
---@field category string
---@field width integer
---@field height integer
---@field specs integer[]
---@field specs2 integer[]
---@field bfTiles integer
---@field centerX integer
---@field centerY integer

---Get the init data of a tile.
---@param name string The name of the tile
---@return TileData? tileData The table containing the data for the tile, or null if the tile does not exist.
function rained.tiles.getTileInfo(name) end

---@class TileTable
---@field ld string
---@field lu string
---@field rd string
---@field ru string
---@field vertical string
---@field horizontal string
---@field tr string?
---@field tu string?
---@field tl string?
---@field td string?
---@field x string?
---@field capRight string?
---@field capUp string?
---@field capLeft string?
---@field capDown string?
---@field placeJunctions boolean?
---@field placeCaps boolean?

---Perform standard path autotiling
---@param tileTable TileTable The autotile parameter table.
---@param layer integer The layer to run the autotiler on.
---@param segments PathSegment[] The segments to autotile with.
---@param startIndex integer? The index of the starting segment. Defaults to 1.
---@param endIndex integer? The number of segments to place. Defaults to the length of the segment array.
---@param forceModifier ForceModifier
function rained.tiles.autotilePath(tileTable, layer, segments, forceModifier, startIndex, endIndex) end

---Check if a tile is installed
---@param tileName string The name of the tile to check
---@return boolean
function rained.tiles.hasTile(tileName) end

---Place a tile in the level.
---@param tileName string The name of the tile to place
---@param x integer The X coordinate of the tile root
---@param y integer The Y coordinate of the tile root
---@param layer integer The layer to place the tile, in the range [1, 3]
---@param forceModifier ForceModifier? The force-placement mode to use, or nil if placing normally
function rained.tiles.placeTile(tileName, x, y, layer, forceModifier) end

---Get the name of the tile at the given position.
---@param x integer
---@param y integer
---@param layer integer The given layer, in the range [1, 3]
---@return string? tileName The name of the tile, or nil if there is no tile at the given location.
function rained.tiles.getTileAt(x, y, layer) end

---Check if the tile at the given position is a tile head.
---@param x integer
---@param y integer
---@param layer integer The given layer, in the range [1, 3]
---@return boolean hasTileHead True if the cell has a tile head, false if not.
function rained.tiles.hasTileHead(x, y, layer) end

---Delete the tile at the given position
---@param x integer
---@param y integer
---@param layer integer The given layer, in the range [1, 3]
---@param removeGeo boolean? If the action should also remove the geometry.
---@overload fun(x: integer, y: integer, layer: integer)
function rained.tiles.deleteTile(x, y, layer, removeGeo) end

---@alias AutotileType
---| "path"
---| "rect"

---@alias ForceModifier
---Force-place the tile
---| "force"
---Force-place with the required geometry
---| "geometry"

---@class Autotile
---@field name string The name of the autotile.
---@field type AutotileType The fill type of the autotile.
---@field allowIntersections boolean True if self-intersections are allowed.
---@field requiredTiles string[]? The list of all the tiles required by the autotile.
---@field tilePath fun(self: Autotile, layer: integer, segments: PathSegment[], forceModifier: ForceModifier)? The path autotiling callback
---@field tileRect fun(self: Autotile, layer: integer, left: integer, top: integer, right: integer, bottom: integer, forceModifier: ForceModifier)? The rect autotiling callback
---@field onOptionChanged fun(self: Autotile, id: string)? The callback that is invoked when an option is changed.
local Autotile = {}

---Add a toggle option for the autotile.
---@param id string The unique ID of the new option.
---@param label string The display label of the option.
---@param defaultValue boolean The initial value of the option.
function Autotile:addToggleOption(id, label, defaultValue) end

---Add an integer option for the autotile.
---@param id string The unique ID of the new option.
---@param label string The display label of the option.
---@param defaultValue integer The initial value of the option.
---@param min integer? The minimum value of the option. Defaults to `-math.huge`.
---@param max integer? The maximum value of the option. Defaults to `math.huge`.
function Autotile:addIntOption(id, label, defaultValue, min, max) end

---Get the value of an option
---@param id string The ID of the option to read
---@return boolean|integer value The value of the given option
function Autotile:getOption(id) end

---@class PathSegment
---@field left boolean If there is another segment to the left of this one
---@field right boolean If there is another segment to the right of this one
---@field up boolean If there is another segment above this one
---@field down boolean If there is another segment below this one
---@field x integer The X coordinate of the segment
---@field y integer The Y coordinate of the segment
local PathSegment = {}