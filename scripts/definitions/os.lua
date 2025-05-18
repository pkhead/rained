---@meta

---Returns the current working directory of the process.
function os.getcwd() end

---Iterate through the items of a directory.
---Gives the full path to each item.
---@param p string
---@param filter string? The filter string (e.g. "*.txt")
---@return fun() iterator
function os.list(p, filter) end

---Iterate through the files of a directory.
---Gives the full path to each item.
---@param p string
---@param filter string? The filter string (e.g. "*.txt")
---@return fun() iterator
function os.listfiles(p, filter) end

---Iterate through the subdirectories of a directory.
---Gives the full path to each item.
---@param p string
---@param filter string? The filter string (e.g. "*.txt")
---@return fun() iterator
function os.listdirs(p, filter) end