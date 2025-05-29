#!/usr/bin/env python3
import json

class MetaParameter:
    def __init__(self, type: str, name: str):
        self.type = type
        self.name = name

def is_lua_keyword(word):
    return word in ["repeat"]

def str_to_utf8_buf(string: str):
    assert string[0] == "\""
    assert string[-1] == "\""

    buf = string[1:-1].encode("utf-8")
    out = []
    for byte in buf:
        out.append(str(byte))
    out.append("0")

    return "[" + ', '.join(out) + "]"

def main():
    with open('src/ImGui.NET/src/CodeGenerator/definitions/cimgui/definitions.json') as f:
        json_funcs = json.load(f)

    with open('src/ImGui.NET/src/CodeGenerator/definitions/cimgui/structs_and_enums.json') as f:
        json_defs = json.load(f)

    enums_json = json_defs['enums']

    meta_source = ["---@meta\nlocal imgui = {}\n"]

    cs_source = ["""using System.Numerics;
using ImGuiNET;
using KeraLua;
namespace Rained.LuaScripting.Modules;
                 
static partial class ImGuiModule
{
    private static unsafe void GeneratedFuncs(Lua lua)
    {
"""]

    for func_def_k in json_funcs:
        func_def = json_funcs[func_def_k]
        for ovr_def in func_def:
            func_name = ovr_def['ov_cimguiname']
            if func_name[:2] == 'ig' and not ('imgui_internal' in ovr_def['location']):
                out_func_name = func_name[2:]
                success = True

                func_def = []
                func_def.append("var lua = Lua.FromIntPtr(luaPtr);\n")

                local_idx = 0
                param_idx = 1
                str_bufs = []
                func_parameters = []
                extra_pushes = []
                meta_params = []
                meta_returns = []

                for arg in ovr_def['argsT']:
                    arg_name = arg['name']

                    default_value: str = None
                    if arg['name'] in ovr_def['defaults']:
                        default_value = ovr_def['defaults'][arg['name']]
                        
                        if default_value == "NULL":
                            default_value = "null"
                        else:
                            default_value = default_value.replace("FLT_MIN", "float.MinValue")
                            default_value = default_value.replace("FLT_MAX", "float.MaxValue")

                    local_name = "l"+str(local_idx)
                    
                    if arg['type'] == 'float' or arg['type'] == 'const float':
                        func_parameters.append(local_name)
                        func_def.append(f"float {local_name} = ")

                        if default_value:
                            func_def.append("(float)lua.OptNumber(" + str(param_idx) + ", " + default_value + ");\n")
                            meta_params.append(MetaParameter("number?", arg_name))
                        else:
                            func_def.append(f"(float)lua.CheckNumber({param_idx});\n")
                            meta_params.append(MetaParameter("number", arg_name))
                        
                        param_idx = param_idx + 1

                    elif arg['type'] == 'float*':
                        func_parameters.append("&" + local_name)
                        func_def.append(f"float {local_name} = ")

                        if default_value:
                            func_def.append("(float)lua.OptNumber(" + str(param_idx) + ", " + default_value + ");\n")
                            meta_params.append(MetaParameter("number?", arg_name))
                        else:
                            func_def.append(f"(float)lua.CheckNumber({param_idx});\n")
                            meta_params.append(MetaParameter("number", arg_name))

                        extra_pushes.append(f"lua.PushNumber((double)l{local_idx});\n")
                        meta_returns.append(MetaParameter("number", arg_name))

                        param_idx = param_idx + 1

                    elif arg['type'] == 'int' or arg['type'] == 'const int' or (arg['type'] + '_') in enums_json:
                        if (arg['type'] + '_') in enums_json:
                            func_parameters.append("("+arg['type']+")"+local_name)
                        else:
                            func_parameters.append(local_name)
                        func_def.append(f"int {local_name} = ")

                        if default_value:
                            func_def.append("(int)lua.OptInteger(" + str(param_idx) + ", " + default_value + ");\n")
                            meta_params.append(MetaParameter("integer?", arg_name))
                        else:
                            func_def.append(f"(int)lua.CheckInteger({param_idx});\n")
                            meta_params.append(MetaParameter("integer", arg_name))

                        param_idx = param_idx + 1

                    elif arg['type'] == 'bool' or arg['type'] == 'const bool':
                        func_parameters.append(local_name)
                        func_def.append(f"byte {local_name} = ")

                        if default_value:
                            # func_def.append(f"(float)lua.OptNumber(" + str(param_idx) + ", " + default_value + ");\n")
                            func_def.append(f"(lua.IsNoneOrNil({param_idx}) ? {default_value} : lua.ToBoolean({param_idx})) ? (byte)1 : (byte)0;\n")
                            meta_params.append(MetaParameter("boolean?", arg_name))
                        else:
                            func_def.append(f"lua.ToBoolean({param_idx}) ? (byte)1 : (byte)0;\n")
                            meta_params.append(MetaParameter("boolean", arg_name))
                        
                        param_idx = param_idx + 1

                    elif arg['type'] == 'bool*':
                        func_def.append(f"byte {local_name} = ")

                        if default_value:
                            assert default_value == "null"
                            func_parameters.append(f"(lua.IsNoneOrNil({param_idx}) ? null : &{local_name})")
                            meta_params.append(MetaParameter("boolean?", arg_name))
                        else:
                            func_parameters.append(f"&{local_name}")
                            meta_params.append(MetaParameter("boolean", arg_name))
                        
                        extra_pushes.append(f"lua.PushBoolean({local_name} != 0);\n")
                        meta_returns.append(MetaParameter("boolean", arg_name))
                        
                        func_def.append(f"lua.ToBoolean({param_idx}) ? (byte)1 : (byte)0;\n")
                        
                        param_idx = param_idx + 1

                    elif arg['type'] == 'const ImVec2' or arg['type'] == 'ImVec2' or arg['type'] == "ImVec2*":
                        if arg['type'] == "ImVec2*":
                            func_parameters.append("&"+local_name)
                            extra_pushes.append(f"lua.PushNumber((double){local_name}.X);\n")
                            extra_pushes.append(f"lua.PushNumber((double){local_name}.Y);\n")
                            meta_returns.append(MetaParameter("number", arg_name + "_x"))
                            meta_returns.append(MetaParameter("number", arg_name + "_y"))
                        else:
                            func_parameters.append(local_name)
                        
                        func_def.append(f"Vector2 {local_name} = ReadVec2(lua, {param_idx}, {param_idx+1}")
                        if default_value:
                            func_def.append(", " + default_value.replace('ImVec2', 'new Vector2'))
                            meta_params.append(MetaParameter("number?", arg_name + "_x"))
                            meta_params.append(MetaParameter("number?", arg_name + "_y"))
                        else:
                            meta_params.append(MetaParameter("number", arg_name + "_x"))
                            meta_params.append(MetaParameter("number", arg_name + "_y"))
                        
                        func_def.append(");\n")

                        param_idx = param_idx + 2

                    elif arg['type'] == 'const char*':
                        func_parameters.append(local_name)
                        str_bufs.append(local_name)
                        func_def.append(f"byte* {local_name} = ")

                        if default_value:
                            func_def.append(f"GetStr(lua, {param_idx}, {("null" if default_value == "null" else str_to_utf8_buf(default_value))});\n")
                            meta_params.append(MetaParameter("string?", arg_name))
                        else:
                            func_def.append(f"GetStr(lua, {param_idx});\n")
                            meta_params.append(MetaParameter("string", arg_name))
                        
                        param_idx = param_idx + 1

                    elif arg['type'] == '...':
                        continue

                    else:
                        print("unsupported arg type: " + arg['type'])
                        success = False

                    local_idx = local_idx + 1

                return_count = 0
                does_return = ovr_def['ret'] != 'void'
                if does_return:
                    func_def.append("var ret = ")
                    return_count = return_count + 1
                
                func_def.append(f"ImGuiNative.{func_name}({(', '.join(func_parameters))});\n")

                if does_return:
                    if ovr_def['ret'] == 'bool' or ovr_def['ret'] == 'const bool':
                        meta_returns.insert(0, MetaParameter('boolean', 's'))
                        func_def.append("lua.PushBoolean(ret != 0);\n")

                    elif ovr_def['ret'] == 'int' or ovr_def['ret'] == 'const int':
                        meta_returns.insert(0, MetaParameter('integer', 's'))
                        func_def.append("lua.PushInteger(ret);\n")

                    elif ovr_def['ret'] == 'float' or ovr_def['ret'] == 'const float':
                        meta_returns.insert(0, MetaParameter('number', 'num'))
                        func_def.append("lua.PushNumber((double)ret);\n")

                    else:
                        print("unsupported return type: " + ovr_def['ret'])
                        success = False

                for v in extra_pushes:
                    return_count = return_count + 1
                    func_def.append(v)

                for buf in str_bufs:
                    func_def.append(f"StrFree({buf});\n")

                func_def.append("return " + str(return_count) + ";\n")

                if success:
                    cs_source.append("        LuaHelpers.ModuleFunction(lua, \"")
                    cs_source.append(out_func_name)
                    cs_source.append("\", static (nint luaPtr) =>\n        {\n")

                    for l in ''.join(func_def).splitlines():
                        cs_source.append("            ")
                        cs_source.append(l)
                        cs_source.append("\n")
                        
                    cs_source.append("        });\n")

                    for p in meta_params:
                        if is_lua_keyword(p.name):
                            p.name = p.name + "_"
                        
                        meta_source.append(f"---@param {(p.name)} {(p.type)}\n")

                    if meta_returns:
                        meta_source.append("---@return ")
                        meta_return_strs = []
                        for ret in meta_returns:
                            if is_lua_keyword(ret.name):
                                ret.name = ret.name + "_"

                            if ret.name:
                                meta_return_strs.append(f"{(ret.type)} {(ret.name)}")
                            else:
                                meta_return_strs.append(ret.type)
                        meta_source.append(', '.join(meta_return_strs))
                        meta_source.append("\n")

                    meta_source.append(f"function imgui.{out_func_name}(")
                    meta_source.append(', '.join([v.name for v in meta_params]))
                    meta_source.append(") end\n\n")


    for enum_type in enums_json:
        enum_json = enums_json[enum_type]
        for enum_data in enum_json:
            out_name = enum_data['name']
            if out_name[:5] == 'ImGui':
                out_name = out_name[5:]
            
            cs_source.append(f"        lua.PushInteger({(enum_data['calc_value'])});\n")
            cs_source.append(f"        lua.SetField(-2, \"{(out_name)}\");\n")
            meta_source.append(f"imgui.{out_name} = {(enum_data['calc_value'])}\n")

    cs_source.append("    }\n")
    cs_source.append("}\n")

    meta_source.append("\nreturn imgui\n")

    with open('src/Rained/LuaScripting/Modules/ImGuiModule.gen.cs', 'w') as f:
        f.write(''.join(cs_source))

    with open('scripts/definitions/imgui.lua', 'w') as f:
        f.write(''.join(meta_source))

if __name__ == '__main__':
    main()