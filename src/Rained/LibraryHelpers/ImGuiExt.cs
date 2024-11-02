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

        byte[] nameBytes = Encoding.UTF8.GetBytes(iniFilename + "\0");
        iniFilenameAlloc = Marshal.AllocHGlobal(nameBytes.Length);
        Marshal.Copy(nameBytes, 0, iniFilenameAlloc, nameBytes.Length);
        
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

    public static void ImageRenderTexture(Framebuffer framebuffer, int slot, Glib.Color color)
    {
        var tex = framebuffer.GetTexture(slot);

        // determine if vertical flip is necessary
        if (Rained.RainEd.RenderContext!.OriginBottomLeft)
            ImGui.Image(TextureID(tex), new Vector2(tex.Width, tex.Height), new Vector2(0f, 1f), new Vector2(1f, 0f), (Vector4) color);
        else
            ImGui.Image(TextureID(tex), new Vector2(tex.Width, tex.Height), new Vector2(0f, 0f), new Vector2(1f, 1f), (Vector4) color);
    }

    public static void ImageRenderTextureScaled(Framebuffer framebuffer, Vector2 scale, int slot, Glib.Color color)
    {
        var tex = framebuffer.GetTexture(slot);

        // determine if vertical flip is necessary
        if (Rained.RainEd.RenderContext!.OriginBottomLeft)
            ImGui.Image(TextureID(tex), new Vector2(tex.Width, tex.Height) * scale, new Vector2(0f, 1f), new Vector2(1f, 0f), (Vector4) color);
        else
            ImGui.Image(TextureID(tex), new Vector2(tex.Width, tex.Height) * scale, new Vector2(0f, 0f), new Vector2(1f, 1f), (Vector4) color);
    }

    public static void ImageRenderTexture(Framebuffer framebuffer, int slot = 0)
        => ImageRenderTexture(framebuffer, slot, Glib.Color.White);
    
    public static void ImageRenderTextureScaled(Framebuffer framebuffer, Vector2 scale, int slot = 0)
        => ImageRenderTextureScaled(framebuffer, scale, slot, Glib.Color.White);

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
        => ImageRenderTexture(framebuffer.ID!);
    
    public static void ImageRenderTextureScaled(RenderTexture2D framebuffer, Vector2 scale)
        => ImageRenderTextureScaled(framebuffer.ID!, scale);
    
    public static void ImageRenderTexture(RenderTexture2D framebuffer, Raylib_cs.Color color)
        => ImageRenderTexture(framebuffer.ID!, 0, Raylib.ToGlibColor(color));

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

    public static bool ButtonSwitch(string id, ReadOnlySpan<string> options, ref int selected)
    {
        var activeCol = ImGui.GetStyle().Colors[(int)ImGuiCol.Button];
        var activeColHover = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered];
        var activeColActive = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive];

        var inactiveCol = new Vector4(activeCol.X, activeCol.Y, activeCol.Z, activeCol.W / 2f);
        var inactiveColHover = new Vector4(activeColHover.X, activeColHover.Y, activeColHover.Z, activeColHover.W / 2f);
        var inactiveColActive = new Vector4(activeColActive.X, activeColActive.Y, activeColActive.Z, activeColActive.W / 2f);

        var itemSpacing = ImGui.GetStyle().ItemInnerSpacing;
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, itemSpacing);

        ImGui.PushID(id);

        var returnValue = false;
        var itemSize = new Vector2((ImGui.CalcItemWidth() + itemSpacing.X * (1 - options.Length)) / options.Length, 0f);

        for (int i = 0; i < options.Length; i++)
        {
            if (selected == i)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, activeCol);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, activeColHover);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, activeColActive);
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, inactiveCol);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, inactiveColHover);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, inactiveColActive);
            }

            ImGui.PushID(i);
            if (i > 0) ImGui.SameLine();
            if (ImGui.Button(options[i], itemSize))
            {
                if (selected != i) returnValue = true;
                selected = i;
            }
            ImGui.PopID();

            ImGui.PopStyleColor(3);
        }

        ImGui.PopID();
        ImGui.PopStyleVar();

        return returnValue;
    }

    public static bool ButtonFlags(string id, ReadOnlySpan<string> flagNames, ref int flags)
    {
        var activeCol = ImGui.GetStyle().Colors[(int)ImGuiCol.Button];
        var activeColHover = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered];
        var activeColActive = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive];

        var inactiveCol = new Vector4(activeCol.X, activeCol.Y, activeCol.Z, activeCol.W / 2f);
        var inactiveColHover = new Vector4(activeColHover.X, activeColHover.Y, activeColHover.Z, activeColHover.W / 2f);
        var inactiveColActive = new Vector4(activeColActive.X, activeColActive.Y, activeColActive.Z, activeColActive.W / 2f);

        var itemSpacing = ImGui.GetStyle().ItemInnerSpacing;
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, itemSpacing);

        ImGui.PushID(id);

        var returnValue = false;
        var itemSize = new Vector2((ImGui.CalcItemWidth() + itemSpacing.X * (1 - flagNames.Length)) / flagNames.Length, 0f);

        for (int i = 0; i < flagNames.Length; i++)
        {
            var flag = 1 << i;
            var isActive = (flags & flag) != 0;

            if (isActive)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, activeCol);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, activeColHover);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, activeColActive);
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, inactiveCol);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, inactiveColHover);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, inactiveColActive);
            }

            ImGui.PushID(i);
            if (i > 0) ImGui.SameLine();
            if (ImGui.Button(flagNames[i], itemSize))
            {
                if (isActive)
                    flags &= ~flag;
                else
                    flags |= flag;
                
                returnValue = true;
            }
            ImGui.PopID();

            ImGui.PopStyleColor(3);
        }

        ImGui.PopID();
        ImGui.PopStyleVar();

        return returnValue;
    }

    public static bool ButtonFlags(string id, ReadOnlySpan<string> flagNames, Span<bool> values)
    {
        if (flagNames.Length != values.Length)
            throw new ArgumentException("Array size mismatch", nameof(values));
        
        var activeCol = ImGui.GetStyle().Colors[(int)ImGuiCol.Button];
        var activeColHover = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered];
        var activeColActive = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive];

        var inactiveCol = new Vector4(activeCol.X, activeCol.Y, activeCol.Z, activeCol.W / 2f);
        var inactiveColHover = new Vector4(activeColHover.X, activeColHover.Y, activeColHover.Z, activeColHover.W / 2f);
        var inactiveColActive = new Vector4(activeColActive.X, activeColActive.Y, activeColActive.Z, activeColActive.W / 2f);

        var itemSpacing = ImGui.GetStyle().ItemInnerSpacing;
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, itemSpacing);

        ImGui.PushID(id);

        var returnValue = false;
        var itemSize = new Vector2((ImGui.CalcItemWidth() + itemSpacing.X * (1 - flagNames.Length)) / flagNames.Length, 0f);

        for (int i = 0; i < flagNames.Length; i++)
        {
            var flag = 1 << i;
            var isActive = values[i];

            if (isActive)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, activeCol);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, activeColHover);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, activeColActive);
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, inactiveCol);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, inactiveColHover);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, inactiveColActive);
            }

            ImGui.PushID(i);
            if (i > 0) ImGui.SameLine();
            if (ImGui.Button(flagNames[i], itemSize))
            {
                values[i] = !values[i];
                returnValue = true;
            }
            ImGui.PopID();

            ImGui.PopStyleColor(3);
        }

        ImGui.PopID();
        ImGui.PopStyleVar();

        return returnValue;
    }
}