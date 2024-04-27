
local helpers = require("helpers") -- load the helpers.lua module

-- setup autotile data
local autotile = rained.createAutotile("Big Pipe", "Pipes")
autotile.type = "path"
autotile.pathThickness = 6
autotile.segmentLength = 1

-- Rained will not allow the user to use this autotile
-- if any of the tiles in this table are not installed
autotile.requiredTiles = {
    "Big Pipe Vertical",
    "Big Pipe Horizontal",
    "Big Pipe LU",
    "Big Pipe RU",
    "Big Pipe RD",
    "Big Pipe LD"
}

-- table of tiles to use for the standard autotiler function
-- which is helpers.autotilePath
local tileTable = {
    ld = "Big Pipe LD",
    lu = "Big Pipe LU",
    rd = "Big Pipe RD",
    ru = "Big Pipe RU",
    vertical = "Big Pipe Vertical",
    horizontal = "Big Pipe Horizontal"
}

-- this is the callback function that Rained invokes when the user
-- wants to autotile a given path
---@param layer integer The layer to run the autotiler on
---@param segments PathSegment[] The list of path segments
---@param forceModifier ForceModifier Force-placement mode, as a string. Can be nil, "force", or "geometry".
function autotile:tilePath(layer, segments, forceModifier)
    helpers.autotilePath(tileTable, layer, segments, forceModifier)
end