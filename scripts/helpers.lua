local module = {}

function module.autotilePath(tileTable, layer, segments, forceModifier)
    for _, seg in ipairs(segments) do
        -- turns
        if seg.left and seg.down then
            rained.placeTile(tileTable.ws, layer, seg.x, seg.y, forceModifier)
        elseif seg.left and seg.up then
            rained.placeTile(tileTable.wn, layer, seg.x, seg.y, forceModifier)
        elseif seg.right and seg.down then
            rained.placeTile(tileTable.es, layer, seg.x, seg.y, forceModifier)
        elseif seg.right and seg.up then
            rained.placeTile(tileTable.en, layer, seg.x, seg.y, forceModifier)
        
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