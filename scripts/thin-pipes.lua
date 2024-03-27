local helpers = require("helpers")

local autotile = rained.createAutotile()
autotile.name = "Thin Pipes"
autotile.type = "path"
autotile.pathThickness = 1
autotile.segmentLength = 1

autotile:addOption("inward", "Inward Pipes", true)
autotile:addOption("plain", "Plain Pipes", false)

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

local tileTable = {
    ws = "Pipe WS",
    wn = "Pipe WN",
    es = "Pipe ES",
    en = "Pipe EN",

    -- these values will be set in autotile:fillPath
    -- according to the "plain" option
    -- before tileTable is passed to the helper function
    vertical = "Vertical Pipe",
    horizontal = "Horizontal Pipe"
}

function autotile:fillPath(layer, segments, forceModifier)
    tileTable.vertical = self:getOption("plain") and "Vertical Plain Pipe" or "Vertical Pipe"
    tileTable.horizontal = self:getOption("plain") and "Horizontal Plain Pipe" or "Horizontal Pipe"
    helpers.autotilePath(tileTable, layer, segments, forceModifier)
end