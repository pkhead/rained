-- setup autotile data
-- setup autotile data
local autotile = rained.createAutotile("Fence")
autotile.type = "rect"

autotile:addToggleOption("altFence", "Use Ext. Fence", false)
autotile:addToggleOption("altWire", "Use Ext. Barbed Wire", false)
autotile:addToggleOption("poleMode", "Use Pole Count", true)
autotile:addIntOption("poleSpacing", "Pole Spacing", 8, 1, math.huge)
autotile:addIntOption("numPoles", "Pole Count", 8, 0, math.huge)
autotile:addIntOption("wireHeight", "Barbed Wire Height", 1, 0, math.huge)

-- Rained will not allow the user to use this autotile
-- if any of the tiles in this table are not installed
autotile.requiredTiles = {
    "fence",
    "fenceExtended",
    "fence with pole",
    "fence top end",
    "fence with pole top end",
    "barbed wire pole",
    "barbed wire",
    "barbed wire pole 2",
    "barbed wire 2",
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
    local wireHeight = autotile:getOption("wireHeight")
    local fenceTop = top + wireHeight

    local fenceTile = autotile:getOption("altFence") and "fenceExtended" or "fence"
    local wireTile = autotile:getOption("altWire") and "barbed wire 2" or "barbed wire"
    local wirePoleTile = autotile:getOption("altWire") and "barbed wire pole 2" or "barbed wire pole"

    for x = left, right do
        -- place barbed wire
        for y = top, fenceTop-1 do
            rained.placeTile(wireTile, x, y, layer, forceModifier)
        end

        -- place fence
        rained.placeTile("fence top end", x, fenceTop, layer, forceModifier)
        for y = fenceTop, bottom do
            rained.placeTile(fenceTile, x, y, layer, forceModifier)
        end
    end

    -- calculate pole spacing
    local poleSpacing = autotile:getOption("poleSpacing")

    if autotile:getOption("poleMode") and autotile:getOption("numPoles") > 0 then
        poleSpacing = math.ceil((right - left + 1) / (autotile:getOption("numPoles") + 1))
    end

    -- place poles
    for x = left + poleSpacing - 1, right, poleSpacing do
        for y = top, bottom do
            rained.deleteTile(x, y, layer)

            if y < fenceTop then
                rained.placeTile(wirePoleTile, x, y, layer, forceModifier)
            elseif y == fenceTop then
                rained.placeTile("fence with pole top end", x, y, layer, forceModifier)
            else
                rained.placeTile("fence with pole", x, y, layer, forceModifier)
            end
        end
    end
end