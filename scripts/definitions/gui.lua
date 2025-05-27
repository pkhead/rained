---@meta
rained.gui = {}

---@alias MenuName
---| "file"
---| "edit"
---| "view"
---| "help"

---@alias FileBrowserOpenModeSingle
---| "write"
---| "read"
---| "directory"

---A given filter has three formats:
---1. `{name (string), extWithPeriod (string)}` (ex: `{"PNG Image", ".png"}`)
---2. `{name (string), extensions (string[])}` (ex: `{"Image", {".png", ".jpg", ".tga", ".bmp"}}`)
---3. `rained.fileFilters.level`
---@alias FileBrowserFilter
---| table
---| userdata

---@class FileBrowser
local FileBrowser = {}

---Render the file browser popup window.
---@return boolean isOpen True if the window is still open, false if not.
function FileBrowser:render() end

---@type table<string, FileBrowserFilter>
rained.gui.fileFilters = {}

---@type userdata
local ud
rained.gui.fileFilters.level = ud

---Renders an ImGui file browser widget.
---@param id string
---@param openMode FileBrowserOpenModeSingle
---@param path string?
---@param filters FileBrowserFilter[]?
---@return boolean s, string? path
function rained.gui.fileBrowserWidget(id, openMode, path, filters) end

---Open the file browser, blocking the script until the user has either submitted or canceled.
---
---A given filter has three formats:
---1. `{name (string), extWithPeriod (string)}` (ex: `{"PNG Image", ".png"}`)
---2. `{name (string), extensions (string[])}` (ex: `{"Image", {".png", ".jpg", ".tga", ".bmp"}}`)
---3. `rained.fileFilters.level`
---
---@param openMode FileBrowserOpenMode
---@param filters FileBrowserFilter[]?
---@param callback fun(files: string[]) The callback to run when the user finishes using the file browser.
---@return FileBrowser
function rained.gui.openFileBrowser(openMode, filters, callback) end

---Register a hook into one or more menus from the menubar. If a menu of
---menuName does not already exist, it will be inserted into the menubar.
---
---It is expected that the callback function will insert ImGui widgets. Each
---individual callback that occupies the same menu is also split by separators.
---@param menuName string The name of the menu to hook into (case-sensitive)
---@param func fun() The function to run.
---@return CallbackHandle
function rained.gui.menuHook(menuName, func) end

---Register a hook into the preferences window.
---It is expected that the callback function will insert ImGui widgets.
---They will be rendered into the scripts page in the preferences window.
---@param func fun() The function to run.
---@return CallbackHandle
function rained.gui.prefsHook(func) end