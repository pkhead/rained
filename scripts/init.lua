--[[
    This is the Lua script that Rained runs to initialize scripts/plugins.
    Load all of your desired Lua files here!
--]]

print("Rained Version " .. rained.getVersion())

require("thin-pipes") -- load the Thin Pipes autotile