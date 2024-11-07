local module = {}

-- this data is for the patternBox helper function.
-- adapted from tileEditor.lingo
local patterns = {
    {tiles = {"A"}, upper = "dense", lower = "dense", tall = 1, freq = 5},
    {tiles = {"B1"}, upper = "espaced", lower = "dense", tall = 1, freq = 5},
    {tiles = {"B2"}, upper = "dense", lower = "espaced", tall = 1, freq = 5},
    {tiles = {"B3"}, upper = "ospaced", lower = "dense", tall = 1, freq = 5},
    {tiles = {"B4"}, upper = "dense", lower = "ospaced", tall = 1, freq = 5},
    {tiles = {"C1"}, upper = "espaced", lower = "espaced", tall = 1, freq = 5},
    {tiles = {"C2"}, upper = "ospaced", lower = "ospaced", tall = 1, freq = 5},
    {tiles = {"E1"}, upper = "ospaced", lower = "espaced", tall = 1, freq = 5},
    {tiles = {"E2"}, upper = "espaced", lower = "ospaced", tall = 1, freq = 5},
    {tiles = {"F1"}, upper = "dense", lower = "dense", tall = 2, freq = 1},
    {tiles = {"F2"}, upper = "dense", lower = "dense", tall = 2, freq = 1},
    {tiles = {"F1", "F2"}, upper = "dense", lower = "dense", tall = 2, freq = 5},
    {tiles = {"F3"}, upper = "dense", lower = "dense", tall = 2, freq = 5},
    {tiles = {"F4"}, upper = "dense", lower = "dense", tall = 2, freq = 5},
    {tiles = {"G1", "G2"}, upper = "dense", lower = "ospaced", tall = 2, freq = 5},
    {tiles = {"I"}, upper = "espaced", lower = "dense", tall = 1, freq = 4},
    {tiles = {"J1"}, upper = "ospaced", lower = "ospaced", tall = 2, freq = 1},
    {tiles = {"J2"}, upper = "ospaced", lower = "ospaced", tall = 2, freq = 1},
    {tiles = {"J1", "J2"}, upper = "ospaced", lower = "ospaced", tall = 2, freq = 2},
    {tiles = {"J3"}, upper = "espaced", lower = "espaced", tall = 2, freq = 1},
    {tiles = {"J4"}, upper = "espaced", lower = "espaced", tall = 2, freq = 1},
    {tiles = {"J3", "J4"}, upper = "espaced", lower = "espaced", tall = 2, freq = 2},
    {tiles = {"B1", "I"}, upper = "espaced", lower = "dense", tall = 1, freq = 2}
}

---The algorithm used to generate SH pattern box, SH grate box, and Alt Grate Box.
---Adapted from the original Lingo code.
---@param prefix string The string to prepend to the tile name when placing.
---@param layer integer The layer to tile.
---@param left integer The left side of the rectangle.
---@param top integer The top side of the rectangle.
---@param right integer The right side of the rectangle.
---@param bottom integer The bottom side of the rectangle.
---@param forceModifier ForceModifier
---@param placeBorder boolean? True if the border should be placed, false if not. (defaults to true)
function module.patternBox(prefix, layer, left, top, right, bottom, forceModifier, placeBorder)
    if placeBorder == nil then
        placeBorder = true
    end

    if placeBorder then
        rained.tiles.placeTile("Block Corner NW", left, top, layer, forceModifier)
        rained.tiles.placeTile("Block Corner NE", right, top, layer, forceModifier)
        rained.tiles.placeTile("Block Corner SE", right, bottom, layer, forceModifier)
        rained.tiles.placeTile("Block Corner SW", left, bottom, layer, forceModifier)

        -- fill sides
        for x = left + 1, right - 1 do
            rained.tiles.placeTile("Block Edge N", x, top, layer, forceModifier)
            rained.tiles.placeTile("Block Edge S", x, bottom, layer, forceModifier)
        end

        for y = top + 1, bottom - 1 do
            rained.tiles.placeTile("Block Edge W", left, y, layer, forceModifier)
            rained.tiles.placeTile("Block Edge E", right, y, layer, forceModifier)
        end
    else
        left = left - 1
        top = top - 1
        right = right + 1
        bottom = bottom + 1
    end

    -- the following code is translated straight from the lingo code
    local py = top + 1
    local currentPattern = patterns[math.random(#patterns)]

    while py < bottom do
        local possiblePatterns = {}

        for q=1, #patterns do
            if patterns[q].upper == currentPattern.lower and py + patterns[q].tall < bottom + 1 then
                for _=1, patterns[q].freq do
                    possiblePatterns[#possiblePatterns+1] = q
                end
            end
        end

        currentPattern = patterns[possiblePatterns[math.random(#possiblePatterns)]]
        local tl = math.random(#currentPattern.tiles)

        for px = left + 1, right - 1 do
            tl = tl + 1
            if tl > #currentPattern.tiles then
                tl = 1
            end

            rained.tiles.placeTile(prefix .. currentPattern.tiles[tl], px, py, layer, forceModifier)
        end

        py = py + currentPattern.tall
    end
end

return module