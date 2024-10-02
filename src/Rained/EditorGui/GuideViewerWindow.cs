using ImGuiNET;
using System.Numerics;
namespace Rained.EditorGui;

// fancy class to show guide.md in the rained window
static class GuideViewerWindow
{
    public const string WindowTitle = "Guide";
    public static bool IsWindowOpen = false;
    
    abstract record class DisplayCommand;
    record class DisplayHeader(int Level, string Text) : DisplayCommand;
    record class DisplayBulletPoint() : DisplayCommand;
    record class DisplayBoldText(string Text) : DisplayCommand; // yeah uh... i can't actually make text bold... this just adds an underline under them.
    record class DisplayText(string Text) : DisplayCommand;
    record class DisplayPreformatted(string Text) : DisplayCommand; // just adds a gray background behind the text
    record class DisplayLink(string Text, string Link) : DisplayCommand;
    record class NewParagraph() : DisplayCommand;

    private static bool needUpdateMarkdown = true;
    private static readonly List<(string, List<DisplayCommand>)> pages = [];
    private static int selectedPage = 0;

    public static void ShowWindow()
    {
        if (IsWindowOpen)
        {
            if (needUpdateMarkdown)
            {
                needUpdateMarkdown = false;
#if DEBUG
                ProcessMarkdown(Path.Combine(Boot.AppDataPath, "dist","GUIDE.md"));
#else
                ProcessMarkdown(Path.Combine(Boot.AppDataPath, "GUIDE.md"));
#endif
            }

            ImGui.SetNextWindowSize(new Vector2(50.0f, 36.0f) * ImGui.GetFontSize(), ImGuiCond.FirstUseEver);

            //ImGui.PushTextWrapPos(ImGui.GetWindowWidth() - 1.0f);
            if (ImGui.Begin(WindowTitle, ref IsWindowOpen, ImGuiWindowFlags.None))
            {
                // show navigation sidebar
                ImGui.BeginChild("Nav", new Vector2(ImGui.GetTextLineHeight() * 12.0f, ImGui.GetContentRegionAvail().Y), ImGuiChildFlags.Border);
                {
                    for (int i = 0; i < pages.Count; i++)
                    {
                        if (ImGui.Selectable(pages[i].Item1, i == selectedPage))
                        {
                            selectedPage = i;
                        }
                    }
                }
                ImGui.EndChild();

                ImGui.SameLine();
                ImGui.BeginChild("PageContents", ImGui.GetContentRegionAvail());

                var drawList = ImGui.GetWindowDrawList();
                Vector2 drawOrigin = ImGui.GetCursorScreenPos();
                Vector2 drawCursor = drawOrigin;
                float textWrap = drawOrigin.X + ImGui.GetContentRegionAvail().X;
                float textLeftAnchor = drawOrigin.X;

                void DrawText(string text, bool underline = false)
                {
                    var textColor = ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[(int)ImGuiCol.Text]);
                    string word = "";

                    for (int i = 0; i <= text.Length; i++)
                    {
                        if (i < text.Length)
                        {
                            word += text[i];
                        }

                        var textWidth = ImGui.CalcTextSize(word).X;
                        if (i >= text.Length || char.IsWhiteSpace(text[i]))
                        {
                            if (drawCursor.X + textWidth >= textWrap)
                            {
                                drawCursor.X = textLeftAnchor;
                                drawCursor.Y += ImGui.GetTextLineHeight();
                            }

                            drawList.AddText(drawCursor, textColor, word);

                            if (underline)
                            {
                                drawList.AddLine(
                                    drawCursor + new Vector2(0f, ImGui.GetFontSize()),
                                    drawCursor + new Vector2(textWidth, ImGui.GetFontSize()),
                                    textColor
                                );
                            }

                            drawCursor.X += textWidth;
                            word = "";
                        }
                    }
                }

                int buttonId = 0;
                void DrawLink(string text, string link)
                {
                    var textColor = ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered]);
                    string word = "";
                    bool isLinkPressed = false;

                    List<(Vector2, Vector2, string)> words = [];

                    for (int i = 0; i <= text.Length; i++)
                    {
                        if (i < text.Length)
                        {
                            word += text[i];
                        }

                        var wordSize = ImGui.CalcTextSize(word);
                        if (i >= text.Length || char.IsWhiteSpace(text[i]))
                        {
                            if (drawCursor.X + wordSize.X >= textWrap)
                            {
                                drawCursor.X = textLeftAnchor;
                                drawCursor.Y += ImGui.GetTextLineHeight();
                            }

                            ImGui.PushID(buttonId++);
                            ImGui.SetCursorPos(drawCursor - drawOrigin);
                            if (ImGui.InvisibleButton("link", wordSize))
                                isLinkPressed = true;

                            if (ImGui.IsItemHovered())
                                textColor = ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);
                            
                            words.Add((drawCursor, wordSize, word));
                            drawCursor.X += wordSize.X;
                            word = "";
                            ImGui.PopID();
                        }
                    }

