---@meta
rained.materials = {}

---Get a list of all available materials.
---@return string[]
function rained.materials.getMaterialCatalog() end

---Get a list of all available material categories.
---@return string[]
function rained.materials.getMaterialCategories() end

---Get a list of all materials in a category.
---@param categoryName string The name of the category.
---@return string[]
function rained.materials.getMaterialsInCategory(categoryName) end

---Get the default material.
---@return string materialName
function rained.materials.getDefaultMaterial() end

---Get the default material.
---@return string materialName
function rained.materials.getDefaultMaterialId() end

---Set the default material.
---@param material string|integer The name or ID of the material.
function rained.materials.setDefaultMaterial(material) end
