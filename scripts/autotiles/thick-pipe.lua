
local helpers = require("helpers") -- load the helpers.lua module

-- setup autotile data
local autotile = rained.createAutotile()
autotile.name = "Thick Pipe"
autotile.type = "path"
autotile.pathThickness = 2
autotile.segmentLength = 2

-- Rained will not allow the user to use this autotile
-- if any of the tiles in this table are not installed
autotile.requiredTiles = {
    "Thick pipe vertical",
    "Thick pipe horizontal",
    "Thick Pipe WS",
    "Thick Pipe WN",
    "Thick Pipe ES",
    "Thick Pipe EN"
}

-- table of tiles to use for the standard autotiler function
-- which is helpers.autotilePath
local tileTable = {
    ld = "Thick Pipe WS",
    lu = "Thick Pipe WN",
    rd = "Thick Pipe ES",
    ru = "Thick Pipe EN",
    vertical = "Thick pipe vertical",
    horizontal = "Thick pipe horizontal"
}

-- this is the callback function that Rained invokes when the user
-- wants to autotile a given path
---@param layer integer The layer to run the autotiler on
---@param segments PathSegment[] The list of path segments
---@param forceModifier ForceModifier Force-placement mode, as a string. Can be nil, "force", or "geometry".
function autotile:tilePath(layer, segments, forceModifier)
    helpers.autotilePath(tileTable, layer, segments, forceModifier)
end