---@meta
-- Type annotations for Lua analzyers and linters, such as sumneko-lua

---automatically require all Lua files in a require path
---@param path string The path to require
---@param recurse boolean? If the function should recurse through all sub-paths. Defaults to false.
function autorequire(path, recurse) end

rained = {}

---Get the current version number.
---@return string version The version number as a string 
function rained.getVersion() end

---Show a notification to the user.
---@param msg string The message to show
function rained.alert(msg) end

---Register a command invokable by the user.
---@param name string The display name of the command.
---@param callback function The action to run on command.
function rained.registerCommand(name, callback) end

---Create an autotile
---@param name string The name of the autotile.
---@param category string? The category to place the autotile in. Defaults to Misc
---@return Autotile autotile The new autotile
---@overload fun(name: string)
function rained.createAutotile(name, category) end

---Check if a tile is installed
---@param tileName string The name of the tile to check
---@return boolean
function rained.hasTile(tileName) end

---Place a tile in the level.
---@param tileName string The name of the tile to place
---@param x integer The X coordinate of the tile root
---@param y integer The Y coordinate of the tile root
---@param layer integer The layer to place the tile, in the range [1, 3]
---@param forceModifier ForceModifier? The force-placement mode to use, or nil if placing normally
function rained.placeTile(tileName, x, y, layer, forceModifier) end

---Get the name of the tile at the given position.
---@param x integer
---@param y integer
---@param layer integer The given layer, in the range [1, 3]
---@return string? tileName The name of the tile, or nil if there is no tile at the given location.
function rained.getTileAt(x, y, layer) end

---Check if the tile at the given position is a tile head.
---@param x integer
---@param y integer
---@param layer integer The given layer, in the range [1, 3]
---@return boolean hasTileHead True if the cell has a tile head, false if not.
function rained.hasTileHead(x, y, layer) end

---Delete the tile at the given position
---@param x integer
---@param y integer
---@param layer integer The given layer, in the range [1, 3]
---@param removeGeo boolean? If the action should also remove the geometry.
---@overload fun(x: integer, y: integer, layer: integer)
function rained.deleteTile(x, y, layer, removeGeo) end

---@alias AutotileType
---| "path"
---| "rect"

---@alias ForceModifier
---Force-place the tile
---| "force"
---Force-place with the required geometry
---| "geometry"

---@class Autotile
---@field name string The name of the autotile
---@field type AutotileType The fill type of the autotile
---@field pathThickness integer The width/thickness of the path
---@field segmentLength integer The size of each path segment
---@field requiredTiles string[]? The list of all the tiles required by the autotile
---@field tilePath fun(self: Autotile, layer: integer, segments: PathSegment[], forceModifier: ForceModifier)? The path autotiling callback
---@field tileRect fun(self: Autotile, layer: integer, left: integer, top: integer, right: integer, bottom: integer, forceModifier: ForceModifier)? The rect autotiling callback
local Autotile = {}

---Add an on/off option to the autotile.
---@param id string The unique ID of the new option
---@param name string The display name of the option
---@param defaultValue boolean The initial value of the option
function Autotile:addOption(id, name, defaultValue) end

---Get the value of an option
---@param id string The ID of the option to read
---@return boolean value The value of the given option
function Autotile:getOption(id) end

---@class PathSegment
---@field left boolean If there is another segment to the left of this one
---@field right boolean If there is another segment to the right of this one
---@field up boolean If there is another segment above this one
---@field down boolean If there is another segment below this one
---@field x integer The X coordinate of the segment
---@field y integer The Y coordinate of the segment
local PathSegment = {}