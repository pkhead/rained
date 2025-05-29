--[[
    This is a helper script which will automatically copy render files to
    a user-defined target directory.
    
    Configure this in the preferences window.
--]]

local path = require("path")
local imgui = require("imgui")

-- location to copy files to
local copyDir = nil
-- enable/disable flag
local isEnabled = false

-- helper function to open file, and error on failure
local function openFile(p, mode)
    local f = io.open(p, mode)
    if not f then
        error("could not open " .. p, 2)
    end
    return f
end

-- helper function to copy file a to b
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

-- render hook
rained.onPostRender(function(src, dstGeo, pngPaths)
    if not isEnabled then
        return
    end

    if copyDir == nil or not path.isdir(copyDir) then
        return
    end

    local levelName = path.splitext(path.basename(src))
    print(("copy %s to %s"):format(levelName, copyDir))

    -- copy geo
    copyFile(dstGeo, path.join(copyDir, path.basename(dstGeo)))

    -- copy pngs
    for _, v in ipairs(pngPaths) do
        copyFile(v, path.join(copyDir, path.basename(v)))
    end
end)

-- initial read of config file
local configFilePath = path.join(rained.getConfigDirectory(), "rendercopy.conf")
if path.isfile(configFilePath) then
    local f = openFile(configFilePath, "r")
    copyDir = f:read("l") -- first line: copy directory
    isEnabled = f:read("l") == "true" -- second line: enable flag
    f:close()

    if not path.isdir(copyDir) then
        warn("rendercopy: " .. copyDir .. " does not exist!")
        copyDir = nil
    end
end

-- the gui
if not rained.isBatchMode() then
    -- update config file with current settings
    local function updateFile()
        -- sanity check?
        if not path.isdir(copyDir) then
            warn("rendercopy: " .. copyDir .. " does not exist!")
            copyDir = nil
        end
        
        local f = openFile(configFilePath, "w")
        f:write(copyDir)
        f:write("\n")
        f:write(isEnabled and "true" or "false")
        f:write("\n")
        f:close()
    end

    -- preferences ui
    rained.gui.prefsHook(function()
        imgui.SeparatorText("RenderCopy")

        imgui.Text("author: pkhead")
        imgui.TextWrapped("This is a script which copies files from renders to a given directory. You can put any directory, but you probably want to copy it to your region's rooms folder.")

        imgui.AlignTextToFramePadding()
        imgui.Text("Copy Directory")
        imgui.SameLine()

        local s
        s, copyDir = rained.gui.fileBrowserWidget("Copy Directory", "directory", copyDir)
        if s then
            updateFile()
        end

        s, isEnabled = imgui.Checkbox("Enabled", isEnabled)
        if s then
            updateFile()
        end
    end)
end
