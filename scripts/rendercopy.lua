--[[
    This is a helper script which will automatically copy render files to
    a user-defined target directory.

    Create a file named rendercopy.conf in the config folder, and
    write the path to the directory you want rendered level files to be
    copied to.
    
    I suppose I should make this file more documented. I would also like
    if there was a GUI for this in the editor, but I have yet to implement
    a GUI API...
--]]
local path = require("path")
local copyDir = nil

local function openFile(p, mode)
    local f = io.open(p, mode)
    if not f then
        error("could not open " .. p, 2)
    end
    return f
end

local function copyFile(a, b)
    local fileA = io.open(a, "rb")
    if not fileA then
        error("could not open " .. a, 2)
    end

    local fileB = io.open(b, "wb")
    if not fileB then
        fileA:close()
        error("could not open " .. b, 2)
    end

    print(("copy %s to %s"):format(a, b))

    fileB:write(fileA:read("a"))
    fileA:close()
    fileB:close()
end

-- read /config/rendercopyConfig.txt
local configFilePath = path.join(rained.getConfigDirectory(), "rendercopy.conf")
if path.isfile(configFilePath) then
    local f = openFile(configFilePath, "r")
    copyDir = f:read("l")
    f:close()

    if path.isdir(copyDir) then
        rained.onPostRender(function(src, dstGeo, pngPaths)
            local levelName = path.splitext(path.basename(src))
            print(("copy %s to %s"):format(levelName, copyDir))
    
            -- copy geo
            copyFile(dstGeo, path.join(copyDir, path.basename(dstGeo)))
    
            -- copy pngs
            for _, v in ipairs(pngPaths) do
                copyFile(v, path.join(copyDir, path.basename(v)))
            end
        end)
    else
        warn("rendercopy: " .. copyDir .. " does not exist!")
    end
end
