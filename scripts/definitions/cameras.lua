---@meta
rained.cameras = {}

---@class Camera
---@field x number The X position of the top-left corner of the camera.
---@field y number The Y position of the top-left corner of the camera.
---@field index integer? (Read-only) The current index of the camera, or nil if not added to the level.
local Camera = {}

---Get the corner offset of a camera quad.
--- - Index 0: Top-left
--- - Index 1: Top-right
--- - Index 2: Bottom-right
--- - Index 3: Bottom-left
---@param index integer The index of the corner.
---@returns dx number, dy number
function Camera:getCornerOffset(index) end

---Set the corner offset of a camera quad.
--- - Index 0: Top-left
--- - Index 1: Top-right
--- - Index 2: Bottom-right
--- - Index 3: Bottom-left
---@param index integer The index of the corner.
---@param x number The X offset of the corner.
---@param y number The Y offset of the corner.
function Camera:setCornerOffset(index, x, y) end

---Create a clone of this camera.
---@return Camera
function Camera:clone() end

---Get the number of cameras in the level.
---@return integer
function rained.cameras.getCount() end

---Get the index of the currently prioritized camera.
---
---Returns nil if unset.
---@return integer?
function rained.cameras.getPriority() end

---Set the index of the currently prioritized camera. Pass in nil to unset.
---@param cameraIndex integer?
function rained.cameras.setPriority(cameraIndex) end

---Create a new camera.
---@param x number? The X position of the top-left corner of the newly created camera, or nil for a default one.
---@param y number? The Y position of the top-left corner of the newly created camera, or nil for a default one.
---@return Camera camera The newly created camera.
function rained.cameras.newCamera(x, y) end

---Add a camera at a given index.
---If the index is nil, it will be added to the end of the list.
---If the camera was already in the list, it will be moved.
---@param camera Camera The camera to add.
---@param index integer? The index at which the camera will be inserted.
function rained.cameras.addCamera(camera, index) end

---Swap the indices of two cameras.
---@param index1 integer The index of the first camera.
---@param index2 integer The index of the second camera.
function rained.cameras.swap(index1, index2) end

---Remove a camera.
---@param index integer The index of the camera to remove.
function rained.cameras.removeCamera(index) end

---Get an object representing the camera at the given index.
---@param cameraIndex integer The camera index.
---@returns Camera
function rained.cameras.getCamera(cameraIndex) end

---Returns the full size of all cameras in grid units.
---@returns number, number
function rained.cameras.getFullSize() end

---Returns the widescreen (16:9) size of all cameras in grid units.
---@returns number, number
function rained.cameras.getWidescreenSize() end

---Returns the fullscreen (4:3) size of all cameras in grid units.
---@returns number, number
function rained.cameras.getFullscreenSize() end