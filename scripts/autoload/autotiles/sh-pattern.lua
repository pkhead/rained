local helpers = require("helpers")

-- setup autotile data
local autotile = rained.createAutotile("SH pattern box", "Pattern Boxes")
autotile.type = "rect"

-- Rained will not allow the user to use this autotile
-- if any of the tiles in this table are not installed
autotile.requiredTiles = {
    "Block Corner NW",
    "Block Corner NE",
    "Block Corner SE",
    "Block Corner SW",
    "SUPatternA",
    "SUPatternB1",
    "SUPatternB2",
    "SUPatternB3",
    "SUPatternB4",
    "SUPatternC1",
    "SUPatternC2",
    "SUPatternE1",
    "SUPatternE2",
    "SUPatternF1",
    "SUPatternF2",
    "SUPatternF1",
    "SUPatternF2",
    "SUPatternF3",
    "SUPatternF4",
    "SUPatternG1",
    "SUPatternG2",
    "SUPatternI",
    "SUPatternJ1",
    "SUPatternJ2",
    "SUPatternJ1",
    "SUPatternJ2",
    "SUPatternJ3",
    "SUPatternJ4",
    "SUPatternJ3",
    "SUPatternB1"
}

-- this is the callback function that Rained invokes when the user
-- wants to autotile a given rectangle
---@param layer integer The layer to run the autotiler on
---@param left integer The X coordinate of the left side of the rectangle.
---@param top integer The Y coordinate of the top side of the rectangle.
---@param right integer The X coordinate of the right side of the rectangle.
---@param bottom integer The Y coordinate of the bottom side of the rectangle.
---@param forceModifier ForceModifier Force-placement mode, as a string. Can be nil, "force", or "geometry".
function autotile:tileRect(layer, left, top, right, bottom, forceModifier)
    helpers.patternBox("SUPattern", layer, left, top, right, bottom, forceModifier)
end