using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using ImGuiNET;
using KeraLua;
namespace Rained.LuaScripting.Modules;

static partial class ImGuiModule
{
    private static readonly byte[] _tmpBuffer0 = new byte[256];
    private static readonly ObjectWrap<byte[]> bufferMt = new("Buffer", "IMGUI_BUFFER");

    private static byte[] NullTerminated(byte[] src, int index)
    {
        var buf = (index == 0 && src.Length + 1 <= _tmpBuffer0.Length) ? _tmpBuffer0 : new byte[src.Length + 1];
        Buffer.BlockCopy(src, 0, buf, 0, src.Length);
        buf[src.Length] = 0;
        return buf;
    }

    unsafe struct InputTextCallbackUserData
    {
        // guess i'm using my own implementation of a resizable byte array,
        // because i need to pass a byte array to imgui.
        public byte* buf;
        public uint bufSize;
        public uint bufCapacity;
    }

    // private static unsafe int Override_igBegin(nint luaPtr)
    // {
    //     var lua = Lua.FromIntPtr(luaPtr);
    //     var flags = lua.IsNoneOrNil(3) ? 0 : lua.CheckInteger(3);
    //     var buf = NullTerminated(lua.CheckBuffer(1), 0);

    //     if (lua.IsNoneOrNil(2))
    //     {
    //         fixed (byte* p = buf)
    //             lua.PushBoolean(ImGuiNative.igBegin(p, null, (ImGuiWindowFlags)flags) != 0);
    //         return 1;
    //     }
    //     else
    //     {
    //         byte p_open = lua.ToBoolean(2) ? (byte)1 : (byte)0;
    //         fixed (byte* p = buf)
    //             lua.PushBoolean(ImGuiNative.igBegin(p, &p_open, (ImGuiWindowFlags)flags) != 0);
    //         lua.PushBoolean(p_open != 0);
    //         return 2;
    //     }
    // }

    // private static readonly Dictionary<string, LuaFunction> _overrides = new Dictionary<string, LuaFunction> {
    //     {"Begin", Override_igBegin}
    // };

    private static unsafe byte* GetStr(Lua lua, int idx)
    {
        var luaStr = lua.CheckBuffer(idx);
        var sz = (nuint)luaStr.Length;
        byte *buf = (byte*)NativeMemory.Alloc(sz + 1);
        fixed (byte* p = luaStr)
        {
            Buffer.MemoryCopy(p, buf, sz+1, sz);
        }
        buf[sz] = 0;
        return buf;
    }

    private static unsafe byte* GetStr(Lua lua, int idx, byte[]? defaultValue)
    {
        if (lua.IsNoneOrNil(idx))
        {
            if (defaultValue is null)
                return null;
            
            var sz = (nuint)defaultValue.Length;
            byte *buf = (byte*) NativeMemory.Alloc(sz);
            fixed (byte* p = defaultValue)
            {
                Buffer.MemoryCopy(p, buf, sz, sz);
            }
            return buf;
        }
        else
        {
            return GetStr(lua, idx);
        }
    }

    private static unsafe void StrFree(byte* v)
    {
        NativeMemory.Free((void*)v);
    }

    private static Vector2 ReadVec2(Lua lua, int idx1, int idx2, Vector2 defaultVal)
    {
        return new Vector2(
            (float)lua.OptNumber(idx1, defaultVal.X),
            (float)lua.OptNumber(idx2, defaultVal.Y)
        );
    }

    private static Vector2 ReadVec2(Lua lua, int idx1, int idx2)
    {
        return new Vector2(
            (float)lua.CheckNumber(idx1),
            (float)lua.CheckNumber(idx2)
        );
    }

