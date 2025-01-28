---@meta
rained.effects = {}

---@class Effect
---@field name string (Read-only) The name of the active effect.
---@field index integer? (Read-only) The index of the effect, or nil if not added to the level.
local Effect = {}

---Create a clone of this effect instance.
---@return Effect
function Effect:clone() end

---Set the value of an effect option.
---Throws an error if the option doesn't exist or is type-incompatible with the given value.
---@param name string The name of the option.
---@param value boolean|integer|string The value of the option.
function Effect:setOption(name, value) end

---Set a value in the effect matrix.
---@param x integer X position. X = 0 represents the leftmost column of elements.
---@param y integer Y position. Y = 0 represents the topmost row of elements.
---@param value number Value in the range 0-1. Will be clamped.
function Effect:setMatrixValue(x, y, value) end

---Get a value in the effect matrix. Will throw an error if out of bounds.
---@param x integer X position. X = 0 represents the leftmost column of elements.
---@param y integer Y position. Y = 0 represents the topmost row of elements.
---@return number
function Effect:getMatrixValue(x, y) end

---Get the effect matrix as a column-major two-dimensional array.
---@return number[][]
function Effect:getMatrix() end

---Set the effect matrix to a column-major two dimensional array. Will throw an error
---for a dimension mismatch. Each element will also be clamped to 0-1.
---@param matrix number[][]
function Effect:setMatrix(matrix) end

---Remove this effect from the effect list.
function Effect:remove() end

---@alias EffectType
---| "boolean",
---| "integer",
---| "enum"

---Get the number of currently active effects.
---@return integer
function rained.effects.getCount() end

---Get an object representing an active effect.
---@param index integer The index of the effect.
---@return Effect
function rained.effects.getEffect(index) end

---Get the name of an active effect.
---@param index integer The index of the effect to query.
---@return string
function rained.effects.getEffectName(index) end

---Remove an effect instace at the given index.
---
---Does nothing if there is no effect at that index.
---@param index integer The index to remove.
function rained.effects.removeEffect(index) end

---Add an effect instance at a given index.
---If the index is nil, it will be added to the end of the list.
---If the effect instance was already in the list, it will be moved.
---@param effect Effect The effect to add.
---@param index integer? The index at which the effect will be inserted.
function rained.effects.addEffect(effect, index) end

---Create a new effect instance.
---@param effectName string The name of the effect init.
---@return Effect effect The newly created effect, or nil if the name was not recognized.
function rained.effects.newEffect(effectName) end

---Get the type of an effect option. Returns nil if the option does not exist.
---@param effectName string The name of the effect.
---@param optionName string The name of the option.
---@return EffectType type, string[]? enumOptions
function rained.effects.getOptionType(effectName, optionName) end

---Get the default value for an effect option. Returns nil if the option does not exist.
---@param effectName string The name of the effect.
---@param optionName string The name of the option.
---@return boolean|integer|string
function rained.effects.getOptionDefaultValue(effectName, optionName) end

---Return the index of the selected effect, or nil if none was selected.
---@return integer?
function rained.effects.getSelectedEffect() end

---Get a list of all available effects.
---@return string[]
function rained.effects.getEffectCatalog() end

---Get a list of all available effect categories.
---@return string[]
function rained.effects.getEffectCategories() end

---Get a list of all effects in an effect category.
---@param categoryName string The name of the category.
---@return string[]
function rained.effects.getEffectsInCategory(categoryName) end