using System.Text;
using System.Numerics;
namespace ImGuiNET;

class ImGuiExt
{
    /// <summary>
    /// stupid ImGuiNET, i have to make my own native function wrapper because
    /// they don't support flags without p_open
    /// </summary>
    /// <param name="label">The display name and ImGui ID for the tab item</param>
    /// <param name="flags">The flags for the tab item</param>
    /// <returns>True if the tab item is selected</returns>
    public unsafe static bool BeginTabItem(string label, ImGuiTabItemFlags flags)
    {
        int byteCount = Encoding.UTF8.GetByteCount(label);
        byte* charArr = stackalloc byte[byteCount + 1];

        int count;
        fixed (char* utf16ptr = label)
        {
            count = Encoding.UTF8.GetBytes(utf16ptr, label.Length, charArr, byteCount);
        }
        charArr[count] = 0;

        return ImGuiNative.igBeginTabItem(charArr, null, flags) != 0;
    }

    /// <summary>
    /// stupid ImGuiNET, i have to make my own native function wrapper because
    /// they don't support flags without p_open
    /// </summary>
    /// <param name="label">The display name and ImGui ID for the tab item</param>
    /// <param name="flags">The flags for the tab item</param>
    /// <returns>True if the tab item is selected</returns>
    public unsafe static bool BeginPopupModal(string name, ImGuiWindowFlags flags)
    {
        int byteCount = Encoding.UTF8.GetByteCount(name);
        byte* charArr = stackalloc byte[byteCount + 1];

        int count;
        fixed (char* utf16ptr = name)
        {
            count = Encoding.UTF8.GetBytes(utf16ptr, name.Length, charArr, byteCount);
        }
        charArr[count] = 0;

        return ImGuiNative.igBeginPopupModal(charArr, null, flags) != 0;
    }

    public static void CenterNextWindow(ImGuiCond cond)
    {
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.GetCenter(), cond, new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// Ensure that the popup is open
    /// </summary>
    /// <param name="id">The ID of the popup to open</param>
    /// <returns>True if the popup was not open, false if not.</returns>
    public static bool EnsurePopupIsOpen(string id)
    {
        if (!ImGui.IsPopupOpen(id))
        {
            ImGui.OpenPopup(id);
            return true;
        }

        return false;
    }
}