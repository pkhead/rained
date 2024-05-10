-- setup autotile data
local autotile = rained.createAutotile("Thin Pipes", "Misc")
autotile.type = "path"
autotile:addToggleOption("cap", "Cap with inward pipes", false)
autotile:addToggleOption("plain", "Use plain pipes", false)
autotile:addToggleOption("junctions", "Allow Junctions", false)

-- change "allowIntersections" property when junctions is turned on/off
function autotile:onOptionChanged(id)
    if id == "junctions" then
        autotile.allowIntersections = autotile:getOption(id)
    end
end

-- Rained will not allow the user to use this autotile
-- if any of the tiles in this table are not installed
autotile.requiredTiles = {
    "Vertical Pipe",
    "Horizontal Pipe",
    "Vertical Plain Pipe",
    "Horizontal Plain Pipe",
    "Pipe WS",
    "Pipe WN",
    "Pipe ES",
    "Pipe EN",
    "Pipe TJunct E",
    "Pipe TJunct N",
    "Pipe TJunct W",
    "Pipe TJunct S",
    "Pipe XJunct",
    "Pipe Inwards E",
    "Pipe Inwards N",
    "Pipe Inwards W",
    "Pipe Inwards S"
}

-- table of tiles to use for the standard autotiler function
-- which is helpers.autotilePath
local tileTable = {
    ld = "Pipe WS",
    lu = "Pipe WN",
    rd = "Pipe ES",
    ru = "Pipe EN",
    tr = "Pipe TJunct E",
    tu = "Pipe TJunct N",
    tl = "Pipe TJunct W",
    td = "Pipe TJunct S",
    x = "Pipe XJunct",
    capRight = "Pipe Inwards E",
    capUp = "Pipe Inwards S",
    capLeft = "Pipe Inwards W",
    capDown = "Pipe Inwards N",

    -- these values will be set in the autotile callback function
    -- to adjust according the "plain" option
    -- before calling the standard autotiler
    vertical = "Vertical Pipe",
    horizontal = "Horizontal Pipe",

    placeJunctions = false,
    placeCaps = false
}

-- this is the callback function that Rained invokes when the user
-- wants to autotile a given path
---@param layer integer The layer to run the autotiler on
---@param segments PathSegment[] The list of path segments
---@param forceModifier ForceModifier Force-placement mode, as a string. Can be nil, "force", or "geometry".
function autotile:tilePath(layer, segments, forceModifier)
    -- if the "plain" option is checked, use the plain variant of straight pipes instead
    tileTable.vertical = self:getOption("plain") and "Vertical Plain Pipe" or "Vertical Pipe"
    tileTable.horizontal = self:getOption("plain") and "Horizontal Plain Pipe" or "Horizontal Pipe"

    -- set options for the standard autotiler based on
    -- the GUI options
    tileTable.placeJunctions = self:getOption("junctions")
    tileTable.placeCaps = self:getOption("cap")

    -- run the standard autotiler
    rained.autotilePath(tileTable, layer, segments, forceModifier)
end