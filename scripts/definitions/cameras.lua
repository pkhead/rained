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
---@return number dx, number dy
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

---Get the angle and strength of a camera quad corner.
--- - Index 0: Top-left
--- - Index 1: Top-right
--- - Index 2: Bottom-right
--- - Index 3: Bottom-left
---@param index integer The index of the corner.
---@return number angle The angle in radians
---@return number strength The corner strength. This is the distance from the rest position divided by 4.
function Camera:getCornerAngle(index) end

---Set the angle and strength of a camera quad corner.
--- - Index 0: Top-left
--- - Index 1: Top-right
--- - Index 2: Bottom-right
--- - Index 3: Bottom-left
---@param index integer The index of the corner.
---@param angle number The angle in radians
---@param strength number The corner strength. This is the distance from the rest position divided by 4.
function Camera:setCornerAngle(index, angle, strength) end


---Create a clone of this camera.
---@return Camera
function Camera:clone() end

---Get the number of cameras in the level.
---@return integer
function rained.cameras.getCount() end

---Obtain a list of all cameras in the level.
---@return Camera[]
function rained.cameras.getCameras() end

---Get the currently prioritized camera, or nil if unset.
---@return Camera?
function rained.cameras.getPriority() end

---Set the currently prioritized camera. Pass in nil to unset.
---@param camera Camera?
function rained.cameras.setPriority(camera) end

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

---Remove a camera.
---@param index integer The index of the camera to remove.
---@return boolean s True if the camera was in the level and was successfully removed. False if not.
function rained.cameras.removeCamera(index) end

---Get an object representing the camera at the given index.
---@param cameraIndex integer The camera index.
---@return Camera
function rained.cameras.getCamera(cameraIndex) end

---Returns the full size of all cameras in grid units.
---@return number, number
function rained.cameras.getFullSize() end

---Returns the widescreen (16:9) size of all cameras in grid units.
---@return number, number
function rained.cameras.getWidescreenSize() end

---Returns the fullscreen (4:3) size of all cameras in grid units.
---@return number, number
function rained.cameras.getFullscreenSize() end