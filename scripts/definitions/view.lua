---@meta

---@class View
---@field viewX number Y position of the top-left corner of the viewport.
---@field viewY number X position of the top-left corner of the viewport.
---@field viewZoom number The viewport zoom factor. 1.0 means no zoom.
---@field workLayer integer The current work layer, from 1 to 3 inclusive.
---@field geoLayer1 boolean True if layer 1 is enabled in the geometry editor.
---@field geoLayer2 boolean True if layer 2 is enabled in the geometry editor.
---@field geoLayer3 boolean True if layer 3 is enabled in the geometry editor.
---@field palettesEnabled boolean True if palette rendering is enabled, false if not.
---@field palette1 integer Index of the main palette.
---@field palette2 integer Index of the fade palette.
---@field paletteMix number Strength of the fade, from 0 to 1.
rained.view = {}