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

    -- these values will be set in the autotile callback function
    -- to adjust according the "plain" option
    -- before calling the standard autotiler
    vertical = "Vertical Pipe",
    horizontal = "Horizontal Pipe",
}

---Helper function to place an inward pipe at a tile cap
---@param layer integer
---@param segments PathSegment[]
---@param index integer
---@param forceModifier ForceModifier
local function placePathCap(layer, segments, index, forceModifier)
    local seg = segments[index]

    local neighbors = 0
    if seg.left then neighbors = neighbors + 1 end
    if seg.right then neighbors = neighbors + 1 end
    if seg.up then neighbors = neighbors + 1 end
    if seg.down then neighbors = neighbors + 1 end

    if neighbors > 1 then
        rained.autotilePath(tileTable, layer, segments, autotile:getOption("junctions"), forceModifier, index, index)
    end

    if seg.left then
        rained.placeTile("Pipe Inwards E", seg.x, seg.y, layer, forceModifier)
    end

    if seg.right then
        rained.placeTile("Pipe Inwards W", seg.x, seg.y, layer, forceModifier)
    end

    if seg.up then
        rained.placeTile("Pipe Inwards N", seg.x, seg.y, layer, forceModifier)
    end

    if seg.down then
        rained.placeTile("Pipe Inwards S", seg.x, seg.y, layer, forceModifier)
    end
end

-- this is the callback function that Rained invokes when the user
-- wants to autotile a given path
---@param layer integer The layer to run the autotiler on
---@param segments PathSegment[] The list of path segments
---@param forceModifier ForceModifier Force-placement mode, as a string. Can be nil, "force", or "geometry".
function autotile:tilePath(layer, segments, forceModifier)
    -- if the "plain" option is checked, use the plain variant of straight pipes instead
    tileTable.vertical = self:getOption("plain") and "Vertical Plain Pipe" or "Vertical Pipe"
    tileTable.horizontal = self:getOption("plain") and "Horizontal Plain Pipe" or "Horizontal Pipe"
    
    -- if the user wants to place inward pipes on the caps
    -- only place caps if there are at least 2 path segments
    if self:getOption("cap") and #segments >= 2 then
        -- first, call the standard autotiler for the non-cap segments
        -- so, ignoring the first item and last item
        rained.autotilePath(tileTable, layer, segments, self:getOption("junctions"), forceModifier, 2, #segments - 1)

        -- then, place the cap segments at the ends of the path
        placePathCap(layer, segments, 1, forceModifier)
        placePathCap(layer, segments, #segments, forceModifier)

    -- the user does not want to place inward pipes on the caps
    else
        -- run the standard autotiler for the entire path
        rained.autotilePath(tileTable, layer, segments, self:getOption("junctions"), forceModifier)
    end
end