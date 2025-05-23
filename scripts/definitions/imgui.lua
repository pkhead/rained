---@meta
local imgui = {}
function imgui.AlignTextToFramePadding() end

---@param name string
---@param p_open boolean?
---@param flags integer?
---@return boolean s, boolean p_open
function imgui.Begin(name, p_open, flags) end

---@param str_id string
---@param size_x number?
---@param size_y number?
---@param child_flags integer?
---@param window_flags integer?
---@return boolean s
function imgui.BeginChild_Str(str_id, size_x, size_y, child_flags, window_flags) end

---@param label string
---@param preview_value string
---@param flags integer?
---@return boolean s
function imgui.BeginCombo(label, preview_value, flags) end

---@param disabled boolean?
function imgui.BeginDisabled(disabled) end

---@param flags integer?
---@return boolean s
function imgui.BeginDragDropSource(flags) end

---@return boolean s
function imgui.BeginDragDropTarget() end

function imgui.BeginGroup() end

---@return boolean s
function imgui.BeginItemTooltip() end

---@param label string
---@param size_x number?
---@param size_y number?
---@return boolean s
function imgui.BeginListBox(label, size_x, size_y) end

---@return boolean s
function imgui.BeginMainMenuBar() end

---@param label string
---@param enabled boolean?
---@return boolean s
function imgui.BeginMenu(label, enabled) end

---@return boolean s
function imgui.BeginMenuBar() end

---@param str_id string
---@param flags integer?
---@return boolean s
function imgui.BeginPopup(str_id, flags) end

---@param str_id string?
---@param popup_flags integer?
---@return boolean s
function imgui.BeginPopupContextItem(str_id, popup_flags) end

---@param str_id string?
---@param popup_flags integer?
---@return boolean s
function imgui.BeginPopupContextVoid(str_id, popup_flags) end

---@param str_id string?
---@param popup_flags integer?
---@return boolean s
function imgui.BeginPopupContextWindow(str_id, popup_flags) end

---@param name string
---@param p_open boolean?
---@param flags integer?
---@return boolean s, boolean p_open
function imgui.BeginPopupModal(name, p_open, flags) end

---@param str_id string
---@param flags integer?
---@return boolean s
function imgui.BeginTabBar(str_id, flags) end

---@param label string
---@param p_open boolean?
---@param flags integer?
---@return boolean s, boolean p_open
function imgui.BeginTabItem(label, p_open, flags) end

---@param str_id string
---@param columns integer
---@param flags integer?
---@param outer_size_x number?
---@param outer_size_y number?
---@param inner_width number?
---@return boolean s
function imgui.BeginTable(str_id, columns, flags, outer_size_x, outer_size_y, inner_width) end

---@return boolean s
function imgui.BeginTooltip() end

function imgui.Bullet() end

---@param fmt string
function imgui.BulletText(fmt) end

---@param label string
---@param size_x number?
---@param size_y number?
---@return boolean s
function imgui.Button(label, size_x, size_y) end

---@param pOut_x number
---@param pOut_y number
---@param text string
---@param text_end string?
---@param hide_text_after_double_hash boolean?
---@param wrap_width number?
---@return number pOut_x, number pOut_y
function imgui.CalcTextSize(pOut_x, pOut_y, text, text_end, hide_text_after_double_hash, wrap_width) end

---@param label string
---@param v boolean
---@return boolean s, boolean v
function imgui.Checkbox(label, v) end

function imgui.CloseCurrentPopup() end

---@param label string
---@param flags integer?
---@return boolean s
function imgui.CollapsingHeader_TreeNodeFlags(label, flags) end

---@param label string
---@param p_visible boolean
---@param flags integer?
---@return boolean s, boolean p_visible
function imgui.CollapsingHeader_BoolPtr(label, p_visible, flags) end

---@param h number
---@param s number
---@param v number
---@param out_r number
---@param out_g number
---@param out_b number
---@return number out_r, number out_g, number out_b
function imgui.ColorConvertHSVtoRGB(h, s, v, out_r, out_g, out_b) end

---@param r number
---@param g number
---@param b number
---@param out_h number
---@param out_s number
---@param out_v number
---@return number out_h, number out_s, number out_v
function imgui.ColorConvertRGBtoHSV(r, g, b, out_h, out_s, out_v) end

---@param count integer?
---@param id string?
---@param border boolean?
function imgui.Columns(count, id, border) end

---@param idx integer
function imgui.DebugFlashStyleColor(idx) end

function imgui.DebugStartItemPicker() end

---@param text string
function imgui.DebugTextEncoding(text) end

function imgui.DestroyPlatformWindows() end

---@param label string
---@param v number
---@param v_speed number?
---@param v_min number?
---@param v_max number?
---@param format string?
---@param flags integer?
---@return boolean s, number v
function imgui.DragFloat(label, v, v_speed, v_min, v_max, format, flags) end

---@param label string
---@param v_current_min number
---@param v_current_max number
---@param v_speed number?
---@param v_min number?
---@param v_max number?
---@param format string?
---@param format_max string?
---@param flags integer?
---@return boolean s, number v_current_min, number v_current_max
function imgui.DragFloatRange2(label, v_current_min, v_current_max, v_speed, v_min, v_max, format, format_max, flags) end

---@param size_x number
---@param size_y number
function imgui.Dummy(size_x, size_y) end

function imgui.End() end

function imgui.EndChild() end

function imgui.EndCombo() end

function imgui.EndDisabled() end

function imgui.EndDragDropSource() end

function imgui.EndDragDropTarget() end

function imgui.EndFrame() end

function imgui.EndGroup() end

function imgui.EndListBox() end

function imgui.EndMainMenuBar() end

function imgui.EndMenu() end

function imgui.EndMenuBar() end

function imgui.EndPopup() end

function imgui.EndTabBar() end

function imgui.EndTabItem() end

function imgui.EndTable() end

function imgui.EndTooltip() end

---@return integer s
function imgui.GetColumnIndex() end

---@return integer s
function imgui.GetColumnsCount() end

---@param pOut_x number
---@param pOut_y number
---@return number pOut_x, number pOut_y
function imgui.GetContentRegionAvail(pOut_x, pOut_y) end

---@param pOut_x number
---@param pOut_y number
---@return number pOut_x, number pOut_y
function imgui.GetContentRegionMax(pOut_x, pOut_y) end

---@param pOut_x number
---@param pOut_y number
---@return number pOut_x, number pOut_y
function imgui.GetCursorPos(pOut_x, pOut_y) end

---@param pOut_x number
---@param pOut_y number
---@return number pOut_x, number pOut_y
function imgui.GetCursorScreenPos(pOut_x, pOut_y) end

---@param pOut_x number
---@param pOut_y number
---@return number pOut_x, number pOut_y
function imgui.GetCursorStartPos(pOut_x, pOut_y) end

---@param pOut_x number
---@param pOut_y number
---@return number pOut_x, number pOut_y
function imgui.GetFontTexUvWhitePixel(pOut_x, pOut_y) end

---@return integer s
function imgui.GetFrameCount() end

---@param pOut_x number
---@param pOut_y number
---@return number pOut_x, number pOut_y
function imgui.GetItemRectMax(pOut_x, pOut_y) end

---@param pOut_x number
---@param pOut_y number
---@return number pOut_x, number pOut_y
function imgui.GetItemRectMin(pOut_x, pOut_y) end

---@param pOut_x number
---@param pOut_y number
---@return number pOut_x, number pOut_y
function imgui.GetItemRectSize(pOut_x, pOut_y) end

---@param button integer
---@return integer s
function imgui.GetMouseClickedCount(button) end

---@param pOut_x number
---@param pOut_y number
---@param button integer?
---@param lock_threshold number?
---@return number pOut_x, number pOut_y
function imgui.GetMouseDragDelta(pOut_x, pOut_y, button, lock_threshold) end

---@param pOut_x number
---@param pOut_y number
---@return number pOut_x, number pOut_y
function imgui.GetMousePos(pOut_x, pOut_y) end

---@param pOut_x number
---@param pOut_y number
---@return number pOut_x, number pOut_y
function imgui.GetMousePosOnOpeningCurrentPopup(pOut_x, pOut_y) end

---@param pOut_x number
---@param pOut_y number
---@return number pOut_x, number pOut_y
function imgui.GetWindowContentRegionMax(pOut_x, pOut_y) end

---@param pOut_x number
---@param pOut_y number
---@return number pOut_x, number pOut_y
function imgui.GetWindowContentRegionMin(pOut_x, pOut_y) end

---@param pOut_x number
---@param pOut_y number
---@return number pOut_x, number pOut_y
function imgui.GetWindowPos(pOut_x, pOut_y) end

---@param pOut_x number
---@param pOut_y number
---@return number pOut_x, number pOut_y
function imgui.GetWindowSize(pOut_x, pOut_y) end

---@param indent_w number?
function imgui.Indent(indent_w) end

---@param label string
---@param v number
---@param step number?
---@param step_fast number?
---@param format string?
---@param flags integer?
---@return boolean s, number v
function imgui.InputFloat(label, v, step, step_fast, format, flags) end

---@param str_id string
---@param size_x number
---@param size_y number
---@param flags integer?
---@return boolean s
function imgui.InvisibleButton(str_id, size_x, size_y, flags) end

---@return boolean s
function imgui.IsAnyItemActive() end

