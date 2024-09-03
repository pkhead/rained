-- script to map cast name to cast file name in drizzle cast data
-- (so that rained can properly read it...)

local lfs = require("lfs") -- luafilesystem
local initFile = assert(io.open("assets/drizzle-cast/Drought_393439_Drought Needed Init.txt", "r"), "could not open file")
local initTxt = initFile:read("a")
initFile:close()

local castData = {}

for name in lfs.dir("assets/drizzle-cast/") do
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

-- i'm running this on msys2 (too lazy to get lua on windows properly)
local outFile = assert(io.popen("clip.exe", "w"), "could not open clip.exe")
outFile:write(table.concat(outputLines, "\n"))
outFile:close()
print("Copied to clipboard!")