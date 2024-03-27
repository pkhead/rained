---@meta
-- Type annotations for sumneko-lua

rained = {}

---Get the current version number.
---@return string version The version number as a string 
function rained.getVersion() end

---Create an autotile.
---@return Autotile autotile The new autotile
function rained.createAutotile() end

---Place a tile in the level.
---@param tileName string The name of the tile to place
---@param layer integer The layer to place the tile, in the range [1, 3]
---@param x integer The X coordinate of the tile root
---@param y integer The Y coordinate of the tile root
---@param forceModifier ForceModifier? The force-placement mode to use, or nil if placing normally
function rained.placeTile(tileName, layer, x, y, forceModifier) end

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
---@field tileRect fun(self: Autotile, layer: integer, top: integer, left: integer, right: integer, bottom: integer, forceModifier: ForceModifier)? The rect autotiling callback
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