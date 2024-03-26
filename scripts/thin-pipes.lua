local autotile = Rained.createAutotile()
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

function autotile:fillPath(layer, segments, forceModifier)
    local vertPipe = self:getOption("plain") and "Vertical Plain Pipe" or "Vertical Pipe"
    local horizPipe = self:getOption("plain") and "Horizontal Plain Pipe" or "Horizontal Pipe"

    for seg in ipairs(segments) do
        -- turns
        if seg.left and seg.down then
            Rained.placeTile("Pipe WS", layer, seg.x, seg.y, forceModifier)
        elseif seg.left and seg.up then
            Rained.placeTile("Pipe WN", layer, seg.x, seg.y, forceModifier)
        elseif seg.right and seg.down then
            Rained.placeTile("Pipe ES", layer, seg.x, seg.y, forceModifier)
        elseif seg.right and seg.up then
            Rained.placeTile("Pipe EN", layer, seg.x, seg.y, forceModifier)
        
        -- straight
        elseif seg.down and seg.up then
            Rained.placeTile(
                vertPipe,
                layer, seg.x, seg.y, forceModifier
            )
        elseif seg.right and seg.left then
            Rained.placeTile(
                horizPipe,
                layer, seg.x, seg.y, forceModifier
            )
        end
    end
end