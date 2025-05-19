---@meta
rained.gui = {}

---@alias MenuName
---| "file"
---| "edit"
---| "view"
---| "help"

---Register a hook into one or more menus from the menubar.
---It is expected that the callback function will insert ImGui widgets. Each
---individual callback is also split by separators.
---@param func fun(menuName: MenuName) The function to run.
---@return CallbackHandle
function rained.gui.menuHook(func) end

---Register a hook into the preferences window.
---It is expected that the callback function will insert ImGui widgets.
---They will be rendered into the scripts page in the preferences window.
---@param func fun() The function to run.
---@return CallbackHandle
function rained.gui.prefsHook(func) end