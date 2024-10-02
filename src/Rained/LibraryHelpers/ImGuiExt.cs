using System.Text;
using System.Numerics;
using Glib;
using Raylib_cs;
using System.Runtime.InteropServices;
using Rained;
namespace ImGuiNET;

static class ImGuiExt
{
    private static nint iniFilenameAlloc = 0;

    public static void SetIniFilename(string iniFilename)
    {
        if (iniFilenameAlloc != 0)
            Marshal.FreeHGlobal(iniFilenameAlloc);

        byte[] nameBytes = Encoding.ASCII.GetBytes(iniFilename + "\0");
        iniFilenameAlloc = Marshal.AllocHGlobal(nameBytes.Length);
        Marshal.Copy(nameBytes, 0, iniFilenameAlloc, nameBytes.Count());
        
        unsafe
        {
            ImGui.GetIO().NativePtr->IniFilename = (byte*) iniFilenameAlloc;
        }
    }

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

    public static unsafe void FilterDraw(this ref ImGuiTextFilter filter, string label = "Filter (inc,-exc)", float width = 0f)
    {
        fixed (ImGuiTextFilter* p = &filter)
        {
            ImGuiTextFilterPtr ptr = new(p);
            ptr.Draw(label, width);
        }
    }

    public static unsafe bool FilterPassFilter(this ref ImGuiTextFilter filter, string text)
    {
        fixed (ImGuiTextFilter* p = &filter)
        {
            ImGuiTextFilterPtr ptr = new(p);
            return ptr.PassFilter(text);
        }
        
    }

    public static unsafe void SaveStyleRef(ImGuiStylePtr src, ref ImGuiStyle dst)
    {
        fixed (ImGuiStyle* dstPtr = &dst)
        {
            Buffer.MemoryCopy(src.NativePtr, dstPtr, sizeof(ImGuiStyle), sizeof(ImGuiStyle));
        }
    }

    public static unsafe void LoadStyleRef(ImGuiStyle src, ImGuiStylePtr dst)
    {
        Buffer.MemoryCopy(&src, dst, sizeof(ImGuiStyle), sizeof(ImGuiStyle)); 
    }

    public static unsafe Vector4 GetColor(this ref ImGuiStyle self, int index)
    {
        if (index < 0 || index >= (int)ImGuiCol.COUNT)
            throw new ArgumentOutOfRangeException(nameof(index));
        
        fixed (Vector4* arrStart = &self.Colors_0)
            return arrStart[index]; 
    }