    public static unsafe int Loader(Lua lua)
    {
        lua.NewTable();

        if (!LuaInterface.Host.IsGui)
        {
            lua.NewTable();
            lua.ModuleFunction("__index", static (Lua lua) =>
            {
                return lua.ErrorWhere("cannot use imgui module in batch mode", 2);
            });
            lua.ModuleFunction("__metatable", static (Lua lua) =>
            {
                lua.PushString("The metatable is locked.");
                return 1;
            });

            lua.SetMetaTable(-2);
        }
        else
        {
            InitBufferType(lua);

            LuaHelpers.ModuleFunction(lua, "newBuffer", static (nint luaPtr) =>
            {
                var lua = Lua.FromIntPtr(luaPtr);
                var capac = lua.ToInteger(1);
                if (capac < 1)
                    lua.ErrorWhere("given capacity is out of range", 2);

                bufferMt.PushWrapper(lua, new byte[capac]);
                return 1;
            });

            GeneratedFuncs(lua);

            LuaHelpers.ModuleFunction(lua, "InputText", static (nint luaPtr) =>
            {
                var lua = Lua.FromIntPtr(luaPtr);
                var label = GetStr(lua, 1);
                var str = bufferMt.GetRef(lua, 2);
                var flags = (ImGuiInputTextFlags)lua.OptInteger(3, 0);

                int s;
                fixed (byte* p = str)
                    s = ImGuiNative.igInputText(label, p, (uint)str.Length, flags, null, null);

                StrFree(label);

                lua.PushBoolean(s != 0);
                return 2;
            });

            LuaHelpers.ModuleFunction(lua, "InputTextMultiline", static (nint luaPtr) =>
            {
                var lua = Lua.FromIntPtr(luaPtr);
                var label = GetStr(lua, 1);
                var str = bufferMt.GetRef(lua, 2);
                var size = new Vector2((float)lua.OptNumber(3, 0), (float)lua.OptNumber(4, 0));
                var flags = (ImGuiInputTextFlags)lua.OptInteger(5, 0);

                int s;
                fixed (byte* p = str)
                    s = ImGuiNative.igInputTextMultiline(label, p, (uint)str.Length, size, flags, null, null);

                StrFree(label);

                lua.PushBoolean(s != 0);
                return 2;
            });

            LuaHelpers.ModuleFunction(lua, "InputTextWithHint", static (nint luaPtr) =>
            {
                var lua = Lua.FromIntPtr(luaPtr);
                var label = GetStr(lua, 1);
                var hint = GetStr(lua, 2);
                var str = bufferMt.GetRef(lua, 3);
                var flags = (ImGuiInputTextFlags)lua.OptInteger(4, 0);

                int s;
                fixed (byte* p = str)
                    s = ImGuiNative.igInputTextWithHint(label, hint, p, (uint)str.Length, flags, null, null);

                StrFree(label);
                StrFree(hint);

                lua.PushBoolean(s != 0);
                return 2;
            });
        }

        // foreach (var (k, v) in _overrides)
        // {
        //     lua.ModuleFunction(k, v);
        // }

        return 1;
    }
    
    private static void InitBufferType(Lua lua)
    {
        bufferMt.InitMetatable(lua);

        lua.ModuleFunction("__tostring", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte[] buf = bufferMt.GetRef(lua, 1);

            int strlen;
            for (strlen = 0; buf[strlen] != 0; strlen++)
            {
                if (strlen >= buf.Length)
                    lua.ErrorWhere("buffer does not contain a null terminator character", 2);
            }

            lua.PushBuffer(buf[0..strlen]);
            return 1;
        });

