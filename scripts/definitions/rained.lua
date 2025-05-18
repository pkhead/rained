---@meta
-- Type annotations for Lua analzyers and linters, such as sumneko-lua

---automatically require all Lua files in a require path
---@param path string The path to require
---@param recurse boolean? If the function should recurse through all sub-paths. Defaults to false.
function autorequire(path, recurse) end

rained = {}

GEO_TYPE = {
    AIR = 0,
    SOLID = 1,
    SLOPE_RIGHT_UP = 2,
    SLOPE_LEFT_UP = 3,
    SLOPE_RIGHT_DOWN = 4,
    SLOPE_LEFT_DOWN = 5,
    FLOOR = 6,
    SHORTCUT_ENTRANCE = 7,
    GLASS = 9
}

OBJECT_TYPE = {
    NONE = 0,
    HORIZONTAL_BEAM = 1,
    VERTICAL_BEAM = 2,
    HIVE = 3,
    SHORTCUT = 5,
    ENTRANCE = 6,
    CREATURE_DEN = 7,
    ROCK = 9,
    SPEAR = 10,
    CRACK = 11,
    FORBID_FLY_CHAIN = 12,
    GARBAGE_WORM = 13,
    WATERFALL = 18,
    WHACK_A_MOLE_HOLE = 19,
    WORM_GRASS = 20,
    SCAVENGER_HOLE = 21
}

---@alias FileBrowserOpenMode
---| "write"
---| "read"
---| "multiRead"
---| "directory"
---| "multiDirectory"

---@class CallbackHandle
---@field disconnect fun(self:CallbackHandle) Disconnect the callback handle.

---@class CommandInfo
---@field name string The display name of the command.
---@field callback fun() The function to run when the command is invoked.
---@field autoHistory boolean? If set to true, the application will automatically group all changes of the action to a single change in the change history. The command should then not call beginChange or endChange. Defaults to true.
---@field requiresLevel boolean? If set to true, the command will not be able to run if there is no active level. Defaults to true.

---@class LevelLoadDiagnostics
---@field hadUnrecognizedAssets boolean
---@field unrecognizedMaterials string[]?
---@field unrecognizedTiles string[]?
---@field unrecognizedEffects string[]?
---@field unrecognizedProps string[]?

rained.scriptParams = {}

---Get the current application version.
---@return string version The version number as a string 
function rained.getVersion() end

---Get the current API version. This is separate from the application version.
---@return integer major, integer minor, integer revision
function rained.getApiVersion() end

---Returns true if Rained is in CLI/batch mode, and false if
---it was opened normally and so is in GUI mode.
---
---Due to the lack of a GUI, several things are different in batch mode:
--- - commands, autotiles, and onUpdate are no-ops
--- - the history module will not be defined
--- - alert will instead print messages to stdout.
---@return boolean isConsole
function rained.isBatchMode() end

---Get the path of Rained's asset directory.
---@return string path
function rained.getAssetDirectory() end

---Get the path of Rained's config directory.
---@return string path
function rained.getConfigDirectory() end

---Get the path of Drizzle data directory.
---@return string path
function rained.getDataDirectory() end

---Show a notification to the user.
---@param msg string The message to show
function rained.alert(msg) end

---Register a command invokable by the user.
---@param info CommandInfo Initialization parameters for the command.
function rained.registerCommand(info) end

---Register a command invokable by the user.
---@param name string The display name of the command.
---@param callback function The action to run on command.
function rained.registerCommand(name, callback) end

---@class DocumentInfo
---@field name string
---@field filePath string

---Get the number of open documents.
---
---Do note that documents are ordered based on the time they were opened, not
---the order that they appear in the user's tab list.
---@return integer count
function rained.getDocumentCount() end

---Get the name of the document at the given index. Returns nil if there was no document.
---
---Do note that documents are ordered based on the time they were opened, not
---the order that they appear in the user's tab list.
---@param index integer
---@return string?
function rained.getDocumentName(index) end

---Get info about the document at the given index. Returns nil if there was no document.
---
---Do note that documents are ordered based on the time they were opened, not
---the order that they appear in the user's tab list.
---@param index integer
---@return DocumentInfo?
function rained.getDocumentInfo(index) end

---Get the index of the active document. Nil if the Home tab is selected.
---
---Do note that documents are ordered based on the time they were opened, not
---the order that they appear in the user's tab list.
---@return integer|nil
function rained.getActiveDocument() end

---Set the active document index. This might take a while.
---
---Do note that documents are ordered based on the time they were opened, not
---the order that they appear in the user's tab list.
---@param index integer The index of the new active document.
---@return boolean s True on success, false otherwise.
function rained.setActiveDocument(index) end

---Close a document without saving.
---
---Do note that documents are ordered based on the time they were opened, not
---the order that they appear in the user's tab list.
---@param index integer The index of the document to close.
function rained.closeDocument(index) end

---Check if a document is open.
---@return boolean
function rained.isDocumentOpen() end

---Opens a level from file. Returns diagnostic information if there were problems loading the level
---but it could be opened regardless, but nil if successful.
---@param filePath string The file path to the level txt file.
---@return LevelLoadDiagnostics? diagnostics Table containing diagnostic information if there were problems loading the level.
function rained.openLevel(filePath) end

---Creates and opens a new level.
---@param width integer The width of the newly created level.
---@param height integer The height of the newly created level.
---@param filePath string? The optional file path to associate with the level.
function rained.newLevel(width, height, filePath) end

---Open the file browser, blocking the script until the user has either submitted or canceled.
---
---A given filter has three formats:
---1. `{name: string, extWithPeriod: string}`
---2. `{name: string, extensions: string[]}`
---3. `rained.fileFilters.level`
---
---@param openMode FileBrowserOpenMode
---@param filters any[]?
---@return string[]
function rained.openFileBrowser(openMode, filters) end

rained.fileFilters = {}

---@type userdata
local ud
rained.fileFilters.level = ud

---@deprecated
---**DEPRECATED: Use `rained.level.width` instead.**
---
---Get the width of the level.
---@return integer
function rained.getLevelWidth() end

---@deprecated
---**DEPRECATED: Use `rained.level.height` instead.**
---
---Get the height of the level.
---@return integer
function rained.getLevelHeight() end

---@deprecated
---**DEPRECATED: Use `rained.level.isInBounds` instead.**
---
---Check if a given coordinate is in the bounds of the level.
---@param x integer
---@param y integer
---@return boolean
function rained.isInBounds(x, y) end

---Register a callback to be ran per frame.
---@param func fun(dt:number) The function to run on every frame.
---@return CallbackHandle handle The callback handle.
function rained.onUpdate(func) end

---Register a callback to be ran before a level render starts.
---@param func fun(sourceTxt:string) The function to run.
---@return CallbackHandle handle The callback handle.
function rained.onPreRender(func) end

---Register a callback to be ran after the completion of a level render.
---@param func fun(sourceTxt:string, destTxt:string, destPngs:...) The function to run.
---@return CallbackHandle handle The callback handle.
function rained.onPostRender(func) end

---Register a callback to be ran when a level render fails.
---If the error reason is nil, that means the render was canceled.
---@param func fun(sourceTxt:string, reason:string?) The function to run.
---@return CallbackHandle handle The callback handle.
function rained.onRenderFailure(func) end