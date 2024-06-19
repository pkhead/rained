-- setup autotile data
local autotile = rained.tiles.createAutotile("Ventbox")
autotile.type = "rect"

-- Rained will not allow the user to use this autotile
-- if any of the tiles in this table are not installed
autotile.requiredTiles = {
    "Ventbox NW",
    "Ventbox NE",
    "Ventbox SE",
    "Ventbox SW",
    "Ventbox"
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
    -- the minimum size of the box is 4x4
    if (right - left) + 1 < 4 or (bottom - top) + 1 < 4 then
        rained.alert("The box is too small!")
        return
    end

    -- place ventbox corners
    rained.tiles.placeTile("Ventbox NW", left, top, layer, forceModifier)
    rained.tiles.placeTile("Ventbox NE", right-1, top, layer, forceModifier)
    rained.tiles.placeTile("Ventbox SW", left, bottom-1, layer, forceModifier)
    rained.tiles.placeTile("Ventbox SE", right-1, bottom-1, layer, forceModifier)

    -- place ventbox sides
    for x=left+2, right-2 do
        rained.tiles.placeTile("Ventbox N", x, top, layer, forceModifier)
        rained.tiles.placeTile("Ventbox S", x, bottom-1, layer, forceModifier)
    end

    for y=top+2, bottom-2 do
        rained.tiles.placeTile("Ventbox W", left, y, layer, forceModifier)
        rained.tiles.placeTile("Ventbox E", right-1, y, layer, forceModifier)
    end

    -- place ventbox interiors
    for x=left+2, right-2 do
        for y=top+2, bottom-2 do
            rained.tiles.placeTile("Ventbox", x, y, layer, forceModifier)
        end
    end
end