    public static unsafe void SetColor(this ref ImGuiStyle self, int index, Vector4 color)
    {
        if (index < 0 || index >= (int)ImGuiCol.COUNT)
            throw new ArgumentOutOfRangeException(nameof(index));

        fixed (Vector4* arrStart = &self.Colors_0)
            arrStart[index] = color; 
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

    private static nint TextureID(Texture texture) => Boot.ImGuiController!.UseTexture(texture);

    public static void Image(Texture texture, Glib.Color color)
    {
        ImGui.Image(TextureID(texture), new Vector2(texture.Width, texture.Height), Vector2.Zero, Vector2.One, (Vector4) color);
    }

    public static void Image(Texture texture)
    {
        Image(texture, Glib.Color.White);
    }

    public static void Image(Texture2D texture, Raylib_cs.Color color)
    {
        Image(texture.ID!, Raylib.ToGlibColor(color));
    }

    public static void Image(Texture2D texture)
    {
        Image(texture.ID!);
    }

    public static void ImageSize(Texture texture, float width, float height)
    {
        ImGui.Image(TextureID(texture), new Vector2(width, height), Vector2.Zero, Vector2.One);
    }

    public static void ImageRect(Texture texture, float width, float height, Glib.Rectangle srcRec, Glib.Color color)
    {
        var texSize = new Vector2(texture.Width, texture.Height);
        ImGui.Image(
            TextureID(texture),
            new Vector2(width, height),
            srcRec.Position / texSize,
            (srcRec.Position + srcRec.Size) / texSize,
            (Vector4) color
        );
    }

    public static void ImageRect(Texture texture, float width, float height, Glib.Rectangle srcRec)
    {
        ImageRect(texture, width, height, srcRec, Glib.Color.White);
    }

    public static void ImageRect(Texture2D texture, float width, float height, Raylib_cs.Rectangle srcRec)
    {
        ImageRect(texture.ID!, width, height, new Glib.Rectangle(srcRec.Position, srcRec.Size));
    }

    public static void ImageRect(Texture2D texture, float width, float height, Raylib_cs.Rectangle srcRec, Raylib_cs.Color color)
    {
        ImageRect(texture.ID!, width, height, new Glib.Rectangle(srcRec.Position, srcRec.Size), Raylib.ToGlibColor(color));
    }

    public static void ImageRect(Texture2D texture, float width, float height, Raylib_cs.Rectangle srcRec, Vector4 color)
    {
        ImageRect(texture.ID!, width, height, new Glib.Rectangle(srcRec.Position, srcRec.Size), new Glib.Color(color.X, color.Y, color.Z, color.W));
    }

    public static void ImageRenderTexture(Framebuffer framebuffer, int slot = 0)
    {
        var tex = framebuffer.GetTexture(slot);

        // determine if vertical flip is necessary
        if (Rained.RainEd.RenderContext!.OriginBottomLeft)
            ImGui.Image(TextureID(tex), new Vector2(tex.Width, tex.Height), new Vector2(0f, 1f), new Vector2(1f, 0f));
        else
            ImGui.Image(TextureID(tex), new Vector2(tex.Width, tex.Height));
    }

    public static bool ImageButtonRect(string id, Texture tex, float width, float height, Glib.Rectangle srcRec, Glib.Color color)
    {
        var texSize = new Vector2(tex.Width, tex.Height);
        return ImGui.ImageButton(
            str_id: id,
            user_texture_id: TextureID(tex),
            image_size: new Vector2(width, height),
            uv0: srcRec.Position / texSize,
            uv1: (srcRec.Position + srcRec.Size) / texSize,
            bg_col: (Vector4)Glib.Color.Transparent, 
            tint_col: (Vector4)color
        );
    }

    public static bool ImageButtonRect(string id, Texture tex, float width, float height, Glib.Rectangle srcRec)
    {
        return ImageButtonRect(id, tex, width, height, srcRec, Glib.Color.White);
    }

    public static void ImageSize(Texture2D texture, float width, float height)
    {
        ImageSize(texture.ID!, width, height);
    }

    public static void ImageRenderTexture(RenderTexture2D framebuffer)
    {
        ImageRenderTexture(framebuffer.ID!);
    }

    public static bool ImageButtonRect(string id, Texture2D tex, float width, float height, Raylib_cs.Rectangle srcRec, Raylib_cs.Color color)
    {
        return ImageButtonRect(
            id: id,
            tex: tex.ID!,
            width: width,
            height: height,
            srcRec: new Glib.Rectangle(srcRec.X, srcRec.Y, srcRec.Width, srcRec.Height),
            color: Raylib.ToGlibColor(color)
        );
    }

    public static bool ImageButtonRect(string id, Texture2D tex, float width, float height, Raylib_cs.Rectangle srcRec)
    {
        return ImageButtonRect(id, tex, width, height, srcRec, Raylib_cs.Color.White);
    }

    public static bool ImageButtonRect(string id, Texture2D tex, float width, float height, Raylib_cs.Rectangle srcRec, Vector4 color)
    {
        return ImageButtonRect(
            id: id,
            tex: tex.ID!,
            width: width,
            height: height,
            srcRec: new Glib.Rectangle(srcRec.X, srcRec.Y, srcRec.Width, srcRec.Height),
            color: new Glib.Color(color.X, color.Y, color.Z, color.W)
        );
    }

    // they added link text in a more recent version of imgui... interesting
    public static void LinkText(string id, string link)
    {
        string display;
        if (id[0] == '#')
        {
            display = link;
        }
        else
        {
            display = id;
        }
        
        var cursorPos = ImGui.GetCursorPos();
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var textSize = ImGui.CalcTextSize(display);

        // link interactive
        if (ImGui.InvisibleButton(id, textSize))
        {
            if (!Rained.Platform.OpenURL(link))
            {
                Rained.Log.Error("Could not open URL on user platform.");
            }
        }

        // draw link text
        ImGui.SetCursorPos(cursorPos);
        Vector4 textColor;
        if (ImGui.IsItemHovered() || ImGui.IsItemActive())
        {
            textColor = ImGui.GetStyle().Colors[(int) ImGuiCol.ButtonActive];
        }
        else
        {
            textColor = ImGui.GetStyle().Colors[(int) ImGuiCol.ButtonHovered];
        }
        
        ImGui.TextColored(textColor, display);

        // underline
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddLine(cursorScreenPos + textSize * new Vector2(0f, 1f), cursorScreenPos + textSize, ImGui.ColorConvertFloat4ToU32(textColor));
    }
}