---@return boolean s
function imgui.IsAnyItemFocused() end

---@return boolean s
function imgui.IsAnyItemHovered() end

---@return boolean s
function imgui.IsAnyMouseDown() end

---@return boolean s
function imgui.IsItemActivated() end

---@return boolean s
function imgui.IsItemActive() end

---@param mouse_button integer?
---@return boolean s
function imgui.IsItemClicked(mouse_button) end

---@return boolean s
function imgui.IsItemDeactivated() end

---@return boolean s
function imgui.IsItemDeactivatedAfterEdit() end

---@return boolean s
function imgui.IsItemEdited() end

---@return boolean s
function imgui.IsItemFocused() end

---@param flags integer?
---@return boolean s
function imgui.IsItemHovered(flags) end

---@return boolean s
function imgui.IsItemToggledOpen() end

---@return boolean s
function imgui.IsItemVisible() end

---@param button integer
---@param repeat_ boolean?
---@return boolean s
function imgui.IsMouseClicked_Bool(button, repeat_) end

---@param button integer
---@return boolean s
function imgui.IsMouseDoubleClicked_Nil(button) end

---@param button integer
---@return boolean s
function imgui.IsMouseDown_Nil(button) end

---@param button integer
---@param lock_threshold number?
---@return boolean s
function imgui.IsMouseDragging(button, lock_threshold) end

---@param r_min_x number
---@param r_min_y number
---@param r_max_x number
---@param r_max_y number
---@param clip boolean?
---@return boolean s
function imgui.IsMouseHoveringRect(r_min_x, r_min_y, r_max_x, r_max_y, clip) end

---@param button integer
---@return boolean s
function imgui.IsMouseReleased_Nil(button) end

---@param str_id string
---@param flags integer?
---@return boolean s
function imgui.IsPopupOpen_Str(str_id, flags) end

---@param size_x number
---@param size_y number
---@return boolean s
function imgui.IsRectVisible_Nil(size_x, size_y) end

---@param rect_min_x number
---@param rect_min_y number
---@param rect_max_x number
---@param rect_max_y number
---@return boolean s
function imgui.IsRectVisible_Vec2(rect_min_x, rect_min_y, rect_max_x, rect_max_y) end

---@return boolean s
function imgui.IsWindowAppearing() end

---@return boolean s
function imgui.IsWindowCollapsed() end

---@return boolean s
function imgui.IsWindowDocked() end

---@param flags integer?
---@return boolean s
function imgui.IsWindowFocused(flags) end

---@param flags integer?
---@return boolean s
function imgui.IsWindowHovered(flags) end

---@param label string
---@param fmt string
function imgui.LabelText(label, fmt) end

---@param ini_filename string
function imgui.LoadIniSettingsFromDisk(ini_filename) end

function imgui.LogButtons() end

function imgui.LogFinish() end

---@param fmt string
function imgui.LogText(fmt) end

---@param auto_open_depth integer?
function imgui.LogToClipboard(auto_open_depth) end

---@param auto_open_depth integer?
---@param filename string?
function imgui.LogToFile(auto_open_depth, filename) end

---@param auto_open_depth integer?
function imgui.LogToTTY(auto_open_depth) end

---@param label string
---@param shortcut string?
---@param selected boolean?
---@param enabled boolean?
---@return boolean s
function imgui.MenuItem_Bool(label, shortcut, selected, enabled) end

---@param label string
---@param shortcut string
---@param p_selected boolean
---@param enabled boolean?
---@return boolean s, boolean p_selected
function imgui.MenuItem_BoolPtr(label, shortcut, p_selected, enabled) end

function imgui.NewFrame() end

function imgui.NewLine() end

function imgui.NextColumn() end

---@param str_id string
---@param popup_flags integer?
function imgui.OpenPopup_Str(str_id, popup_flags) end

---@param str_id string?
---@param popup_flags integer?
function imgui.OpenPopupOnItemClick(str_id, popup_flags) end

function imgui.PopButtonRepeat() end

function imgui.PopClipRect() end

function imgui.PopFont() end

function imgui.PopID() end

function imgui.PopItemWidth() end

---@param count integer?
function imgui.PopStyleColor(count) end

---@param count integer?
function imgui.PopStyleVar(count) end

function imgui.PopTabStop() end

function imgui.PopTextWrapPos() end

---@param fraction number
---@param size_arg_x number?
---@param size_arg_y number?
---@param overlay string?
function imgui.ProgressBar(fraction, size_arg_x, size_arg_y, overlay) end

---@param repeat_ boolean
function imgui.PushButtonRepeat(repeat_) end

---@param clip_rect_min_x number
---@param clip_rect_min_y number
---@param clip_rect_max_x number
---@param clip_rect_max_y number
---@param intersect_with_current_clip_rect boolean
function imgui.PushClipRect(clip_rect_min_x, clip_rect_min_y, clip_rect_max_x, clip_rect_max_y, intersect_with_current_clip_rect) end

---@param str_id string
function imgui.PushID_Str(str_id) end

---@param str_id_begin string
---@param str_id_end string
function imgui.PushID_StrStr(str_id_begin, str_id_end) end

---@param int_id integer
function imgui.PushID_Int(int_id) end

---@param item_width number
function imgui.PushItemWidth(item_width) end

---@param idx integer
---@param val number
function imgui.PushStyleVar_Float(idx, val) end

---@param idx integer
---@param val_x number
---@param val_y number
function imgui.PushStyleVar_Vec2(idx, val_x, val_y) end

---@param tab_stop boolean
function imgui.PushTabStop(tab_stop) end

---@param wrap_local_pos_x number?
function imgui.PushTextWrapPos(wrap_local_pos_x) end

---@param label string
---@param active boolean
---@return boolean s
function imgui.RadioButton_Bool(label, active) end

function imgui.Render() end

---@param button integer?
function imgui.ResetMouseDragDelta(button) end

---@param offset_from_start_x number?
---@param spacing number?
function imgui.SameLine(offset_from_start_x, spacing) end

---@param ini_filename string
function imgui.SaveIniSettingsToDisk(ini_filename) end

---@param label string
---@param selected boolean?
---@param flags integer?
---@param size_x number?
---@param size_y number?
---@return boolean s
function imgui.Selectable_Bool(label, selected, flags, size_x, size_y) end

---@param label string
---@param p_selected boolean
---@param flags integer?
---@param size_x number?
---@param size_y number?
---@return boolean s, boolean p_selected
function imgui.Selectable_BoolPtr(label, p_selected, flags, size_x, size_y) end

function imgui.Separator() end

---@param label string
function imgui.SeparatorText(label) end

---@param text string
function imgui.SetClipboardText(text) end

---@param flags integer
function imgui.SetColorEditOptions(flags) end

---@param column_index integer
---@param offset_x number
function imgui.SetColumnOffset(column_index, offset_x) end

---@param column_index integer
---@param width number
function imgui.SetColumnWidth(column_index, width) end

---@param local_pos_x number
---@param local_pos_y number
function imgui.SetCursorPos(local_pos_x, local_pos_y) end

---@param local_x number
function imgui.SetCursorPosX(local_x) end

---@param local_y number
function imgui.SetCursorPosY(local_y) end

---@param pos_x number
---@param pos_y number
function imgui.SetCursorScreenPos(pos_x, pos_y) end

function imgui.SetItemDefaultFocus() end

---@param fmt string
function imgui.SetItemTooltip(fmt) end

---@param offset integer?
function imgui.SetKeyboardFocusHere(offset) end

---@param cursor_type integer
function imgui.SetMouseCursor(cursor_type) end

---@param want_capture_keyboard boolean
function imgui.SetNextFrameWantCaptureKeyboard(want_capture_keyboard) end

---@param want_capture_mouse boolean
function imgui.SetNextFrameWantCaptureMouse(want_capture_mouse) end

function imgui.SetNextItemAllowOverlap() end

---@param is_open boolean
---@param cond integer?
function imgui.SetNextItemOpen(is_open, cond) end

---@param item_width number
function imgui.SetNextItemWidth(item_width) end

---@param alpha number
function imgui.SetNextWindowBgAlpha(alpha) end

---@param collapsed boolean
---@param cond integer?
function imgui.SetNextWindowCollapsed(collapsed, cond) end

---@param size_x number
---@param size_y number
function imgui.SetNextWindowContentSize(size_x, size_y) end

function imgui.SetNextWindowFocus() end

---@param pos_x number
---@param pos_y number
---@param cond integer?
---@param pivot_x number?
---@param pivot_y number?
function imgui.SetNextWindowPos(pos_x, pos_y, cond, pivot_x, pivot_y) end

---@param scroll_x number
---@param scroll_y number
function imgui.SetNextWindowScroll(scroll_x, scroll_y) end

---@param size_x number
---@param size_y number
---@param cond integer?
function imgui.SetNextWindowSize(size_x, size_y, cond) end

---@param local_x number
---@param center_x_ratio number?
function imgui.SetScrollFromPosX_Float(local_x, center_x_ratio) end

---@param local_y number
---@param center_y_ratio number?
function imgui.SetScrollFromPosY_Float(local_y, center_y_ratio) end

---@param center_x_ratio number?
function imgui.SetScrollHereX(center_x_ratio) end

---@param center_y_ratio number?
function imgui.SetScrollHereY(center_y_ratio) end

