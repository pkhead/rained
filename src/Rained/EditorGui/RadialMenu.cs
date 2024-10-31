using System.Numerics;
using ImGuiNET;
namespace Rained.EditorGui;

static class RadialMenu
{
    private static Vector2 popupCenter;

    private static uint ImColor(uint r, uint g, uint b)
    {
        return ImGui.ColorConvertFloat4ToU32(new Vector4(r / 255f, g / 255f, b / 255f, 1.0f));
    }

    public static void OpenPopupRadialMenu(string id)
    {
        popupCenter = ImGui.GetMousePos();
        ImGui.OpenPopup(id);
    }

    public static int PopupRadialMenu(string id, KeyShortcut shortcut, ReadOnlySpan<string> items, int p_selected)
    {
        if (KeyShortcuts.Activated(shortcut))
            OpenPopupRadialMenu(id);
        
        int ret = -1;
        const float RadiusMin = 30.0f;
        const float RadiusMax = 120.0f;
        const float DetectionRadius = RadiusMax * 4f;
        const float RadiusInteractMin = 20.0f;
        const float ItemPadding = 2.0f;
        const int ItemsMin = 6;

        //ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 0));
        //ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0, 0, 0, 0));
        if (ImGui.IsPopupOpen(id))
        {
            ImGui.SetNextWindowPos(popupCenter - new Vector2(DetectionRadius, DetectionRadius));
            ImGui.SetNextWindowSize(new Vector2(DetectionRadius, DetectionRadius) * 2f);
        }

        if (ImGui.BeginPopup(id, ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground))
        {
            var dragDelta = new Vector2(ImGui.GetMousePos().X - popupCenter.X, ImGui.GetMousePos().Y - popupCenter.Y);
            float dragDist2 = dragDelta.LengthSquared();

            var style = ImGui.GetStyle();
            var btnColor = ImGui.GetColorU32(ImGuiCol.Button);
            var btnColorHover = ImGui.GetColorU32(ImGuiCol.ButtonHovered);
            var btnColorSelected = ImGui.GetColorU32(ImGuiCol.ButtonActive);

            var drawList = ImGui.GetWindowDrawList();
            //ImGuiWindow* window = ImGui::GetCurrentWindow();
            drawList.PushClipRectFullScreen();
            //drawList.PathArcTo(popupCenter, (RadiusMin + RadiusMax) / 2f, 0.0f, MathF.PI*2.0f*0.99f, 32);   // FIXME: 0.99f look like full arc with closed thick stroke has a bug now
            //drawList.PathStroke(ImColor(0,0,0), ImDrawFlags.None, RadiusMax - RadiusMin);

            float itemArcSpan = 2.0f*MathF.PI / Math.Max(ItemsMin, items.Length);
            float dragAngle = MathF.Atan2(dragDelta.Y, dragDelta.X);
            if (dragAngle < -0.5f*itemArcSpan)
                dragAngle += 2.0f*MathF.PI;
            //ImGui::Text("%f", drag_angle);    // [Debug]
            
            var itemHovered = -1;
            for (int i = 0; i < items.Length; i++)
            {
                var itemLabel = items[i];
                float angPadding0 = ItemPadding / RadiusMin;
                float angPadding1 = ItemPadding / RadiusMax;

                float itemAngMin0 = itemArcSpan * (i + angPadding0) - itemArcSpan * 0.5f;
                float itemAngMax0 = itemArcSpan * (i + (1f - angPadding0)) - itemArcSpan * 0.5f;
                float itemAngMin1 = itemArcSpan * (i + angPadding1) - itemArcSpan * 0.5f;
                float itemAngMax1 = itemArcSpan * (i + (1f - angPadding1)) - itemArcSpan * 0.5f;

                bool hovered = false;
                if (dragDist2 >= RadiusInteractMin * RadiusInteractMin && dragDist2 < DetectionRadius * DetectionRadius)
                {
                    if (dragAngle >= itemAngMin1 && dragAngle < itemAngMax1)
                        hovered = true;
                }
                bool selected = p_selected == i;

                int arc_segments = (int)(32 * itemArcSpan / (2f * MathF.PI)) + 1;
                drawList.PathArcTo(popupCenter, RadiusMax - style.ItemInnerSpacing.X, itemAngMin1, itemAngMax1, arc_segments);
                drawList.PathArcTo(popupCenter, RadiusMin + style.ItemInnerSpacing.X, itemAngMax0, itemAngMin0, arc_segments);
                //draw_list->PathFill(window->Color(hovered ? ImGuiCol_HeaderHovered : ImGuiCol_FrameBg));
                drawList.PathFillConcave(hovered ? btnColorHover : selected ? btnColorSelected : btnColor);

                Vector2 text_size = ImGui.GetFont().CalcTextSizeA(ImGui.GetFontSize(), float.PositiveInfinity, 0.0f, itemLabel);
                Vector2 text_pos = new Vector2(
                    popupCenter.X + MathF.Cos((itemAngMin1 + itemAngMax1) * 0.5f) * (RadiusMin + RadiusMax) * 0.5f - text_size.X * 0.5f,
                    popupCenter.Y + MathF.Sin((itemAngMin1 + itemAngMax1) * 0.5f) * (RadiusMin + RadiusMax) * 0.5f - text_size.Y * 0.5f
                );
                drawList.AddText(text_pos, ImColor(255,255,255), itemLabel);

                if (hovered)
                    itemHovered = i;
            }

            drawList.PopClipRect();

            if (KeyShortcuts.Deactivated(shortcut))
            {
                ImGui.CloseCurrentPopup();
                ret = itemHovered;
            }

            // escape to cancel
            if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        //ImGui.PopStyleColor(2);
        return ret;
    }
}