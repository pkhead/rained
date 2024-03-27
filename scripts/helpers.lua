local module = {}

---Perform standard path autotiling
---@param tileTable {ld: string, lu: string, rd: string, ru: string, vertical: string, horizontal: string}
---@param layer integer
---@param segments PathSegment[]
---@param startIndex integer? The index of the starting segment. Defaults to 1
---@param endIndex integer? The number of segments to place. Defaults to the length of the segment array
---@param forceModifier ForceModifier
function module.autotilePath(tileTable, layer, segments, forceModifier, startIndex, endIndex)
    for i=startIndex or 1, endIndex or #segments do
        local seg = segments[i]

        -- turns
        if seg.left and seg.down then
            rained.placeTile(tileTable.ld, layer, seg.x, seg.y, forceModifier)
        elseif seg.left and seg.up then
            rained.placeTile(tileTable.lu, layer, seg.x, seg.y, forceModifier)
        elseif seg.right and seg.down then
            rained.placeTile(tileTable.rd, layer, seg.x, seg.y, forceModifier)
        elseif seg.right and seg.up then
            rained.placeTile(tileTable.ru, layer, seg.x, seg.y, forceModifier)
        
        -- straight
        elseif seg.down or seg.up then
            rained.placeTile(
                tileTable.vertical,
                layer, seg.x, seg.y, forceModifier
            )
        elseif seg.right or seg.left then
            rained.placeTile(
                tileTable.horizontal,
                layer, seg.x, seg.y, forceModifier
            )
        end
    end
end

return module