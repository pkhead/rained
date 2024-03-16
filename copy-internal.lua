-- script to copy drought internal tile images to assets/extra-previews
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
            name = string.match(name, "[A-Za-z ]+_%d+_(.*)")
        })
    end
end

for tileName in string.gmatch(initTxt, "%[#nm:\"(.-)\",.-%]") do
    for _, cast in ipairs(castData) do
        if cast.name:lower() == tileName:lower() .. ".png" then
            print(cast.name)

            local destFile = assert(io.open("assets/internal/" .. cast.name, "wb"))
            local srcFile = assert(io.open("Data/Cast/" .. cast.fileName, "rb"))
            destFile:write(srcFile:read("a"))

            destFile:close()
            srcFile:close()

            break
        end
    end
end