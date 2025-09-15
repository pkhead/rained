--[[
    This is a helper script which will automatically copy render files to
    a user-defined target directory.
    
    Configure this in the preferences window.
--]]

local path = require("path")
local helpers = require("helpers")

local config = {
    version = "2.0.0",

    enabled = false,
    copyDirectories = {},

    fallbackMode = "off", -- "off", "on", or "force"
    fallbackDirectory = nil
}

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

-- initial read of config file
local configFilePath = path.join(rained.getConfigDirectory(), "rendercopy.conf")
if path.isfile(configFilePath) then
    -- read file line-by-line
    local f = openFile(configFilePath, "r")
    local configTxtLines = {}
    for l in f:lines() do
        configTxtLines[#configTxtLines+1] = l
    end
    f:close()

    -- v2 and above write a lua table as the config
    local configTxt = table.concat(configTxtLines, "\n")
    if configTxt:sub(0, 6) == "return" then
        local chunk, err = load(configTxt, "@" .. configFilePath, "t")
        if chunk then
            local s, data = pcall(chunk)

            if s then
                config = data
            else
                rained.alert("Error loading rendercopy config")
                error(("error loading rendercopy.conf: %s"):format(err))
            end
        else
            rained.alert("Error loading rendercopy config")
            error(("error loading rendercopy.conf: %s"):format(err))
        end
    else
        print("rendercopy: reading v1 config file")

        config.fallbackMode = "on"
        config.fallbackDirectory = configTxtLines[1] -- first line: copy directory
        config.enabled = configTxtLines[2] == "true" -- second line: enable flag
    end
    -- loadfile(configFilePath, "t")()
    -- local f = openFile(configFilePath, "r")
    -- local txt = f:read("a")
    -- f:close()

    -- configFilePath = load(txt, "@" .. configFilePath, "t")()

    -- copyDir = f:read("l") -- first line: copy directory
    -- isEnabled = f:read("l") == "true" -- second line: enable flag
    -- f:close()

    -- if not path.isdir(copyDir) then
    --     warn("rendercopy: " .. copyDir .. " does not exist!")
    --     copyDir = nil
    -- end
end

-- render hook
rained.onPostRender(function(src, dstGeo, pngPaths)
    if not config.enabled then
        return
    end

    local levelName = path.splitext(path.basename(src))
    local prefix = string.match(string.upper(levelName), "(.-)_.*")
    local copyDir = nil

    if config.fallbackMode ~= "force" then
        for _, dir in ipairs(config.copyDirectories) do
            if string.upper(dir.prefix) == prefix then
                copyDir = dir.path
                break
            end
        end
    end

    if copyDir == nil and config.fallbackMode ~= "off" then
        copyDir = config.fallbackDirectory
    end

    if copyDir ~= nil and path.isdir(copyDir) then
        print(("copy %s to %s"):format(levelName, copyDir))

        -- copy geo
        copyFile(dstGeo, path.join(copyDir, path.basename(dstGeo)))

        -- copy pngs
        for _, v in ipairs(pngPaths) do
            copyFile(v, path.join(copyDir, path.basename(v)))
        end    
    end
end)

-- the gui
if not rained.isBatchMode() then
    local imgui = require("imgui")

    local currentlyEditedRowIndex = nil
    local rowEdit = nil
    local fileBrowser = nil

    -- update config file with current settings
    local function saveConfig()
        -- sanity check?
        -- if not path.isdir(copyDir) then
        --     warn("rendercopy: " .. copyDir .. " does not exist!")
        --     copyDir = nil
        -- end

        local serialized = helpers.serialize(config)
        local f = openFile(configFilePath, "w")
        f:write("return ")
        f:write(serialized)
        f:write("\n")
        f:close()
    end

    -- https://github.com/ocornut/imgui/issues/5267#issuecomment-2408993109
    local function textTruncated(p_text, p_truncated_width)
        local truncated_text = p_text;

        local text_width = imgui.CalcTextSize(0,0, p_text, nil, true);

        local doTruncate = text_width > p_truncated_width
        if doTruncate then
            local ELLIPSIS = "...";
            local ellipsis_size = imgui.CalcTextSize(0,0, ELLIPSIS);

            local visible_chars = 0;
            for i=0, string.len(p_text) - 1 do
                local current_width = imgui.CalcTextSize(0,0,
                        p_text:sub(1, i+1), nil, true)
                
                if (current_width + ellipsis_size > p_truncated_width) then
                    break
                end

                visible_chars = i;
            end

            truncated_text = (p_text:sub(1, visible_chars+1) .. ELLIPSIS);
        end

        imgui.Text(truncated_text);
        if doTruncate then
            if imgui.IsItemHovered(imgui.HoveredFlags_DelayNormal) then
                imgui.SetTooltip(p_text)
            end
        end
    end

    -- -- preferences ui
    rained.gui.prefsHook(function()
        local s

        imgui.SeparatorText("RenderCopy")

        imgui.Text("author: pkhead")
        imgui.TextWrapped("This is a script which copies files from renders to a given folder. You can put any folder, but you probably want to copy it to your region's rooms folder.")

        s, config.enabled = imgui.Checkbox("Enable", config.enabled)
        if s then
            saveConfig()
        end

        local acronymColWidth = imgui.CalcTextSize(0, 0, "XXXXXX")

        if imgui.BeginTable("configTable", 3, imgui.TableFlags_Borders | imgui.TableFlags_RowBg | imgui.TableFlags_Resizable | imgui.TableFlags_SizingFixedFit) then
            imgui.TableSetupColumn("Acronym", imgui.TableColumnFlags_WidthFixed, acronymColWidth)
            imgui.TableSetupColumn("Folder", imgui.TableColumnFlags_WidthStretch)
            imgui.TableSetupColumn("Actions")
            imgui.TableHeadersRow()

            local indexToRemove = nil

            for i, dirConfig in ipairs(config.copyDirectories) do
                imgui.PushID_Int(i)

                imgui.TableNextRow()
                
                if i == currentlyEditedRowIndex then
                    assert(rowEdit)

                    imgui.TableNextColumn()
                    imgui.PushStyleVar_Vec2(imgui.StyleVar_FramePadding, 0, 0)
                    imgui.InputText("##acronym", rowEdit.acronymInputBuf)
                    imgui.PopStyleVar()

                    imgui.TableNextColumn()
                    _, rowEdit.path = rained.gui.fileBrowserWidget("##path", "directory", rowEdit.path)
                    if imgui.IsItemHovered(imgui.HoveredFlags_DelayNormal) then
                        imgui.SetTooltip(rowEdit.path)
                    end

                    imgui.TableNextColumn()
                    if imgui.SmallButton("OK") then
                        dirConfig.prefix = string.upper(tostring(rowEdit.acronymInputBuf))
                        dirConfig.path = rowEdit.path

                        currentlyEditedRowIndex = nil
                        rowEdit = nil
                        saveConfig()
                    end

                    imgui.SameLine()
                    if imgui.SmallButton("Cancel") then
                        currentlyEditedRowIndex = nil
                        rowEdit = nil
                    end
                else
                    imgui.TableNextColumn()
                    imgui.Text(dirConfig.prefix)

                    imgui.TableNextColumn()
                    textTruncated(dirConfig.path, imgui.GetContentRegionAvail(0,0))

                    imgui.TableNextColumn()
                    imgui.BeginDisabled(currentlyEditedRowIndex ~= nil)

                    if imgui.SmallButton("Edit") then
                        currentlyEditedRowIndex = i
                        rowEdit = {
                            acronymInputBuf = imgui.newBuffer(64),
                            path = dirConfig.path
                        }

                        rowEdit.acronymInputBuf:set(dirConfig.prefix)
                    end

                    imgui.SameLine()
                    if imgui.SmallButton("Delete") then
                        indexToRemove = i
                    end

                    imgui.EndDisabled()
                end

                imgui.PopID()
            end

            imgui.EndTable()

            if indexToRemove then
                table.remove(config.copyDirectories, indexToRemove)
                saveConfig()
            end
        end

        if imgui.Button("Add", -0.0000001, 0) then
            fileBrowser = rained.gui.openFileBrowser("directory", {}, function(dirs)
                if dirs[1] then
                    local prefix = "??"
                    local dirName = string.upper(path.basename(path.dirname(dirs[1])))
                    print(dirName)
                    local matchPrefix = string.match(dirName, "(.-)%-ROOMS")
                    if matchPrefix then
                        prefix = matchPrefix
                    end
                    
                    table.insert(config.copyDirectories, {
                        path = dirs[1],
                        prefix = prefix
                    })

                    saveConfig()
                end

                fileBrowser = nil
            end)
        end

        imgui.Separator()

        imgui.BeginGroup()
            imgui.AlignTextToFramePadding()
            imgui.Text("Fallback Mode")
            imgui.SameLine()
            imgui.TextDisabled("(?)")
            if imgui.BeginItemTooltip() then
                imgui.PushTextWrapPos(imgui.GetTextLineHeight() * 20)
                imgui.TextWrapped("When a level is rendered and its acronym prefix is not registered, it will copy the level renders to the fallback directory.")
                imgui.TextWrapped("If \"force\", it will copy all level renders to the fallback directory even if its acronym prefix is registered.")
                imgui.PopTextWrapPos()
                imgui.EndTooltip()
            end
            imgui.AlignTextToFramePadding()
            imgui.Text("Fallback Folder")
        imgui.EndGroup()
        imgui.SameLine()
        imgui.BeginGroup()
            imgui.SetNextItemWidth(imgui.GetTextLineHeight() * 7.0)
            if imgui.BeginCombo("##fallbackmode", config.fallbackMode) then
                if imgui.Selectable_Bool("off", config.fallbackMode == "off") then
                    config.fallbackMode = "off"
                    saveConfig()
                end

                if imgui.Selectable_Bool("on", config.fallbackMode == "on") then
                    config.fallbackMode = "on"
                    saveConfig()
                end

                if imgui.Selectable_Bool("force", config.fallbackMode == "force") then
                    config.fallbackMode = "force"
                    saveConfig()
                end

                imgui.EndCombo()
            end

            imgui.PushID_Str("fallback")
            s, config.fallbackDirectory = rained.gui.fileBrowserWidget("##fallbackPath", "directory", config.fallbackDirectory)
            if s then
                saveConfig()
            end
            
            imgui.PopID()
        imgui.EndGroup()

        if fileBrowser then
            fileBrowser:render()
        end

        -- imgui.AlignTextToFramePadding()
        -- imgui.Text("Copy Directory")
        -- imgui.SameLine()

        -- local s
        -- s, copyDir = rained.gui.fileBrowserWidget("Copy Directory", "directory", copyDir)
        -- if s then
        --     updateFile()
        -- end

        -- imgui.BeginDisabled(copyDir == nil)
        -- s, isEnabled = imgui.Checkbox("Enabled", isEnabled)
        -- if s then
        --     updateFile()
        -- end
        -- imgui.EndDisabled()
    end)
end
