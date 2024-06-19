--[[
    This script adds a command/"macro" that will
    fix all of the detached tile bodies in the level.
]]

local function fixTileHead(rootX, rootY, rootL)
    local tileName = assert(rained.tiles.getTileAt(rootX, rootY, rootL))
    local tileInfo = assert(rained.tiles.getTileInfo(tileName))

    local specIndex = 1
    for localX=0, tileInfo.width - 1 do
        for localY=0, tileInfo.height - 1 do
            local worldX = rootX - tileInfo.centerX + localX
            local worldY = rootY - tileInfo.centerY + localY

            if worldX ~= rootX or worldY ~= rootY then
                -- fix on layer 1
                if tileInfo.specs[specIndex] >= 0 then
                    local tname, rx, ry, rl = rained.cells.getTileData(worldX, worldY, rootL)
                    if tname ~= nil or rx ~= rootX or ry ~= rootY ~= rl ~= rootL then
                        rained.cells.setTileHead(worldX, worldY, rootL, nil)
                        rained.cells.setTileRoot(worldX, worldY, rootL, rootX, rootY, rootL)
                    end
                end

                -- fix on layer 2
                if tileInfo.specs2 ~= nil then
                    local layer2 = rootL + 1
                    if layer2 <= 3 and tileInfo.specs2[specIndex] >= 0 then
                        local tname, rx, ry, rl = rained.cells.getTileData(worldX, worldY, layer2)
                        if tname ~= nil or rx ~= rootX or ry ~= rootY ~= rl ~= rootL then
                            rained.cells.setTileHead(worldX, worldY, layer2, nil)
                            rained.cells.setTileRoot(worldX, worldY, layer2, rootX, rootY, rootL)
                        end
                    end
                end
            end

            specIndex = specIndex + 1
        end
    end
end

rained.registerCommand("Fix Detached Tiles", function()
    for x=0, rained.getLevelWidth() - 1 do
        for y=0, rained.getLevelHeight() - 1 do
            for layer=1, 3 do
                if rained.tiles.hasTileHead(x, y, layer) then
                    fixTileHead(x, y, layer)
                end
            end
        end
    end
end)