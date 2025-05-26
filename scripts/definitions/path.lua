---@meta
local path = {}

path.sep = "/"

---Returns the normalized and absolutized version of a path.
---@param p string
---@return string
function path.abspath(p) end

---Returns the file name of a path, including its extension.
---@param p string
---@return string
function path.basename(p) end

---Returns the directory name of the path.
---@param p string
---@return string
function path.dirname(p) end

---Returns true if a file or directory exists at the path, false if not.
---@param p string
---@return boolean
function path.exists(p) end

---Returns true if the path points to a file.
---@param p string
---@return boolean
function path.isfile(p) end

---Returns true if the path points to a directory.
---@param p string
---@return boolean
function path.isdir(p) end

---Returns true if the given path is an absolute path.
---@param p string
---@return boolean
function path.isabs(p) end

---Joins the given paths together.
---@param ... string
---@return string
function path.join(...) end

---Normalize casing (on Windows) and correct directory separators.
---@param p string
---@return string
function path.normcase(p) end

---Removes redundant directory separators and resolves .. and . items.
---@param p string
---@return string
function path.normpath(p) end

---Returns the relative path from a directory to a path.
---@param p string The target path.
---@param from string The directory.
---@return string
function path.relpath(p, from) end

---Returns the directory path and the base name.
---@param p string
---@return string dir, string basename
function path.split(p) end

---Returns the basename without its extension, and the extension itself.
---@param p string
---@return string root, string ext
function path.splitext(p) end

---Returns the extension of a file.
---@param p string
---@return string
function path.getext(p) end

return path