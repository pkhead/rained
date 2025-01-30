---@meta

---This is an interface to Rained's change history system.
---When you want to make a change that can be undone/redone, you first
---call the "beginChange" function with the appropriate category.
---Then, you make the changes. Once you are done, call "endChange". The
---user will then be able to undo/redo changes made in between those calls.
---
---Internally, "beginChange" will create a snapshot of relevant state of the level.
---Then, when "endChange" is called, Rained will calculate the diff between
---the snapshot created earlier and the current state of the level, and use this
---to determine how to handle undoing and redoing upon the user's request.
rained.history = {}

---@alias LevelComponent
---| "properties"
---| "cells"
---| "cameras"
---| "effects"
---| "props"
---| "all"

---Begin changes to the cells of the level. If a change was already active, it will throw an error.
---
---@param ... LevelComponent Components to include. If unspecified, will include every component.
---@return boolean valid True if there was not an active change when this was called, false otherwise.
function rained.history.beginChange(...) end

---End changes for the cells of the level. If no change was active, it will throw an error.
---
---@return boolean valid True if there was an active change when this was called, false otherwise.
function rained.history.endChange() end

---Check if a change is active.
---@return boolean
function rained.history.isChangeActive() end