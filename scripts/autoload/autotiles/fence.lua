-- setup autotile data
-- setup autotile data
local autotile = rained.createAutotile("Fence")
autotile.type = "rect"

autotile:addToggleOption("altWire", "Use Alt Barbed Wire", false)
autotile:addToggleOption("poleMode", "Use Pole Count", false)
autotile:addIntOption("poleSpacing", "Pole Spacing", 8, 1, math.huge)
autotile:addIntOption("numPoles", "Pole Count", 5, 0, math.huge)
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

    local altWire = autotile:getOption("altWire")
    
    for x = left, right do
        -- place barbed wire
        for y = top, fenceTop-1 do
            local wireTile = "barbed wire"
            if altWire and (y - top) % 2 == 0 then
                wireTile = "barbed wire 2"
            end

            rained.placeTile(wireTile, x, y, layer, forceModifier)
        end

        -- place fence
        rained.placeTile("fence top end", x, fenceTop, layer, forceModifier)
        for y = fenceTop, bottom do
            rained.placeTile("fence", x, y, layer, forceModifier)
        end
    end

    -- calculate pole spacing
    local poleSpacing
    local poleOffset = 0
    local boxWidth = right - left + 1

    if autotile:getOption("poleMode") then
        poleSpacing = math.ceil(boxWidth / autotile:getOption("numPoles"))

        -- this will attempt to center the poles
        local fit = (autotile:getOption("numPoles") - 1) * poleSpacing + 1
        poleOffset = math.floor((boxWidth - fit) / 2)
    else
        poleSpacing = autotile:getOption("poleSpacing")

        -- this will attempt to center the poles
        local fit = (math.ceil(boxWidth / poleSpacing) - 1) * poleSpacing + 1
        poleOffset = math.floor((boxWidth - fit) / 2)
    end

    -- place poles
    for x = left + math.max(0, poleOffset), right, poleSpacing do
        for y = top, bottom do
            rained.deleteTile(x, y, layer)

            if y < fenceTop then
                local wirePoleTile = "barbed wire pole"
                if altWire and (y - top) % 2 == 0 then
                    wirePoleTile = "barbed wire pole 2"
                end

                rained.placeTile(wirePoleTile, x, y, layer, forceModifier)
            elseif y == fenceTop then
                rained.placeTile("fence with pole top end", x, y, layer, forceModifier)
            else
                rained.placeTile("fence with pole", x, y, layer, forceModifier)
            end
        end
    end
end