---@param scroll_x number
function imgui.SetScrollX_Float(scroll_x) end

---@param scroll_y number
function imgui.SetScrollY_Float(scroll_y) end

---@param tab_or_docked_window_label string
function imgui.SetTabItemClosed(tab_or_docked_window_label) end

---@param fmt string
function imgui.SetTooltip(fmt) end

---@param collapsed boolean
---@param cond integer?
function imgui.SetWindowCollapsed_Bool(collapsed, cond) end

---@param name string
---@param collapsed boolean
---@param cond integer?
function imgui.SetWindowCollapsed_Str(name, collapsed, cond) end

function imgui.SetWindowFocus_Nil() end

---@param name string
function imgui.SetWindowFocus_Str(name) end

---@param scale number
function imgui.SetWindowFontScale(scale) end

---@param pos_x number
---@param pos_y number
---@param cond integer?
function imgui.SetWindowPos_Vec2(pos_x, pos_y, cond) end

---@param name string
---@param pos_x number
---@param pos_y number
---@param cond integer?
function imgui.SetWindowPos_Str(name, pos_x, pos_y, cond) end

---@param size_x number
---@param size_y number
---@param cond integer?
function imgui.SetWindowSize_Vec2(size_x, size_y, cond) end

---@param name string
---@param size_x number
---@param size_y number
---@param cond integer?
function imgui.SetWindowSize_Str(name, size_x, size_y, cond) end

---@param p_open boolean?
---@return boolean p_open
function imgui.ShowAboutWindow(p_open) end

---@param p_open boolean?
---@return boolean p_open
function imgui.ShowDebugLogWindow(p_open) end

---@param p_open boolean?
---@return boolean p_open
function imgui.ShowDemoWindow(p_open) end

---@param label string
function imgui.ShowFontSelector(label) end

---@param p_open boolean?
---@return boolean p_open
function imgui.ShowIDStackToolWindow(p_open) end

---@param p_open boolean?
---@return boolean p_open
function imgui.ShowMetricsWindow(p_open) end

---@param label string
---@return boolean s
function imgui.ShowStyleSelector(label) end

function imgui.ShowUserGuide() end

---@param label string
---@param v_rad number
---@param v_degrees_min number?
---@param v_degrees_max number?
---@param format string?
---@param flags integer?
---@return boolean s, number v_rad
function imgui.SliderAngle(label, v_rad, v_degrees_min, v_degrees_max, format, flags) end

---@param label string
---@param v number
---@param v_min number
---@param v_max number
---@param format string?
---@param flags integer?
---@return boolean s, number v
function imgui.SliderFloat(label, v, v_min, v_max, format, flags) end

---@param label string
---@return boolean s
function imgui.SmallButton(label) end

function imgui.Spacing() end

---@param label string
---@param flags integer?
---@return boolean s
function imgui.TabItemButton(label, flags) end

function imgui.TableAngledHeadersRow() end

---@return integer s
function imgui.TableGetColumnCount() end

---@return integer s
function imgui.TableGetColumnIndex() end

---@return integer s
function imgui.TableGetHoveredColumn() end

---@return integer s
function imgui.TableGetRowIndex() end

---@param label string
function imgui.TableHeader(label) end

function imgui.TableHeadersRow() end

---@return boolean s
function imgui.TableNextColumn() end

---@param row_flags integer?
---@param min_row_height number?
function imgui.TableNextRow(row_flags, min_row_height) end

---@param column_n integer
---@param v boolean
function imgui.TableSetColumnEnabled(column_n, v) end

---@param column_n integer
---@return boolean s
function imgui.TableSetColumnIndex(column_n) end

---@param cols integer
---@param rows integer
function imgui.TableSetupScrollFreeze(cols, rows) end

---@param fmt string
function imgui.Text(fmt) end

---@param fmt string
function imgui.TextDisabled(fmt) end

---@param text string
---@param text_end string?
function imgui.TextUnformatted(text, text_end) end

---@param fmt string
function imgui.TextWrapped(fmt) end

---@param label string
---@return boolean s
function imgui.TreeNode_Str(label) end

---@param str_id string
---@param fmt string
---@return boolean s
function imgui.TreeNode_StrStr(str_id, fmt) end

---@param label string
---@param flags integer?
---@return boolean s
function imgui.TreeNodeEx_Str(label, flags) end

---@param str_id string
---@param flags integer
---@param fmt string
---@return boolean s
function imgui.TreeNodeEx_StrStr(str_id, flags, fmt) end

function imgui.TreePop() end

---@param str_id string
function imgui.TreePush_Str(str_id) end

---@param indent_w number?
function imgui.Unindent(indent_w) end

function imgui.UpdatePlatformWindows() end

---@param label string
---@param size_x number
---@param size_y number
---@param v number
---@param v_min number
---@param v_max number
---@param format string?
---@param flags integer?
---@return boolean s, number v
function imgui.VSliderFloat(label, size_x, size_y, v, v_min, v_max, format, flags) end

---@param prefix string
---@param b boolean
function imgui.Value_Bool(prefix, b) end

---@param prefix string
---@param v integer
function imgui.Value_Int(prefix, v) end

---@param prefix string
---@param v number
---@param float_format string?
function imgui.Value_Float(prefix, v, float_format) end