        lua.ModuleFunction("__len", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte[] buf = bufferMt.GetRef(lua, 1);

            int strlen;
            for (strlen = 0; buf[strlen] != 0; strlen++)
            {
                if (strlen >= buf.Length)
                    lua.ErrorWhere("buffer does not contain a null terminator character", 2);
            }

            lua.PushInteger(strlen);
            return 1;
        });

        lua.ModuleFunction("__index", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte[] obj = bufferMt.GetRef(lua, 1);

            if (lua.IsInteger(2))
            {
                var i = lua.ToInteger(2) - 1;
                if (i < 0 || i >= obj.Length)
                    return lua.ErrorWhere("index out of range", 2);

                lua.PushInteger(obj[i]);
                return 1;
            }
            else
            {
                switch (lua.ToString(2))
                {
                    case "capacity":
                    {
                        lua.PushInteger(obj.Length);
                        break;
                    }

                    case "set":
                    {
                        lua.PushCFunction(static (luaPtr) =>
                        {
                            var lua = Lua.FromIntPtr(luaPtr);

                            byte[] self = bufferMt.GetRef(lua, 1);
                            var str = lua.CheckString(2);

                            var strBytes = Encoding.UTF8.GetBytes(str);
                            var len = Math.Min(strBytes.Length, self.Length - 1);
                            Array.Copy(strBytes, self, len);
                            self[len] = 0;

                            return 0;
                        });
                        break;
                    }

                    default:
                        lua.PushNil();
                        break;
                }

            }

            return 1;
        });

        lua.ModuleFunction("__newindex", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte[] obj = bufferMt.GetRef(lua, 1);

            if (lua.IsInteger(2))
            {
                var i = lua.ToInteger(2) - 1;
                if (i < 0 || i >= obj.Length)
                    return lua.ErrorWhere("index out of range", 2);

                var v = (byte)Math.Clamp(lua.ToInteger(3), 0, 255);
                obj[i] = v;
            }
            else
            {
                var k = lua.ToString(2);
                switch (k)
                {
                    case "capacity":
                    {
                        return lua.ErrorWhere("capacity is read-only", 2);
                    }

                    default:
                        return lua.ErrorWhere($"unknown field \"{k}\"");
                }

            }

            return 0;
        });

        lua.Pop(1);
    }

    // public static int Loader(Lua lua)
    // {
    //     lua.NewTable();
        
    //     foreach (var method in typeof(ImGuiNative).GetMethods(BindingFlags.Static | BindingFlags.Public))
    //     {
    //         if (method.Name.Length > 2 && method.Name[..2] == "ig")
    //         {
    //             var strippedName = method.Name[2..];
                
    //             LuaFunction? func = null;
    //             if (!_overrides.TryGetValue(method.Name, out func))
    //             {
    //                 Console.WriteLine("gen " + method.Name);
    //                 bool success = true;

    //                 {
    //                     DynamicMethod dynMethod = new DynamicMethod(strippedName, typeof(int), [typeof(nint)]);
    //                     Debug.Assert(dynMethod.IsStatic);
    //                     ILGenerator il = dynMethod.GetILGenerator(512);

    //                     var luaLoc = il.DeclareLocal(typeof(Lua));
    //                     if (luaLoc.LocalIndex != 0) throw new Exception("exception while generating imgui funcs");

    //                     il.Emit(OpCodes.Ldarg_0);
    //                     il.Emit(OpCodes.Call, typeof(Lua).GetMethod("FromIntPtr", BindingFlags.Static | BindingFlags.Public)??throw new NullReferenceException());

    //                     // duplicate lua value, prepare for pushing return value of ig function to lua stack
    //                     bool doesReturn = method.ReturnType != typeof(void);
    //                     if (doesReturn)
    //                     {
    //                         il.Emit(OpCodes.Dup);
    //                     }

    //                     il.Emit(OpCodes.Stloc_0);

    //                     var paramIndex = 1;
    //                     List<LocalBuilder> returnVals = [];
    //                     List<LocalBuilder> bufferLocals = [];
    //                     LocalBuilder? tmpBufLength = null;

    //                     const BindingFlags publicStatic = BindingFlags.Public | BindingFlags.Static;
                        
    //                     void PushIntParam(bool ptr)
    //                     {
    //                         il.Emit(OpCodes.Ldloc_0); // load Lua instance
    //                         il.Emit(OpCodes.Ldc_I4, paramIndex++);
    //                         il.Emit(OpCodes.Call, typeof(Lua).GetMethod("CheckInteger")??throw new NullReferenceException());
    //                         il.Emit(OpCodes.Conv_I4); // convert long to int

    //                         if (ptr)
    //                         {
    //                             var loc = il.DeclareLocal(typeof(int));
    //                             il.Emit(OpCodes.Stloc_S, loc);
    //                             returnVals.Add(loc);

    //                             il.Emit(OpCodes.Ldloca_S, loc);
    //                         }
    //                     }

    //                     void PushFloatParam(bool ptr, int cnt = 1)
    //                     {
    //                         for (int i = 0; i < cnt; i++)
    //                         {
    //                             il.Emit(OpCodes.Ldloc_0); // load Lua instance
    //                             il.Emit(OpCodes.Ldc_I4, paramIndex++);
    //                             il.Emit(OpCodes.Call, typeof(Lua).GetMethod("CheckNumber")??throw new NullReferenceException());
    //                             il.Emit(OpCodes.Conv_R4); // convert double to float
    //                         }

    //                         if (ptr)
    //                         {
    //                             LocalBuilder? loc;

    //                             switch (cnt)
    //                             {
    //                                 case 1:
    //                                     loc = il.DeclareLocal(typeof(float));
    //                                     break;

    //                                 case 2:
    //                                     loc = il.DeclareLocal(typeof(Vector2));
    //                                     il.Emit(OpCodes.Newobj, typeof(Vector2).GetConstructor([typeof(float),typeof(float)])!);
    //                                     break;

    //                                 case 3:
    //                                     loc = il.DeclareLocal(typeof(Vector3));
    //                                     il.Emit(OpCodes.Newobj, typeof(Vector3).GetConstructor([typeof(float), typeof(float),typeof(float)])!);
    //                                     break;

    //                                 case 4:
    //                                     loc = il.DeclareLocal(typeof(Vector4));
    //                                     il.Emit(OpCodes.Newobj, typeof(Vector4).GetConstructor([typeof(float),typeof(float),typeof(float),typeof(float)])!);
    //                                     break;

    //                                 default:
    //                                     throw new ArgumentOutOfRangeException(nameof(cnt));
    //                             }

    //                             returnVals.Add(loc);
    //                             il.Emit(OpCodes.Stloc_S, loc);                          
    //                             il.Emit(OpCodes.Ldloca_S, loc.LocalIndex);
    //                         }
    //                         else
    //                         {
    //                             switch (cnt)
    //                             {
    //                                 case 1:
    //                                     break;

    //                                 case 2:
    //                                     il.Emit(OpCodes.Newobj, typeof(Vector2).GetConstructor([typeof(float),typeof(float)])!);
    //                                     break;

    //                                 case 3:
    //                                     il.Emit(OpCodes.Newobj, typeof(Vector3).GetConstructor([typeof(float), typeof(float),typeof(float)])!);
    //                                     break;

    //                                 case 4:
    //                                     il.Emit(OpCodes.Newobj, typeof(Vector4).GetConstructor([typeof(float),typeof(float),typeof(float),typeof(float)])!);
    //                                     break;

    //                                 default:
    //                                     throw new ArgumentOutOfRangeException(nameof(cnt));
    //                             }
    //                         }
    //                     }

    //                     void PushStringParam()
    //                     {
    //                         /*
    //                         var buf = lua.CheckBuffer(P);
    //                         var mem = Marshal.AllocHGlobal(buf.Length + 1);
    //                         Marshal.Copy(buf, 0, mem, buf.Length);
    //                         // pass mem to ig function
    //                         */

    //                         // obtain lua string buffer
    //                         il.Emit(OpCodes.Ldloc_0);
    //                         il.Emit(OpCodes.Ldc_I4, paramIndex++);
    //                         il.Emit(OpCodes.Call, typeof(Lua).GetMethod("CheckBuffer")??throw new NullReferenceException());

    //                         // Marshal.Copy call
    //                         // first arg: the buffer
    //                         il.Emit(OpCodes.Dup);
                            
    //                         // store buffer length into local
    //                         tmpBufLength ??= il.DeclareLocal(typeof(int));
    //                         il.Emit(OpCodes.Ldlen);
    //                         il.Emit(OpCodes.Conv_I4);
    //                         il.Emit(OpCodes.Stloc_S, tmpBufLength);

    //                         // second arg: the number 0
    //                         il.Emit(OpCodes.Ldc_I4_0);
                            
    //                         // third arg: new null-terminated buffer
    //                         var bufLocal = il.DeclareLocal(typeof(nint));
    //                         bufferLocals.Add(bufLocal);
    //                         il.Emit(OpCodes.Ldloc_S, tmpBufLength);
    //                         il.Emit(OpCodes.Ldc_I4_1);
    //                         il.Emit(OpCodes.Add);
    //                         il.Emit(OpCodes.Call, typeof(Marshal).GetMethod("AllocHGlobal",
    //                             bindingAttr: publicStatic,
    //                             types: [typeof(int)]
    //                         )??throw new NullReferenceException());

    //                         // store the unmanaged buffer into new local
    //                         il.Emit(OpCodes.Dup);
    //                         il.Emit(OpCodes.Stloc_S, bufLocal);

    //                         // fourth arg: buffer length
    //                         il.Emit(OpCodes.Ldloc_S, tmpBufLength);
                            
    //                         // call function
    //                         il.Emit(OpCodes.Call, typeof(Marshal).GetMethod("Copy", publicStatic, [typeof(byte[]), typeof(int), typeof(nint), typeof(int)])??throw new NullReferenceException());

    //                         // load unmanaged buffer as argument to ig function
    //                         il.Emit(OpCodes.Ldloc_S, bufLocal);
    //                         il.Emit(OpCodes.Conv_Ovf_U);
    //                     }

    //                     foreach (var param in method.GetParameters())
    //                     {

    //                         var t = param.ParameterType;
    //                         if (t == typeof(int) || t == typeof(uint) || t.IsEnum)
    //                             PushIntParam(false);
                            
    //                         else if (t == typeof(int*) || (t.IsEnum && t.IsPointer))
    //                             PushIntParam(true);
                            
    //                         else if (t == typeof(float))
    //                             PushFloatParam(false);
                            
    //                         else if (t == typeof(float*))
    //                             PushFloatParam(true);
                            
    //                         else if (t == typeof(Vector2) || t == typeof(Vector2*))
    //                         {
    //                             PushFloatParam(t.IsPointer, 2);
    //                         }
    //                         else if (t == typeof(Vector3) || t == typeof(Vector3*))
    //                         {
    //                             PushFloatParam(t.IsPointer, 3);
    //                         }
    //                         else if (t == typeof(Vector4) || t == typeof(Vector4*))
    //                         {
    //                             PushFloatParam(t.IsPointer, 4);
    //                         }

    //                         else if (t == typeof(byte*))
    //                         {
    //                             PushStringParam();
    //                         }
    //                         else
    //                         {
    //                             success = false;
    //                             Console.WriteLine("unknown type: " + param.ParameterType);
    //                         }

    //                         // paramIndex++;
    //                     }

    //                     il.Emit(OpCodes.Call, method);
    //                     int returnCount = 0;

    //                     // push return value onto lua stack
    //                     if (doesReturn)
    //                     {
    //                         returnCount++;

    //                         // lua value should already be on stack, first argument to
    //                         // a lua.PushXXX function
    //                         if (method.ReturnType == typeof(byte))
    //                         {
    //                             il.Emit(OpCodes.Ldc_I4_0);
    //                             il.Emit(OpCodes.Cgt_Un);
    //                             il.Emit(OpCodes.Call, typeof(Lua).GetMethod("PushBoolean")??throw new NullReferenceException());
    //                         }
    //                         else if (method.ReturnType == typeof(float))
    //                         {
    //                             il.Emit(OpCodes.Conv_R8);
    //                             il.Emit(OpCodes.Call, typeof(Lua).GetMethod("PushNumber")??throw new NullReferenceException());
    //                         }
    //                         else
    //                         {
    //                             Console.WriteLine("unknown return type: " + method.ReturnType);
    //                             success = false;
    //                         }
    //                     }

    //                     // free buffers
    //                     foreach (var bufLocal in bufferLocals)
    //                     {
    //                         il.Emit(OpCodes.Ldloc_S, bufLocal);
    //                         il.Emit(OpCodes.Call, typeof(Marshal).GetMethod("FreeHGlobal", publicStatic)??throw new NullReferenceException());
    //                     }

    //                     // push extra return values onto stack
    //                     foreach (var retLocal in returnVals)
    //                     {
    //                         if (retLocal.LocalType == typeof(int))
    //                         {
    //                             il.Emit(OpCodes.Ldloc_0);
    //                             il.Emit(OpCodes.Ldloc_S, (byte)retLocal.LocalIndex);
    //                             il.Emit(OpCodes.Conv_I8);
    //                             il.Emit(OpCodes.Call, typeof(Lua).GetMethod("PushInteger")??throw new NullReferenceException());
    //                             returnCount++;
    //                         }
    //                         else if (retLocal.LocalType == typeof(float))
    //                         {
    //                             il.Emit(OpCodes.Ldloc_0);
    //                             il.Emit(OpCodes.Ldloc_S, (byte)retLocal.LocalIndex);
    //                             il.Emit(OpCodes.Conv_R8);
    //                             il.Emit(OpCodes.Call, typeof(Lua).GetMethod("PushNumber")??throw new NullReferenceException());
    //                             returnCount++;
    //                         }
    //                         else if (retLocal.LocalType == typeof(Vector2))
    //                         {
    //                             il.Emit(OpCodes.Ldloc_0);
    //                             il.Emit(OpCodes.Ldloc_S, retLocal);
    //                             il.Emit(OpCodes.Ldfld, typeof(Vector2).GetField("X")!);
    //                             il.Emit(OpCodes.Conv_R8);
    //                             il.Emit(OpCodes.Call, typeof(Lua).GetMethod("PushNumber")!);
    //                             returnCount++;

    //                             il.Emit(OpCodes.Ldloc_0);
    //                             il.Emit(OpCodes.Ldloc_S, retLocal);
    //                             il.Emit(OpCodes.Ldfld, typeof(Vector2).GetField("Y")!);
    //                             il.Emit(OpCodes.Conv_R8);
    //                             il.Emit(OpCodes.Call, typeof(Lua).GetMethod("PushNumber")!);
    //                             returnCount++;
    //                         }
    //                         else if (retLocal.LocalType == typeof(Vector3))
    //                         {
    //                             il.Emit(OpCodes.Ldloc_0);
    //                             il.Emit(OpCodes.Ldloc_S, retLocal);
    //                             il.Emit(OpCodes.Ldfld, typeof(Vector3).GetField("X")!);
    //                             il.Emit(OpCodes.Conv_R8);
    //                             il.Emit(OpCodes.Call, typeof(Lua).GetMethod("PushNumber")!);
    //                             returnCount++;

    //                             il.Emit(OpCodes.Ldloc_0);
    //                             il.Emit(OpCodes.Ldloc_S, retLocal);
    //                             il.Emit(OpCodes.Ldfld, typeof(Vector3).GetField("Y")!);
    //                             il.Emit(OpCodes.Conv_R8);
    //                             il.Emit(OpCodes.Call, typeof(Lua).GetMethod("PushNumber")!);
    //                             returnCount++;

    //                             il.Emit(OpCodes.Ldloc_0);
    //                             il.Emit(OpCodes.Ldloc_S, retLocal);
    //                             il.Emit(OpCodes.Ldfld, typeof(Vector3).GetField("Z")!);
    //                             il.Emit(OpCodes.Conv_R8);
    //                             il.Emit(OpCodes.Call, typeof(Lua).GetMethod("PushNumber")!);
    //                             returnCount++;
    //                         }
    //                         else if (retLocal.LocalType == typeof(Vector4))
    //                         {
    //                             il.Emit(OpCodes.Ldloc_0);
    //                             il.Emit(OpCodes.Ldloc_S, retLocal);
    //                             il.Emit(OpCodes.Ldfld, typeof(Vector4).GetField("X")!);
    //                             il.Emit(OpCodes.Conv_R8);
    //                             il.Emit(OpCodes.Call, typeof(Lua).GetMethod("PushNumber")!);
    //                             returnCount++;

    //                             il.Emit(OpCodes.Ldloc_0);
    //                             il.Emit(OpCodes.Ldloc_S, retLocal);
    //                             il.Emit(OpCodes.Ldfld, typeof(Vector4).GetField("Y")!);
    //                             il.Emit(OpCodes.Conv_R8);
    //                             il.Emit(OpCodes.Call, typeof(Lua).GetMethod("PushNumber")!);
    //                             returnCount++;

    //                             il.Emit(OpCodes.Ldloc_0);
    //                             il.Emit(OpCodes.Ldloc_S, retLocal);
    //                             il.Emit(OpCodes.Ldfld, typeof(Vector4).GetField("Z")!);
    //                             il.Emit(OpCodes.Conv_R8);
    //                             il.Emit(OpCodes.Call, typeof(Lua).GetMethod("PushNumber")!);
    //                             returnCount++;

    //                             il.Emit(OpCodes.Ldloc_0);
    //                             il.Emit(OpCodes.Ldloc_S, retLocal);
    //                             il.Emit(OpCodes.Ldfld, typeof(Vector4).GetField("W")!);
    //                             il.Emit(OpCodes.Conv_R8);
    //                             il.Emit(OpCodes.Call, typeof(Lua).GetMethod("PushNumber")!);
    //                             returnCount++;
    //                         }
    //                         else throw new UnreachableException();
    //                     }

    //                     il.Emit(OpCodes.Ldc_I4, returnCount);
    //                     il.Emit(OpCodes.Ret);

    //                     dynMethod.DefineParameter(1, ParameterAttributes.None, "luaPtr");

    //                     if (success)
    //                     {
    //                         func = (LuaFunction) dynMethod.CreateDelegate(typeof(LuaFunction));
    //                     }
    //                     // il.Emit(OpCodes.Ldloc_0);
    //                     // il.Emit(OpCodes.Ldstr, "Hello, world!");
    //                     // il.Emit(OpCodes.Call, typeof(Lua).GetMethod("PushString", 0, [typeof(string)])!);

    //                     // il.Emit(OpCodes.Ldc_I4_1);
    //                     // il.Emit(OpCodes.Ret);

    //                     // func = (LuaFunction) dynMethod.CreateDelegate(typeof(LuaFunction));
    //                 }
    //             }

    //             if (func is not null)
    //             {
    //                 var handle = GCHandle.Alloc(func);
    //                 LuaHelpers.ModuleFunction(lua, strippedName, func);
    //             }
    //         }
    //     }

    //     return 1;
    // }
}