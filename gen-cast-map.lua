-- script to map cast name to cast file name in drizzle cast data
-- (so that rained can properly read it...)
-- ok i know lua is a weird choice but i never bothered to learn python

local lfs = require("lfs") -- luafilesystem
local initFile = assert(io.open("Data/Cast/Drought_393439_Drought Needed Init.txt", "r"), "could not open file")
local initTxt = initFile:read("a")
initFile:close()

local castData = {}

for name in lfs.dir("Data/Cast/") do
    if string.sub(name, string.len(name) - 3) == ".png" then
        table.insert(castData, {
            fileName = name,
            name = string.match(name, "[A-Za-z ]+_%d+_(.+)")
        })
    end
end

local outputLines = {}

for tileName in string.gmatch(initTxt, "%[#nm:\"(.-)\",.-%]") do
    for _, cast in ipairs(castData) do
        if cast.name:lower() == tileName:lower() .. ".png" then
            table.insert(outputLines, ("{\"%s\", \"%s\"},"):format(tileName, cast.fileName))
            break
        end
    end
end

local outFile = assert(io.open("assets/internal-map.ini", "w"), "could not open file")
outFile:write(table.concat(outputLines, "\n"))
outFile:close()