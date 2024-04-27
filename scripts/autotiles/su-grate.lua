-- setup autotile data
local autotile = rained.createAutotile("SU Grate Box")
autotile.type = "rect"

-- Rained will not allow the user to use this autotile
-- if any of the tiles in this table are not installed
autotile.requiredTiles = {
    "empty"
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
    for x=left, right do
        for y=top, bottom do
            rained.placeTile("empty", layer, x, y, forceModifier)
        end
    end
end