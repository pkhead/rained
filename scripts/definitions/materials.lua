---@meta
rained.materials = {}

---Get the default material.
---@return string materialName
function rained.materials.getDefaultMaterial() end

---Get the default material.
---@return string materialName
function rained.materials.getDefaultMaterialId() end

---Set the default material. Throws an error if the material was unrecognized.
---@param material string|integer The name or ID of the material.
function rained.materials.setDefaultMaterial(material) end

---Check if a material is installed.
---@param matName string The name to check.
---@return boolean
function rained.materials.isInstalled(matName) end

---Get a list of the names of all available materials.
---@return string[]
function rained.materials.getMaterialCatalog() end

---Get a list of all available material categories.
---@return string[]
function rained.materials.getMaterialCategories() end

---Get a list of the names of all materials in a category.
---@param categoryName string The name of the category.
---@return string[]
function rained.materials.getMaterialsInCategory(categoryName) end

---Get the ID associated with a material name.
---@param name string The material name.
---@return integer? id The ID of the material, or nil if the name was not recognized.
function rained.materials.getMaterialId(name) end

---Get the name associated with a material ID.
---@param id integer The material ID.
---@return string? name The name of the material, or nil if the ID was not recognized.
function rained.materials.getMaterialName(id) end