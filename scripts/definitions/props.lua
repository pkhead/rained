---@meta
rained.props = {}

---@class PropRect
---@field x number The X position of the rectangle's center.
---@field y number The Y position of the rectangle's center.
---@field width number
---@field height number
---@field rotation number The rotation of the rectangle in radians.

---@alias PropRenderTime
---| "preEffects",
---| "postEffects"

---@class Prop
---@field name string (Read-only) The name of the prop init.
---@field renderOrder integer
---@field depthOffset integer The depth offset of the prop. 0 is the first sublayer and 29 is the last.
---@field seed integer The seed used for graphics generation, in range [0-1000)
---@field renderTime PropRenderTime Must be either "preEffects" or "postEffects"
---@field variation integer The variation of the prop used. 1 is the first variation, and the maximum can be deduced from `rained.props.getPropInfo`.
---@field applyColor boolean
---@field customDepth integer The custom depth of the prop. Minimum value is 1.
---@field customColor string The color of the prop decal used. Must be a string available in `rained.props.getCustomColors`.
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
---@param centerX number The X position of the prop center.
---@param centerY number The Y position of the prop center.
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

---Remove a prop from the level.
---@param prop Prop The prop to remove.
---@return boolean s True if the prop had been in the level and was successfully removed. False if not.
function rained.props.removeProp(prop) end

---Return a list of all props in the level.
---@return Prop[]
function rained.props.getProps() end

---Return a list of all selected props.
---@return Prop[]
function rained.props.getSelection() end

---Deselect all props.
function rained.props.clearSelection() end

---Select a prop if it isn't already selected.
---@param prop Prop[] The prop to select.
function rained.props.addToSelection(prop) end

---Deselect a prop. Does nothing if the prop wasn't selected.
---@param prop Prop[] The prop to deselect.
---@return boolean s True if the prop had been selected, false if not.
function rained.props.removeFromSelection(prop) end

---Check if a prop is installed
---@param propName string The name of the prop to check.
---@return boolean
function rained.props.isInstalled(propName) end

---Get a list of all available prop names, including tile-as-props.
---@return string[]
function rained.props.getPropCatalog() end

---Get a list of all available prop categories, excluding the "Tiles as Prop" ones.
---@return string[]
function rained.props.getPropCategories() end

---Get a list of all tile categories that contain a tile that can be used as a prop.
---@return string[]
function rained.props.getTileAsPropCategories() end

---Get a list of prop init names in the prop category given by name.
---@param categoryName string The name of the category.
---@return string[]
function rained.props.getPropsInCategory(categoryName) end

---Get a list of prop init names in the tile-as-prop category given by name.
---@param categoryName string The name of the tile-as-prop category.
---@return string[]
function rained.props.getPropsInTileCategory(categoryName) end

---Get the list of available custom colors.
---@return string[]
function rained.props.getCustomColors() end

---@alias PropType
---| "standard"
---| "variedStandard"
---| "soft"
---| "coloredSoft"
---| "variedSoft"
---| "simpleDecal"
---| "variedDecal"
---| "antimatter"
---| "rope"
---| "long"

---@alias PropColorTreatment
---| "standard"
---| "bevel"

---@class PropData
---@field name string The name of the prop init.
---@field category string The name of the category the prop belongs to. If this is a tile as prop, it will be the name of the tile category it originates from.
---@field type PropType
---@field colorTreatment PropColorTreatment?
---@field variationCount integer
---@field tileAsProp boolean True if this is a tile-as-prop, false if not.

---Get information about a prop init.
---@param name string The name of the prop init.
---@return PropData? data The prop data, or nil if the name is not registered.
function rained.props.getPropInfo(name) end