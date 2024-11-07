local helpers = require("helpers")

-- setup autotile data
local autotile = rained.tiles.createAutotile("Alt Grate box", "Pattern Boxes")
autotile.type = "rect"
autotile:addToggleOption("border", "Place Border", true)

-- Rained will not allow the user to use this autotile
-- if any of the tiles in this table are not installed
autotile.requiredTiles = {
    "Block Corner NW",
    "Block Corner NE",
    "Block Corner SE",
    "Block Corner SW",
    "AltGrateA",
    "AltGrateB1",
    "AltGrateB2",
    "AltGrateB3",
    "AltGrateB4",
    "AltGrateC1",
    "AltGrateC2",
    "AltGrateE1",
    "AltGrateE2",
    "AltGrateF1",
    "AltGrateF2",
    "AltGrateF1",
    "AltGrateF2",
    "AltGrateF3",
    "AltGrateF4",
    "AltGrateG1",
    "AltGrateG2",
    "AltGrateI",
    "AltGrateJ1",
    "AltGrateJ2",
    "AltGrateJ1",
    "AltGrateJ2",
    "AltGrateJ3",
    "AltGrateJ4",
    "AltGrateJ3",
    "AltGrateB1"
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
    helpers.patternBox("AltGrate", layer, left, top, right, bottom, forceModifier, autotile:getOption("border"))
end