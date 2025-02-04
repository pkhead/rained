---@meta

---@class Level
---@field filePath string? (Read-only) The path to the level file on disk.
---@field name string (Read-only) The name of the level.
---@field width integer (Read-only) The width of the level.
---@field height integer (Read-only) The height of the level.
---@field defaultMedium boolean Equates to the "Enclosed Room" switch. True if the level's default geometry is solid, false if it is air. (This option doesn't actually do anything.)
---@field hasSunlight boolean True if the light map applies, otherwise the level will be completely enclosed in darkness.
---@field hasWater boolean True if the level has water, false if not.
---@field waterLevel integer The height of the water from the bottom of the level.
---@field isWaterInFront boolean True if water renders in front of the level. Otherwise, it will render behind the first work layer.
---@field tileSeed integer The seed used for procedurally generating tile graphics when rendering. Clamped to the range [0, 400].
---@field borderLeft integer The offset of the left side of the border from the same side of the level. Clamped to be at least 0.
---@field borderTop integer The offset of the top side of the border from the same side of the level. Clamped to be at least 0.
---@field borderRight integer The offset of the right side of the border from the same side of the level. Clamped to be at least 0.
---@field borderBottom integer The offset of the bottom side of the border from the same side of the level. Clamped to be at least 0.
rained.level = {}

---Check if a given coordinate is in the bounds of the level.
---@param x integer X coordinate. X = 0 represents the leftmost column.
---@param y integer Y coordinate. Y = 0 represents the topmost row.
---@return boolean
function rained.level.isInBounds(x, y) end

---@class ResizeParameters
---@field width integer The width of the level in grid units.
---@field height integer The height of the level in grid units.
---@field borderLeft integer
---@field borderTop integer
---@field borderRight integer
---@field borderBottom integer
---@field anchorX integer -1, 0, or 1.
---@field anchorY integer -1, 0, or 1.

---Resize the level.
---@param parameters ResizeParameters The parameters for level resizing.
function rained.level.resize(parameters) end

---Convert screen units to cell units.
---@param screenX number X in screen units.
---@param screenY number Y in screen units.
---@return number x, number u
function rained.level.screenToCell(screenX, screenY) end

---Convert cell units to screen units.
---@param x number X in cell units.
---@param y number Y in cell units.
---@return number screenX, number screenY
function rained.level.cellToScreen(x, y) end

---Save the level to disk. Will throw an error if the level is not associated with a file on disk.
function rained.level.save() end