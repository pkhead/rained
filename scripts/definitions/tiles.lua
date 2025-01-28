---@meta
rained.tiles = {}

---Get the seed used for procedurally generating tile graphics when rendering.
---@return integer seed
function rained.tiles.getRandomSeed() end

---Set the seed used for procedurally generating tile graphics when rendering.
---@param randomSeed integer
function rained.tiles.setRandomSeed(randomSeed) end

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

---Get a list of all available tiles.
---@return string[]
function rained.tiles.getTileCatalog() end

---Get a list of all available tile categories.
---@return string[]
function rained.tiles.getTileCategories() end

---Get a list of all tiles in a category.
---@param categoryName string The name of the category.
---@return string[]
function rained.tiles.getTilesInCategory(categoryName) end

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