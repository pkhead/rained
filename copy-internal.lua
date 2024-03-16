-- script to copy drought internal tile images to assets/extra-previews
-- ok i know lua is a weird choice but i never bothered to learn python

local lfs = require("lfs") -- luafilesystem
local initFile = assert(io.open("Data/Cast/Drought_393439_Drought Needed Init.txt", "r"), "could not open file")
local initTxt = initFile:read("a")
initFile:close()

local files = {}

for name in lfs.dir("Data/Cast/") do
    if string.sub(name, 1, 8) == "Drought_" then
        table.insert(files, name)
    end
end

for tileName in string.gmatch(initTxt, "%[#nm:\"(.-)\",.-%]") do
    for _, fileName in ipairs(files) do
        local castName = string.sub(fileName, 16)
        if castName:lower() == tileName:lower() .. ".png" then
            print(castName)

            local destFile = assert(io.open("assets/internal/" .. castName, "wb"))
            local srcFile = assert(io.open("Data/Cast/" .. fileName, "rb"))
            destFile:write(srcFile:read("a"))

            destFile:close()
            srcFile:close()

            break
        end
    end
end