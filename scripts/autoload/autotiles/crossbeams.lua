-- Cross Beam A (negative slope)
do
    local autotile = rained.tiles.createAutotile("Cross Beam A", "Cross Beams")
    autotile.type = "rect"
    autotile.constrainToSquare = true
    autotile:addToggleOption("distant", "Distant", false)
    autotile:addToggleOption("secure", "Secured Ends", false)

    local tiles = {
        default = {
            main = "Cross Beam A",
            endA = "Cross Beam Secured NW",
            endB = "Cross Beam Secured SE"
        },
        distant = {
            main = "Cross Beam A Distant",
            endA = "Cross Beam Secured NW Distant",
            endB = "Cross Beam Secured SE Distant"
        }
    }

    autotile.requiredTiles = {
        tiles.default.main,
        tiles.default.endA,
        tiles.default.endB,

        tiles.distant.main,
        tiles.distant.endA,
        tiles.distant.endB,
    }

    function autotile:tileRect(layer, left, top, right, bottom, forceModifier)
        assert(right - left == bottom - top, "rect is not a square!")

        local conf = autotile:getOption("distant") and tiles.distant or tiles.default

        local endA, endB
        if autotile:getOption("secure") then
            endA = conf.endA
            endB = conf.endB
        else
            endA = conf.main
            endB = conf.main
        end

        rained.tiles.placeTile(endA, left, top, layer, forceModifier)
        rained.tiles.placeTile(endB, right, bottom, layer, forceModifier)

        for i=1, right - left - 1 do
            rained.tiles.placeTile(conf.main, left+i, top+i, layer, forceModifier)
        end
    end
end

-- Cross Beam B (positive slope)
do
    local autotile = rained.tiles.createAutotile("Cross Beam B", "Cross Beams")
    autotile.type = "rect"
    autotile.constrainToSquare = true
    autotile:addToggleOption("distant", "Distant", false)
    autotile:addToggleOption("secure", "Secured Ends", false)

    local tiles = {
        default = {
            main = "Cross Beam B",
            endA = "Cross Beam Secured NE",
            endB = "Cross Beam Secured SW"
        },
        distant = {
            main = "Cross Beam A Distant",
            endA = "Cross Beam Secured NE Distant",
            endB = "Cross Beam Secured SW Distant"
        }
    }

    autotile.requiredTiles = {
        tiles.default.main,
        tiles.default.endA,
        tiles.default.endB,

        tiles.distant.main,
        tiles.distant.endA,
        tiles.distant.endB,
    }

    function autotile:tileRect(layer, left, top, right, bottom, forceModifier)
        assert(right - left == bottom - top, "rect is not a square!")

        local conf = autotile:getOption("distant") and tiles.distant or tiles.default

        local endA, endB
        if autotile:getOption("secure") then
            endA = conf.endA
            endB = conf.endB
        else
            endA = conf.main
            endB = conf.main
        end

        rained.tiles.placeTile(endA, right, top, layer, forceModifier)
        rained.tiles.placeTile(endB, left, bottom, layer, forceModifier)

        for i=1, right - left - 1 do
            rained.tiles.placeTile(conf.main, right-i, top+i, layer, forceModifier)
        end
    end
end

-- Cross Beam Intersection
do
    local autotile = rained.tiles.createAutotile("Cross Beam Intersection", "Cross Beams")
    autotile.type = "rect"
    autotile.constrainToSquare = true
    autotile:addToggleOption("distant", "Distant", false)
    autotile:addToggleOption("secure", "Secured Ends", false)

    local tiles = {
        default = {
            a = "Cross Beam A",
            b = "Cross Beam B",
            middle = "Cross Beam Intersection",
            nw = "Cross Beam Secured NW",
            ne = "Cross Beam Secured NE",
            se = "Cross Beam Secured SE",
            sw = "Cross Beam Secured SW"
        },
        distant = {
            a = "Cross Beam A Distant",
            b = "Cross Beam B Distant",
            middle = "Cross Beam Intersection",
            nw = "Cross Beam Secured NW Distant",
            ne = "Cross Beam Secured NE Distant",
            se = "Cross Beam Secured SE Distant",
            sw = "Cross Beam Secured SW Distant"
        }
    }

    autotile.requiredTiles = {
        tiles.default.a,
        tiles.default.b,
        tiles.default.nw,
        tiles.default.ne,
        tiles.default.se,
        tiles.default.sw,
        tiles.default.middle,

        tiles.distant.a,
        tiles.distant.b,
        tiles.distant.nw,
        tiles.distant.ne,
        tiles.distant.se,
        tiles.distant.sw,
        tiles.distant.middle
    }

    function autotile:tileRect(layer, left, top, right, bottom, forceModifier)
        assert(right - left == bottom - top, "rect is not a square!")

        local conf = autotile:getOption("distant") and tiles.distant or tiles.default

        local endNW, endNE, endSE, endSW
        if autotile:getOption("secure") then
            endNW = conf.nw
            endNE = conf.ne
            endSE = conf.se
            endSW = conf.sw
        else
            endNW = conf.a
            endNE = conf.b
            endSE = conf.a
            endSW = conf.b
        end

        -- place the four ends
        rained.tiles.placeTile(endNW, left, top, layer, forceModifier)
        rained.tiles.placeTile(endNE, right, top, layer, forceModifier)
        rained.tiles.placeTile(endSE, right, bottom, layer, forceModifier)
        rained.tiles.placeTile(endSW, left, bottom, layer, forceModifier)

        -- place middle
        rained.tiles.placeTile(conf.middle, math.floor((left + right) / 2), math.floor((top + bottom) / 2), layer, "geometry")

        local w = right - left + 1
        local h = bottom - top + 1

        -- place beam A
        for i=1, math.floor(w / 2) - 1 do
            rained.tiles.placeTile(conf.a, left+i, top+i, layer, forceModifier)
            rained.tiles.placeTile(conf.a, right-i, bottom-i, layer, forceModifier)

            rained.tiles.placeTile(conf.b, right-i, top+i, layer, forceModifier)
            rained.tiles.placeTile(conf.b, left+i, bottom-i, layer, forceModifier)
        end

        -- for i=1, right - left - 1 do
        --     rained.tiles.placeTile(conf.main, right-i, top+i, layer, forceModifier)
        -- end
    end

    -- size needs to be odd for the middle section to be placed correctly
    function autotile:verifySize(left, top, right, bottom)
        local w = right - left + 1
        local h = bottom - top + 1

        return w > 1 and h > 1 and w % 2 == 1 and h % 2 == 1
    end
end