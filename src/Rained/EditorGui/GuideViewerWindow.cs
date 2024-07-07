using ImGuiNET;
using System.Numerics;
namespace RainEd;

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
                ProcessMarkdown(Path.Combine(Boot.AppDataPath, "dist","GUIDE.md"));
            }

            ImGui.SetNextWindowSize(new Vector2(40.0f, 50.0f) * ImGui.GetFontSize(), ImGuiCond.FirstUseEver);

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
                foreach (var cmd in pages[selectedPage].Item2)
                {
                    switch (cmd)
                    {
                        case DisplayHeader header:
                            ImGui.SeparatorText(header.Text);
                            break;

                        case DisplayBulletPoint:
                            ImGui.NewLine();
                            ImGui.Bullet();
                            break;

                        case DisplayBoldText boldText:
                            ImGui.TextUnformatted("[" + boldText.Text + "]");
                            ImGui.SameLine();
                            break;

                        case DisplayText displayText:
                            ImGui.TextUnformatted(displayText.Text);
                            ImGui.SameLine();
                            break;

                        case DisplayPreformatted preformatted:
                            ImGui.TextUnformatted("Pref`" + preformatted.Text + "`");
                            ImGui.SameLine();
                            break;

                        case DisplayLink:
                            ImGui.TextUnformatted("LINK");
                            ImGui.SameLine();
                            break;

                        case NewParagraph:
                            ImGui.NewLine();
                            ImGui.NewLine();
                            break;
                    }
                }
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

        void FlushParagraph()
        {
            if (!string.IsNullOrWhiteSpace(paragraphText))
            {
                int i = 0;
                while (i < paragraphText.Length)
                {
                    if (paragraphText[i] == '*') ProcessStyled(ref i);
                    else if (paragraphText[i] == '`') ProcessPreformatted(ref i);
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