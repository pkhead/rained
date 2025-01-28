---@meta
-- Type annotations for Lua analzyers and linters, such as sumneko-lua

---automatically require all Lua files in a require path
---@param path string The path to require
---@param recurse boolean? If the function should recurse through all sub-paths. Defaults to false.
function autorequire(path, recurse) end

rained = {}

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

---Get the current application version.
---@return string version The version number as a string 
function rained.getVersion() end

---Get the current API version. This is separate from the application version.
---@return integer major, integer minor, integer revision
function rained.getApiVersion() end

---Show a notification to the user.
---@param msg string The message to show
function rained.alert(msg) end

---Register a command invokable by the user.
---@param name string The display name of the command.
---@param callback function The action to run on command.
function rained.registerCommand(name, callback) end

---@deprecated
---**DEPRECATED: Use `rained.level.width` instead.**
---
---Get the width of the level.
---@return integer
function rained.getLevelWidth() end

---@deprecated
---**DEPRECATED: Use `rained.level.height` instead.**
---
---Get the height of the level.
---@return integer
function rained.getLevelHeight() end

---@deprecated
---**DEPRECATED: Use `rained.level.isInBounds` instead.**
---
---Check if a given coordinate is in the bounds of the level.
---@param x integer
---@param y integer
---@return boolean
function rained.isInBounds(x, y) end