                    // draw words after the link buttons
                    foreach (var w in words)
                    {
                        drawList.AddText(w.Item1, textColor, w.Item3);
                        drawList.AddLine(
                            w.Item1 + new Vector2(0f, ImGui.GetFontSize()),
                            w.Item1 + new Vector2(w.Item2.X, ImGui.GetFontSize()),
                            textColor
                        );
                    }

                    if (isLinkPressed)
                    {
                        if (!Platform.OpenURL(link))
                        {
                            Log.Error("Could not open URL on platform");
                        }
                    }
                }
                
                foreach (var cmd in pages[selectedPage].Item2)
                {
                    switch (cmd)
                    {
                        case DisplayHeader header:
                            ImGui.SetCursorPos(drawCursor - drawOrigin);
                            ImGui.SeparatorText(header.Text);
                            drawCursor.Y += ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y;
                            drawCursor.X = drawOrigin.X;
                            break;

                        case DisplayBulletPoint:
                            drawCursor.X = drawOrigin.X;
                            drawCursor.Y += ImGui.GetTextLineHeightWithSpacing();
                            ImGui.SetCursorPos(drawCursor - drawOrigin);
                            ImGui.Bullet();
                            drawCursor.X += ImGui.GetFontSize() + ImGui.GetStyle().ItemSpacing.X;
                            textLeftAnchor = drawCursor.X;
                            break;

                        case DisplayBoldText boldText:
                            DrawText(boldText.Text, true);
                            break;

                        case DisplayText displayText:
                            DrawText(displayText.Text);
                            break;

                        case DisplayPreformatted preformatted:
                            DrawText("<" + preformatted.Text + ">");
                            break;

                        case DisplayLink link:
                            ImGui.SetCursorPos(drawCursor - drawOrigin);
                            DrawLink(link.Text, link.Link);
                            break;

                        case NewParagraph:
                            textLeftAnchor = drawOrigin.X;
                            drawCursor.X = textLeftAnchor;
                            drawCursor.Y += ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y;
                            break;
                    }
                }

                // this is here to help imgui know the size of the window contents
                ImGui.SetCursorPos(new Vector2(textWrap, drawCursor.Y) - drawOrigin);
                ImGui.Dummy(Vector2.One);

                ImGui.EndChild();
            }
            //ImGui.PopTextWrapPos();
            ImGui.End();
        }
        else
        {
            pages.Clear();
            needUpdateMarkdown = true;
        }
    }

    private static void ProcessMarkdown(string mdFilePath)
    {
        pages.Clear();

        string pageName = "";
        List<DisplayCommand>? displayCommands = null;
        
        string paragraphText = "";
        bool pushNewParagraph = false;

        void ProcessPlainText(ref int i)
        {
            string buf = "";
            for (; i < paragraphText.Length; i++)
            {
                if (paragraphText[i] == '*') break;
                if (paragraphText[i] == '`') break;
                if (paragraphText[i] == '[') break;
                buf += paragraphText[i];
            }
            displayCommands?.Add(new DisplayText(buf));
        }

        void ProcessStyled(ref int i)
        {
            int asterikCount = 0;
            for (; i < paragraphText.Length; i++)
            {
                if (paragraphText[i] != '*') break;
                asterikCount++;
            }

            if (asterikCount == 1)
            {
                displayCommands?.Add(new DisplayText("*"));
                return;
            }

            string textBuf = "";

            for (; i < paragraphText.Length; i++)
            {
                if (paragraphText[i] == '*')
                {
                    int j = 0;
                    for (; i < paragraphText.Length && paragraphText[i] == '*'; i++)
                    {
                        j++;
                    }

                    if (asterikCount == j)
                    {
                        break;
                    }
                    else
                    {
                        textBuf += new string('*', j);
                    }
                }
                else
                {
                    textBuf += paragraphText[i];
                }
            }

            displayCommands?.Add(new DisplayBoldText(textBuf));
        }

        void ProcessPreformatted(ref int i)
        {
            i++; // skip first backtick
            string textBuf = "";

            for (; i < paragraphText.Length && paragraphText[i] != '`'; i++)
            {
                textBuf += paragraphText[i];
            }

            i++; // skip last backtick
            displayCommands?.Add(new DisplayPreformatted(textBuf));
        }

        void ProcessLink(ref int i)
        {
            string displayText = "";
            string link = "";

            i++; // skip opening bracket
            for (; i < paragraphText.Length && paragraphText[i] != ']'; i++)
            {
                displayText += paragraphText[i];
            }

            i++; // skip closing bracket
            i++; // skip opening parenthesis
            for (; i < paragraphText.Length && paragraphText[i] != ')'; i++)
            {
                link += paragraphText[i];
            }
            i++; // skip closing parenthesis

            displayCommands?.Add(new DisplayLink(displayText, link));
        }

        void FlushParagraph()
        {
            if (!string.IsNullOrWhiteSpace(paragraphText))
            {
                int i = 0;
                while (i < paragraphText.Length)
                {
                    if (paragraphText[i] == '*') ProcessStyled(ref i);
                    else if (paragraphText[i] == '`') ProcessPreformatted(ref i);
                    else if (paragraphText[i] == '[') ProcessLink(ref i);
                    else ProcessPlainText(ref i);
                }
            }

            paragraphText = "";
        }

        void ProcessText(string contents)
        {
            // newlines in the middle of paragraphs are parsed as spaces
            if (paragraphText.Length > 0 && !char.IsWhiteSpace(paragraphText[^1]))
            {
                paragraphText += ' ';
            }

            for (int i = 0; i < contents.Length; i++)
            {
                // make sure white space isn't duplicated
                if (char.IsWhiteSpace(contents[i]) && (paragraphText.Length == 0 || !char.IsWhiteSpace(paragraphText[^1])))
                {
                    paragraphText += ' ';
                }
                else
                {
                    paragraphText += contents[i];
                }
            }
        }

        foreach (var lineUntrimmed in File.ReadLines(mdFilePath))
        {
            string line = lineUntrimmed.Trim();

            // new paragraph...
            if (string.IsNullOrWhiteSpace(line))
            {
                if (pushNewParagraph)
                {
                    FlushParagraph();
                    displayCommands?.Add(new NewParagraph());
                    pushNewParagraph = false;
                }
            }

            // get header level
            else
            {
                pushNewParagraph = true;
                int headerLevel = 0;
                int textIndex;
                for (textIndex = 0; textIndex < line.Length && line[textIndex] == '#'; textIndex++)
                    headerLevel++;
                
                if (headerLevel > 0)
                {
                    if (headerLevel > 1)
                    {
                        FlushParagraph();
                        
                        if (headerLevel > 2)
                        {
                            displayCommands?.Add(new DisplayHeader(headerLevel, line[textIndex..].Trim()));
                        }
                        else // headerLevel == 2
                        {
                            if (!string.IsNullOrEmpty(pageName))
                            {
                                FlushParagraph();
                                pages.Add((pageName, displayCommands!));    
                            }

                            pageName = line[textIndex..].Trim();
                            displayCommands = [];
                        }
                    }
                    //
                }

                // no header here
                else
                {
                    // check if this is a bullet point
                    if (line[0] == '-' && line[1] == ' ')
                    {
                        FlushParagraph();
                        displayCommands?.Add(new DisplayBulletPoint());
                        ProcessText(line[1..].Trim());
                    }
                    else
                    {
                        ProcessText(line);
                    }
                }
            }
        }

        FlushParagraph();
        if (displayCommands is not null)
        {
            pages.Add((pageName, displayCommands));
        }
    }
}