---@meta

---Returns the current working directory of the process.
function os.getcwd() end

---Create a directory if it doesn't already exist.
---@param p string
function os.mkdir(p) end

---Remove an empty directory. Can remove non-empty directories if `recursive` is true.
---@param p string
---@param recursive boolean? Recursively delete files and subdirectories as well.
function os.rmdir(p, recursive) end

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