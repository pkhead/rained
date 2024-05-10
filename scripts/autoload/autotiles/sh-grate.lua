local helpers = require("helpers")

-- setup autotile data
local autotile = rained.createAutotile("SH grate box", "Pattern Boxes")
autotile.type = "rect"

-- Rained will not allow the user to use this autotile
-- if any of the tiles in this table are not installed
autotile.requiredTiles = {
    "Block Corner NW",
    "Block Corner NE",
    "Block Corner SE",
    "Block Corner SW",
    "SUGrateA",
    "SUGrateB1",
    "SUGrateB2",
    "SUGrateB3",
    "SUGrateB4",
    "SUGrateC1",
    "SUGrateC2",
    "SUGrateE1",
    "SUGrateE2",
    "SUGrateF1",
    "SUGrateF2",
    "SUGrateF1",
    "SUGrateF2",
    "SUGrateF3",
    "SUGrateF4",
    "SUGrateG1",
    "SUGrateG2",
    "SUGrateI",
    "SUGrateJ1",
    "SUGrateJ2",
    "SUGrateJ1",
    "SUGrateJ2",
    "SUGrateJ3",
    "SUGrateJ4",
    "SUGrateJ3",
    "SUGrateB1"
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
    helpers.patternBox("SUGrate", layer, left, top, right, bottom, forceModifier)
end