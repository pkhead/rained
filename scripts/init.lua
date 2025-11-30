--[[
    This is the Lua script that Rained runs to initialize scripts/plugins.
    Load all of your desired Lua files here!

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

-- rained provides this autorequire function.
-- this will recursively require all lua submodules within autoload.
-- in other words, it executes all the lua files in that directory.
autorequire("autoload", true)

rained.onUpdate(function(dt)
    if rained.isDocumentOpen() then
        for _, prop in ipairs(rained.props.getSelection()) do
            prop.fezTreeTrunkAngle = 3
        end
    end
end)