imgui.ImDrawFlags_None = 0
imgui.ImDrawFlags_Closed = 1
imgui.ImDrawFlags_RoundCornersTopLeft = 16
imgui.ImDrawFlags_RoundCornersTopRight = 32
imgui.ImDrawFlags_RoundCornersBottomLeft = 64
imgui.ImDrawFlags_RoundCornersBottomRight = 128
imgui.ImDrawFlags_RoundCornersNone = 256
imgui.ImDrawFlags_RoundCornersTop = 48
imgui.ImDrawFlags_RoundCornersBottom = 192
imgui.ImDrawFlags_RoundCornersLeft = 80
imgui.ImDrawFlags_RoundCornersRight = 160
imgui.ImDrawFlags_RoundCornersAll = 240
imgui.ImDrawFlags_RoundCornersDefault_ = 240
imgui.ImDrawFlags_RoundCornersMask_ = 496
imgui.ImDrawListFlags_None = 0
imgui.ImDrawListFlags_AntiAliasedLines = 1
imgui.ImDrawListFlags_AntiAliasedLinesUseTex = 2
imgui.ImDrawListFlags_AntiAliasedFill = 4
imgui.ImDrawListFlags_AllowVtxOffset = 8
imgui.ImFontAtlasFlags_None = 0
imgui.ImFontAtlasFlags_NoPowerOfTwoHeight = 1
imgui.ImFontAtlasFlags_NoMouseCursors = 2
imgui.ImFontAtlasFlags_NoBakedLines = 4
imgui.ActivateFlags_None = 0
imgui.ActivateFlags_PreferInput = 1
imgui.ActivateFlags_PreferTweak = 2
imgui.ActivateFlags_TryToPreserveState = 4
imgui.ActivateFlags_FromTabbing = 8
imgui.ActivateFlags_FromShortcut = 16
imgui.Axis_None = -1
imgui.Axis_X = 0
imgui.Axis_Y = 1
imgui.BackendFlags_None = 0
imgui.BackendFlags_HasGamepad = 1
imgui.BackendFlags_HasMouseCursors = 2
imgui.BackendFlags_HasSetMousePos = 4
imgui.BackendFlags_RendererHasVtxOffset = 8
imgui.BackendFlags_PlatformHasViewports = 1024
imgui.BackendFlags_HasMouseHoveredViewport = 2048
imgui.BackendFlags_RendererHasViewports = 4096
imgui.ButtonFlags_PressedOnClick = 16
imgui.ButtonFlags_PressedOnClickRelease = 32
imgui.ButtonFlags_PressedOnClickReleaseAnywhere = 64
imgui.ButtonFlags_PressedOnRelease = 128
imgui.ButtonFlags_PressedOnDoubleClick = 256
imgui.ButtonFlags_PressedOnDragDropHold = 512
imgui.ButtonFlags_Repeat = 1024
imgui.ButtonFlags_FlattenChildren = 2048
imgui.ButtonFlags_AllowOverlap = 4096
imgui.ButtonFlags_DontClosePopups = 8192
imgui.ButtonFlags_AlignTextBaseLine = 32768
imgui.ButtonFlags_NoKeyModifiers = 65536
imgui.ButtonFlags_NoHoldingActiveId = 131072
imgui.ButtonFlags_NoNavFocus = 262144
imgui.ButtonFlags_NoHoveredOnFocus = 524288
imgui.ButtonFlags_NoSetKeyOwner = 1048576
imgui.ButtonFlags_NoTestKeyOwner = 2097152
imgui.ButtonFlags_PressedOnMask_ = 1008
imgui.ButtonFlags_PressedOnDefault_ = 32
imgui.ButtonFlags_None = 0
imgui.ButtonFlags_MouseButtonLeft = 1
imgui.ButtonFlags_MouseButtonRight = 2
imgui.ButtonFlags_MouseButtonMiddle = 4
imgui.ButtonFlags_MouseButtonMask_ = 7
imgui.ChildFlags_None = 0
imgui.ChildFlags_Border = 1
imgui.ChildFlags_AlwaysUseWindowPadding = 2
imgui.ChildFlags_ResizeX = 4
imgui.ChildFlags_ResizeY = 8
imgui.ChildFlags_AutoResizeX = 16
imgui.ChildFlags_AutoResizeY = 32
imgui.ChildFlags_AlwaysAutoResize = 64
imgui.ChildFlags_FrameStyle = 128
imgui.ChildFlags_NavFlattened = 256
imgui.Col_Text = 0
imgui.Col_TextDisabled = 1
imgui.Col_WindowBg = 2
imgui.Col_ChildBg = 3
imgui.Col_PopupBg = 4
imgui.Col_Border = 5
imgui.Col_BorderShadow = 6
imgui.Col_FrameBg = 7
imgui.Col_FrameBgHovered = 8
imgui.Col_FrameBgActive = 9
imgui.Col_TitleBg = 10
imgui.Col_TitleBgActive = 11
imgui.Col_TitleBgCollapsed = 12
imgui.Col_MenuBarBg = 13
imgui.Col_ScrollbarBg = 14
imgui.Col_ScrollbarGrab = 15
imgui.Col_ScrollbarGrabHovered = 16
imgui.Col_ScrollbarGrabActive = 17
imgui.Col_CheckMark = 18
imgui.Col_SliderGrab = 19
imgui.Col_SliderGrabActive = 20
imgui.Col_Button = 21
imgui.Col_ButtonHovered = 22
imgui.Col_ButtonActive = 23
imgui.Col_Header = 24
imgui.Col_HeaderHovered = 25
imgui.Col_HeaderActive = 26
imgui.Col_Separator = 27
imgui.Col_SeparatorHovered = 28
imgui.Col_SeparatorActive = 29
imgui.Col_ResizeGrip = 30
imgui.Col_ResizeGripHovered = 31
imgui.Col_ResizeGripActive = 32
imgui.Col_TabHovered = 33
imgui.Col_Tab = 34
imgui.Col_TabSelected = 35
imgui.Col_TabSelectedOverline = 36
imgui.Col_TabDimmed = 37
imgui.Col_TabDimmedSelected = 38
imgui.Col_TabDimmedSelectedOverline = 39
imgui.Col_DockingPreview = 40
imgui.Col_DockingEmptyBg = 41
imgui.Col_PlotLines = 42
imgui.Col_PlotLinesHovered = 43
imgui.Col_PlotHistogram = 44
imgui.Col_PlotHistogramHovered = 45
imgui.Col_TableHeaderBg = 46
imgui.Col_TableBorderStrong = 47
imgui.Col_TableBorderLight = 48
imgui.Col_TableRowBg = 49
imgui.Col_TableRowBgAlt = 50
imgui.Col_TextSelectedBg = 51
imgui.Col_DragDropTarget = 52
imgui.Col_NavHighlight = 53
imgui.Col_NavWindowingHighlight = 54
imgui.Col_NavWindowingDimBg = 55
imgui.Col_ModalWindowDimBg = 56
imgui.Col_COUNT = 57
imgui.ColorEditFlags_None = 0
imgui.ColorEditFlags_NoAlpha = 2
imgui.ColorEditFlags_NoPicker = 4
imgui.ColorEditFlags_NoOptions = 8
imgui.ColorEditFlags_NoSmallPreview = 16
imgui.ColorEditFlags_NoInputs = 32
imgui.ColorEditFlags_NoTooltip = 64
imgui.ColorEditFlags_NoLabel = 128
imgui.ColorEditFlags_NoSidePreview = 256
imgui.ColorEditFlags_NoDragDrop = 512
imgui.ColorEditFlags_NoBorder = 1024
imgui.ColorEditFlags_AlphaBar = 65536
imgui.ColorEditFlags_AlphaPreview = 131072
imgui.ColorEditFlags_AlphaPreviewHalf = 262144
imgui.ColorEditFlags_HDR = 524288
imgui.ColorEditFlags_DisplayRGB = 1048576
imgui.ColorEditFlags_DisplayHSV = 2097152
imgui.ColorEditFlags_DisplayHex = 4194304
imgui.ColorEditFlags_Uint8 = 8388608
imgui.ColorEditFlags_Float = 16777216
imgui.ColorEditFlags_PickerHueBar = 33554432
imgui.ColorEditFlags_PickerHueWheel = 67108864
imgui.ColorEditFlags_InputRGB = 134217728
imgui.ColorEditFlags_InputHSV = 268435456
imgui.ColorEditFlags_DefaultOptions_ = 177209344
imgui.ColorEditFlags_DisplayMask_ = 7340032
imgui.ColorEditFlags_DataTypeMask_ = 25165824
imgui.ColorEditFlags_PickerMask_ = 100663296
imgui.ColorEditFlags_InputMask_ = 402653184
imgui.ComboFlags_CustomPreview = 1048576
imgui.ComboFlags_None = 0
imgui.ComboFlags_PopupAlignLeft = 1
imgui.ComboFlags_HeightSmall = 2
imgui.ComboFlags_HeightRegular = 4
imgui.ComboFlags_HeightLarge = 8
imgui.ComboFlags_HeightLargest = 16
imgui.ComboFlags_NoArrowButton = 32
imgui.ComboFlags_NoPreview = 64
imgui.ComboFlags_WidthFitPreview = 128
imgui.ComboFlags_HeightMask_ = 30
imgui.Cond_None = 0
imgui.Cond_Always = 1
imgui.Cond_Once = 2
imgui.Cond_FirstUseEver = 4
imgui.Cond_Appearing = 8
imgui.ConfigFlags_None = 0
imgui.ConfigFlags_NavEnableKeyboard = 1
imgui.ConfigFlags_NavEnableGamepad = 2
imgui.ConfigFlags_NavEnableSetMousePos = 4
imgui.ConfigFlags_NavNoCaptureKeyboard = 8
imgui.ConfigFlags_NoMouse = 16
imgui.ConfigFlags_NoMouseCursorChange = 32
imgui.ConfigFlags_NoKeyboard = 64
imgui.ConfigFlags_DockingEnable = 128
imgui.ConfigFlags_ViewportsEnable = 1024
imgui.ConfigFlags_DpiEnableScaleViewports = 16384
imgui.ConfigFlags_DpiEnableScaleFonts = 32768
imgui.ConfigFlags_IsSRGB = 1048576
imgui.ConfigFlags_IsTouchScreen = 2097152
imgui.ContextHookType_NewFramePre = 0
imgui.ContextHookType_NewFramePost = 1
imgui.ContextHookType_EndFramePre = 2
imgui.ContextHookType_EndFramePost = 3
imgui.ContextHookType_RenderPre = 4
imgui.ContextHookType_RenderPost = 5
imgui.ContextHookType_Shutdown = 6
imgui.ContextHookType_PendingRemoval_ = 7
imgui.DataAuthority_Auto = 0
imgui.DataAuthority_DockNode = 1
imgui.DataAuthority_Window = 2
imgui.DataType_String = 11
imgui.DataType_Pointer = 12
imgui.DataType_ID = 13
imgui.DataType_S8 = 0
imgui.DataType_U8 = 1
imgui.DataType_S16 = 2
imgui.DataType_U16 = 3
imgui.DataType_S32 = 4
imgui.DataType_U32 = 5
imgui.DataType_S64 = 6
imgui.DataType_U64 = 7
imgui.DataType_Float = 8
imgui.DataType_Double = 9
imgui.DataType_COUNT = 10
imgui.DebugLogFlags_None = 0
imgui.DebugLogFlags_EventActiveId = 1
imgui.DebugLogFlags_EventFocus = 2
imgui.DebugLogFlags_EventPopup = 4
imgui.DebugLogFlags_EventNav = 8
imgui.DebugLogFlags_EventClipper = 16
imgui.DebugLogFlags_EventSelection = 32
imgui.DebugLogFlags_EventIO = 64
imgui.DebugLogFlags_EventInputRouting = 128
imgui.DebugLogFlags_EventDocking = 256
imgui.DebugLogFlags_EventViewport = 512
imgui.DebugLogFlags_EventMask_ = 1023
imgui.DebugLogFlags_OutputToTTY = 1048576
imgui.DebugLogFlags_OutputToTestEngine = 2097152
imgui.Dir_None = -1
imgui.Dir_Left = 0
imgui.Dir_Right = 1
imgui.Dir_Up = 2
imgui.Dir_Down = 3
imgui.Dir_COUNT = 4
imgui.DockNodeFlags_DockSpace = 1024
imgui.DockNodeFlags_CentralNode = 2048
imgui.DockNodeFlags_NoTabBar = 4096
imgui.DockNodeFlags_HiddenTabBar = 8192
imgui.DockNodeFlags_NoWindowMenuButton = 16384
imgui.DockNodeFlags_NoCloseButton = 32768
imgui.DockNodeFlags_NoResizeX = 65536
imgui.DockNodeFlags_NoResizeY = 131072
imgui.DockNodeFlags_DockedWindowsInFocusRoute = 262144
imgui.DockNodeFlags_NoDockingSplitOther = 524288
imgui.DockNodeFlags_NoDockingOverMe = 1048576
imgui.DockNodeFlags_NoDockingOverOther = 2097152
imgui.DockNodeFlags_NoDockingOverEmpty = 4194304
imgui.DockNodeFlags_NoDocking = 7864336
imgui.DockNodeFlags_SharedFlagsInheritMask_ = -1
imgui.DockNodeFlags_NoResizeFlagsMask_ = 196640
imgui.DockNodeFlags_LocalFlagsTransferMask_ = 260208
imgui.DockNodeFlags_SavedFlagsMask_ = 261152
imgui.DockNodeFlags_None = 0
imgui.DockNodeFlags_KeepAliveOnly = 1
imgui.DockNodeFlags_NoDockingOverCentralNode = 4
imgui.DockNodeFlags_PassthruCentralNode = 8
imgui.DockNodeFlags_NoDockingSplit = 16
imgui.DockNodeFlags_NoResize = 32
imgui.DockNodeFlags_AutoHideTabBar = 64
imgui.DockNodeFlags_NoUndocking = 128
imgui.DockNodeState_Unknown = 0
imgui.DockNodeState_HostWindowHiddenBecauseSingleWindow = 1
imgui.DockNodeState_HostWindowHiddenBecauseWindowsAreResizing = 2
imgui.DockNodeState_HostWindowVisible = 3
imgui.DragDropFlags_None = 0
imgui.DragDropFlags_SourceNoPreviewTooltip = 1
imgui.DragDropFlags_SourceNoDisableHover = 2
imgui.DragDropFlags_SourceNoHoldToOpenOthers = 4
imgui.DragDropFlags_SourceAllowNullID = 8
imgui.DragDropFlags_SourceExtern = 16
imgui.DragDropFlags_PayloadAutoExpire = 32
imgui.DragDropFlags_PayloadNoCrossContext = 64
imgui.DragDropFlags_PayloadNoCrossProcess = 128
imgui.DragDropFlags_AcceptBeforeDelivery = 1024
imgui.DragDropFlags_AcceptNoDrawDefaultRect = 2048
imgui.DragDropFlags_AcceptNoPreviewTooltip = 4096
imgui.DragDropFlags_AcceptPeekOnly = 3072
imgui.FocusRequestFlags_None = 0
imgui.FocusRequestFlags_RestoreFocusedChild = 1
imgui.FocusRequestFlags_UnlessBelowModal = 2
imgui.FocusedFlags_None = 0
imgui.FocusedFlags_ChildWindows = 1
imgui.FocusedFlags_RootWindow = 2
imgui.FocusedFlags_AnyWindow = 4
imgui.FocusedFlags_NoPopupHierarchy = 8
imgui.FocusedFlags_DockHierarchy = 16
imgui.FocusedFlags_RootAndChildWindows = 3
imgui.FreeTypeBuilderFlags_NoHinting = 1
imgui.FreeTypeBuilderFlags_NoAutoHint = 2
imgui.FreeTypeBuilderFlags_ForceAutoHint = 4
imgui.FreeTypeBuilderFlags_LightHinting = 8
imgui.FreeTypeBuilderFlags_MonoHinting = 16
imgui.FreeTypeBuilderFlags_Bold = 32
imgui.FreeTypeBuilderFlags_Oblique = 64
imgui.FreeTypeBuilderFlags_Monochrome = 128
imgui.FreeTypeBuilderFlags_LoadColor = 256
imgui.FreeTypeBuilderFlags_Bitmap = 512
imgui.HoveredFlags_DelayMask_ = 245760
imgui.HoveredFlags_AllowedMaskForIsWindowHovered = 12479
imgui.HoveredFlags_AllowedMaskForIsItemHovered = 262048
imgui.HoveredFlags_None = 0
imgui.HoveredFlags_ChildWindows = 1
imgui.HoveredFlags_RootWindow = 2
imgui.HoveredFlags_AnyWindow = 4
imgui.HoveredFlags_NoPopupHierarchy = 8
imgui.HoveredFlags_DockHierarchy = 16
imgui.HoveredFlags_AllowWhenBlockedByPopup = 32
imgui.HoveredFlags_AllowWhenBlockedByActiveItem = 128
imgui.HoveredFlags_AllowWhenOverlappedByItem = 256
imgui.HoveredFlags_AllowWhenOverlappedByWindow = 512
imgui.HoveredFlags_AllowWhenDisabled = 1024
imgui.HoveredFlags_NoNavOverride = 2048
imgui.HoveredFlags_AllowWhenOverlapped = 768
imgui.HoveredFlags_RectOnly = 928
imgui.HoveredFlags_RootAndChildWindows = 3
imgui.HoveredFlags_ForTooltip = 4096
imgui.HoveredFlags_Stationary = 8192
imgui.HoveredFlags_DelayNone = 16384
imgui.HoveredFlags_DelayShort = 32768
imgui.HoveredFlags_DelayNormal = 65536
imgui.HoveredFlags_NoSharedDelay = 131072
imgui.InputEventType_None = 0
imgui.InputEventType_MousePos = 1
imgui.InputEventType_MouseWheel = 2
imgui.InputEventType_MouseButton = 3
imgui.InputEventType_MouseViewport = 4
imgui.InputEventType_Key = 5
imgui.InputEventType_Text = 6
imgui.InputEventType_Focus = 7
imgui.InputEventType_COUNT = 8
imgui.InputFlags_RepeatRateDefault = 2
imgui.InputFlags_RepeatRateNavMove = 4
imgui.InputFlags_RepeatRateNavTweak = 8
imgui.InputFlags_RepeatUntilRelease = 16
imgui.InputFlags_RepeatUntilKeyModsChange = 32
imgui.InputFlags_RepeatUntilKeyModsChangeFromNone = 64
imgui.InputFlags_RepeatUntilOtherKeyPress = 128
imgui.InputFlags_LockThisFrame = 1048576
imgui.InputFlags_LockUntilRelease = 2097152
imgui.InputFlags_CondHovered = 4194304
imgui.InputFlags_CondActive = 8388608
imgui.InputFlags_CondDefault_ = 12582912
imgui.InputFlags_RepeatRateMask_ = 14
imgui.InputFlags_RepeatUntilMask_ = 240
imgui.InputFlags_RepeatMask_ = 255
imgui.InputFlags_CondMask_ = 12582912
imgui.InputFlags_RouteTypeMask_ = 15360
imgui.InputFlags_RouteOptionsMask_ = 245760
imgui.InputFlags_SupportedByIsKeyPressed = 255
imgui.InputFlags_SupportedByIsMouseClicked = 1
imgui.InputFlags_SupportedByShortcut = 261375
imgui.InputFlags_SupportedBySetNextItemShortcut = 523519
imgui.InputFlags_SupportedBySetKeyOwner = 3145728
imgui.InputFlags_SupportedBySetItemKeyOwner = 15728640
imgui.InputFlags_None = 0
imgui.InputFlags_Repeat = 1
imgui.InputFlags_RouteActive = 1024
imgui.InputFlags_RouteFocused = 2048
imgui.InputFlags_RouteGlobal = 4096
imgui.InputFlags_RouteAlways = 8192
imgui.InputFlags_RouteOverFocused = 16384
imgui.InputFlags_RouteOverActive = 32768
imgui.InputFlags_RouteUnlessBgFocused = 65536
imgui.InputFlags_RouteFromRootWindow = 131072
imgui.InputFlags_Tooltip = 262144
imgui.InputSource_None = 0
imgui.InputSource_Mouse = 1
imgui.InputSource_Keyboard = 2
imgui.InputSource_Gamepad = 3
imgui.InputSource_COUNT = 4
imgui.InputTextFlags_Multiline = 67108864
imgui.InputTextFlags_NoMarkEdited = 134217728
imgui.InputTextFlags_MergedItem = 268435456
imgui.InputTextFlags_LocalizeDecimalPoint = 536870912
imgui.InputTextFlags_None = 0
imgui.InputTextFlags_CharsDecimal = 1
imgui.InputTextFlags_CharsHexadecimal = 2
imgui.InputTextFlags_CharsScientific = 4
imgui.InputTextFlags_CharsUppercase = 8
imgui.InputTextFlags_CharsNoBlank = 16
imgui.InputTextFlags_AllowTabInput = 32
imgui.InputTextFlags_EnterReturnsTrue = 64
imgui.InputTextFlags_EscapeClearsAll = 128
imgui.InputTextFlags_CtrlEnterForNewLine = 256
imgui.InputTextFlags_ReadOnly = 512
imgui.InputTextFlags_Password = 1024
imgui.InputTextFlags_AlwaysOverwrite = 2048
imgui.InputTextFlags_AutoSelectAll = 4096
imgui.InputTextFlags_ParseEmptyRefVal = 8192
imgui.InputTextFlags_DisplayEmptyRefVal = 16384
imgui.InputTextFlags_NoHorizontalScroll = 32768
imgui.InputTextFlags_NoUndoRedo = 65536
imgui.InputTextFlags_CallbackCompletion = 131072
imgui.InputTextFlags_CallbackHistory = 262144
imgui.InputTextFlags_CallbackAlways = 524288
imgui.InputTextFlags_CallbackCharFilter = 1048576
imgui.InputTextFlags_CallbackResize = 2097152
imgui.InputTextFlags_CallbackEdit = 4194304
imgui.ItemFlags_None = 0
imgui.ItemFlags_NoTabStop = 1
imgui.ItemFlags_ButtonRepeat = 2
imgui.ItemFlags_Disabled = 4
imgui.ItemFlags_NoNav = 8
imgui.ItemFlags_NoNavDefaultFocus = 16
imgui.ItemFlags_SelectableDontClosePopup = 32
imgui.ItemFlags_MixedValue = 64
imgui.ItemFlags_ReadOnly = 128
imgui.ItemFlags_NoWindowHoverableCheck = 256
imgui.ItemFlags_AllowOverlap = 512
imgui.ItemFlags_Inputable = 1024
imgui.ItemFlags_HasSelectionUserData = 2048
imgui.ItemStatusFlags_None = 0
imgui.ItemStatusFlags_HoveredRect = 1
imgui.ItemStatusFlags_HasDisplayRect = 2
imgui.ItemStatusFlags_Edited = 4
imgui.ItemStatusFlags_ToggledSelection = 8
imgui.ItemStatusFlags_ToggledOpen = 16
imgui.ItemStatusFlags_HasDeactivated = 32
imgui.ItemStatusFlags_Deactivated = 64
imgui.ItemStatusFlags_HoveredWindow = 128
imgui.ItemStatusFlags_Visible = 256
imgui.ItemStatusFlags_HasClipRect = 512
imgui.ItemStatusFlags_HasShortcut = 1024
imgui.Key_None = 0
imgui.Key_Tab = 512
imgui.Key_LeftArrow = 513
imgui.Key_RightArrow = 514
imgui.Key_UpArrow = 515
imgui.Key_DownArrow = 516
imgui.Key_PageUp = 517
imgui.Key_PageDown = 518
imgui.Key_Home = 519
imgui.Key_End = 520
imgui.Key_Insert = 521
imgui.Key_Delete = 522
imgui.Key_Backspace = 523
imgui.Key_Space = 524
imgui.Key_Enter = 525
imgui.Key_Escape = 526
imgui.Key_LeftCtrl = 527
imgui.Key_LeftShift = 528
imgui.Key_LeftAlt = 529
imgui.Key_LeftSuper = 530
imgui.Key_RightCtrl = 531
imgui.Key_RightShift = 532
imgui.Key_RightAlt = 533
imgui.Key_RightSuper = 534
imgui.Key_Menu = 535
imgui.Key_0 = 536
imgui.Key_1 = 537
imgui.Key_2 = 538
imgui.Key_3 = 539
imgui.Key_4 = 540
imgui.Key_5 = 541
imgui.Key_6 = 542
imgui.Key_7 = 543
imgui.Key_8 = 544
imgui.Key_9 = 545
imgui.Key_A = 546
imgui.Key_B = 547
imgui.Key_C = 548
imgui.Key_D = 549
imgui.Key_E = 550
imgui.Key_F = 551
imgui.Key_G = 552
imgui.Key_H = 553
imgui.Key_I = 554
imgui.Key_J = 555
imgui.Key_K = 556
imgui.Key_L = 557
imgui.Key_M = 558
imgui.Key_N = 559
imgui.Key_O = 560
imgui.Key_P = 561
imgui.Key_Q = 562
imgui.Key_R = 563
imgui.Key_S = 564
imgui.Key_T = 565
imgui.Key_U = 566
imgui.Key_V = 567
imgui.Key_W = 568
imgui.Key_X = 569
imgui.Key_Y = 570
imgui.Key_Z = 571
imgui.Key_F1 = 572
imgui.Key_F2 = 573
imgui.Key_F3 = 574
imgui.Key_F4 = 575
imgui.Key_F5 = 576
imgui.Key_F6 = 577
imgui.Key_F7 = 578
imgui.Key_F8 = 579
imgui.Key_F9 = 580
imgui.Key_F10 = 581
imgui.Key_F11 = 582
imgui.Key_F12 = 583
imgui.Key_F13 = 584
imgui.Key_F14 = 585
imgui.Key_F15 = 586
imgui.Key_F16 = 587
imgui.Key_F17 = 588
imgui.Key_F18 = 589
imgui.Key_F19 = 590
imgui.Key_F20 = 591
imgui.Key_F21 = 592
imgui.Key_F22 = 593
imgui.Key_F23 = 594
imgui.Key_F24 = 595
imgui.Key_Apostrophe = 596
imgui.Key_Comma = 597
imgui.Key_Minus = 598
imgui.Key_Period = 599
imgui.Key_Slash = 600
imgui.Key_Semicolon = 601
imgui.Key_Equal = 602
imgui.Key_LeftBracket = 603
imgui.Key_Backslash = 604
imgui.Key_RightBracket = 605
imgui.Key_GraveAccent = 606
imgui.Key_CapsLock = 607
imgui.Key_ScrollLock = 608
imgui.Key_NumLock = 609
imgui.Key_PrintScreen = 610
imgui.Key_Pause = 611
imgui.Key_Keypad0 = 612
imgui.Key_Keypad1 = 613
imgui.Key_Keypad2 = 614
imgui.Key_Keypad3 = 615
imgui.Key_Keypad4 = 616
imgui.Key_Keypad5 = 617
imgui.Key_Keypad6 = 618
imgui.Key_Keypad7 = 619
imgui.Key_Keypad8 = 620
imgui.Key_Keypad9 = 621
imgui.Key_KeypadDecimal = 622
imgui.Key_KeypadDivide = 623
imgui.Key_KeypadMultiply = 624
imgui.Key_KeypadSubtract = 625
imgui.Key_KeypadAdd = 626
imgui.Key_KeypadEnter = 627
imgui.Key_KeypadEqual = 628
imgui.Key_AppBack = 629
imgui.Key_AppForward = 630
imgui.Key_GamepadStart = 631
imgui.Key_GamepadBack = 632
imgui.Key_GamepadFaceLeft = 633
imgui.Key_GamepadFaceRight = 634
imgui.Key_GamepadFaceUp = 635
imgui.Key_GamepadFaceDown = 636
imgui.Key_GamepadDpadLeft = 637
imgui.Key_GamepadDpadRight = 638
imgui.Key_GamepadDpadUp = 639
imgui.Key_GamepadDpadDown = 640
imgui.Key_GamepadL1 = 641
imgui.Key_GamepadR1 = 642
imgui.Key_GamepadL2 = 643
imgui.Key_GamepadR2 = 644
imgui.Key_GamepadL3 = 645
imgui.Key_GamepadR3 = 646
imgui.Key_GamepadLStickLeft = 647
imgui.Key_GamepadLStickRight = 648
imgui.Key_GamepadLStickUp = 649
imgui.Key_GamepadLStickDown = 650
imgui.Key_GamepadRStickLeft = 651
imgui.Key_GamepadRStickRight = 652
imgui.Key_GamepadRStickUp = 653
imgui.Key_GamepadRStickDown = 654
imgui.Key_MouseLeft = 655
imgui.Key_MouseRight = 656
imgui.Key_MouseMiddle = 657
imgui.Key_MouseX1 = 658
imgui.Key_MouseX2 = 659
imgui.Key_MouseWheelX = 660
imgui.Key_MouseWheelY = 661
imgui.Key_ReservedForModCtrl = 662
imgui.Key_ReservedForModShift = 663
imgui.Key_ReservedForModAlt = 664
imgui.Key_ReservedForModSuper = 665
imgui.Key_COUNT = 666
imgui.Mod_None = 0
imgui.Mod_Ctrl = 4096
imgui.Mod_Shift = 8192
imgui.Mod_Alt = 16384
imgui.Mod_Super = 32768
imgui.Mod_Mask_ = 61440
imgui.Key_NamedKey_BEGIN = 512
imgui.Key_NamedKey_END = 666
imgui.Key_NamedKey_COUNT = 154
imgui.Key_KeysData_SIZE = 154
imgui.Key_KeysData_OFFSET = 512
imgui.LayoutType_Horizontal = 0
imgui.LayoutType_Vertical = 1
imgui.LocKey_VersionStr = 0
imgui.LocKey_TableSizeOne = 1
imgui.LocKey_TableSizeAllFit = 2
imgui.LocKey_TableSizeAllDefault = 3
imgui.LocKey_TableResetOrder = 4
imgui.LocKey_WindowingMainMenuBar = 5
imgui.LocKey_WindowingPopup = 6
imgui.LocKey_WindowingUntitled = 7
imgui.LocKey_DockingHideTabBar = 8
imgui.LocKey_DockingHoldShiftToDock = 9
imgui.LocKey_DockingDragToUndockOrMoveNode = 10
imgui.LocKey_COUNT = 11
imgui.LogType_None = 0
imgui.LogType_TTY = 1
imgui.LogType_File = 2
imgui.LogType_Buffer = 3
imgui.LogType_Clipboard = 4
imgui.MouseButton_Left = 0
imgui.MouseButton_Right = 1
imgui.MouseButton_Middle = 2
imgui.MouseButton_COUNT = 5
imgui.MouseCursor_None = -1
imgui.MouseCursor_Arrow = 0
imgui.MouseCursor_TextInput = 1
imgui.MouseCursor_ResizeAll = 2
imgui.MouseCursor_ResizeNS = 3
imgui.MouseCursor_ResizeEW = 4
imgui.MouseCursor_ResizeNESW = 5
imgui.MouseCursor_ResizeNWSE = 6
imgui.MouseCursor_Hand = 7
imgui.MouseCursor_NotAllowed = 8
imgui.MouseCursor_COUNT = 9
imgui.MouseSource_Mouse = 0
imgui.MouseSource_TouchScreen = 1
imgui.MouseSource_Pen = 2
imgui.MouseSource_COUNT = 3
imgui.NavHighlightFlags_None = 0
imgui.NavHighlightFlags_Compact = 2
imgui.NavHighlightFlags_AlwaysDraw = 4
imgui.NavHighlightFlags_NoRounding = 8
imgui.NavLayer_Main = 0
imgui.NavLayer_Menu = 1
imgui.NavLayer_COUNT = 2
imgui.NavMoveFlags_None = 0
imgui.NavMoveFlags_LoopX = 1
imgui.NavMoveFlags_LoopY = 2
imgui.NavMoveFlags_WrapX = 4
imgui.NavMoveFlags_WrapY = 8
imgui.NavMoveFlags_WrapMask_ = 15
imgui.NavMoveFlags_AllowCurrentNavId = 16
imgui.NavMoveFlags_AlsoScoreVisibleSet = 32
imgui.NavMoveFlags_ScrollToEdgeY = 64
imgui.NavMoveFlags_Forwarded = 128
imgui.NavMoveFlags_DebugNoResult = 256
imgui.NavMoveFlags_FocusApi = 512
imgui.NavMoveFlags_IsTabbing = 1024
imgui.NavMoveFlags_IsPageMove = 2048
imgui.NavMoveFlags_Activate = 4096
imgui.NavMoveFlags_NoSelect = 8192
imgui.NavMoveFlags_NoSetNavHighlight = 16384
imgui.NavMoveFlags_NoClearActiveId = 32768
imgui.NextItemDataFlags_None = 0
imgui.NextItemDataFlags_HasWidth = 1
imgui.NextItemDataFlags_HasOpen = 2
imgui.NextItemDataFlags_HasShortcut = 4
imgui.NextItemDataFlags_HasRefVal = 8
imgui.NextWindowDataFlags_None = 0
imgui.NextWindowDataFlags_HasPos = 1
imgui.NextWindowDataFlags_HasSize = 2
imgui.NextWindowDataFlags_HasContentSize = 4
imgui.NextWindowDataFlags_HasCollapsed = 8
imgui.NextWindowDataFlags_HasSizeConstraint = 16
imgui.NextWindowDataFlags_HasFocus = 32
imgui.NextWindowDataFlags_HasBgAlpha = 64
imgui.NextWindowDataFlags_HasScroll = 128
imgui.NextWindowDataFlags_HasChildFlags = 256
imgui.NextWindowDataFlags_HasRefreshPolicy = 512
imgui.NextWindowDataFlags_HasViewport = 1024
imgui.NextWindowDataFlags_HasDock = 2048
imgui.NextWindowDataFlags_HasWindowClass = 4096
imgui.OldColumnFlags_None = 0
imgui.OldColumnFlags_NoBorder = 1
imgui.OldColumnFlags_NoResize = 2
imgui.OldColumnFlags_NoPreserveWidths = 4
imgui.OldColumnFlags_NoForceWithinWindow = 8
imgui.OldColumnFlags_GrowParentContentsSize = 16
imgui.PlotType_Lines = 0
imgui.PlotType_Histogram = 1
imgui.PopupFlags_None = 0
imgui.PopupFlags_MouseButtonLeft = 0
imgui.PopupFlags_MouseButtonRight = 1
imgui.PopupFlags_MouseButtonMiddle = 2
imgui.PopupFlags_MouseButtonMask_ = 31
imgui.PopupFlags_MouseButtonDefault_ = 1
imgui.PopupFlags_NoReopen = 32
imgui.PopupFlags_NoOpenOverExistingPopup = 128
imgui.PopupFlags_NoOpenOverItems = 256
imgui.PopupFlags_AnyPopupId = 1024
imgui.PopupFlags_AnyPopupLevel = 2048
imgui.PopupFlags_AnyPopup = 3072
imgui.PopupPositionPolicy_Default = 0
imgui.PopupPositionPolicy_ComboBox = 1
imgui.PopupPositionPolicy_Tooltip = 2
imgui.ScrollFlags_None = 0
imgui.ScrollFlags_KeepVisibleEdgeX = 1
imgui.ScrollFlags_KeepVisibleEdgeY = 2
imgui.ScrollFlags_KeepVisibleCenterX = 4
imgui.ScrollFlags_KeepVisibleCenterY = 8
imgui.ScrollFlags_AlwaysCenterX = 16
imgui.ScrollFlags_AlwaysCenterY = 32
imgui.ScrollFlags_NoScrollParent = 64
imgui.ScrollFlags_MaskX_ = 21
imgui.ScrollFlags_MaskY_ = 42
imgui.SelectableFlags_NoHoldingActiveID = 1048576
imgui.SelectableFlags_SelectOnNav = 2097152
imgui.SelectableFlags_SelectOnClick = 4194304
imgui.SelectableFlags_SelectOnRelease = 8388608
imgui.SelectableFlags_SpanAvailWidth = 16777216
imgui.SelectableFlags_SetNavIdOnHover = 33554432
imgui.SelectableFlags_NoPadWithHalfSpacing = 67108864
imgui.SelectableFlags_NoSetKeyOwner = 134217728
imgui.SelectableFlags_None = 0
imgui.SelectableFlags_DontClosePopups = 1
imgui.SelectableFlags_SpanAllColumns = 2
imgui.SelectableFlags_AllowDoubleClick = 4
imgui.SelectableFlags_Disabled = 8
imgui.SelectableFlags_AllowOverlap = 16
imgui.SeparatorFlags_None = 0
imgui.SeparatorFlags_Horizontal = 1
imgui.SeparatorFlags_Vertical = 2
imgui.SeparatorFlags_SpanAllColumns = 4
imgui.SliderFlags_Vertical = 1048576
imgui.SliderFlags_ReadOnly = 2097152
imgui.SliderFlags_None = 0
imgui.SliderFlags_AlwaysClamp = 16
imgui.SliderFlags_Logarithmic = 32
imgui.SliderFlags_NoRoundToFormat = 64
imgui.SliderFlags_NoInput = 128
imgui.SliderFlags_WrapAround = 256
imgui.SliderFlags_InvalidMask_ = 1879048207
imgui.SortDirection_None = 0
imgui.SortDirection_Ascending = 1
imgui.SortDirection_Descending = 2
imgui.StyleVar_Alpha = 0
imgui.StyleVar_DisabledAlpha = 1
imgui.StyleVar_WindowPadding = 2
imgui.StyleVar_WindowRounding = 3
imgui.StyleVar_WindowBorderSize = 4
imgui.StyleVar_WindowMinSize = 5
imgui.StyleVar_WindowTitleAlign = 6
imgui.StyleVar_ChildRounding = 7
imgui.StyleVar_ChildBorderSize = 8
imgui.StyleVar_PopupRounding = 9
imgui.StyleVar_PopupBorderSize = 10
imgui.StyleVar_FramePadding = 11
imgui.StyleVar_FrameRounding = 12
imgui.StyleVar_FrameBorderSize = 13
imgui.StyleVar_ItemSpacing = 14
imgui.StyleVar_ItemInnerSpacing = 15
imgui.StyleVar_IndentSpacing = 16
imgui.StyleVar_CellPadding = 17
imgui.StyleVar_ScrollbarSize = 18
imgui.StyleVar_ScrollbarRounding = 19
imgui.StyleVar_GrabMinSize = 20
imgui.StyleVar_GrabRounding = 21
imgui.StyleVar_TabRounding = 22
imgui.StyleVar_TabBorderSize = 23
imgui.StyleVar_TabBarBorderSize = 24
imgui.StyleVar_TableAngledHeadersAngle = 25
imgui.StyleVar_TableAngledHeadersTextAlign = 26
imgui.StyleVar_ButtonTextAlign = 27
imgui.StyleVar_SelectableTextAlign = 28
imgui.StyleVar_SeparatorTextBorderSize = 29
imgui.StyleVar_SeparatorTextAlign = 30
imgui.StyleVar_SeparatorTextPadding = 31
imgui.StyleVar_DockingSeparatorSize = 32
imgui.StyleVar_COUNT = 33
imgui.TabBarFlags_DockNode = 1048576
imgui.TabBarFlags_IsFocused = 2097152
imgui.TabBarFlags_SaveSettings = 4194304
imgui.TabBarFlags_None = 0
imgui.TabBarFlags_Reorderable = 1
imgui.TabBarFlags_AutoSelectNewTabs = 2
imgui.TabBarFlags_TabListPopupButton = 4
imgui.TabBarFlags_NoCloseWithMiddleMouseButton = 8
imgui.TabBarFlags_NoTabListScrollingButtons = 16
imgui.TabBarFlags_NoTooltip = 32
imgui.TabBarFlags_DrawSelectedOverline = 64
imgui.TabBarFlags_FittingPolicyResizeDown = 128
imgui.TabBarFlags_FittingPolicyScroll = 256
imgui.TabBarFlags_FittingPolicyMask_ = 384
imgui.TabBarFlags_FittingPolicyDefault_ = 128
imgui.TabItemFlags_SectionMask_ = 192
imgui.TabItemFlags_NoCloseButton = 1048576
imgui.TabItemFlags_Button = 2097152
imgui.TabItemFlags_Unsorted = 4194304
imgui.TabItemFlags_None = 0
imgui.TabItemFlags_UnsavedDocument = 1
imgui.TabItemFlags_SetSelected = 2
imgui.TabItemFlags_NoCloseWithMiddleMouseButton = 4
imgui.TabItemFlags_NoPushId = 8
imgui.TabItemFlags_NoTooltip = 16
imgui.TabItemFlags_NoReorder = 32
imgui.TabItemFlags_Leading = 64
imgui.TabItemFlags_Trailing = 128
imgui.TabItemFlags_NoAssumedClosure = 256
imgui.TableBgTarget_None = 0
imgui.TableBgTarget_RowBg0 = 1
imgui.TableBgTarget_RowBg1 = 2
imgui.TableBgTarget_CellBg = 3
imgui.TableColumnFlags_None = 0
imgui.TableColumnFlags_Disabled = 1
imgui.TableColumnFlags_DefaultHide = 2
imgui.TableColumnFlags_DefaultSort = 4
imgui.TableColumnFlags_WidthStretch = 8
imgui.TableColumnFlags_WidthFixed = 16
imgui.TableColumnFlags_NoResize = 32
imgui.TableColumnFlags_NoReorder = 64
imgui.TableColumnFlags_NoHide = 128
imgui.TableColumnFlags_NoClip = 256
imgui.TableColumnFlags_NoSort = 512
imgui.TableColumnFlags_NoSortAscending = 1024
imgui.TableColumnFlags_NoSortDescending = 2048
imgui.TableColumnFlags_NoHeaderLabel = 4096
imgui.TableColumnFlags_NoHeaderWidth = 8192
imgui.TableColumnFlags_PreferSortAscending = 16384
imgui.TableColumnFlags_PreferSortDescending = 32768
imgui.TableColumnFlags_IndentEnable = 65536
imgui.TableColumnFlags_IndentDisable = 131072
imgui.TableColumnFlags_AngledHeader = 262144
imgui.TableColumnFlags_IsEnabled = 16777216
imgui.TableColumnFlags_IsVisible = 33554432
imgui.TableColumnFlags_IsSorted = 67108864
imgui.TableColumnFlags_IsHovered = 134217728
imgui.TableColumnFlags_WidthMask_ = 24
imgui.TableColumnFlags_IndentMask_ = 196608
imgui.TableColumnFlags_StatusMask_ = 251658240
imgui.TableColumnFlags_NoDirectResize_ = 1073741824
imgui.TableFlags_None = 0
imgui.TableFlags_Resizable = 1
imgui.TableFlags_Reorderable = 2
imgui.TableFlags_Hideable = 4
imgui.TableFlags_Sortable = 8
imgui.TableFlags_NoSavedSettings = 16
imgui.TableFlags_ContextMenuInBody = 32
imgui.TableFlags_RowBg = 64
imgui.TableFlags_BordersInnerH = 128
imgui.TableFlags_BordersOuterH = 256
imgui.TableFlags_BordersInnerV = 512
imgui.TableFlags_BordersOuterV = 1024
imgui.TableFlags_BordersH = 384
imgui.TableFlags_BordersV = 1536
imgui.TableFlags_BordersInner = 640
imgui.TableFlags_BordersOuter = 1280
imgui.TableFlags_Borders = 1920
imgui.TableFlags_NoBordersInBody = 2048
imgui.TableFlags_NoBordersInBodyUntilResize = 4096
imgui.TableFlags_SizingFixedFit = 8192
imgui.TableFlags_SizingFixedSame = 16384
imgui.TableFlags_SizingStretchProp = 24576
imgui.TableFlags_SizingStretchSame = 32768
imgui.TableFlags_NoHostExtendX = 65536
imgui.TableFlags_NoHostExtendY = 131072
imgui.TableFlags_NoKeepColumnsVisible = 262144
imgui.TableFlags_PreciseWidths = 524288
imgui.TableFlags_NoClip = 1048576
imgui.TableFlags_PadOuterX = 2097152
imgui.TableFlags_NoPadOuterX = 4194304
imgui.TableFlags_NoPadInnerX = 8388608
imgui.TableFlags_ScrollX = 16777216
imgui.TableFlags_ScrollY = 33554432
imgui.TableFlags_SortMulti = 67108864
imgui.TableFlags_SortTristate = 134217728
imgui.TableFlags_HighlightHoveredColumn = 268435456
imgui.TableFlags_SizingMask_ = 57344
imgui.TableRowFlags_None = 0
imgui.TableRowFlags_Headers = 1
imgui.TextFlags_None = 0
imgui.TextFlags_NoWidthForLargeClippedText = 1
imgui.TooltipFlags_None = 0
imgui.TooltipFlags_OverridePrevious = 2
imgui.TreeNodeFlags_ClipLabelForTrailingButton = 1048576
imgui.TreeNodeFlags_UpsideDownArrow = 2097152
imgui.TreeNodeFlags_None = 0
imgui.TreeNodeFlags_Selected = 1
imgui.TreeNodeFlags_Framed = 2
imgui.TreeNodeFlags_AllowOverlap = 4
imgui.TreeNodeFlags_NoTreePushOnOpen = 8
imgui.TreeNodeFlags_NoAutoOpenOnLog = 16
imgui.TreeNodeFlags_DefaultOpen = 32
imgui.TreeNodeFlags_OpenOnDoubleClick = 64
imgui.TreeNodeFlags_OpenOnArrow = 128
imgui.TreeNodeFlags_Leaf = 256
imgui.TreeNodeFlags_Bullet = 512
imgui.TreeNodeFlags_FramePadding = 1024
imgui.TreeNodeFlags_SpanAvailWidth = 2048
imgui.TreeNodeFlags_SpanFullWidth = 4096
imgui.TreeNodeFlags_SpanTextWidth = 8192
imgui.TreeNodeFlags_SpanAllColumns = 16384
imgui.TreeNodeFlags_NavLeftJumpsBackHere = 32768
imgui.TreeNodeFlags_CollapsingHeader = 26
imgui.TypingSelectFlags_None = 0
imgui.TypingSelectFlags_AllowBackspace = 1
imgui.TypingSelectFlags_AllowSingleCharMode = 2
imgui.ViewportFlags_None = 0
imgui.ViewportFlags_IsPlatformWindow = 1
imgui.ViewportFlags_IsPlatformMonitor = 2
imgui.ViewportFlags_OwnedByApp = 4
imgui.ViewportFlags_NoDecoration = 8
imgui.ViewportFlags_NoTaskBarIcon = 16
imgui.ViewportFlags_NoFocusOnAppearing = 32
imgui.ViewportFlags_NoFocusOnClick = 64
imgui.ViewportFlags_NoInputs = 128
imgui.ViewportFlags_NoRendererClear = 256
imgui.ViewportFlags_NoAutoMerge = 512
imgui.ViewportFlags_TopMost = 1024
imgui.ViewportFlags_CanHostOtherWindows = 2048
imgui.ViewportFlags_IsMinimized = 4096
imgui.ViewportFlags_IsFocused = 8192
imgui.WindowDockStyleCol_Text = 0
imgui.WindowDockStyleCol_TabHovered = 1
imgui.WindowDockStyleCol_TabFocused = 2
imgui.WindowDockStyleCol_TabSelected = 3
imgui.WindowDockStyleCol_TabSelectedOverline = 4
imgui.WindowDockStyleCol_TabDimmed = 5
imgui.WindowDockStyleCol_TabDimmedSelected = 6
imgui.WindowDockStyleCol_TabDimmedSelectedOverline = 7
imgui.WindowDockStyleCol_COUNT = 8
imgui.WindowFlags_None = 0
imgui.WindowFlags_NoTitleBar = 1
imgui.WindowFlags_NoResize = 2
imgui.WindowFlags_NoMove = 4
imgui.WindowFlags_NoScrollbar = 8
imgui.WindowFlags_NoScrollWithMouse = 16
imgui.WindowFlags_NoCollapse = 32
imgui.WindowFlags_AlwaysAutoResize = 64
imgui.WindowFlags_NoBackground = 128
imgui.WindowFlags_NoSavedSettings = 256
imgui.WindowFlags_NoMouseInputs = 512
imgui.WindowFlags_MenuBar = 1024
imgui.WindowFlags_HorizontalScrollbar = 2048
imgui.WindowFlags_NoFocusOnAppearing = 4096
imgui.WindowFlags_NoBringToFrontOnFocus = 8192
imgui.WindowFlags_AlwaysVerticalScrollbar = 16384
imgui.WindowFlags_AlwaysHorizontalScrollbar = 32768
imgui.WindowFlags_NoNavInputs = 65536
imgui.WindowFlags_NoNavFocus = 131072
imgui.WindowFlags_UnsavedDocument = 262144
imgui.WindowFlags_NoDocking = 524288
imgui.WindowFlags_NoNav = 196608
imgui.WindowFlags_NoDecoration = 43
imgui.WindowFlags_NoInputs = 197120
imgui.WindowFlags_ChildWindow = 16777216
imgui.WindowFlags_Tooltip = 33554432
imgui.WindowFlags_Popup = 67108864
imgui.WindowFlags_Modal = 134217728
imgui.WindowFlags_ChildMenu = 268435456
imgui.WindowFlags_DockNodeHost = 536870912
imgui.WindowRefreshFlags_None = 0
imgui.WindowRefreshFlags_TryToAvoidRefresh = 1
imgui.WindowRefreshFlags_RefreshOnHover = 2
imgui.WindowRefreshFlags_RefreshOnFocus = 4

return imgui
