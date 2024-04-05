--[[
    This is the Lua script that Rained runs to initialize scripts/plugins.
    Load all of your desired Lua files here!

    I wrote type annotations and function documentation that should be recognized by
    Lua static analyzers such as sumneko-lua. Text for such are marked by a line that
    begins with three dashes.
--]]

print("Rained Version " .. rained.getVersion())

-- load the autotiles
require("autotiles.thin-pipes")
require("autotiles.big-pipe")
require("autotiles.thick-pipe")