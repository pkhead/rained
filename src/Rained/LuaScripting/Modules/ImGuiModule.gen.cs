using System.Numerics;
using ImGuiNET;
using KeraLua;
namespace Rained.LuaScripting.Modules;
                 
static partial class ImGuiModule
{
    private static unsafe void GeneratedFuncs(Lua lua)
    {
        LuaHelpers.ModuleFunction(lua, "AlignTextToFramePadding", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igAlignTextToFramePadding();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "Begin", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            byte l1 = lua.ToBoolean(2) ? (byte)1 : (byte)0;
            int l2 = (int)lua.OptInteger(3, 0);
            var ret = ImGuiNative.igBegin(l0, (lua.IsNoneOrNil(2) ? null : &l1), (ImGuiWindowFlags)l2);
            lua.PushBoolean(ret != 0);
            lua.PushBoolean(l1 != 0);
            StrFree(l0);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "BeginChild_Str", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            Vector2 l1 = ReadVec2(lua, 2, 3, new Vector2(0,0));
            int l2 = (int)lua.OptInteger(4, 0);
            int l3 = (int)lua.OptInteger(5, 0);
            var ret = ImGuiNative.igBeginChild_Str(l0, l1, (ImGuiChildFlags)l2, (ImGuiWindowFlags)l3);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "BeginCombo", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            byte* l1 = GetStr(lua, 2);
            int l2 = (int)lua.OptInteger(3, 0);
            var ret = ImGuiNative.igBeginCombo(l0, l1, (ImGuiComboFlags)l2);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            StrFree(l1);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "BeginDisabled", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte l0 = (lua.IsNoneOrNil(1) ? true : lua.ToBoolean(1)) ? (byte)1 : (byte)0;
            ImGuiNative.igBeginDisabled(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "BeginDragDropSource", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.OptInteger(1, 0);
            var ret = ImGuiNative.igBeginDragDropSource((ImGuiDragDropFlags)l0);
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "BeginDragDropTarget", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igBeginDragDropTarget();
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "BeginGroup", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igBeginGroup();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "BeginItemTooltip", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igBeginItemTooltip();
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "BeginListBox", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            Vector2 l1 = ReadVec2(lua, 2, 3, new Vector2(0,0));
            var ret = ImGuiNative.igBeginListBox(l0, l1);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "BeginMainMenuBar", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igBeginMainMenuBar();
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "BeginMenu", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            byte l1 = (lua.IsNoneOrNil(2) ? true : lua.ToBoolean(2)) ? (byte)1 : (byte)0;
            var ret = ImGuiNative.igBeginMenu(l0, l1);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "BeginMenuBar", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igBeginMenuBar();
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "BeginPopup", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            int l1 = (int)lua.OptInteger(2, 0);
            var ret = ImGuiNative.igBeginPopup(l0, (ImGuiWindowFlags)l1);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "BeginPopupContextItem", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1, null);
            int l1 = (int)lua.OptInteger(2, 1);
            var ret = ImGuiNative.igBeginPopupContextItem(l0, (ImGuiPopupFlags)l1);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "BeginPopupContextVoid", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1, null);
            int l1 = (int)lua.OptInteger(2, 1);
            var ret = ImGuiNative.igBeginPopupContextVoid(l0, (ImGuiPopupFlags)l1);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "BeginPopupContextWindow", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1, null);
            int l1 = (int)lua.OptInteger(2, 1);
            var ret = ImGuiNative.igBeginPopupContextWindow(l0, (ImGuiPopupFlags)l1);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "BeginPopupModal", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            byte l1 = lua.ToBoolean(2) ? (byte)1 : (byte)0;
            int l2 = (int)lua.OptInteger(3, 0);
            var ret = ImGuiNative.igBeginPopupModal(l0, (lua.IsNoneOrNil(2) ? null : &l1), (ImGuiWindowFlags)l2);
            lua.PushBoolean(ret != 0);
            lua.PushBoolean(l1 != 0);
            StrFree(l0);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "BeginTabBar", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            int l1 = (int)lua.OptInteger(2, 0);
            var ret = ImGuiNative.igBeginTabBar(l0, (ImGuiTabBarFlags)l1);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "BeginTabItem", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            byte l1 = lua.ToBoolean(2) ? (byte)1 : (byte)0;
            int l2 = (int)lua.OptInteger(3, 0);
            var ret = ImGuiNative.igBeginTabItem(l0, (lua.IsNoneOrNil(2) ? null : &l1), (ImGuiTabItemFlags)l2);
            lua.PushBoolean(ret != 0);
            lua.PushBoolean(l1 != 0);
            StrFree(l0);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "BeginTable", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            int l1 = (int)lua.CheckInteger(2);
            int l2 = (int)lua.OptInteger(3, 0);
            Vector2 l3 = ReadVec2(lua, 4, 5, new Vector2(0.0f,0.0f));
            float l4 = (float)lua.OptNumber(6, 0.0f);
            var ret = ImGuiNative.igBeginTable(l0, l1, (ImGuiTableFlags)l2, l3, l4);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "BeginTooltip", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igBeginTooltip();
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "Bullet", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igBullet();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "BulletText", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            ImGuiNative.igBulletText(l0);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "Button", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            Vector2 l1 = ReadVec2(lua, 2, 3, new Vector2(0,0));
            var ret = ImGuiNative.igButton(l0, l1);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "CalcTextSize", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            byte* l1 = GetStr(lua, 3);
            byte* l2 = GetStr(lua, 4, null);
            byte l3 = (lua.IsNoneOrNil(5) ? false : lua.ToBoolean(5)) ? (byte)1 : (byte)0;
            float l4 = (float)lua.OptNumber(6, -1.0f);
            ImGuiNative.igCalcTextSize(&l0, l1, l2, l3, l4);
            lua.PushNumber((double)l0.X);
            lua.PushNumber((double)l0.Y);
            StrFree(l1);
            StrFree(l2);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "Checkbox", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            byte l1 = lua.ToBoolean(2) ? (byte)1 : (byte)0;
            var ret = ImGuiNative.igCheckbox(l0, &l1);
            lua.PushBoolean(ret != 0);
            lua.PushBoolean(l1 != 0);
            StrFree(l0);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "CloseCurrentPopup", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igCloseCurrentPopup();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "CollapsingHeader_TreeNodeFlags", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            int l1 = (int)lua.OptInteger(2, 0);
            var ret = ImGuiNative.igCollapsingHeader_TreeNodeFlags(l0, (ImGuiTreeNodeFlags)l1);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "CollapsingHeader_BoolPtr", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            byte l1 = lua.ToBoolean(2) ? (byte)1 : (byte)0;
            int l2 = (int)lua.OptInteger(3, 0);
            var ret = ImGuiNative.igCollapsingHeader_BoolPtr(l0, &l1, (ImGuiTreeNodeFlags)l2);
            lua.PushBoolean(ret != 0);
            lua.PushBoolean(l1 != 0);
            StrFree(l0);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "ColorConvertHSVtoRGB", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            float l0 = (float)lua.CheckNumber(1);
            float l1 = (float)lua.CheckNumber(2);
            float l2 = (float)lua.CheckNumber(3);
            float l3 = (float)lua.CheckNumber(4);
            float l4 = (float)lua.CheckNumber(5);
            float l5 = (float)lua.CheckNumber(6);
            ImGuiNative.igColorConvertHSVtoRGB(l0, l1, l2, &l3, &l4, &l5);
            lua.PushNumber((double)l3);
            lua.PushNumber((double)l4);
            lua.PushNumber((double)l5);
            return 3;
        });
        LuaHelpers.ModuleFunction(lua, "ColorConvertRGBtoHSV", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            float l0 = (float)lua.CheckNumber(1);
            float l1 = (float)lua.CheckNumber(2);
            float l2 = (float)lua.CheckNumber(3);
            float l3 = (float)lua.CheckNumber(4);
            float l4 = (float)lua.CheckNumber(5);
            float l5 = (float)lua.CheckNumber(6);
            ImGuiNative.igColorConvertRGBtoHSV(l0, l1, l2, &l3, &l4, &l5);
            lua.PushNumber((double)l3);
            lua.PushNumber((double)l4);
            lua.PushNumber((double)l5);
            return 3;
        });
        LuaHelpers.ModuleFunction(lua, "Columns", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.OptInteger(1, 1);
            byte* l1 = GetStr(lua, 2, null);
            byte l2 = (lua.IsNoneOrNil(3) ? true : lua.ToBoolean(3)) ? (byte)1 : (byte)0;
            ImGuiNative.igColumns(l0, l1, l2);
            StrFree(l1);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "DebugFlashStyleColor", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.CheckInteger(1);
            ImGuiNative.igDebugFlashStyleColor((ImGuiCol)l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "DebugStartItemPicker", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igDebugStartItemPicker();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "DebugTextEncoding", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            ImGuiNative.igDebugTextEncoding(l0);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "DestroyPlatformWindows", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igDestroyPlatformWindows();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "DragFloat", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            float l1 = (float)lua.CheckNumber(2);
            float l2 = (float)lua.OptNumber(3, 1.0f);
            float l3 = (float)lua.OptNumber(4, 0.0f);
            float l4 = (float)lua.OptNumber(5, 0.0f);
            byte* l5 = GetStr(lua, 6, [37, 46, 51, 102, 0]);
            int l6 = (int)lua.OptInteger(7, 0);
            var ret = ImGuiNative.igDragFloat(l0, &l1, l2, l3, l4, l5, (ImGuiSliderFlags)l6);
            lua.PushBoolean(ret != 0);
            lua.PushNumber((double)l1);
            StrFree(l0);
            StrFree(l5);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "DragFloatRange2", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            float l1 = (float)lua.CheckNumber(2);
            float l2 = (float)lua.CheckNumber(3);
            float l3 = (float)lua.OptNumber(4, 1.0f);
            float l4 = (float)lua.OptNumber(5, 0.0f);
            float l5 = (float)lua.OptNumber(6, 0.0f);
            byte* l6 = GetStr(lua, 7, [37, 46, 51, 102, 0]);
            byte* l7 = GetStr(lua, 8, null);
            int l8 = (int)lua.OptInteger(9, 0);
            var ret = ImGuiNative.igDragFloatRange2(l0, &l1, &l2, l3, l4, l5, l6, l7, (ImGuiSliderFlags)l8);
            lua.PushBoolean(ret != 0);
            lua.PushNumber((double)l1);
            lua.PushNumber((double)l2);
            StrFree(l0);
            StrFree(l6);
            StrFree(l7);
            return 3;
        });
        LuaHelpers.ModuleFunction(lua, "Dummy", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            ImGuiNative.igDummy(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "End", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igEnd();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "EndChild", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igEndChild();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "EndCombo", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igEndCombo();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "EndDisabled", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igEndDisabled();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "EndDragDropSource", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igEndDragDropSource();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "EndDragDropTarget", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igEndDragDropTarget();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "EndFrame", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igEndFrame();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "EndGroup", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igEndGroup();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "EndListBox", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igEndListBox();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "EndMainMenuBar", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igEndMainMenuBar();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "EndMenu", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igEndMenu();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "EndMenuBar", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igEndMenuBar();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "EndPopup", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igEndPopup();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "EndTabBar", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igEndTabBar();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "EndTabItem", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igEndTabItem();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "EndTable", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igEndTable();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "EndTooltip", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igEndTooltip();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "GetColumnIndex", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igGetColumnIndex();
            lua.PushInteger(ret);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "GetColumnsCount", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igGetColumnsCount();
            lua.PushInteger(ret);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "GetContentRegionAvail", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            ImGuiNative.igGetContentRegionAvail(&l0);
            lua.PushNumber((double)l0.X);
            lua.PushNumber((double)l0.Y);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "GetContentRegionMax", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            ImGuiNative.igGetContentRegionMax(&l0);
            lua.PushNumber((double)l0.X);
            lua.PushNumber((double)l0.Y);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "GetCursorPos", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            ImGuiNative.igGetCursorPos(&l0);
            lua.PushNumber((double)l0.X);
            lua.PushNumber((double)l0.Y);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "GetCursorScreenPos", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            ImGuiNative.igGetCursorScreenPos(&l0);
            lua.PushNumber((double)l0.X);
            lua.PushNumber((double)l0.Y);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "GetCursorStartPos", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            ImGuiNative.igGetCursorStartPos(&l0);
            lua.PushNumber((double)l0.X);
            lua.PushNumber((double)l0.Y);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "GetFontTexUvWhitePixel", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            ImGuiNative.igGetFontTexUvWhitePixel(&l0);
            lua.PushNumber((double)l0.X);
            lua.PushNumber((double)l0.Y);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "GetFrameCount", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igGetFrameCount();
            lua.PushInteger(ret);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "GetItemRectMax", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            ImGuiNative.igGetItemRectMax(&l0);
            lua.PushNumber((double)l0.X);
            lua.PushNumber((double)l0.Y);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "GetItemRectMin", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            ImGuiNative.igGetItemRectMin(&l0);
            lua.PushNumber((double)l0.X);
            lua.PushNumber((double)l0.Y);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "GetItemRectSize", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            ImGuiNative.igGetItemRectSize(&l0);
            lua.PushNumber((double)l0.X);
            lua.PushNumber((double)l0.Y);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "GetMouseClickedCount", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.CheckInteger(1);
            var ret = ImGuiNative.igGetMouseClickedCount((ImGuiMouseButton)l0);
            lua.PushInteger(ret);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "GetMouseDragDelta", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            int l1 = (int)lua.OptInteger(3, 0);
            float l2 = (float)lua.OptNumber(4, -1.0f);
            ImGuiNative.igGetMouseDragDelta(&l0, (ImGuiMouseButton)l1, l2);
            lua.PushNumber((double)l0.X);
            lua.PushNumber((double)l0.Y);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "GetMousePos", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            ImGuiNative.igGetMousePos(&l0);
            lua.PushNumber((double)l0.X);
            lua.PushNumber((double)l0.Y);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "GetMousePosOnOpeningCurrentPopup", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            ImGuiNative.igGetMousePosOnOpeningCurrentPopup(&l0);
            lua.PushNumber((double)l0.X);
            lua.PushNumber((double)l0.Y);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "GetWindowContentRegionMax", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            ImGuiNative.igGetWindowContentRegionMax(&l0);
            lua.PushNumber((double)l0.X);
            lua.PushNumber((double)l0.Y);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "GetWindowContentRegionMin", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            ImGuiNative.igGetWindowContentRegionMin(&l0);
            lua.PushNumber((double)l0.X);
            lua.PushNumber((double)l0.Y);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "GetWindowPos", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            ImGuiNative.igGetWindowPos(&l0);
            lua.PushNumber((double)l0.X);
            lua.PushNumber((double)l0.Y);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "GetWindowSize", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            ImGuiNative.igGetWindowSize(&l0);
            lua.PushNumber((double)l0.X);
            lua.PushNumber((double)l0.Y);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "Indent", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            float l0 = (float)lua.OptNumber(1, 0.0f);
            ImGuiNative.igIndent(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "InputFloat", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            float l1 = (float)lua.CheckNumber(2);
            float l2 = (float)lua.OptNumber(3, 0.0f);
            float l3 = (float)lua.OptNumber(4, 0.0f);
            byte* l4 = GetStr(lua, 5, [37, 46, 51, 102, 0]);
            int l5 = (int)lua.OptInteger(6, 0);
            var ret = ImGuiNative.igInputFloat(l0, &l1, l2, l3, l4, (ImGuiInputTextFlags)l5);
            lua.PushBoolean(ret != 0);
            lua.PushNumber((double)l1);
            StrFree(l0);
            StrFree(l4);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "InvisibleButton", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            Vector2 l1 = ReadVec2(lua, 2, 3);
            int l2 = (int)lua.OptInteger(4, 0);
            var ret = ImGuiNative.igInvisibleButton(l0, l1, (ImGuiButtonFlags)l2);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsAnyItemActive", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igIsAnyItemActive();
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsAnyItemFocused", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igIsAnyItemFocused();
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsAnyItemHovered", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igIsAnyItemHovered();
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsAnyMouseDown", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igIsAnyMouseDown();
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsItemActivated", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igIsItemActivated();
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsItemActive", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igIsItemActive();
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsItemClicked", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.OptInteger(1, 0);
            var ret = ImGuiNative.igIsItemClicked((ImGuiMouseButton)l0);
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsItemDeactivated", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igIsItemDeactivated();
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsItemDeactivatedAfterEdit", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igIsItemDeactivatedAfterEdit();
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsItemEdited", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igIsItemEdited();
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsItemFocused", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igIsItemFocused();
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsItemHovered", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.OptInteger(1, 0);
            var ret = ImGuiNative.igIsItemHovered((ImGuiHoveredFlags)l0);
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsItemToggledOpen", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igIsItemToggledOpen();
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsItemVisible", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igIsItemVisible();
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsMouseClicked_Bool", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.CheckInteger(1);
            byte l1 = (lua.IsNoneOrNil(2) ? false : lua.ToBoolean(2)) ? (byte)1 : (byte)0;
            var ret = ImGuiNative.igIsMouseClicked_Bool((ImGuiMouseButton)l0, l1);
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsMouseDoubleClicked_Nil", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.CheckInteger(1);
            var ret = ImGuiNative.igIsMouseDoubleClicked_Nil((ImGuiMouseButton)l0);
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsMouseDown_Nil", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.CheckInteger(1);
            var ret = ImGuiNative.igIsMouseDown_Nil((ImGuiMouseButton)l0);
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsMouseDragging", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.CheckInteger(1);
            float l1 = (float)lua.OptNumber(2, -1.0f);
            var ret = ImGuiNative.igIsMouseDragging((ImGuiMouseButton)l0, l1);
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsMouseHoveringRect", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            Vector2 l1 = ReadVec2(lua, 3, 4);
            byte l2 = (lua.IsNoneOrNil(5) ? true : lua.ToBoolean(5)) ? (byte)1 : (byte)0;
            var ret = ImGuiNative.igIsMouseHoveringRect(l0, l1, l2);
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsMouseReleased_Nil", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.CheckInteger(1);
            var ret = ImGuiNative.igIsMouseReleased_Nil((ImGuiMouseButton)l0);
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsPopupOpen_Str", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            int l1 = (int)lua.OptInteger(2, 0);
            var ret = ImGuiNative.igIsPopupOpen_Str(l0, (ImGuiPopupFlags)l1);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsRectVisible_Nil", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            var ret = ImGuiNative.igIsRectVisible_Nil(l0);
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsRectVisible_Vec2", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            Vector2 l1 = ReadVec2(lua, 3, 4);
            var ret = ImGuiNative.igIsRectVisible_Vec2(l0, l1);
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsWindowAppearing", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igIsWindowAppearing();
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsWindowCollapsed", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igIsWindowCollapsed();
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsWindowDocked", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igIsWindowDocked();
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsWindowFocused", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.OptInteger(1, 0);
            var ret = ImGuiNative.igIsWindowFocused((ImGuiFocusedFlags)l0);
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "IsWindowHovered", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.OptInteger(1, 0);
            var ret = ImGuiNative.igIsWindowHovered((ImGuiHoveredFlags)l0);
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "LabelText", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            byte* l1 = GetStr(lua, 2);
            ImGuiNative.igLabelText(l0, l1);
            StrFree(l0);
            StrFree(l1);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "LoadIniSettingsFromDisk", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            ImGuiNative.igLoadIniSettingsFromDisk(l0);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "LogButtons", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igLogButtons();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "LogFinish", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igLogFinish();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "LogText", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            ImGuiNative.igLogText(l0);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "LogToClipboard", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.OptInteger(1, -1);
            ImGuiNative.igLogToClipboard(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "LogToFile", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.OptInteger(1, -1);
            byte* l1 = GetStr(lua, 2, null);
            ImGuiNative.igLogToFile(l0, l1);
            StrFree(l1);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "LogToTTY", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.OptInteger(1, -1);
            ImGuiNative.igLogToTTY(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "MenuItem_Bool", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            byte* l1 = GetStr(lua, 2, null);
            byte l2 = (lua.IsNoneOrNil(3) ? false : lua.ToBoolean(3)) ? (byte)1 : (byte)0;
            byte l3 = (lua.IsNoneOrNil(4) ? true : lua.ToBoolean(4)) ? (byte)1 : (byte)0;
            var ret = ImGuiNative.igMenuItem_Bool(l0, l1, l2, l3);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            StrFree(l1);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "MenuItem_BoolPtr", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            byte* l1 = GetStr(lua, 2);
            byte l2 = lua.ToBoolean(3) ? (byte)1 : (byte)0;
            byte l3 = (lua.IsNoneOrNil(4) ? true : lua.ToBoolean(4)) ? (byte)1 : (byte)0;
            var ret = ImGuiNative.igMenuItem_BoolPtr(l0, l1, &l2, l3);
            lua.PushBoolean(ret != 0);
            lua.PushBoolean(l2 != 0);
            StrFree(l0);
            StrFree(l1);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "NewFrame", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igNewFrame();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "NewLine", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igNewLine();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "NextColumn", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igNextColumn();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "OpenPopup_Str", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            int l1 = (int)lua.OptInteger(2, 0);
            ImGuiNative.igOpenPopup_Str(l0, (ImGuiPopupFlags)l1);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "OpenPopupOnItemClick", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1, null);
            int l1 = (int)lua.OptInteger(2, 1);
            ImGuiNative.igOpenPopupOnItemClick(l0, (ImGuiPopupFlags)l1);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "PopButtonRepeat", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igPopButtonRepeat();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "PopClipRect", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igPopClipRect();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "PopFont", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igPopFont();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "PopID", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igPopID();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "PopItemWidth", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igPopItemWidth();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "PopStyleColor", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.OptInteger(1, 1);
            ImGuiNative.igPopStyleColor(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "PopStyleVar", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.OptInteger(1, 1);
            ImGuiNative.igPopStyleVar(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "PopTabStop", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igPopTabStop();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "PopTextWrapPos", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igPopTextWrapPos();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "ProgressBar", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            float l0 = (float)lua.CheckNumber(1);
            Vector2 l1 = ReadVec2(lua, 2, 3, new Vector2(-float.MinValue,0));
            byte* l2 = GetStr(lua, 4, null);
            ImGuiNative.igProgressBar(l0, l1, l2);
            StrFree(l2);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "PushButtonRepeat", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte l0 = lua.ToBoolean(1) ? (byte)1 : (byte)0;
            ImGuiNative.igPushButtonRepeat(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "PushClipRect", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            Vector2 l1 = ReadVec2(lua, 3, 4);
            byte l2 = lua.ToBoolean(5) ? (byte)1 : (byte)0;
            ImGuiNative.igPushClipRect(l0, l1, l2);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "PushID_Str", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            ImGuiNative.igPushID_Str(l0);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "PushID_StrStr", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            byte* l1 = GetStr(lua, 2);
            ImGuiNative.igPushID_StrStr(l0, l1);
            StrFree(l0);
            StrFree(l1);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "PushID_Int", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.CheckInteger(1);
            ImGuiNative.igPushID_Int(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "PushItemWidth", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            float l0 = (float)lua.CheckNumber(1);
            ImGuiNative.igPushItemWidth(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "PushStyleVar_Float", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.CheckInteger(1);
            float l1 = (float)lua.CheckNumber(2);
            ImGuiNative.igPushStyleVar_Float((ImGuiStyleVar)l0, l1);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "PushStyleVar_Vec2", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.CheckInteger(1);
            Vector2 l1 = ReadVec2(lua, 2, 3);
            ImGuiNative.igPushStyleVar_Vec2((ImGuiStyleVar)l0, l1);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "PushTabStop", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte l0 = lua.ToBoolean(1) ? (byte)1 : (byte)0;
            ImGuiNative.igPushTabStop(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "PushTextWrapPos", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            float l0 = (float)lua.OptNumber(1, 0.0f);
            ImGuiNative.igPushTextWrapPos(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "RadioButton_Bool", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            byte l1 = lua.ToBoolean(2) ? (byte)1 : (byte)0;
            var ret = ImGuiNative.igRadioButton_Bool(l0, l1);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "Render", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igRender();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "ResetMouseDragDelta", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.OptInteger(1, 0);
            ImGuiNative.igResetMouseDragDelta((ImGuiMouseButton)l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SameLine", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            float l0 = (float)lua.OptNumber(1, 0.0f);
            float l1 = (float)lua.OptNumber(2, -1.0f);
            ImGuiNative.igSameLine(l0, l1);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SaveIniSettingsToDisk", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            ImGuiNative.igSaveIniSettingsToDisk(l0);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "Selectable_Bool", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            byte l1 = (lua.IsNoneOrNil(2) ? false : lua.ToBoolean(2)) ? (byte)1 : (byte)0;
            int l2 = (int)lua.OptInteger(3, 0);
            Vector2 l3 = ReadVec2(lua, 4, 5, new Vector2(0,0));
            var ret = ImGuiNative.igSelectable_Bool(l0, l1, (ImGuiSelectableFlags)l2, l3);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "Selectable_BoolPtr", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            byte l1 = lua.ToBoolean(2) ? (byte)1 : (byte)0;
            int l2 = (int)lua.OptInteger(3, 0);
            Vector2 l3 = ReadVec2(lua, 4, 5, new Vector2(0,0));
            var ret = ImGuiNative.igSelectable_BoolPtr(l0, &l1, (ImGuiSelectableFlags)l2, l3);
            lua.PushBoolean(ret != 0);
            lua.PushBoolean(l1 != 0);
            StrFree(l0);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "Separator", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igSeparator();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SeparatorText", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            ImGuiNative.igSeparatorText(l0);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetClipboardText", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            ImGuiNative.igSetClipboardText(l0);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetColorEditOptions", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.CheckInteger(1);
            ImGuiNative.igSetColorEditOptions((ImGuiColorEditFlags)l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetColumnOffset", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.CheckInteger(1);
            float l1 = (float)lua.CheckNumber(2);
            ImGuiNative.igSetColumnOffset(l0, l1);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetColumnWidth", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.CheckInteger(1);
            float l1 = (float)lua.CheckNumber(2);
            ImGuiNative.igSetColumnWidth(l0, l1);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetCursorPos", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            ImGuiNative.igSetCursorPos(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetCursorPosX", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            float l0 = (float)lua.CheckNumber(1);
            ImGuiNative.igSetCursorPosX(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetCursorPosY", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            float l0 = (float)lua.CheckNumber(1);
            ImGuiNative.igSetCursorPosY(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetCursorScreenPos", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            ImGuiNative.igSetCursorScreenPos(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetItemDefaultFocus", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igSetItemDefaultFocus();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetItemTooltip", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            ImGuiNative.igSetItemTooltip(l0);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetKeyboardFocusHere", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.OptInteger(1, 0);
            ImGuiNative.igSetKeyboardFocusHere(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetMouseCursor", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.CheckInteger(1);
            ImGuiNative.igSetMouseCursor((ImGuiMouseCursor)l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetNextFrameWantCaptureKeyboard", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte l0 = lua.ToBoolean(1) ? (byte)1 : (byte)0;
            ImGuiNative.igSetNextFrameWantCaptureKeyboard(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetNextFrameWantCaptureMouse", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte l0 = lua.ToBoolean(1) ? (byte)1 : (byte)0;
            ImGuiNative.igSetNextFrameWantCaptureMouse(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetNextItemAllowOverlap", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igSetNextItemAllowOverlap();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetNextItemOpen", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte l0 = lua.ToBoolean(1) ? (byte)1 : (byte)0;
            int l1 = (int)lua.OptInteger(2, 0);
            ImGuiNative.igSetNextItemOpen(l0, (ImGuiCond)l1);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetNextItemWidth", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            float l0 = (float)lua.CheckNumber(1);
            ImGuiNative.igSetNextItemWidth(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetNextWindowBgAlpha", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            float l0 = (float)lua.CheckNumber(1);
            ImGuiNative.igSetNextWindowBgAlpha(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetNextWindowCollapsed", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte l0 = lua.ToBoolean(1) ? (byte)1 : (byte)0;
            int l1 = (int)lua.OptInteger(2, 0);
            ImGuiNative.igSetNextWindowCollapsed(l0, (ImGuiCond)l1);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetNextWindowContentSize", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            ImGuiNative.igSetNextWindowContentSize(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetNextWindowFocus", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igSetNextWindowFocus();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetNextWindowPos", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            int l1 = (int)lua.OptInteger(3, 0);
            Vector2 l2 = ReadVec2(lua, 4, 5, new Vector2(0,0));
            ImGuiNative.igSetNextWindowPos(l0, (ImGuiCond)l1, l2);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetNextWindowScroll", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            ImGuiNative.igSetNextWindowScroll(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetNextWindowSize", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            int l1 = (int)lua.OptInteger(3, 0);
            ImGuiNative.igSetNextWindowSize(l0, (ImGuiCond)l1);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetScrollFromPosX_Float", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            float l0 = (float)lua.CheckNumber(1);
            float l1 = (float)lua.OptNumber(2, 0.5f);
            ImGuiNative.igSetScrollFromPosX_Float(l0, l1);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetScrollFromPosY_Float", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            float l0 = (float)lua.CheckNumber(1);
            float l1 = (float)lua.OptNumber(2, 0.5f);
            ImGuiNative.igSetScrollFromPosY_Float(l0, l1);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetScrollHereX", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            float l0 = (float)lua.OptNumber(1, 0.5f);
            ImGuiNative.igSetScrollHereX(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetScrollHereY", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            float l0 = (float)lua.OptNumber(1, 0.5f);
            ImGuiNative.igSetScrollHereY(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetScrollX_Float", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            float l0 = (float)lua.CheckNumber(1);
            ImGuiNative.igSetScrollX_Float(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetScrollY_Float", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            float l0 = (float)lua.CheckNumber(1);
            ImGuiNative.igSetScrollY_Float(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetTabItemClosed", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            ImGuiNative.igSetTabItemClosed(l0);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetTooltip", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            ImGuiNative.igSetTooltip(l0);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetWindowCollapsed_Bool", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte l0 = lua.ToBoolean(1) ? (byte)1 : (byte)0;
            int l1 = (int)lua.OptInteger(2, 0);
            ImGuiNative.igSetWindowCollapsed_Bool(l0, (ImGuiCond)l1);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetWindowCollapsed_Str", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            byte l1 = lua.ToBoolean(2) ? (byte)1 : (byte)0;
            int l2 = (int)lua.OptInteger(3, 0);
            ImGuiNative.igSetWindowCollapsed_Str(l0, l1, (ImGuiCond)l2);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetWindowFocus_Nil", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igSetWindowFocus_Nil();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetWindowFocus_Str", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            ImGuiNative.igSetWindowFocus_Str(l0);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetWindowFontScale", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            float l0 = (float)lua.CheckNumber(1);
            ImGuiNative.igSetWindowFontScale(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetWindowPos_Vec2", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            int l1 = (int)lua.OptInteger(3, 0);
            ImGuiNative.igSetWindowPos_Vec2(l0, (ImGuiCond)l1);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetWindowPos_Str", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            Vector2 l1 = ReadVec2(lua, 2, 3);
            int l2 = (int)lua.OptInteger(4, 0);
            ImGuiNative.igSetWindowPos_Str(l0, l1, (ImGuiCond)l2);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetWindowSize_Vec2", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            Vector2 l0 = ReadVec2(lua, 1, 2);
            int l1 = (int)lua.OptInteger(3, 0);
            ImGuiNative.igSetWindowSize_Vec2(l0, (ImGuiCond)l1);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SetWindowSize_Str", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            Vector2 l1 = ReadVec2(lua, 2, 3);
            int l2 = (int)lua.OptInteger(4, 0);
            ImGuiNative.igSetWindowSize_Str(l0, l1, (ImGuiCond)l2);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "ShowAboutWindow", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte l0 = lua.ToBoolean(1) ? (byte)1 : (byte)0;
            ImGuiNative.igShowAboutWindow((lua.IsNoneOrNil(1) ? null : &l0));
            lua.PushBoolean(l0 != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "ShowDebugLogWindow", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte l0 = lua.ToBoolean(1) ? (byte)1 : (byte)0;
            ImGuiNative.igShowDebugLogWindow((lua.IsNoneOrNil(1) ? null : &l0));
            lua.PushBoolean(l0 != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "ShowDemoWindow", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte l0 = lua.ToBoolean(1) ? (byte)1 : (byte)0;
            ImGuiNative.igShowDemoWindow((lua.IsNoneOrNil(1) ? null : &l0));
            lua.PushBoolean(l0 != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "ShowFontSelector", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            ImGuiNative.igShowFontSelector(l0);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "ShowIDStackToolWindow", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte l0 = lua.ToBoolean(1) ? (byte)1 : (byte)0;
            ImGuiNative.igShowIDStackToolWindow((lua.IsNoneOrNil(1) ? null : &l0));
            lua.PushBoolean(l0 != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "ShowMetricsWindow", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte l0 = lua.ToBoolean(1) ? (byte)1 : (byte)0;
            ImGuiNative.igShowMetricsWindow((lua.IsNoneOrNil(1) ? null : &l0));
            lua.PushBoolean(l0 != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "ShowStyleSelector", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            var ret = ImGuiNative.igShowStyleSelector(l0);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "ShowUserGuide", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igShowUserGuide();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "SliderAngle", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            float l1 = (float)lua.CheckNumber(2);
            float l2 = (float)lua.OptNumber(3, -360.0f);
            float l3 = (float)lua.OptNumber(4, +360.0f);
            byte* l4 = GetStr(lua, 5, [37, 46, 48, 102, 32, 100, 101, 103, 0]);
            int l5 = (int)lua.OptInteger(6, 0);
            var ret = ImGuiNative.igSliderAngle(l0, &l1, l2, l3, l4, (ImGuiSliderFlags)l5);
            lua.PushBoolean(ret != 0);
            lua.PushNumber((double)l1);
            StrFree(l0);
            StrFree(l4);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "SliderFloat", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            float l1 = (float)lua.CheckNumber(2);
            float l2 = (float)lua.CheckNumber(3);
            float l3 = (float)lua.CheckNumber(4);
            byte* l4 = GetStr(lua, 5, [37, 46, 51, 102, 0]);
            int l5 = (int)lua.OptInteger(6, 0);
            var ret = ImGuiNative.igSliderFloat(l0, &l1, l2, l3, l4, (ImGuiSliderFlags)l5);
            lua.PushBoolean(ret != 0);
            lua.PushNumber((double)l1);
            StrFree(l0);
            StrFree(l4);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "SmallButton", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            var ret = ImGuiNative.igSmallButton(l0);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "Spacing", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igSpacing();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "TabItemButton", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            int l1 = (int)lua.OptInteger(2, 0);
            var ret = ImGuiNative.igTabItemButton(l0, (ImGuiTabItemFlags)l1);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "TableAngledHeadersRow", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igTableAngledHeadersRow();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "TableGetColumnCount", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igTableGetColumnCount();
            lua.PushInteger(ret);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "TableGetColumnIndex", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igTableGetColumnIndex();
            lua.PushInteger(ret);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "TableGetHoveredColumn", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igTableGetHoveredColumn();
            lua.PushInteger(ret);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "TableGetRowIndex", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igTableGetRowIndex();
            lua.PushInteger(ret);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "TableHeader", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            ImGuiNative.igTableHeader(l0);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "TableHeadersRow", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igTableHeadersRow();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "TableNextColumn", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var ret = ImGuiNative.igTableNextColumn();
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "TableNextRow", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.OptInteger(1, 0);
            float l1 = (float)lua.OptNumber(2, 0.0f);
            ImGuiNative.igTableNextRow((ImGuiTableRowFlags)l0, l1);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "TableSetColumnEnabled", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.CheckInteger(1);
            byte l1 = lua.ToBoolean(2) ? (byte)1 : (byte)0;
            ImGuiNative.igTableSetColumnEnabled(l0, l1);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "TableSetColumnIndex", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.CheckInteger(1);
            var ret = ImGuiNative.igTableSetColumnIndex(l0);
            lua.PushBoolean(ret != 0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "TableSetupScrollFreeze", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            int l0 = (int)lua.CheckInteger(1);
            int l1 = (int)lua.CheckInteger(2);
            ImGuiNative.igTableSetupScrollFreeze(l0, l1);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "Text", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            ImGuiNative.igText(l0);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "TextDisabled", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            ImGuiNative.igTextDisabled(l0);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "TextUnformatted", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            byte* l1 = GetStr(lua, 2, null);
            ImGuiNative.igTextUnformatted(l0, l1);
            StrFree(l0);
            StrFree(l1);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "TextWrapped", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            ImGuiNative.igTextWrapped(l0);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "TreeNode_Str", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            var ret = ImGuiNative.igTreeNode_Str(l0);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "TreeNode_StrStr", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            byte* l1 = GetStr(lua, 2);
            var ret = ImGuiNative.igTreeNode_StrStr(l0, l1);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            StrFree(l1);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "TreeNodeEx_Str", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            int l1 = (int)lua.OptInteger(2, 0);
            var ret = ImGuiNative.igTreeNodeEx_Str(l0, (ImGuiTreeNodeFlags)l1);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "TreeNodeEx_StrStr", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            int l1 = (int)lua.CheckInteger(2);
            byte* l2 = GetStr(lua, 3);
            var ret = ImGuiNative.igTreeNodeEx_StrStr(l0, (ImGuiTreeNodeFlags)l1, l2);
            lua.PushBoolean(ret != 0);
            StrFree(l0);
            StrFree(l2);
            return 1;
        });
        LuaHelpers.ModuleFunction(lua, "TreePop", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igTreePop();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "TreePush_Str", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            ImGuiNative.igTreePush_Str(l0);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "Unindent", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            float l0 = (float)lua.OptNumber(1, 0.0f);
            ImGuiNative.igUnindent(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "UpdatePlatformWindows", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            ImGuiNative.igUpdatePlatformWindows();
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "VSliderFloat", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            Vector2 l1 = ReadVec2(lua, 2, 3);
            float l2 = (float)lua.CheckNumber(4);
            float l3 = (float)lua.CheckNumber(5);
            float l4 = (float)lua.CheckNumber(6);
            byte* l5 = GetStr(lua, 7, [37, 46, 51, 102, 0]);
            int l6 = (int)lua.OptInteger(8, 0);
            var ret = ImGuiNative.igVSliderFloat(l0, l1, &l2, l3, l4, l5, (ImGuiSliderFlags)l6);
            lua.PushBoolean(ret != 0);
            lua.PushNumber((double)l2);
            StrFree(l0);
            StrFree(l5);
            return 2;
        });
        LuaHelpers.ModuleFunction(lua, "Value_Bool", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            byte l1 = lua.ToBoolean(2) ? (byte)1 : (byte)0;
            ImGuiNative.igValue_Bool(l0, l1);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "Value_Int", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            int l1 = (int)lua.CheckInteger(2);
            ImGuiNative.igValue_Int(l0, l1);
            StrFree(l0);
            return 0;
        });
        LuaHelpers.ModuleFunction(lua, "Value_Float", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            byte* l0 = GetStr(lua, 1);
            float l1 = (float)lua.CheckNumber(2);
            byte* l2 = GetStr(lua, 3, null);
            ImGuiNative.igValue_Float(l0, l1, l2);
            StrFree(l0);
            StrFree(l2);
            return 0;
        });
        lua.PushInteger(0);
        lua.SetField(-2, "ImDrawFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "ImDrawFlags_Closed");
        lua.PushInteger(16);
        lua.SetField(-2, "ImDrawFlags_RoundCornersTopLeft");
        lua.PushInteger(32);
        lua.SetField(-2, "ImDrawFlags_RoundCornersTopRight");
        lua.PushInteger(64);
        lua.SetField(-2, "ImDrawFlags_RoundCornersBottomLeft");
        lua.PushInteger(128);
        lua.SetField(-2, "ImDrawFlags_RoundCornersBottomRight");
        lua.PushInteger(256);
        lua.SetField(-2, "ImDrawFlags_RoundCornersNone");
        lua.PushInteger(48);
        lua.SetField(-2, "ImDrawFlags_RoundCornersTop");
        lua.PushInteger(192);
        lua.SetField(-2, "ImDrawFlags_RoundCornersBottom");
        lua.PushInteger(80);
        lua.SetField(-2, "ImDrawFlags_RoundCornersLeft");
        lua.PushInteger(160);
        lua.SetField(-2, "ImDrawFlags_RoundCornersRight");
        lua.PushInteger(240);
        lua.SetField(-2, "ImDrawFlags_RoundCornersAll");
        lua.PushInteger(240);
        lua.SetField(-2, "ImDrawFlags_RoundCornersDefault_");
        lua.PushInteger(496);
        lua.SetField(-2, "ImDrawFlags_RoundCornersMask_");
        lua.PushInteger(0);
        lua.SetField(-2, "ImDrawListFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "ImDrawListFlags_AntiAliasedLines");
        lua.PushInteger(2);
        lua.SetField(-2, "ImDrawListFlags_AntiAliasedLinesUseTex");
        lua.PushInteger(4);
        lua.SetField(-2, "ImDrawListFlags_AntiAliasedFill");
        lua.PushInteger(8);
        lua.SetField(-2, "ImDrawListFlags_AllowVtxOffset");
        lua.PushInteger(0);
        lua.SetField(-2, "ImFontAtlasFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "ImFontAtlasFlags_NoPowerOfTwoHeight");
        lua.PushInteger(2);
        lua.SetField(-2, "ImFontAtlasFlags_NoMouseCursors");
        lua.PushInteger(4);
        lua.SetField(-2, "ImFontAtlasFlags_NoBakedLines");
        lua.PushInteger(0);
        lua.SetField(-2, "ActivateFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "ActivateFlags_PreferInput");
        lua.PushInteger(2);
        lua.SetField(-2, "ActivateFlags_PreferTweak");
        lua.PushInteger(4);
        lua.SetField(-2, "ActivateFlags_TryToPreserveState");
        lua.PushInteger(8);
        lua.SetField(-2, "ActivateFlags_FromTabbing");
        lua.PushInteger(16);
        lua.SetField(-2, "ActivateFlags_FromShortcut");
        lua.PushInteger(-1);
        lua.SetField(-2, "Axis_None");
        lua.PushInteger(0);
        lua.SetField(-2, "Axis_X");
        lua.PushInteger(1);
        lua.SetField(-2, "Axis_Y");
        lua.PushInteger(0);
        lua.SetField(-2, "BackendFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "BackendFlags_HasGamepad");
        lua.PushInteger(2);
        lua.SetField(-2, "BackendFlags_HasMouseCursors");
        lua.PushInteger(4);
        lua.SetField(-2, "BackendFlags_HasSetMousePos");
        lua.PushInteger(8);
        lua.SetField(-2, "BackendFlags_RendererHasVtxOffset");
        lua.PushInteger(1024);
        lua.SetField(-2, "BackendFlags_PlatformHasViewports");
        lua.PushInteger(2048);
        lua.SetField(-2, "BackendFlags_HasMouseHoveredViewport");
        lua.PushInteger(4096);
        lua.SetField(-2, "BackendFlags_RendererHasViewports");
        lua.PushInteger(16);
        lua.SetField(-2, "ButtonFlags_PressedOnClick");
        lua.PushInteger(32);
        lua.SetField(-2, "ButtonFlags_PressedOnClickRelease");
        lua.PushInteger(64);
        lua.SetField(-2, "ButtonFlags_PressedOnClickReleaseAnywhere");
        lua.PushInteger(128);
        lua.SetField(-2, "ButtonFlags_PressedOnRelease");
        lua.PushInteger(256);
        lua.SetField(-2, "ButtonFlags_PressedOnDoubleClick");
        lua.PushInteger(512);
        lua.SetField(-2, "ButtonFlags_PressedOnDragDropHold");
        lua.PushInteger(1024);
        lua.SetField(-2, "ButtonFlags_Repeat");
        lua.PushInteger(2048);
        lua.SetField(-2, "ButtonFlags_FlattenChildren");
        lua.PushInteger(4096);
        lua.SetField(-2, "ButtonFlags_AllowOverlap");
        lua.PushInteger(8192);
        lua.SetField(-2, "ButtonFlags_DontClosePopups");
        lua.PushInteger(32768);
        lua.SetField(-2, "ButtonFlags_AlignTextBaseLine");
        lua.PushInteger(65536);
        lua.SetField(-2, "ButtonFlags_NoKeyModifiers");
        lua.PushInteger(131072);
        lua.SetField(-2, "ButtonFlags_NoHoldingActiveId");
        lua.PushInteger(262144);
        lua.SetField(-2, "ButtonFlags_NoNavFocus");
        lua.PushInteger(524288);
        lua.SetField(-2, "ButtonFlags_NoHoveredOnFocus");
        lua.PushInteger(1048576);
        lua.SetField(-2, "ButtonFlags_NoSetKeyOwner");
        lua.PushInteger(2097152);
        lua.SetField(-2, "ButtonFlags_NoTestKeyOwner");
        lua.PushInteger(1008);
        lua.SetField(-2, "ButtonFlags_PressedOnMask_");
        lua.PushInteger(32);
        lua.SetField(-2, "ButtonFlags_PressedOnDefault_");
        lua.PushInteger(0);
        lua.SetField(-2, "ButtonFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "ButtonFlags_MouseButtonLeft");
        lua.PushInteger(2);
        lua.SetField(-2, "ButtonFlags_MouseButtonRight");
        lua.PushInteger(4);
        lua.SetField(-2, "ButtonFlags_MouseButtonMiddle");
        lua.PushInteger(7);
        lua.SetField(-2, "ButtonFlags_MouseButtonMask_");
        lua.PushInteger(0);
        lua.SetField(-2, "ChildFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "ChildFlags_Border");
        lua.PushInteger(2);
        lua.SetField(-2, "ChildFlags_AlwaysUseWindowPadding");
        lua.PushInteger(4);
        lua.SetField(-2, "ChildFlags_ResizeX");
        lua.PushInteger(8);
        lua.SetField(-2, "ChildFlags_ResizeY");
        lua.PushInteger(16);
        lua.SetField(-2, "ChildFlags_AutoResizeX");
        lua.PushInteger(32);
        lua.SetField(-2, "ChildFlags_AutoResizeY");
        lua.PushInteger(64);
        lua.SetField(-2, "ChildFlags_AlwaysAutoResize");
        lua.PushInteger(128);
        lua.SetField(-2, "ChildFlags_FrameStyle");
        lua.PushInteger(256);
        lua.SetField(-2, "ChildFlags_NavFlattened");
        lua.PushInteger(0);
        lua.SetField(-2, "Col_Text");
        lua.PushInteger(1);
        lua.SetField(-2, "Col_TextDisabled");
        lua.PushInteger(2);
        lua.SetField(-2, "Col_WindowBg");
        lua.PushInteger(3);
        lua.SetField(-2, "Col_ChildBg");
        lua.PushInteger(4);
        lua.SetField(-2, "Col_PopupBg");
        lua.PushInteger(5);
        lua.SetField(-2, "Col_Border");
        lua.PushInteger(6);
        lua.SetField(-2, "Col_BorderShadow");
        lua.PushInteger(7);
        lua.SetField(-2, "Col_FrameBg");
        lua.PushInteger(8);
        lua.SetField(-2, "Col_FrameBgHovered");
        lua.PushInteger(9);
        lua.SetField(-2, "Col_FrameBgActive");
        lua.PushInteger(10);
        lua.SetField(-2, "Col_TitleBg");
        lua.PushInteger(11);
        lua.SetField(-2, "Col_TitleBgActive");
        lua.PushInteger(12);
        lua.SetField(-2, "Col_TitleBgCollapsed");
        lua.PushInteger(13);
        lua.SetField(-2, "Col_MenuBarBg");
        lua.PushInteger(14);
        lua.SetField(-2, "Col_ScrollbarBg");
        lua.PushInteger(15);
        lua.SetField(-2, "Col_ScrollbarGrab");
        lua.PushInteger(16);
        lua.SetField(-2, "Col_ScrollbarGrabHovered");
        lua.PushInteger(17);
        lua.SetField(-2, "Col_ScrollbarGrabActive");
        lua.PushInteger(18);
        lua.SetField(-2, "Col_CheckMark");
        lua.PushInteger(19);
        lua.SetField(-2, "Col_SliderGrab");
        lua.PushInteger(20);
        lua.SetField(-2, "Col_SliderGrabActive");
        lua.PushInteger(21);
        lua.SetField(-2, "Col_Button");
        lua.PushInteger(22);
        lua.SetField(-2, "Col_ButtonHovered");
        lua.PushInteger(23);
        lua.SetField(-2, "Col_ButtonActive");
        lua.PushInteger(24);
        lua.SetField(-2, "Col_Header");
        lua.PushInteger(25);
        lua.SetField(-2, "Col_HeaderHovered");
        lua.PushInteger(26);
        lua.SetField(-2, "Col_HeaderActive");
        lua.PushInteger(27);
        lua.SetField(-2, "Col_Separator");
        lua.PushInteger(28);
        lua.SetField(-2, "Col_SeparatorHovered");
        lua.PushInteger(29);
        lua.SetField(-2, "Col_SeparatorActive");
        lua.PushInteger(30);
        lua.SetField(-2, "Col_ResizeGrip");
        lua.PushInteger(31);
        lua.SetField(-2, "Col_ResizeGripHovered");
        lua.PushInteger(32);
        lua.SetField(-2, "Col_ResizeGripActive");
        lua.PushInteger(33);
        lua.SetField(-2, "Col_TabHovered");
        lua.PushInteger(34);
        lua.SetField(-2, "Col_Tab");
        lua.PushInteger(35);
        lua.SetField(-2, "Col_TabSelected");
        lua.PushInteger(36);
        lua.SetField(-2, "Col_TabSelectedOverline");
        lua.PushInteger(37);
        lua.SetField(-2, "Col_TabDimmed");
        lua.PushInteger(38);
        lua.SetField(-2, "Col_TabDimmedSelected");
        lua.PushInteger(39);
        lua.SetField(-2, "Col_TabDimmedSelectedOverline");
        lua.PushInteger(40);
        lua.SetField(-2, "Col_DockingPreview");
        lua.PushInteger(41);
        lua.SetField(-2, "Col_DockingEmptyBg");
        lua.PushInteger(42);
        lua.SetField(-2, "Col_PlotLines");
        lua.PushInteger(43);
        lua.SetField(-2, "Col_PlotLinesHovered");
        lua.PushInteger(44);
        lua.SetField(-2, "Col_PlotHistogram");
        lua.PushInteger(45);
        lua.SetField(-2, "Col_PlotHistogramHovered");
        lua.PushInteger(46);
        lua.SetField(-2, "Col_TableHeaderBg");
        lua.PushInteger(47);
        lua.SetField(-2, "Col_TableBorderStrong");
        lua.PushInteger(48);
        lua.SetField(-2, "Col_TableBorderLight");
        lua.PushInteger(49);
        lua.SetField(-2, "Col_TableRowBg");
        lua.PushInteger(50);
        lua.SetField(-2, "Col_TableRowBgAlt");
        lua.PushInteger(51);
        lua.SetField(-2, "Col_TextSelectedBg");
        lua.PushInteger(52);
        lua.SetField(-2, "Col_DragDropTarget");
        lua.PushInteger(53);
        lua.SetField(-2, "Col_NavHighlight");
        lua.PushInteger(54);
        lua.SetField(-2, "Col_NavWindowingHighlight");
        lua.PushInteger(55);
        lua.SetField(-2, "Col_NavWindowingDimBg");
        lua.PushInteger(56);
        lua.SetField(-2, "Col_ModalWindowDimBg");
        lua.PushInteger(57);
        lua.SetField(-2, "Col_COUNT");
        lua.PushInteger(0);
        lua.SetField(-2, "ColorEditFlags_None");
        lua.PushInteger(2);
        lua.SetField(-2, "ColorEditFlags_NoAlpha");
        lua.PushInteger(4);
        lua.SetField(-2, "ColorEditFlags_NoPicker");
        lua.PushInteger(8);
        lua.SetField(-2, "ColorEditFlags_NoOptions");
        lua.PushInteger(16);
        lua.SetField(-2, "ColorEditFlags_NoSmallPreview");
        lua.PushInteger(32);
        lua.SetField(-2, "ColorEditFlags_NoInputs");
        lua.PushInteger(64);
        lua.SetField(-2, "ColorEditFlags_NoTooltip");
        lua.PushInteger(128);
        lua.SetField(-2, "ColorEditFlags_NoLabel");
        lua.PushInteger(256);
        lua.SetField(-2, "ColorEditFlags_NoSidePreview");
        lua.PushInteger(512);
        lua.SetField(-2, "ColorEditFlags_NoDragDrop");
        lua.PushInteger(1024);
        lua.SetField(-2, "ColorEditFlags_NoBorder");
        lua.PushInteger(65536);
        lua.SetField(-2, "ColorEditFlags_AlphaBar");
        lua.PushInteger(131072);
        lua.SetField(-2, "ColorEditFlags_AlphaPreview");
        lua.PushInteger(262144);
        lua.SetField(-2, "ColorEditFlags_AlphaPreviewHalf");
        lua.PushInteger(524288);
        lua.SetField(-2, "ColorEditFlags_HDR");
        lua.PushInteger(1048576);
        lua.SetField(-2, "ColorEditFlags_DisplayRGB");
        lua.PushInteger(2097152);
        lua.SetField(-2, "ColorEditFlags_DisplayHSV");
        lua.PushInteger(4194304);
        lua.SetField(-2, "ColorEditFlags_DisplayHex");
        lua.PushInteger(8388608);
        lua.SetField(-2, "ColorEditFlags_Uint8");
        lua.PushInteger(16777216);
        lua.SetField(-2, "ColorEditFlags_Float");
        lua.PushInteger(33554432);
        lua.SetField(-2, "ColorEditFlags_PickerHueBar");
        lua.PushInteger(67108864);
        lua.SetField(-2, "ColorEditFlags_PickerHueWheel");
        lua.PushInteger(134217728);
        lua.SetField(-2, "ColorEditFlags_InputRGB");
        lua.PushInteger(268435456);
        lua.SetField(-2, "ColorEditFlags_InputHSV");
        lua.PushInteger(177209344);
        lua.SetField(-2, "ColorEditFlags_DefaultOptions_");
        lua.PushInteger(7340032);
        lua.SetField(-2, "ColorEditFlags_DisplayMask_");
        lua.PushInteger(25165824);
        lua.SetField(-2, "ColorEditFlags_DataTypeMask_");
        lua.PushInteger(100663296);
        lua.SetField(-2, "ColorEditFlags_PickerMask_");
        lua.PushInteger(402653184);
        lua.SetField(-2, "ColorEditFlags_InputMask_");
        lua.PushInteger(1048576);
        lua.SetField(-2, "ComboFlags_CustomPreview");
        lua.PushInteger(0);
        lua.SetField(-2, "ComboFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "ComboFlags_PopupAlignLeft");
        lua.PushInteger(2);
        lua.SetField(-2, "ComboFlags_HeightSmall");
        lua.PushInteger(4);
        lua.SetField(-2, "ComboFlags_HeightRegular");
        lua.PushInteger(8);
        lua.SetField(-2, "ComboFlags_HeightLarge");
        lua.PushInteger(16);
        lua.SetField(-2, "ComboFlags_HeightLargest");
        lua.PushInteger(32);
        lua.SetField(-2, "ComboFlags_NoArrowButton");
        lua.PushInteger(64);
        lua.SetField(-2, "ComboFlags_NoPreview");
        lua.PushInteger(128);
        lua.SetField(-2, "ComboFlags_WidthFitPreview");
        lua.PushInteger(30);
        lua.SetField(-2, "ComboFlags_HeightMask_");
        lua.PushInteger(0);
        lua.SetField(-2, "Cond_None");
        lua.PushInteger(1);
        lua.SetField(-2, "Cond_Always");
        lua.PushInteger(2);
        lua.SetField(-2, "Cond_Once");
        lua.PushInteger(4);
        lua.SetField(-2, "Cond_FirstUseEver");
        lua.PushInteger(8);
        lua.SetField(-2, "Cond_Appearing");
        lua.PushInteger(0);
        lua.SetField(-2, "ConfigFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "ConfigFlags_NavEnableKeyboard");
        lua.PushInteger(2);
        lua.SetField(-2, "ConfigFlags_NavEnableGamepad");
        lua.PushInteger(4);
        lua.SetField(-2, "ConfigFlags_NavEnableSetMousePos");
        lua.PushInteger(8);
        lua.SetField(-2, "ConfigFlags_NavNoCaptureKeyboard");
        lua.PushInteger(16);
        lua.SetField(-2, "ConfigFlags_NoMouse");
        lua.PushInteger(32);
        lua.SetField(-2, "ConfigFlags_NoMouseCursorChange");
        lua.PushInteger(64);
        lua.SetField(-2, "ConfigFlags_NoKeyboard");
        lua.PushInteger(128);
        lua.SetField(-2, "ConfigFlags_DockingEnable");
        lua.PushInteger(1024);
        lua.SetField(-2, "ConfigFlags_ViewportsEnable");
        lua.PushInteger(16384);
        lua.SetField(-2, "ConfigFlags_DpiEnableScaleViewports");
        lua.PushInteger(32768);
        lua.SetField(-2, "ConfigFlags_DpiEnableScaleFonts");
        lua.PushInteger(1048576);
        lua.SetField(-2, "ConfigFlags_IsSRGB");
        lua.PushInteger(2097152);
        lua.SetField(-2, "ConfigFlags_IsTouchScreen");
        lua.PushInteger(0);
        lua.SetField(-2, "ContextHookType_NewFramePre");
        lua.PushInteger(1);
        lua.SetField(-2, "ContextHookType_NewFramePost");
        lua.PushInteger(2);
        lua.SetField(-2, "ContextHookType_EndFramePre");
        lua.PushInteger(3);
        lua.SetField(-2, "ContextHookType_EndFramePost");
        lua.PushInteger(4);
        lua.SetField(-2, "ContextHookType_RenderPre");
        lua.PushInteger(5);
        lua.SetField(-2, "ContextHookType_RenderPost");
        lua.PushInteger(6);
        lua.SetField(-2, "ContextHookType_Shutdown");
        lua.PushInteger(7);
        lua.SetField(-2, "ContextHookType_PendingRemoval_");
        lua.PushInteger(0);
        lua.SetField(-2, "DataAuthority_Auto");
        lua.PushInteger(1);
        lua.SetField(-2, "DataAuthority_DockNode");
        lua.PushInteger(2);
        lua.SetField(-2, "DataAuthority_Window");
        lua.PushInteger(11);
        lua.SetField(-2, "DataType_String");
        lua.PushInteger(12);
        lua.SetField(-2, "DataType_Pointer");
        lua.PushInteger(13);
        lua.SetField(-2, "DataType_ID");
        lua.PushInteger(0);
        lua.SetField(-2, "DataType_S8");
        lua.PushInteger(1);
        lua.SetField(-2, "DataType_U8");
        lua.PushInteger(2);
        lua.SetField(-2, "DataType_S16");
        lua.PushInteger(3);
        lua.SetField(-2, "DataType_U16");
        lua.PushInteger(4);
        lua.SetField(-2, "DataType_S32");
        lua.PushInteger(5);
        lua.SetField(-2, "DataType_U32");
        lua.PushInteger(6);
        lua.SetField(-2, "DataType_S64");
        lua.PushInteger(7);
        lua.SetField(-2, "DataType_U64");
        lua.PushInteger(8);
        lua.SetField(-2, "DataType_Float");
        lua.PushInteger(9);
        lua.SetField(-2, "DataType_Double");
        lua.PushInteger(10);
        lua.SetField(-2, "DataType_COUNT");
        lua.PushInteger(0);
        lua.SetField(-2, "DebugLogFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "DebugLogFlags_EventActiveId");
        lua.PushInteger(2);
        lua.SetField(-2, "DebugLogFlags_EventFocus");
        lua.PushInteger(4);
        lua.SetField(-2, "DebugLogFlags_EventPopup");
        lua.PushInteger(8);
        lua.SetField(-2, "DebugLogFlags_EventNav");
        lua.PushInteger(16);
        lua.SetField(-2, "DebugLogFlags_EventClipper");
        lua.PushInteger(32);
        lua.SetField(-2, "DebugLogFlags_EventSelection");
        lua.PushInteger(64);
        lua.SetField(-2, "DebugLogFlags_EventIO");
        lua.PushInteger(128);
        lua.SetField(-2, "DebugLogFlags_EventInputRouting");
        lua.PushInteger(256);
        lua.SetField(-2, "DebugLogFlags_EventDocking");
        lua.PushInteger(512);
        lua.SetField(-2, "DebugLogFlags_EventViewport");
        lua.PushInteger(1023);
        lua.SetField(-2, "DebugLogFlags_EventMask_");
        lua.PushInteger(1048576);
        lua.SetField(-2, "DebugLogFlags_OutputToTTY");
        lua.PushInteger(2097152);
        lua.SetField(-2, "DebugLogFlags_OutputToTestEngine");
        lua.PushInteger(-1);
        lua.SetField(-2, "Dir_None");
        lua.PushInteger(0);
        lua.SetField(-2, "Dir_Left");
        lua.PushInteger(1);
        lua.SetField(-2, "Dir_Right");
        lua.PushInteger(2);
        lua.SetField(-2, "Dir_Up");
        lua.PushInteger(3);
        lua.SetField(-2, "Dir_Down");
        lua.PushInteger(4);
        lua.SetField(-2, "Dir_COUNT");
        lua.PushInteger(1024);
        lua.SetField(-2, "DockNodeFlags_DockSpace");
        lua.PushInteger(2048);
        lua.SetField(-2, "DockNodeFlags_CentralNode");
        lua.PushInteger(4096);
        lua.SetField(-2, "DockNodeFlags_NoTabBar");
        lua.PushInteger(8192);
        lua.SetField(-2, "DockNodeFlags_HiddenTabBar");
        lua.PushInteger(16384);
        lua.SetField(-2, "DockNodeFlags_NoWindowMenuButton");
        lua.PushInteger(32768);
        lua.SetField(-2, "DockNodeFlags_NoCloseButton");
        lua.PushInteger(65536);
        lua.SetField(-2, "DockNodeFlags_NoResizeX");
        lua.PushInteger(131072);
        lua.SetField(-2, "DockNodeFlags_NoResizeY");
        lua.PushInteger(262144);
        lua.SetField(-2, "DockNodeFlags_DockedWindowsInFocusRoute");
        lua.PushInteger(524288);
        lua.SetField(-2, "DockNodeFlags_NoDockingSplitOther");
        lua.PushInteger(1048576);
        lua.SetField(-2, "DockNodeFlags_NoDockingOverMe");
        lua.PushInteger(2097152);
        lua.SetField(-2, "DockNodeFlags_NoDockingOverOther");
        lua.PushInteger(4194304);
        lua.SetField(-2, "DockNodeFlags_NoDockingOverEmpty");
        lua.PushInteger(7864336);
        lua.SetField(-2, "DockNodeFlags_NoDocking");
        lua.PushInteger(-1);
        lua.SetField(-2, "DockNodeFlags_SharedFlagsInheritMask_");
        lua.PushInteger(196640);
        lua.SetField(-2, "DockNodeFlags_NoResizeFlagsMask_");
        lua.PushInteger(260208);
        lua.SetField(-2, "DockNodeFlags_LocalFlagsTransferMask_");
        lua.PushInteger(261152);
        lua.SetField(-2, "DockNodeFlags_SavedFlagsMask_");
        lua.PushInteger(0);
        lua.SetField(-2, "DockNodeFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "DockNodeFlags_KeepAliveOnly");
        lua.PushInteger(4);
        lua.SetField(-2, "DockNodeFlags_NoDockingOverCentralNode");
        lua.PushInteger(8);
        lua.SetField(-2, "DockNodeFlags_PassthruCentralNode");
        lua.PushInteger(16);
        lua.SetField(-2, "DockNodeFlags_NoDockingSplit");
        lua.PushInteger(32);
        lua.SetField(-2, "DockNodeFlags_NoResize");
        lua.PushInteger(64);
        lua.SetField(-2, "DockNodeFlags_AutoHideTabBar");
        lua.PushInteger(128);
        lua.SetField(-2, "DockNodeFlags_NoUndocking");
        lua.PushInteger(0);
        lua.SetField(-2, "DockNodeState_Unknown");
        lua.PushInteger(1);
        lua.SetField(-2, "DockNodeState_HostWindowHiddenBecauseSingleWindow");
        lua.PushInteger(2);
        lua.SetField(-2, "DockNodeState_HostWindowHiddenBecauseWindowsAreResizing");
        lua.PushInteger(3);
        lua.SetField(-2, "DockNodeState_HostWindowVisible");
        lua.PushInteger(0);
        lua.SetField(-2, "DragDropFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "DragDropFlags_SourceNoPreviewTooltip");
        lua.PushInteger(2);
        lua.SetField(-2, "DragDropFlags_SourceNoDisableHover");
        lua.PushInteger(4);
        lua.SetField(-2, "DragDropFlags_SourceNoHoldToOpenOthers");
        lua.PushInteger(8);
        lua.SetField(-2, "DragDropFlags_SourceAllowNullID");
        lua.PushInteger(16);
        lua.SetField(-2, "DragDropFlags_SourceExtern");
        lua.PushInteger(32);
        lua.SetField(-2, "DragDropFlags_PayloadAutoExpire");
        lua.PushInteger(64);
        lua.SetField(-2, "DragDropFlags_PayloadNoCrossContext");
        lua.PushInteger(128);
        lua.SetField(-2, "DragDropFlags_PayloadNoCrossProcess");
        lua.PushInteger(1024);
        lua.SetField(-2, "DragDropFlags_AcceptBeforeDelivery");
        lua.PushInteger(2048);
        lua.SetField(-2, "DragDropFlags_AcceptNoDrawDefaultRect");
        lua.PushInteger(4096);
        lua.SetField(-2, "DragDropFlags_AcceptNoPreviewTooltip");
        lua.PushInteger(3072);
        lua.SetField(-2, "DragDropFlags_AcceptPeekOnly");
        lua.PushInteger(0);
        lua.SetField(-2, "FocusRequestFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "FocusRequestFlags_RestoreFocusedChild");
        lua.PushInteger(2);
        lua.SetField(-2, "FocusRequestFlags_UnlessBelowModal");
        lua.PushInteger(0);
        lua.SetField(-2, "FocusedFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "FocusedFlags_ChildWindows");
        lua.PushInteger(2);
        lua.SetField(-2, "FocusedFlags_RootWindow");
        lua.PushInteger(4);
        lua.SetField(-2, "FocusedFlags_AnyWindow");
        lua.PushInteger(8);
        lua.SetField(-2, "FocusedFlags_NoPopupHierarchy");
        lua.PushInteger(16);
        lua.SetField(-2, "FocusedFlags_DockHierarchy");
        lua.PushInteger(3);
        lua.SetField(-2, "FocusedFlags_RootAndChildWindows");
        lua.PushInteger(1);
        lua.SetField(-2, "FreeTypeBuilderFlags_NoHinting");
        lua.PushInteger(2);
        lua.SetField(-2, "FreeTypeBuilderFlags_NoAutoHint");
        lua.PushInteger(4);
        lua.SetField(-2, "FreeTypeBuilderFlags_ForceAutoHint");
        lua.PushInteger(8);
        lua.SetField(-2, "FreeTypeBuilderFlags_LightHinting");
        lua.PushInteger(16);
        lua.SetField(-2, "FreeTypeBuilderFlags_MonoHinting");
        lua.PushInteger(32);
        lua.SetField(-2, "FreeTypeBuilderFlags_Bold");
        lua.PushInteger(64);
        lua.SetField(-2, "FreeTypeBuilderFlags_Oblique");
        lua.PushInteger(128);
        lua.SetField(-2, "FreeTypeBuilderFlags_Monochrome");
        lua.PushInteger(256);
        lua.SetField(-2, "FreeTypeBuilderFlags_LoadColor");
        lua.PushInteger(512);
        lua.SetField(-2, "FreeTypeBuilderFlags_Bitmap");
        lua.PushInteger(245760);
        lua.SetField(-2, "HoveredFlags_DelayMask_");
        lua.PushInteger(12479);
        lua.SetField(-2, "HoveredFlags_AllowedMaskForIsWindowHovered");
        lua.PushInteger(262048);
        lua.SetField(-2, "HoveredFlags_AllowedMaskForIsItemHovered");
        lua.PushInteger(0);
        lua.SetField(-2, "HoveredFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "HoveredFlags_ChildWindows");
        lua.PushInteger(2);
        lua.SetField(-2, "HoveredFlags_RootWindow");
        lua.PushInteger(4);
        lua.SetField(-2, "HoveredFlags_AnyWindow");
        lua.PushInteger(8);
        lua.SetField(-2, "HoveredFlags_NoPopupHierarchy");
        lua.PushInteger(16);
        lua.SetField(-2, "HoveredFlags_DockHierarchy");
        lua.PushInteger(32);
        lua.SetField(-2, "HoveredFlags_AllowWhenBlockedByPopup");
        lua.PushInteger(128);
        lua.SetField(-2, "HoveredFlags_AllowWhenBlockedByActiveItem");
        lua.PushInteger(256);
        lua.SetField(-2, "HoveredFlags_AllowWhenOverlappedByItem");
        lua.PushInteger(512);
        lua.SetField(-2, "HoveredFlags_AllowWhenOverlappedByWindow");
        lua.PushInteger(1024);
        lua.SetField(-2, "HoveredFlags_AllowWhenDisabled");
        lua.PushInteger(2048);
        lua.SetField(-2, "HoveredFlags_NoNavOverride");
        lua.PushInteger(768);
        lua.SetField(-2, "HoveredFlags_AllowWhenOverlapped");
        lua.PushInteger(928);
        lua.SetField(-2, "HoveredFlags_RectOnly");
        lua.PushInteger(3);
        lua.SetField(-2, "HoveredFlags_RootAndChildWindows");
        lua.PushInteger(4096);
        lua.SetField(-2, "HoveredFlags_ForTooltip");
        lua.PushInteger(8192);
        lua.SetField(-2, "HoveredFlags_Stationary");
        lua.PushInteger(16384);
        lua.SetField(-2, "HoveredFlags_DelayNone");
        lua.PushInteger(32768);
        lua.SetField(-2, "HoveredFlags_DelayShort");
        lua.PushInteger(65536);
        lua.SetField(-2, "HoveredFlags_DelayNormal");
        lua.PushInteger(131072);
        lua.SetField(-2, "HoveredFlags_NoSharedDelay");
        lua.PushInteger(0);
        lua.SetField(-2, "InputEventType_None");
        lua.PushInteger(1);
        lua.SetField(-2, "InputEventType_MousePos");
        lua.PushInteger(2);
        lua.SetField(-2, "InputEventType_MouseWheel");
        lua.PushInteger(3);
        lua.SetField(-2, "InputEventType_MouseButton");
        lua.PushInteger(4);
        lua.SetField(-2, "InputEventType_MouseViewport");
        lua.PushInteger(5);
        lua.SetField(-2, "InputEventType_Key");
        lua.PushInteger(6);
        lua.SetField(-2, "InputEventType_Text");
        lua.PushInteger(7);
        lua.SetField(-2, "InputEventType_Focus");
        lua.PushInteger(8);
        lua.SetField(-2, "InputEventType_COUNT");
        lua.PushInteger(2);
        lua.SetField(-2, "InputFlags_RepeatRateDefault");
        lua.PushInteger(4);
        lua.SetField(-2, "InputFlags_RepeatRateNavMove");
        lua.PushInteger(8);
        lua.SetField(-2, "InputFlags_RepeatRateNavTweak");
        lua.PushInteger(16);
        lua.SetField(-2, "InputFlags_RepeatUntilRelease");
        lua.PushInteger(32);
        lua.SetField(-2, "InputFlags_RepeatUntilKeyModsChange");
        lua.PushInteger(64);
        lua.SetField(-2, "InputFlags_RepeatUntilKeyModsChangeFromNone");
        lua.PushInteger(128);
        lua.SetField(-2, "InputFlags_RepeatUntilOtherKeyPress");
        lua.PushInteger(1048576);
        lua.SetField(-2, "InputFlags_LockThisFrame");
        lua.PushInteger(2097152);
        lua.SetField(-2, "InputFlags_LockUntilRelease");
        lua.PushInteger(4194304);
        lua.SetField(-2, "InputFlags_CondHovered");
        lua.PushInteger(8388608);
        lua.SetField(-2, "InputFlags_CondActive");
        lua.PushInteger(12582912);
        lua.SetField(-2, "InputFlags_CondDefault_");
        lua.PushInteger(14);
        lua.SetField(-2, "InputFlags_RepeatRateMask_");
        lua.PushInteger(240);
        lua.SetField(-2, "InputFlags_RepeatUntilMask_");
        lua.PushInteger(255);
        lua.SetField(-2, "InputFlags_RepeatMask_");
        lua.PushInteger(12582912);
        lua.SetField(-2, "InputFlags_CondMask_");
        lua.PushInteger(15360);
        lua.SetField(-2, "InputFlags_RouteTypeMask_");
        lua.PushInteger(245760);
        lua.SetField(-2, "InputFlags_RouteOptionsMask_");
        lua.PushInteger(255);
        lua.SetField(-2, "InputFlags_SupportedByIsKeyPressed");
        lua.PushInteger(1);
        lua.SetField(-2, "InputFlags_SupportedByIsMouseClicked");
        lua.PushInteger(261375);
        lua.SetField(-2, "InputFlags_SupportedByShortcut");
        lua.PushInteger(523519);
        lua.SetField(-2, "InputFlags_SupportedBySetNextItemShortcut");
        lua.PushInteger(3145728);
        lua.SetField(-2, "InputFlags_SupportedBySetKeyOwner");
        lua.PushInteger(15728640);
        lua.SetField(-2, "InputFlags_SupportedBySetItemKeyOwner");
        lua.PushInteger(0);
        lua.SetField(-2, "InputFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "InputFlags_Repeat");
        lua.PushInteger(1024);
        lua.SetField(-2, "InputFlags_RouteActive");
        lua.PushInteger(2048);
        lua.SetField(-2, "InputFlags_RouteFocused");
        lua.PushInteger(4096);
        lua.SetField(-2, "InputFlags_RouteGlobal");
        lua.PushInteger(8192);
        lua.SetField(-2, "InputFlags_RouteAlways");
        lua.PushInteger(16384);
        lua.SetField(-2, "InputFlags_RouteOverFocused");
        lua.PushInteger(32768);
        lua.SetField(-2, "InputFlags_RouteOverActive");
        lua.PushInteger(65536);
        lua.SetField(-2, "InputFlags_RouteUnlessBgFocused");
        lua.PushInteger(131072);
        lua.SetField(-2, "InputFlags_RouteFromRootWindow");
        lua.PushInteger(262144);
        lua.SetField(-2, "InputFlags_Tooltip");
        lua.PushInteger(0);
        lua.SetField(-2, "InputSource_None");
        lua.PushInteger(1);
        lua.SetField(-2, "InputSource_Mouse");
        lua.PushInteger(2);
        lua.SetField(-2, "InputSource_Keyboard");
        lua.PushInteger(3);
        lua.SetField(-2, "InputSource_Gamepad");
        lua.PushInteger(4);
        lua.SetField(-2, "InputSource_COUNT");
        lua.PushInteger(67108864);
        lua.SetField(-2, "InputTextFlags_Multiline");
        lua.PushInteger(134217728);
        lua.SetField(-2, "InputTextFlags_NoMarkEdited");
        lua.PushInteger(268435456);
        lua.SetField(-2, "InputTextFlags_MergedItem");
        lua.PushInteger(536870912);
        lua.SetField(-2, "InputTextFlags_LocalizeDecimalPoint");
        lua.PushInteger(0);
        lua.SetField(-2, "InputTextFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "InputTextFlags_CharsDecimal");
        lua.PushInteger(2);
        lua.SetField(-2, "InputTextFlags_CharsHexadecimal");
        lua.PushInteger(4);
        lua.SetField(-2, "InputTextFlags_CharsScientific");
        lua.PushInteger(8);
        lua.SetField(-2, "InputTextFlags_CharsUppercase");
        lua.PushInteger(16);
        lua.SetField(-2, "InputTextFlags_CharsNoBlank");
        lua.PushInteger(32);
        lua.SetField(-2, "InputTextFlags_AllowTabInput");
        lua.PushInteger(64);
        lua.SetField(-2, "InputTextFlags_EnterReturnsTrue");
        lua.PushInteger(128);
        lua.SetField(-2, "InputTextFlags_EscapeClearsAll");
        lua.PushInteger(256);
        lua.SetField(-2, "InputTextFlags_CtrlEnterForNewLine");
        lua.PushInteger(512);
        lua.SetField(-2, "InputTextFlags_ReadOnly");
        lua.PushInteger(1024);
        lua.SetField(-2, "InputTextFlags_Password");
        lua.PushInteger(2048);
        lua.SetField(-2, "InputTextFlags_AlwaysOverwrite");
        lua.PushInteger(4096);
        lua.SetField(-2, "InputTextFlags_AutoSelectAll");
        lua.PushInteger(8192);
        lua.SetField(-2, "InputTextFlags_ParseEmptyRefVal");
        lua.PushInteger(16384);
        lua.SetField(-2, "InputTextFlags_DisplayEmptyRefVal");
        lua.PushInteger(32768);
        lua.SetField(-2, "InputTextFlags_NoHorizontalScroll");
        lua.PushInteger(65536);
        lua.SetField(-2, "InputTextFlags_NoUndoRedo");
        lua.PushInteger(131072);
        lua.SetField(-2, "InputTextFlags_CallbackCompletion");
        lua.PushInteger(262144);
        lua.SetField(-2, "InputTextFlags_CallbackHistory");
        lua.PushInteger(524288);
        lua.SetField(-2, "InputTextFlags_CallbackAlways");
        lua.PushInteger(1048576);
        lua.SetField(-2, "InputTextFlags_CallbackCharFilter");
        lua.PushInteger(2097152);
        lua.SetField(-2, "InputTextFlags_CallbackResize");
        lua.PushInteger(4194304);
        lua.SetField(-2, "InputTextFlags_CallbackEdit");
        lua.PushInteger(0);
        lua.SetField(-2, "ItemFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "ItemFlags_NoTabStop");
        lua.PushInteger(2);
        lua.SetField(-2, "ItemFlags_ButtonRepeat");
        lua.PushInteger(4);
        lua.SetField(-2, "ItemFlags_Disabled");
        lua.PushInteger(8);
        lua.SetField(-2, "ItemFlags_NoNav");
        lua.PushInteger(16);
        lua.SetField(-2, "ItemFlags_NoNavDefaultFocus");
        lua.PushInteger(32);
        lua.SetField(-2, "ItemFlags_SelectableDontClosePopup");
        lua.PushInteger(64);
        lua.SetField(-2, "ItemFlags_MixedValue");
        lua.PushInteger(128);
        lua.SetField(-2, "ItemFlags_ReadOnly");
        lua.PushInteger(256);
        lua.SetField(-2, "ItemFlags_NoWindowHoverableCheck");
        lua.PushInteger(512);
        lua.SetField(-2, "ItemFlags_AllowOverlap");
        lua.PushInteger(1024);
        lua.SetField(-2, "ItemFlags_Inputable");
        lua.PushInteger(2048);
        lua.SetField(-2, "ItemFlags_HasSelectionUserData");
        lua.PushInteger(0);
        lua.SetField(-2, "ItemStatusFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "ItemStatusFlags_HoveredRect");
        lua.PushInteger(2);
        lua.SetField(-2, "ItemStatusFlags_HasDisplayRect");
        lua.PushInteger(4);
        lua.SetField(-2, "ItemStatusFlags_Edited");
        lua.PushInteger(8);
        lua.SetField(-2, "ItemStatusFlags_ToggledSelection");
        lua.PushInteger(16);
        lua.SetField(-2, "ItemStatusFlags_ToggledOpen");
        lua.PushInteger(32);
        lua.SetField(-2, "ItemStatusFlags_HasDeactivated");
        lua.PushInteger(64);
        lua.SetField(-2, "ItemStatusFlags_Deactivated");
        lua.PushInteger(128);
        lua.SetField(-2, "ItemStatusFlags_HoveredWindow");
        lua.PushInteger(256);
        lua.SetField(-2, "ItemStatusFlags_Visible");
        lua.PushInteger(512);
        lua.SetField(-2, "ItemStatusFlags_HasClipRect");
        lua.PushInteger(1024);
        lua.SetField(-2, "ItemStatusFlags_HasShortcut");
        lua.PushInteger(0);
        lua.SetField(-2, "Key_None");
        lua.PushInteger(512);
        lua.SetField(-2, "Key_Tab");
        lua.PushInteger(513);
        lua.SetField(-2, "Key_LeftArrow");
        lua.PushInteger(514);
        lua.SetField(-2, "Key_RightArrow");
        lua.PushInteger(515);
        lua.SetField(-2, "Key_UpArrow");
        lua.PushInteger(516);
        lua.SetField(-2, "Key_DownArrow");
        lua.PushInteger(517);
        lua.SetField(-2, "Key_PageUp");
        lua.PushInteger(518);
        lua.SetField(-2, "Key_PageDown");
        lua.PushInteger(519);
        lua.SetField(-2, "Key_Home");
        lua.PushInteger(520);
        lua.SetField(-2, "Key_End");
        lua.PushInteger(521);
        lua.SetField(-2, "Key_Insert");
        lua.PushInteger(522);
        lua.SetField(-2, "Key_Delete");
        lua.PushInteger(523);
        lua.SetField(-2, "Key_Backspace");
        lua.PushInteger(524);
        lua.SetField(-2, "Key_Space");
        lua.PushInteger(525);
        lua.SetField(-2, "Key_Enter");
        lua.PushInteger(526);
        lua.SetField(-2, "Key_Escape");
        lua.PushInteger(527);
        lua.SetField(-2, "Key_LeftCtrl");
        lua.PushInteger(528);
        lua.SetField(-2, "Key_LeftShift");
        lua.PushInteger(529);
        lua.SetField(-2, "Key_LeftAlt");
        lua.PushInteger(530);
        lua.SetField(-2, "Key_LeftSuper");
        lua.PushInteger(531);
        lua.SetField(-2, "Key_RightCtrl");
        lua.PushInteger(532);
        lua.SetField(-2, "Key_RightShift");
        lua.PushInteger(533);
        lua.SetField(-2, "Key_RightAlt");
        lua.PushInteger(534);
        lua.SetField(-2, "Key_RightSuper");
        lua.PushInteger(535);
        lua.SetField(-2, "Key_Menu");
        lua.PushInteger(536);
        lua.SetField(-2, "Key_0");
        lua.PushInteger(537);
        lua.SetField(-2, "Key_1");
        lua.PushInteger(538);
        lua.SetField(-2, "Key_2");
        lua.PushInteger(539);
        lua.SetField(-2, "Key_3");
        lua.PushInteger(540);
        lua.SetField(-2, "Key_4");
        lua.PushInteger(541);
        lua.SetField(-2, "Key_5");
        lua.PushInteger(542);
        lua.SetField(-2, "Key_6");
        lua.PushInteger(543);
        lua.SetField(-2, "Key_7");
        lua.PushInteger(544);
        lua.SetField(-2, "Key_8");
        lua.PushInteger(545);
        lua.SetField(-2, "Key_9");
        lua.PushInteger(546);
        lua.SetField(-2, "Key_A");
        lua.PushInteger(547);
        lua.SetField(-2, "Key_B");
        lua.PushInteger(548);
        lua.SetField(-2, "Key_C");
        lua.PushInteger(549);
        lua.SetField(-2, "Key_D");
        lua.PushInteger(550);
        lua.SetField(-2, "Key_E");
        lua.PushInteger(551);
        lua.SetField(-2, "Key_F");
        lua.PushInteger(552);
        lua.SetField(-2, "Key_G");
        lua.PushInteger(553);
        lua.SetField(-2, "Key_H");
        lua.PushInteger(554);
        lua.SetField(-2, "Key_I");
        lua.PushInteger(555);
        lua.SetField(-2, "Key_J");
        lua.PushInteger(556);
        lua.SetField(-2, "Key_K");
        lua.PushInteger(557);
        lua.SetField(-2, "Key_L");
        lua.PushInteger(558);
        lua.SetField(-2, "Key_M");
        lua.PushInteger(559);
        lua.SetField(-2, "Key_N");
        lua.PushInteger(560);
        lua.SetField(-2, "Key_O");
        lua.PushInteger(561);
        lua.SetField(-2, "Key_P");
        lua.PushInteger(562);
        lua.SetField(-2, "Key_Q");
        lua.PushInteger(563);
        lua.SetField(-2, "Key_R");
        lua.PushInteger(564);
        lua.SetField(-2, "Key_S");
        lua.PushInteger(565);
        lua.SetField(-2, "Key_T");
        lua.PushInteger(566);
        lua.SetField(-2, "Key_U");
        lua.PushInteger(567);
        lua.SetField(-2, "Key_V");
        lua.PushInteger(568);
        lua.SetField(-2, "Key_W");
        lua.PushInteger(569);
        lua.SetField(-2, "Key_X");
        lua.PushInteger(570);
        lua.SetField(-2, "Key_Y");
        lua.PushInteger(571);
        lua.SetField(-2, "Key_Z");
        lua.PushInteger(572);
        lua.SetField(-2, "Key_F1");
        lua.PushInteger(573);
        lua.SetField(-2, "Key_F2");
        lua.PushInteger(574);
        lua.SetField(-2, "Key_F3");
        lua.PushInteger(575);
        lua.SetField(-2, "Key_F4");
        lua.PushInteger(576);
        lua.SetField(-2, "Key_F5");
        lua.PushInteger(577);
        lua.SetField(-2, "Key_F6");
        lua.PushInteger(578);
        lua.SetField(-2, "Key_F7");
        lua.PushInteger(579);
        lua.SetField(-2, "Key_F8");
        lua.PushInteger(580);
        lua.SetField(-2, "Key_F9");
        lua.PushInteger(581);
        lua.SetField(-2, "Key_F10");
        lua.PushInteger(582);
        lua.SetField(-2, "Key_F11");
        lua.PushInteger(583);
        lua.SetField(-2, "Key_F12");
        lua.PushInteger(584);
        lua.SetField(-2, "Key_F13");
        lua.PushInteger(585);
        lua.SetField(-2, "Key_F14");
        lua.PushInteger(586);
        lua.SetField(-2, "Key_F15");
        lua.PushInteger(587);
        lua.SetField(-2, "Key_F16");
        lua.PushInteger(588);
        lua.SetField(-2, "Key_F17");
        lua.PushInteger(589);
        lua.SetField(-2, "Key_F18");
        lua.PushInteger(590);
        lua.SetField(-2, "Key_F19");
        lua.PushInteger(591);
        lua.SetField(-2, "Key_F20");
        lua.PushInteger(592);
        lua.SetField(-2, "Key_F21");
        lua.PushInteger(593);
        lua.SetField(-2, "Key_F22");
        lua.PushInteger(594);
        lua.SetField(-2, "Key_F23");
        lua.PushInteger(595);
        lua.SetField(-2, "Key_F24");
        lua.PushInteger(596);
        lua.SetField(-2, "Key_Apostrophe");
        lua.PushInteger(597);
        lua.SetField(-2, "Key_Comma");
        lua.PushInteger(598);
        lua.SetField(-2, "Key_Minus");
        lua.PushInteger(599);
        lua.SetField(-2, "Key_Period");
        lua.PushInteger(600);
        lua.SetField(-2, "Key_Slash");
        lua.PushInteger(601);
        lua.SetField(-2, "Key_Semicolon");
        lua.PushInteger(602);
        lua.SetField(-2, "Key_Equal");
        lua.PushInteger(603);
        lua.SetField(-2, "Key_LeftBracket");
        lua.PushInteger(604);
        lua.SetField(-2, "Key_Backslash");
        lua.PushInteger(605);
        lua.SetField(-2, "Key_RightBracket");
        lua.PushInteger(606);
        lua.SetField(-2, "Key_GraveAccent");
        lua.PushInteger(607);
        lua.SetField(-2, "Key_CapsLock");
        lua.PushInteger(608);
        lua.SetField(-2, "Key_ScrollLock");
        lua.PushInteger(609);
        lua.SetField(-2, "Key_NumLock");
        lua.PushInteger(610);
        lua.SetField(-2, "Key_PrintScreen");
        lua.PushInteger(611);
        lua.SetField(-2, "Key_Pause");
        lua.PushInteger(612);
        lua.SetField(-2, "Key_Keypad0");
        lua.PushInteger(613);
        lua.SetField(-2, "Key_Keypad1");
        lua.PushInteger(614);
        lua.SetField(-2, "Key_Keypad2");
        lua.PushInteger(615);
        lua.SetField(-2, "Key_Keypad3");
        lua.PushInteger(616);
        lua.SetField(-2, "Key_Keypad4");
        lua.PushInteger(617);
        lua.SetField(-2, "Key_Keypad5");
        lua.PushInteger(618);
        lua.SetField(-2, "Key_Keypad6");
        lua.PushInteger(619);
        lua.SetField(-2, "Key_Keypad7");
        lua.PushInteger(620);
        lua.SetField(-2, "Key_Keypad8");
        lua.PushInteger(621);
        lua.SetField(-2, "Key_Keypad9");
        lua.PushInteger(622);
        lua.SetField(-2, "Key_KeypadDecimal");
        lua.PushInteger(623);
        lua.SetField(-2, "Key_KeypadDivide");
        lua.PushInteger(624);
        lua.SetField(-2, "Key_KeypadMultiply");
        lua.PushInteger(625);
        lua.SetField(-2, "Key_KeypadSubtract");
        lua.PushInteger(626);
        lua.SetField(-2, "Key_KeypadAdd");
        lua.PushInteger(627);
        lua.SetField(-2, "Key_KeypadEnter");
        lua.PushInteger(628);
        lua.SetField(-2, "Key_KeypadEqual");
        lua.PushInteger(629);
        lua.SetField(-2, "Key_AppBack");
        lua.PushInteger(630);
        lua.SetField(-2, "Key_AppForward");
        lua.PushInteger(631);
        lua.SetField(-2, "Key_GamepadStart");
        lua.PushInteger(632);
        lua.SetField(-2, "Key_GamepadBack");
        lua.PushInteger(633);
        lua.SetField(-2, "Key_GamepadFaceLeft");
        lua.PushInteger(634);
        lua.SetField(-2, "Key_GamepadFaceRight");
        lua.PushInteger(635);
        lua.SetField(-2, "Key_GamepadFaceUp");
        lua.PushInteger(636);
        lua.SetField(-2, "Key_GamepadFaceDown");
        lua.PushInteger(637);
        lua.SetField(-2, "Key_GamepadDpadLeft");
        lua.PushInteger(638);
        lua.SetField(-2, "Key_GamepadDpadRight");
        lua.PushInteger(639);
        lua.SetField(-2, "Key_GamepadDpadUp");
        lua.PushInteger(640);
        lua.SetField(-2, "Key_GamepadDpadDown");
        lua.PushInteger(641);
        lua.SetField(-2, "Key_GamepadL1");
        lua.PushInteger(642);
        lua.SetField(-2, "Key_GamepadR1");
        lua.PushInteger(643);
        lua.SetField(-2, "Key_GamepadL2");
        lua.PushInteger(644);
        lua.SetField(-2, "Key_GamepadR2");
        lua.PushInteger(645);
        lua.SetField(-2, "Key_GamepadL3");
        lua.PushInteger(646);
        lua.SetField(-2, "Key_GamepadR3");
        lua.PushInteger(647);
        lua.SetField(-2, "Key_GamepadLStickLeft");
        lua.PushInteger(648);
        lua.SetField(-2, "Key_GamepadLStickRight");
        lua.PushInteger(649);
        lua.SetField(-2, "Key_GamepadLStickUp");
        lua.PushInteger(650);
        lua.SetField(-2, "Key_GamepadLStickDown");
        lua.PushInteger(651);
        lua.SetField(-2, "Key_GamepadRStickLeft");
        lua.PushInteger(652);
        lua.SetField(-2, "Key_GamepadRStickRight");
        lua.PushInteger(653);
        lua.SetField(-2, "Key_GamepadRStickUp");
        lua.PushInteger(654);
        lua.SetField(-2, "Key_GamepadRStickDown");
        lua.PushInteger(655);
        lua.SetField(-2, "Key_MouseLeft");
        lua.PushInteger(656);
        lua.SetField(-2, "Key_MouseRight");
        lua.PushInteger(657);
        lua.SetField(-2, "Key_MouseMiddle");
        lua.PushInteger(658);
        lua.SetField(-2, "Key_MouseX1");
        lua.PushInteger(659);
        lua.SetField(-2, "Key_MouseX2");
        lua.PushInteger(660);
        lua.SetField(-2, "Key_MouseWheelX");
        lua.PushInteger(661);
        lua.SetField(-2, "Key_MouseWheelY");
        lua.PushInteger(662);
        lua.SetField(-2, "Key_ReservedForModCtrl");
        lua.PushInteger(663);
        lua.SetField(-2, "Key_ReservedForModShift");
        lua.PushInteger(664);
        lua.SetField(-2, "Key_ReservedForModAlt");
        lua.PushInteger(665);
        lua.SetField(-2, "Key_ReservedForModSuper");
        lua.PushInteger(666);
        lua.SetField(-2, "Key_COUNT");
        lua.PushInteger(0);
        lua.SetField(-2, "Mod_None");
        lua.PushInteger(4096);
        lua.SetField(-2, "Mod_Ctrl");
        lua.PushInteger(8192);
        lua.SetField(-2, "Mod_Shift");
        lua.PushInteger(16384);
        lua.SetField(-2, "Mod_Alt");
        lua.PushInteger(32768);
        lua.SetField(-2, "Mod_Super");
        lua.PushInteger(61440);
        lua.SetField(-2, "Mod_Mask_");
        lua.PushInteger(512);
        lua.SetField(-2, "Key_NamedKey_BEGIN");
        lua.PushInteger(666);
        lua.SetField(-2, "Key_NamedKey_END");
        lua.PushInteger(154);
        lua.SetField(-2, "Key_NamedKey_COUNT");
        lua.PushInteger(154);
        lua.SetField(-2, "Key_KeysData_SIZE");
        lua.PushInteger(512);
        lua.SetField(-2, "Key_KeysData_OFFSET");
        lua.PushInteger(0);
        lua.SetField(-2, "LayoutType_Horizontal");
        lua.PushInteger(1);
        lua.SetField(-2, "LayoutType_Vertical");
        lua.PushInteger(0);
        lua.SetField(-2, "LocKey_VersionStr");
        lua.PushInteger(1);
        lua.SetField(-2, "LocKey_TableSizeOne");
        lua.PushInteger(2);
        lua.SetField(-2, "LocKey_TableSizeAllFit");
        lua.PushInteger(3);
        lua.SetField(-2, "LocKey_TableSizeAllDefault");
        lua.PushInteger(4);
        lua.SetField(-2, "LocKey_TableResetOrder");
        lua.PushInteger(5);
        lua.SetField(-2, "LocKey_WindowingMainMenuBar");
        lua.PushInteger(6);
        lua.SetField(-2, "LocKey_WindowingPopup");
        lua.PushInteger(7);
        lua.SetField(-2, "LocKey_WindowingUntitled");
        lua.PushInteger(8);
        lua.SetField(-2, "LocKey_DockingHideTabBar");
        lua.PushInteger(9);
        lua.SetField(-2, "LocKey_DockingHoldShiftToDock");
        lua.PushInteger(10);
        lua.SetField(-2, "LocKey_DockingDragToUndockOrMoveNode");
        lua.PushInteger(11);
        lua.SetField(-2, "LocKey_COUNT");
        lua.PushInteger(0);
        lua.SetField(-2, "LogType_None");
        lua.PushInteger(1);
        lua.SetField(-2, "LogType_TTY");
        lua.PushInteger(2);
        lua.SetField(-2, "LogType_File");
        lua.PushInteger(3);
        lua.SetField(-2, "LogType_Buffer");
        lua.PushInteger(4);
        lua.SetField(-2, "LogType_Clipboard");
        lua.PushInteger(0);
        lua.SetField(-2, "MouseButton_Left");
        lua.PushInteger(1);
        lua.SetField(-2, "MouseButton_Right");
        lua.PushInteger(2);
        lua.SetField(-2, "MouseButton_Middle");
        lua.PushInteger(5);
        lua.SetField(-2, "MouseButton_COUNT");
        lua.PushInteger(-1);
        lua.SetField(-2, "MouseCursor_None");
        lua.PushInteger(0);
        lua.SetField(-2, "MouseCursor_Arrow");
        lua.PushInteger(1);
        lua.SetField(-2, "MouseCursor_TextInput");
        lua.PushInteger(2);
        lua.SetField(-2, "MouseCursor_ResizeAll");
        lua.PushInteger(3);
        lua.SetField(-2, "MouseCursor_ResizeNS");
        lua.PushInteger(4);
        lua.SetField(-2, "MouseCursor_ResizeEW");
        lua.PushInteger(5);
        lua.SetField(-2, "MouseCursor_ResizeNESW");
        lua.PushInteger(6);
        lua.SetField(-2, "MouseCursor_ResizeNWSE");
        lua.PushInteger(7);
        lua.SetField(-2, "MouseCursor_Hand");
        lua.PushInteger(8);
        lua.SetField(-2, "MouseCursor_NotAllowed");
        lua.PushInteger(9);
        lua.SetField(-2, "MouseCursor_COUNT");
        lua.PushInteger(0);
        lua.SetField(-2, "MouseSource_Mouse");
        lua.PushInteger(1);
        lua.SetField(-2, "MouseSource_TouchScreen");
        lua.PushInteger(2);
        lua.SetField(-2, "MouseSource_Pen");
        lua.PushInteger(3);
        lua.SetField(-2, "MouseSource_COUNT");
        lua.PushInteger(0);
        lua.SetField(-2, "NavHighlightFlags_None");
        lua.PushInteger(2);
        lua.SetField(-2, "NavHighlightFlags_Compact");
        lua.PushInteger(4);
        lua.SetField(-2, "NavHighlightFlags_AlwaysDraw");
        lua.PushInteger(8);
        lua.SetField(-2, "NavHighlightFlags_NoRounding");
        lua.PushInteger(0);
        lua.SetField(-2, "NavLayer_Main");
        lua.PushInteger(1);
        lua.SetField(-2, "NavLayer_Menu");
        lua.PushInteger(2);
        lua.SetField(-2, "NavLayer_COUNT");
        lua.PushInteger(0);
        lua.SetField(-2, "NavMoveFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "NavMoveFlags_LoopX");
        lua.PushInteger(2);
        lua.SetField(-2, "NavMoveFlags_LoopY");
        lua.PushInteger(4);
        lua.SetField(-2, "NavMoveFlags_WrapX");
        lua.PushInteger(8);
        lua.SetField(-2, "NavMoveFlags_WrapY");
        lua.PushInteger(15);
        lua.SetField(-2, "NavMoveFlags_WrapMask_");
        lua.PushInteger(16);
        lua.SetField(-2, "NavMoveFlags_AllowCurrentNavId");
        lua.PushInteger(32);
        lua.SetField(-2, "NavMoveFlags_AlsoScoreVisibleSet");
        lua.PushInteger(64);
        lua.SetField(-2, "NavMoveFlags_ScrollToEdgeY");
        lua.PushInteger(128);
        lua.SetField(-2, "NavMoveFlags_Forwarded");
        lua.PushInteger(256);
        lua.SetField(-2, "NavMoveFlags_DebugNoResult");
        lua.PushInteger(512);
        lua.SetField(-2, "NavMoveFlags_FocusApi");
        lua.PushInteger(1024);
        lua.SetField(-2, "NavMoveFlags_IsTabbing");
        lua.PushInteger(2048);
        lua.SetField(-2, "NavMoveFlags_IsPageMove");
        lua.PushInteger(4096);
        lua.SetField(-2, "NavMoveFlags_Activate");
        lua.PushInteger(8192);
        lua.SetField(-2, "NavMoveFlags_NoSelect");
        lua.PushInteger(16384);
        lua.SetField(-2, "NavMoveFlags_NoSetNavHighlight");
        lua.PushInteger(32768);
        lua.SetField(-2, "NavMoveFlags_NoClearActiveId");
        lua.PushInteger(0);
        lua.SetField(-2, "NextItemDataFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "NextItemDataFlags_HasWidth");
        lua.PushInteger(2);
        lua.SetField(-2, "NextItemDataFlags_HasOpen");
        lua.PushInteger(4);
        lua.SetField(-2, "NextItemDataFlags_HasShortcut");
        lua.PushInteger(8);
        lua.SetField(-2, "NextItemDataFlags_HasRefVal");
        lua.PushInteger(0);
        lua.SetField(-2, "NextWindowDataFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "NextWindowDataFlags_HasPos");
        lua.PushInteger(2);
        lua.SetField(-2, "NextWindowDataFlags_HasSize");
        lua.PushInteger(4);
        lua.SetField(-2, "NextWindowDataFlags_HasContentSize");
        lua.PushInteger(8);
        lua.SetField(-2, "NextWindowDataFlags_HasCollapsed");
        lua.PushInteger(16);
        lua.SetField(-2, "NextWindowDataFlags_HasSizeConstraint");
        lua.PushInteger(32);
        lua.SetField(-2, "NextWindowDataFlags_HasFocus");
        lua.PushInteger(64);
        lua.SetField(-2, "NextWindowDataFlags_HasBgAlpha");
        lua.PushInteger(128);
        lua.SetField(-2, "NextWindowDataFlags_HasScroll");
        lua.PushInteger(256);
        lua.SetField(-2, "NextWindowDataFlags_HasChildFlags");
        lua.PushInteger(512);
        lua.SetField(-2, "NextWindowDataFlags_HasRefreshPolicy");
        lua.PushInteger(1024);
        lua.SetField(-2, "NextWindowDataFlags_HasViewport");
        lua.PushInteger(2048);
        lua.SetField(-2, "NextWindowDataFlags_HasDock");
        lua.PushInteger(4096);
        lua.SetField(-2, "NextWindowDataFlags_HasWindowClass");
        lua.PushInteger(0);
        lua.SetField(-2, "OldColumnFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "OldColumnFlags_NoBorder");
        lua.PushInteger(2);
        lua.SetField(-2, "OldColumnFlags_NoResize");
        lua.PushInteger(4);
        lua.SetField(-2, "OldColumnFlags_NoPreserveWidths");
        lua.PushInteger(8);
        lua.SetField(-2, "OldColumnFlags_NoForceWithinWindow");
        lua.PushInteger(16);
        lua.SetField(-2, "OldColumnFlags_GrowParentContentsSize");
        lua.PushInteger(0);
        lua.SetField(-2, "PlotType_Lines");
        lua.PushInteger(1);
        lua.SetField(-2, "PlotType_Histogram");
        lua.PushInteger(0);
        lua.SetField(-2, "PopupFlags_None");
        lua.PushInteger(0);
        lua.SetField(-2, "PopupFlags_MouseButtonLeft");
        lua.PushInteger(1);
        lua.SetField(-2, "PopupFlags_MouseButtonRight");
        lua.PushInteger(2);
        lua.SetField(-2, "PopupFlags_MouseButtonMiddle");
        lua.PushInteger(31);
        lua.SetField(-2, "PopupFlags_MouseButtonMask_");
        lua.PushInteger(1);
        lua.SetField(-2, "PopupFlags_MouseButtonDefault_");
        lua.PushInteger(32);
        lua.SetField(-2, "PopupFlags_NoReopen");
        lua.PushInteger(128);
        lua.SetField(-2, "PopupFlags_NoOpenOverExistingPopup");
        lua.PushInteger(256);
        lua.SetField(-2, "PopupFlags_NoOpenOverItems");
        lua.PushInteger(1024);
        lua.SetField(-2, "PopupFlags_AnyPopupId");
        lua.PushInteger(2048);
        lua.SetField(-2, "PopupFlags_AnyPopupLevel");
        lua.PushInteger(3072);
        lua.SetField(-2, "PopupFlags_AnyPopup");
        lua.PushInteger(0);
        lua.SetField(-2, "PopupPositionPolicy_Default");
        lua.PushInteger(1);
        lua.SetField(-2, "PopupPositionPolicy_ComboBox");
        lua.PushInteger(2);
        lua.SetField(-2, "PopupPositionPolicy_Tooltip");
        lua.PushInteger(0);
        lua.SetField(-2, "ScrollFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "ScrollFlags_KeepVisibleEdgeX");
        lua.PushInteger(2);
        lua.SetField(-2, "ScrollFlags_KeepVisibleEdgeY");
        lua.PushInteger(4);
        lua.SetField(-2, "ScrollFlags_KeepVisibleCenterX");
        lua.PushInteger(8);
        lua.SetField(-2, "ScrollFlags_KeepVisibleCenterY");
        lua.PushInteger(16);
        lua.SetField(-2, "ScrollFlags_AlwaysCenterX");
        lua.PushInteger(32);
        lua.SetField(-2, "ScrollFlags_AlwaysCenterY");
        lua.PushInteger(64);
        lua.SetField(-2, "ScrollFlags_NoScrollParent");
        lua.PushInteger(21);
        lua.SetField(-2, "ScrollFlags_MaskX_");
        lua.PushInteger(42);
        lua.SetField(-2, "ScrollFlags_MaskY_");
        lua.PushInteger(1048576);
        lua.SetField(-2, "SelectableFlags_NoHoldingActiveID");
        lua.PushInteger(2097152);
        lua.SetField(-2, "SelectableFlags_SelectOnNav");
        lua.PushInteger(4194304);
        lua.SetField(-2, "SelectableFlags_SelectOnClick");
        lua.PushInteger(8388608);
        lua.SetField(-2, "SelectableFlags_SelectOnRelease");
        lua.PushInteger(16777216);
        lua.SetField(-2, "SelectableFlags_SpanAvailWidth");
        lua.PushInteger(33554432);
        lua.SetField(-2, "SelectableFlags_SetNavIdOnHover");
        lua.PushInteger(67108864);
        lua.SetField(-2, "SelectableFlags_NoPadWithHalfSpacing");
        lua.PushInteger(134217728);
        lua.SetField(-2, "SelectableFlags_NoSetKeyOwner");
        lua.PushInteger(0);
        lua.SetField(-2, "SelectableFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "SelectableFlags_DontClosePopups");
        lua.PushInteger(2);
        lua.SetField(-2, "SelectableFlags_SpanAllColumns");
        lua.PushInteger(4);
        lua.SetField(-2, "SelectableFlags_AllowDoubleClick");
        lua.PushInteger(8);
        lua.SetField(-2, "SelectableFlags_Disabled");
        lua.PushInteger(16);
        lua.SetField(-2, "SelectableFlags_AllowOverlap");
        lua.PushInteger(0);
        lua.SetField(-2, "SeparatorFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "SeparatorFlags_Horizontal");
        lua.PushInteger(2);
        lua.SetField(-2, "SeparatorFlags_Vertical");
        lua.PushInteger(4);
        lua.SetField(-2, "SeparatorFlags_SpanAllColumns");
        lua.PushInteger(1048576);
        lua.SetField(-2, "SliderFlags_Vertical");
        lua.PushInteger(2097152);
        lua.SetField(-2, "SliderFlags_ReadOnly");
        lua.PushInteger(0);
        lua.SetField(-2, "SliderFlags_None");
        lua.PushInteger(16);
        lua.SetField(-2, "SliderFlags_AlwaysClamp");
        lua.PushInteger(32);
        lua.SetField(-2, "SliderFlags_Logarithmic");
        lua.PushInteger(64);
        lua.SetField(-2, "SliderFlags_NoRoundToFormat");
        lua.PushInteger(128);
        lua.SetField(-2, "SliderFlags_NoInput");
        lua.PushInteger(256);
        lua.SetField(-2, "SliderFlags_WrapAround");
        lua.PushInteger(1879048207);
        lua.SetField(-2, "SliderFlags_InvalidMask_");
        lua.PushInteger(0);
        lua.SetField(-2, "SortDirection_None");
        lua.PushInteger(1);
        lua.SetField(-2, "SortDirection_Ascending");
        lua.PushInteger(2);
        lua.SetField(-2, "SortDirection_Descending");
        lua.PushInteger(0);
        lua.SetField(-2, "StyleVar_Alpha");
        lua.PushInteger(1);
        lua.SetField(-2, "StyleVar_DisabledAlpha");
        lua.PushInteger(2);
        lua.SetField(-2, "StyleVar_WindowPadding");
        lua.PushInteger(3);
        lua.SetField(-2, "StyleVar_WindowRounding");
        lua.PushInteger(4);
        lua.SetField(-2, "StyleVar_WindowBorderSize");
        lua.PushInteger(5);
        lua.SetField(-2, "StyleVar_WindowMinSize");
        lua.PushInteger(6);
        lua.SetField(-2, "StyleVar_WindowTitleAlign");
        lua.PushInteger(7);
        lua.SetField(-2, "StyleVar_ChildRounding");
        lua.PushInteger(8);
        lua.SetField(-2, "StyleVar_ChildBorderSize");
        lua.PushInteger(9);
        lua.SetField(-2, "StyleVar_PopupRounding");
        lua.PushInteger(10);
        lua.SetField(-2, "StyleVar_PopupBorderSize");
        lua.PushInteger(11);
        lua.SetField(-2, "StyleVar_FramePadding");
        lua.PushInteger(12);
        lua.SetField(-2, "StyleVar_FrameRounding");
        lua.PushInteger(13);
        lua.SetField(-2, "StyleVar_FrameBorderSize");
        lua.PushInteger(14);
        lua.SetField(-2, "StyleVar_ItemSpacing");
        lua.PushInteger(15);
        lua.SetField(-2, "StyleVar_ItemInnerSpacing");
        lua.PushInteger(16);
        lua.SetField(-2, "StyleVar_IndentSpacing");
        lua.PushInteger(17);
        lua.SetField(-2, "StyleVar_CellPadding");
        lua.PushInteger(18);
        lua.SetField(-2, "StyleVar_ScrollbarSize");
        lua.PushInteger(19);
        lua.SetField(-2, "StyleVar_ScrollbarRounding");
        lua.PushInteger(20);
        lua.SetField(-2, "StyleVar_GrabMinSize");
        lua.PushInteger(21);
        lua.SetField(-2, "StyleVar_GrabRounding");
        lua.PushInteger(22);
        lua.SetField(-2, "StyleVar_TabRounding");
        lua.PushInteger(23);
        lua.SetField(-2, "StyleVar_TabBorderSize");
        lua.PushInteger(24);
        lua.SetField(-2, "StyleVar_TabBarBorderSize");
        lua.PushInteger(25);
        lua.SetField(-2, "StyleVar_TableAngledHeadersAngle");
        lua.PushInteger(26);
        lua.SetField(-2, "StyleVar_TableAngledHeadersTextAlign");
        lua.PushInteger(27);
        lua.SetField(-2, "StyleVar_ButtonTextAlign");
        lua.PushInteger(28);
        lua.SetField(-2, "StyleVar_SelectableTextAlign");
        lua.PushInteger(29);
        lua.SetField(-2, "StyleVar_SeparatorTextBorderSize");
        lua.PushInteger(30);
        lua.SetField(-2, "StyleVar_SeparatorTextAlign");
        lua.PushInteger(31);
        lua.SetField(-2, "StyleVar_SeparatorTextPadding");
        lua.PushInteger(32);
        lua.SetField(-2, "StyleVar_DockingSeparatorSize");
        lua.PushInteger(33);
        lua.SetField(-2, "StyleVar_COUNT");
        lua.PushInteger(1048576);
        lua.SetField(-2, "TabBarFlags_DockNode");
        lua.PushInteger(2097152);
        lua.SetField(-2, "TabBarFlags_IsFocused");
        lua.PushInteger(4194304);
        lua.SetField(-2, "TabBarFlags_SaveSettings");
        lua.PushInteger(0);
        lua.SetField(-2, "TabBarFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "TabBarFlags_Reorderable");
        lua.PushInteger(2);
        lua.SetField(-2, "TabBarFlags_AutoSelectNewTabs");
        lua.PushInteger(4);
        lua.SetField(-2, "TabBarFlags_TabListPopupButton");
        lua.PushInteger(8);
        lua.SetField(-2, "TabBarFlags_NoCloseWithMiddleMouseButton");
        lua.PushInteger(16);
        lua.SetField(-2, "TabBarFlags_NoTabListScrollingButtons");
        lua.PushInteger(32);
        lua.SetField(-2, "TabBarFlags_NoTooltip");
        lua.PushInteger(64);
        lua.SetField(-2, "TabBarFlags_DrawSelectedOverline");
        lua.PushInteger(128);
        lua.SetField(-2, "TabBarFlags_FittingPolicyResizeDown");
        lua.PushInteger(256);
        lua.SetField(-2, "TabBarFlags_FittingPolicyScroll");
        lua.PushInteger(384);
        lua.SetField(-2, "TabBarFlags_FittingPolicyMask_");
        lua.PushInteger(128);
        lua.SetField(-2, "TabBarFlags_FittingPolicyDefault_");
        lua.PushInteger(192);
        lua.SetField(-2, "TabItemFlags_SectionMask_");
        lua.PushInteger(1048576);
        lua.SetField(-2, "TabItemFlags_NoCloseButton");
        lua.PushInteger(2097152);
        lua.SetField(-2, "TabItemFlags_Button");
        lua.PushInteger(4194304);
        lua.SetField(-2, "TabItemFlags_Unsorted");
        lua.PushInteger(0);
        lua.SetField(-2, "TabItemFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "TabItemFlags_UnsavedDocument");
        lua.PushInteger(2);
        lua.SetField(-2, "TabItemFlags_SetSelected");
        lua.PushInteger(4);
        lua.SetField(-2, "TabItemFlags_NoCloseWithMiddleMouseButton");
        lua.PushInteger(8);
        lua.SetField(-2, "TabItemFlags_NoPushId");
        lua.PushInteger(16);
        lua.SetField(-2, "TabItemFlags_NoTooltip");
        lua.PushInteger(32);
        lua.SetField(-2, "TabItemFlags_NoReorder");
        lua.PushInteger(64);
        lua.SetField(-2, "TabItemFlags_Leading");
        lua.PushInteger(128);
        lua.SetField(-2, "TabItemFlags_Trailing");
        lua.PushInteger(256);
        lua.SetField(-2, "TabItemFlags_NoAssumedClosure");
        lua.PushInteger(0);
        lua.SetField(-2, "TableBgTarget_None");
        lua.PushInteger(1);
        lua.SetField(-2, "TableBgTarget_RowBg0");
        lua.PushInteger(2);
        lua.SetField(-2, "TableBgTarget_RowBg1");
        lua.PushInteger(3);
        lua.SetField(-2, "TableBgTarget_CellBg");
        lua.PushInteger(0);
        lua.SetField(-2, "TableColumnFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "TableColumnFlags_Disabled");
        lua.PushInteger(2);
        lua.SetField(-2, "TableColumnFlags_DefaultHide");
        lua.PushInteger(4);
        lua.SetField(-2, "TableColumnFlags_DefaultSort");
        lua.PushInteger(8);
        lua.SetField(-2, "TableColumnFlags_WidthStretch");
        lua.PushInteger(16);
        lua.SetField(-2, "TableColumnFlags_WidthFixed");
        lua.PushInteger(32);
        lua.SetField(-2, "TableColumnFlags_NoResize");
        lua.PushInteger(64);
        lua.SetField(-2, "TableColumnFlags_NoReorder");
        lua.PushInteger(128);
        lua.SetField(-2, "TableColumnFlags_NoHide");
        lua.PushInteger(256);
        lua.SetField(-2, "TableColumnFlags_NoClip");
        lua.PushInteger(512);
        lua.SetField(-2, "TableColumnFlags_NoSort");
        lua.PushInteger(1024);
        lua.SetField(-2, "TableColumnFlags_NoSortAscending");
        lua.PushInteger(2048);
        lua.SetField(-2, "TableColumnFlags_NoSortDescending");
        lua.PushInteger(4096);
        lua.SetField(-2, "TableColumnFlags_NoHeaderLabel");
        lua.PushInteger(8192);
        lua.SetField(-2, "TableColumnFlags_NoHeaderWidth");
        lua.PushInteger(16384);
        lua.SetField(-2, "TableColumnFlags_PreferSortAscending");
        lua.PushInteger(32768);
        lua.SetField(-2, "TableColumnFlags_PreferSortDescending");
        lua.PushInteger(65536);
        lua.SetField(-2, "TableColumnFlags_IndentEnable");
        lua.PushInteger(131072);
        lua.SetField(-2, "TableColumnFlags_IndentDisable");
        lua.PushInteger(262144);
        lua.SetField(-2, "TableColumnFlags_AngledHeader");
        lua.PushInteger(16777216);
        lua.SetField(-2, "TableColumnFlags_IsEnabled");
        lua.PushInteger(33554432);
        lua.SetField(-2, "TableColumnFlags_IsVisible");
        lua.PushInteger(67108864);
        lua.SetField(-2, "TableColumnFlags_IsSorted");
        lua.PushInteger(134217728);
        lua.SetField(-2, "TableColumnFlags_IsHovered");
        lua.PushInteger(24);
        lua.SetField(-2, "TableColumnFlags_WidthMask_");
        lua.PushInteger(196608);
        lua.SetField(-2, "TableColumnFlags_IndentMask_");
        lua.PushInteger(251658240);
        lua.SetField(-2, "TableColumnFlags_StatusMask_");
        lua.PushInteger(1073741824);
        lua.SetField(-2, "TableColumnFlags_NoDirectResize_");
        lua.PushInteger(0);
        lua.SetField(-2, "TableFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "TableFlags_Resizable");
        lua.PushInteger(2);
        lua.SetField(-2, "TableFlags_Reorderable");
        lua.PushInteger(4);
        lua.SetField(-2, "TableFlags_Hideable");
        lua.PushInteger(8);
        lua.SetField(-2, "TableFlags_Sortable");
        lua.PushInteger(16);
        lua.SetField(-2, "TableFlags_NoSavedSettings");
        lua.PushInteger(32);
        lua.SetField(-2, "TableFlags_ContextMenuInBody");
        lua.PushInteger(64);
        lua.SetField(-2, "TableFlags_RowBg");
        lua.PushInteger(128);
        lua.SetField(-2, "TableFlags_BordersInnerH");
        lua.PushInteger(256);
        lua.SetField(-2, "TableFlags_BordersOuterH");
        lua.PushInteger(512);
        lua.SetField(-2, "TableFlags_BordersInnerV");
        lua.PushInteger(1024);
        lua.SetField(-2, "TableFlags_BordersOuterV");
        lua.PushInteger(384);
        lua.SetField(-2, "TableFlags_BordersH");
        lua.PushInteger(1536);
        lua.SetField(-2, "TableFlags_BordersV");
        lua.PushInteger(640);
        lua.SetField(-2, "TableFlags_BordersInner");
        lua.PushInteger(1280);
        lua.SetField(-2, "TableFlags_BordersOuter");
        lua.PushInteger(1920);
        lua.SetField(-2, "TableFlags_Borders");
        lua.PushInteger(2048);
        lua.SetField(-2, "TableFlags_NoBordersInBody");
        lua.PushInteger(4096);
        lua.SetField(-2, "TableFlags_NoBordersInBodyUntilResize");
        lua.PushInteger(8192);
        lua.SetField(-2, "TableFlags_SizingFixedFit");
        lua.PushInteger(16384);
        lua.SetField(-2, "TableFlags_SizingFixedSame");
        lua.PushInteger(24576);
        lua.SetField(-2, "TableFlags_SizingStretchProp");
        lua.PushInteger(32768);
        lua.SetField(-2, "TableFlags_SizingStretchSame");
        lua.PushInteger(65536);
        lua.SetField(-2, "TableFlags_NoHostExtendX");
        lua.PushInteger(131072);
        lua.SetField(-2, "TableFlags_NoHostExtendY");
        lua.PushInteger(262144);
        lua.SetField(-2, "TableFlags_NoKeepColumnsVisible");
        lua.PushInteger(524288);
        lua.SetField(-2, "TableFlags_PreciseWidths");
        lua.PushInteger(1048576);
        lua.SetField(-2, "TableFlags_NoClip");
        lua.PushInteger(2097152);
        lua.SetField(-2, "TableFlags_PadOuterX");
        lua.PushInteger(4194304);
        lua.SetField(-2, "TableFlags_NoPadOuterX");
        lua.PushInteger(8388608);
        lua.SetField(-2, "TableFlags_NoPadInnerX");
        lua.PushInteger(16777216);
        lua.SetField(-2, "TableFlags_ScrollX");
        lua.PushInteger(33554432);
        lua.SetField(-2, "TableFlags_ScrollY");
        lua.PushInteger(67108864);
        lua.SetField(-2, "TableFlags_SortMulti");
        lua.PushInteger(134217728);
        lua.SetField(-2, "TableFlags_SortTristate");
        lua.PushInteger(268435456);
        lua.SetField(-2, "TableFlags_HighlightHoveredColumn");
        lua.PushInteger(57344);
        lua.SetField(-2, "TableFlags_SizingMask_");
        lua.PushInteger(0);
        lua.SetField(-2, "TableRowFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "TableRowFlags_Headers");
        lua.PushInteger(0);
        lua.SetField(-2, "TextFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "TextFlags_NoWidthForLargeClippedText");
        lua.PushInteger(0);
        lua.SetField(-2, "TooltipFlags_None");
        lua.PushInteger(2);
        lua.SetField(-2, "TooltipFlags_OverridePrevious");
        lua.PushInteger(1048576);
        lua.SetField(-2, "TreeNodeFlags_ClipLabelForTrailingButton");
        lua.PushInteger(2097152);
        lua.SetField(-2, "TreeNodeFlags_UpsideDownArrow");
        lua.PushInteger(0);
        lua.SetField(-2, "TreeNodeFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "TreeNodeFlags_Selected");
        lua.PushInteger(2);
        lua.SetField(-2, "TreeNodeFlags_Framed");
        lua.PushInteger(4);
        lua.SetField(-2, "TreeNodeFlags_AllowOverlap");
        lua.PushInteger(8);
        lua.SetField(-2, "TreeNodeFlags_NoTreePushOnOpen");
        lua.PushInteger(16);
        lua.SetField(-2, "TreeNodeFlags_NoAutoOpenOnLog");
        lua.PushInteger(32);
        lua.SetField(-2, "TreeNodeFlags_DefaultOpen");
        lua.PushInteger(64);
        lua.SetField(-2, "TreeNodeFlags_OpenOnDoubleClick");
        lua.PushInteger(128);
        lua.SetField(-2, "TreeNodeFlags_OpenOnArrow");
        lua.PushInteger(256);
        lua.SetField(-2, "TreeNodeFlags_Leaf");
        lua.PushInteger(512);
        lua.SetField(-2, "TreeNodeFlags_Bullet");
        lua.PushInteger(1024);
        lua.SetField(-2, "TreeNodeFlags_FramePadding");
        lua.PushInteger(2048);
        lua.SetField(-2, "TreeNodeFlags_SpanAvailWidth");
        lua.PushInteger(4096);
        lua.SetField(-2, "TreeNodeFlags_SpanFullWidth");
        lua.PushInteger(8192);
        lua.SetField(-2, "TreeNodeFlags_SpanTextWidth");
        lua.PushInteger(16384);
        lua.SetField(-2, "TreeNodeFlags_SpanAllColumns");
        lua.PushInteger(32768);
        lua.SetField(-2, "TreeNodeFlags_NavLeftJumpsBackHere");
        lua.PushInteger(26);
        lua.SetField(-2, "TreeNodeFlags_CollapsingHeader");
        lua.PushInteger(0);
        lua.SetField(-2, "TypingSelectFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "TypingSelectFlags_AllowBackspace");
        lua.PushInteger(2);
        lua.SetField(-2, "TypingSelectFlags_AllowSingleCharMode");
        lua.PushInteger(0);
        lua.SetField(-2, "ViewportFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "ViewportFlags_IsPlatformWindow");
        lua.PushInteger(2);
        lua.SetField(-2, "ViewportFlags_IsPlatformMonitor");
        lua.PushInteger(4);
        lua.SetField(-2, "ViewportFlags_OwnedByApp");
        lua.PushInteger(8);
        lua.SetField(-2, "ViewportFlags_NoDecoration");
        lua.PushInteger(16);
        lua.SetField(-2, "ViewportFlags_NoTaskBarIcon");
        lua.PushInteger(32);
        lua.SetField(-2, "ViewportFlags_NoFocusOnAppearing");
        lua.PushInteger(64);
        lua.SetField(-2, "ViewportFlags_NoFocusOnClick");
        lua.PushInteger(128);
        lua.SetField(-2, "ViewportFlags_NoInputs");
        lua.PushInteger(256);
        lua.SetField(-2, "ViewportFlags_NoRendererClear");
        lua.PushInteger(512);
        lua.SetField(-2, "ViewportFlags_NoAutoMerge");
        lua.PushInteger(1024);
        lua.SetField(-2, "ViewportFlags_TopMost");
        lua.PushInteger(2048);
        lua.SetField(-2, "ViewportFlags_CanHostOtherWindows");
        lua.PushInteger(4096);
        lua.SetField(-2, "ViewportFlags_IsMinimized");
        lua.PushInteger(8192);
        lua.SetField(-2, "ViewportFlags_IsFocused");
        lua.PushInteger(0);
        lua.SetField(-2, "WindowDockStyleCol_Text");
        lua.PushInteger(1);
        lua.SetField(-2, "WindowDockStyleCol_TabHovered");
        lua.PushInteger(2);
        lua.SetField(-2, "WindowDockStyleCol_TabFocused");
        lua.PushInteger(3);
        lua.SetField(-2, "WindowDockStyleCol_TabSelected");
        lua.PushInteger(4);
        lua.SetField(-2, "WindowDockStyleCol_TabSelectedOverline");
        lua.PushInteger(5);
        lua.SetField(-2, "WindowDockStyleCol_TabDimmed");
        lua.PushInteger(6);
        lua.SetField(-2, "WindowDockStyleCol_TabDimmedSelected");
        lua.PushInteger(7);
        lua.SetField(-2, "WindowDockStyleCol_TabDimmedSelectedOverline");
        lua.PushInteger(8);
        lua.SetField(-2, "WindowDockStyleCol_COUNT");
        lua.PushInteger(0);
        lua.SetField(-2, "WindowFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "WindowFlags_NoTitleBar");
        lua.PushInteger(2);
        lua.SetField(-2, "WindowFlags_NoResize");
        lua.PushInteger(4);
        lua.SetField(-2, "WindowFlags_NoMove");
        lua.PushInteger(8);
        lua.SetField(-2, "WindowFlags_NoScrollbar");
        lua.PushInteger(16);
        lua.SetField(-2, "WindowFlags_NoScrollWithMouse");
        lua.PushInteger(32);
        lua.SetField(-2, "WindowFlags_NoCollapse");
        lua.PushInteger(64);
        lua.SetField(-2, "WindowFlags_AlwaysAutoResize");
        lua.PushInteger(128);
        lua.SetField(-2, "WindowFlags_NoBackground");
        lua.PushInteger(256);
        lua.SetField(-2, "WindowFlags_NoSavedSettings");
        lua.PushInteger(512);
        lua.SetField(-2, "WindowFlags_NoMouseInputs");
        lua.PushInteger(1024);
        lua.SetField(-2, "WindowFlags_MenuBar");
        lua.PushInteger(2048);
        lua.SetField(-2, "WindowFlags_HorizontalScrollbar");
        lua.PushInteger(4096);
        lua.SetField(-2, "WindowFlags_NoFocusOnAppearing");
        lua.PushInteger(8192);
        lua.SetField(-2, "WindowFlags_NoBringToFrontOnFocus");
        lua.PushInteger(16384);
        lua.SetField(-2, "WindowFlags_AlwaysVerticalScrollbar");
        lua.PushInteger(32768);
        lua.SetField(-2, "WindowFlags_AlwaysHorizontalScrollbar");
        lua.PushInteger(65536);
        lua.SetField(-2, "WindowFlags_NoNavInputs");
        lua.PushInteger(131072);
        lua.SetField(-2, "WindowFlags_NoNavFocus");
        lua.PushInteger(262144);
        lua.SetField(-2, "WindowFlags_UnsavedDocument");
        lua.PushInteger(524288);
        lua.SetField(-2, "WindowFlags_NoDocking");
        lua.PushInteger(196608);
        lua.SetField(-2, "WindowFlags_NoNav");
        lua.PushInteger(43);
        lua.SetField(-2, "WindowFlags_NoDecoration");
        lua.PushInteger(197120);
        lua.SetField(-2, "WindowFlags_NoInputs");
        lua.PushInteger(16777216);
        lua.SetField(-2, "WindowFlags_ChildWindow");
        lua.PushInteger(33554432);
        lua.SetField(-2, "WindowFlags_Tooltip");
        lua.PushInteger(67108864);
        lua.SetField(-2, "WindowFlags_Popup");
        lua.PushInteger(134217728);
        lua.SetField(-2, "WindowFlags_Modal");
        lua.PushInteger(268435456);
        lua.SetField(-2, "WindowFlags_ChildMenu");
        lua.PushInteger(536870912);
        lua.SetField(-2, "WindowFlags_DockNodeHost");
        lua.PushInteger(0);
        lua.SetField(-2, "WindowRefreshFlags_None");
        lua.PushInteger(1);
        lua.SetField(-2, "WindowRefreshFlags_TryToAvoidRefresh");
        lua.PushInteger(2);
        lua.SetField(-2, "WindowRefreshFlags_RefreshOnHover");
        lua.PushInteger(4);
        lua.SetField(-2, "WindowRefreshFlags_RefreshOnFocus");
    }
}
