-- setup autotile data
local autotile = rained.createAutotile("Thick Pipe", "Pipes")
autotile.type = "path"
autotile.pathThickness = 2
autotile.segmentLength = 1

-- Rained will not allow the user to use this autotile
-- if any of the tiles in this table are not installed
autotile.requiredTiles = {
    "Thick pipe vertical",
    "Thick pipe horizontal",
    "Thick pipe vertical filler",
    "Thick pipe horizontal filler",
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
    vertical = "Thick pipe vertical filler",
    horizontal = "Thick pipe horizontal filler"
}

---returns true if the given segment is a turn
---@param segment PathSegment
---@returns boolean
local function isTurn(segment)
    return (segment.left or segment.right) and (segment.up or segment.down)
end

-- this is the callback function that Rained invokes when the user
-- wants to autotile a given path
---@param layer integer The layer to run the autotiler on
---@param segments PathSegment[] The list of path segments
---@param forceModifier ForceModifier Force-placement mode, as a string. Can be nil, "force", or "geometry".
function autotile:tilePath(layer, segments, forceModifier)
    -- this code will not run the standard autotiler (helpers.autotilePath)
    -- so that the autotiler may place full or filler pipe tiles when needed
    local skipSegment = false
    for i=1, #segments do
        local seg = segments[i]
        
        local vertical = tileTable.vertical
        local horizontal = tileTable.horizontal
        local didPlaceFull = false

        if not skipSegment then
            -- turns
            if seg.left and seg.down then
                rained.placeTile(tileTable.ld, seg.x, seg.y, layer, forceModifier)
            elseif seg.left and seg.up then
                rained.placeTile(tileTable.lu, seg.x, seg.y, layer, forceModifier)
            elseif seg.right and seg.down then
                rained.placeTile(tileTable.rd, seg.x, seg.y, layer, forceModifier)
            elseif seg.right and seg.up then
                rained.placeTile(tileTable.ru, seg.x, seg.y, layer, forceModifier)

            -- straight tiles
            else
                -- the autotiler will place a full tile on two condition if
                -- the next segment exists and is a turn
                -- it will then make sure it skips unnecessarily placing the next segment
                local x = seg.x
                local y = seg.y

                if segments[i+1] ~= nil and not isTurn(segments[i+1]) then
                    vertical = "Thick pipe vertical"
                    horizontal = "Thick pipe horizontal"
                    didPlaceFull = true

                    -- if the segment is moving left or up,
                    -- using the segment's position directly will
                    -- result in an incorrect tile placement.
                    -- this piece of code corrects that
                    if segments[i+1].x < seg.x then
                        x = x - 1
                    end
                    
                    if segments[i+1].y < seg.y then
                        y = y - 1
                    end
                end

                -- vertical
                if seg.down or seg.up then
                    rained.placeTile(
                        vertical,
                        x, y, layer, forceModifier
                    )

                -- horizontal
                elseif seg.right or seg.left then
                    rained.placeTile(
                        horizontal,
                        x, y, layer, forceModifier
                    )
                end
            end
        end

        skipSegment = didPlaceFull
    end
end