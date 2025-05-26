--[[
    This is the Lua script that Rained runs to initialize scripts/plugins.
    Load all of your desired Lua files here!

    Rained will also automatically load all scripts inside the "autoload" folder.

    I wrote type annotations and function documentation that should be recognized by
    Lua static analyzers such as sumneko-lua. Text for such are marked by a line that
    begins with three dashes.
--]]

-- don't want this to print when rained is running as a
-- console executable
if not rained.isBatchMode() then
    local maj, min, rev = rained.getApiVersion()
    print(("Rained API version: %s.%s.%s"):format(maj, min, rev))
end

require("rendercopy")