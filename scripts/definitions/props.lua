---@meta
rained.props = {}

---@class PropRect
---@field x number
---@field y number
---@field width number
---@field height number
---@field rotation number

---@alias PropRenderTime
---| "preEffects",
---| "postEffects"

---@class Prop
---@field name string (Read-only) The name of the prop init.
---@field renderOrder integer
---@field depthOffset integer
---@field seed integer
---@field renderTime PropRenderTime
---@field variation integer
---@field applyColor boolean
---@field customDepth integer
---@field customColor string
local Prop = {}

---Remove this prop from the level.
function Prop:remove() end

---Create a clone of this prop.
---@return Prop
function Prop:clone() end

---Get the prop shape as a rectangle. If nil, the prop is warped.
---@return PropRect?
function Prop:getRect() end

---Set the prop to the given rectangular shape.
---@param rect PropRect
function Prop:setRect(rect) end

---Get the prop shape as a rectangle. If nil, the prop is warped.
---@return {x: number, y: number}[]
function Prop:getQuad() end

---Set the prop to the given quad.
---@param quad {x: number, y: number}[]
function Prop:setQuad(quad) end

---Reset the prop's transform.
function Prop:resetTransform() end

---Flip the prop horizontally.
function Prop:flipX() end

---Flip the prop vertically.
function Prop:flipY() end

---Create a new prop from a rectangle.
---@param name string The name of the prop init.
---@param centerX number? The X position of the prop center.
---@param centerY number? The Y position of the prop center.
---@param width number The width of the prop.
---@param height number The height of the prop.
---@return Prop prop
function rained.props.newProp(name, centerX, centerY, width, height) end

---Create a new prop from a quad.
---@param name string The name of the prop init.
---@param vertices {x: number, y: number}[] The vertices to initialize the quad with.
function rained.props.newProp(name, vertices) end

---Add a prop to the level.
---@param prop Prop The prop to add.
function rained.props.addProp(prop) end

---Return a list of all props in the level.
---@return Prop[]
function rained.props.getProps() end

---Return a list of all selected props in the level.
---@return Prop[]
function rained.props.getSelectedProps() end

---Get a list of all available prop names.
---@return string[]
function rained.prop.getPropCatalog() end

---Get a list of all available prop categories, excluding the "Tiles as Prop" ones.
---@return string[]
function rained.prop.getPropCategories() end

---Get a list of all tile categories that contain a tile that can be used as a prop.
---@return string[]
function rained.prop.getTileAsPropCategories() end

---Get a list of prop inits in the prop category given by name.
---@param categoryName string The name of the category.
---@return string[]
function rained.prop.getPropsInCategory(categoryName) end

---Get the list of available custom colors.
---@return string[]
function rained.prop.availableCustomColors() end