---@meta
rained.cells = {}

---Get the geometry type of a given cell.
---@param x integer
---@param y integer
---@param layer integer The given layer, in the range [1, 3].
---@return integer
function rained.cells.getGeo(x, y, layer) end

---Set the geometry type of a given cell.
---@param x integer
---@param y integer
---@param layer integer The given layer, in the range [1, 3].
---@param geo integer The geometry type of a cell.
function rained.cells.setGeo(x, y, layer, geo) end

---Set the material of a given cell.
---@param x integer
---@param y integer
---@param layer integer The given layer, in the range [1, 3].
---@param material string The name of the desired material. Nil to clear.
function rained.cells.setMaterial(x, y, layer, material) end

---Get the name of a cell's material.
---@param x integer
---@param y integer
---@param layer integer The given layer, in the range [1, 3].
---@return string name The name of the material, or nil if there was no material there.
function rained.cells.getMaterial(x, y, layer) end

---Set the material ID of a given cell.
---@param x integer
---@param y integer
---@param layer integer The given layer, in the range [1, 3].
---@param material integer The ID of the material. Zero to clear.
function rained.cells.setMaterialId(x, y, layer, material) end

---Get the name of a cell's material ID.
---@param x integer
---@param y integer
---@param layer integer The given layer, in the range [1, 3].
---@return integer id The ID of the material, or 0 if there was no material there.
function rained.cells.getMaterialId(x, y, layer) end

---Get the geometry objects of a given cell.
---@param x integer
---@param y integer
---@param layer integer
---@return integer[]
function rained.cells.getObjects(x, y, layer) end

---Set the geometry objects of a given cell.
---@param x integer
---@param y integer
---@param layer integer
---@param objects integer[] The list of objects.
function rained.cells.setObjects(x, y, layer, objects) end

---Test if a given cell has the given objects.
---@param x integer
---@param y integer
---@param layer integer
---@param ... integer Objects to check for
---@return boolean result
function rained.cells.hasObject(x, y, layer, ...) end

---Add objects to a cell
---@param x integer
---@param y integer
---@param layer integer
---@param ... integer The objects to add
function rained.cells.addObject(x, y, layer, ...) end

---Remove objects from a cell
---@param x integer
---@param y integer
---@param layer integer
---@param ... integer The objects to remove
function rained.cells.removeObject(x, y, layer, ...) end

---Get the tile data of a cell
---@param x integer
---@param y integer
---@param layer integer The given layer, in the range [1, 3].
---@return string? tileName, integer? tileRootX, integer? tileRootY, integer? tileRootL
function rained.cells.getTileData(x, y, layer) end

---Set the tile head of the given cell.
---@param x integer
---@param y integer
---@param layer integer The given layer, in the range [1, 3].
---@param tileName string? The name of the tile, or nil.
function rained.cells.setTileHead(x, y, layer, tileName) end

---Set the tile head pointer of a given cell.
---@param x integer The X position of the cell to modify.
---@param y integer The Y position of the cell to modify.
---@param layer integer The layer of the cell to modify.
---@param rootX integer The X position of the pointed tile head.
---@param rootY integer The Y position of the pointed tile head.
---@param rootL integer The layer of the pointed tile head.
function rained.cells.setTileRoot(x, y, layer, rootX, rootY, rootL) end

---Clear the tile head pointer of a given cell.
---@param x integer
---@param y integer
---@param layer integer
function rained.cells.clearTileRoot(x, y, layer) end