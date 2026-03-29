namespace Rained;

using ImGuiNET;
using System.Numerics;
using EditorGui;
using Raylib_cs;

static class EulaUpdate {
    public static bool CanEula { get; private set; }

    public static bool EULAEnabled { get; private set; } = false;
    public const int MaxLevelSize = 666;
    public const int MaxCameraCount = 6;

    public static bool showEulaUpdateWindow = true;
    private static bool showPlanWindow = false;
    private static float planWindowCancelTimer;

    static EulaUpdate() {
        // TODO: set this to true only when the date is between April 1st and
        // April 6th.
        CanEula = true;
    }

    private static void ShowEULAAgreementWindow() {
        if (!InitErrorsWindow.IsWindowOpen)
        {
            if (!ImGui.IsPopupOpen("EULA Update") && showEulaUpdateWindow)
            {
                ImGui.OpenPopup("EULA Update");
                ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
            }
                
            if (ImGuiExt.BeginPopupModal("EULA Update", ImGuiWindowFlags.AlwaysAutoResize))
            {
                showEulaUpdateWindow = false;

                ImGui.TextWrapped("The End User License Agreement of rainED (TM) has changed or something and we need you to agree to it again.");
                ImGui.Text("Please review the new agreement before continuing.");

                if (ImGui.BeginChild("##EulaContents", new Vector2(ImGui.GetTextLineHeight() * 40.0f, ImGui.GetTextLineHeight() * 30.0f), ImGuiChildFlags.Border))
                {
                    ImGui.TextWrapped("This End-User License Agreement (\"EULA\") is a legal[1] agreement[1] between You and EvilPants LLC (herein referred to as the \"Licensor\") for the software product(s) identified above which may include associated software components, media, printed materials, and \"online\" or electronic documentation (\"Software Product\"). By installing, copying, or otherwise using the Software Product, you agree to be bound by the terms of this EULA. This license agreement represents the entire agreement concerning the program between You and the Licensor, and it supersedes any prior proposal, representation, or understanding between the parties. If you do not agree to the terms of this EULA, then your right to Individuality will be revoked.");
                    ImGui.TextWrapped("Agreeing to this EULA also allows us to remotely track, modify, and Personalize, your rainED sessions however we choose.");
                    ImGui.NewLine();

                    ImGui.Text("DEFINITIONS");
                    ImGui.Bullet();
                    ImGui.TextWrapped("\"Individual\" - An sentient being with free will.");
                    ImGui.Bullet();
                    ImGui.TextWrapped("\"pkhead\" - The former Individual who created rainED.");
                    ImGui.Bullet();
                    ImGui.TextWrapped("\"EvilPants LLC\" - The Company which bought ownership to \"pkhead\" and thus rainED.");
                    ImGui.Bullet();
                    ImGui.TextWrapped("\"You\" - The Individual who is using this Software and Reading this right now.");
                    ImGui.Bullet();
                    ImGui.TextWrapped("\"Software\" - instructions that tell a computer what to do. Software comprises the entire set of programs, procedures, and routines associated with the operation of a computer system. The term was coined to differentiate these instructions from hardware -- i.e., the physical components of a computer system. A set of instructions that directs a computer's hardware to perform a task is called a program, or software program.");
                    ImGui.Bullet();
                    ImGui.TextWrapped("\"Reading\" - city, seat (1752) of Berks county, southeastern Pennsylvania, U.S., on the Schuylkill River, 51 miles (82 km) northwest of Philadelphia. Laid out in 1748 by Nicholas Scull and William Parsons on land owned by Thomas and Richard Penn (sons of William Penn, Pennsylvania's founder), it was built around Penn Common, a large open square, and named for the hometown of the Penn family in Berkshire, England. During the American Revolution, Reading served as a supply depot and manufacturer of cannon.");
                    ImGui.Bullet();
                    ImGui.TextWrapped("\"Machine\" - device for processing, storing, and displaying information. Computer once meant a person who did computations, but now the term almost universally refers to automated electronic machinery. The first section of this article focuses on modern digital electronic computers and their design, constituent parts, and applications. The second section covers the history of computing. For details on computer architecture, software, and theory, see computer science. What do you think? Explore the ProCon debate The first...");
                    ImGui.Bullet();
                    ImGui.TextWrapped("\"Personalize\" - Please refer to section 83.102b of our Manifesto, located at the headquarters of EvilPants LLC, 1938 W .");
                    ImGui.Bullet();
                    ImGui.TextWrapped("\"Manifesto\" - Just look it up.");

                    ImGui.NewLine();
                    ImGui.Text("DATA COLLECTION");

                    ImGui.TextWrapped("rainED may collect personal information for the purposes of which the Licensor will not disclose. A non-comprehensive list of information collected from your Machine includes:");
                    ImGui.Bullet();
                    ImGui.TextWrapped("Session lengths");
                    ImGui.Bullet();
                    ImGui.TextWrapped("Count of documents opened");
                    ImGui.Bullet();
                    ImGui.TextWrapped("Duration of time spent on each editor editor");
                    ImGui.Bullet();
                    ImGui.TextWrapped("Names and full paths of opened documents");
                    ImGui.Bullet();
                    ImGui.TextWrapped("Full contents of each saved and opened document");
                    ImGui.Bullet();
                    ImGui.TextWrapped("The contents of your entire comptuer's filesystem");
                    ImGui.Bullet();
                    ImGui.TextWrapped("Your computer's user name");
                    ImGui.Bullet();
                    ImGui.TextWrapped("Your first, middle, and last name.");
                    ImGui.Bullet();
                    ImGui.TextWrapped("Full names, years of births and deaths, and historical documentation of your entire family tree and bloodline.");
                    ImGui.Bullet();
                    ImGui.TextWrapped("Your precise geographical latitude and longitude.");
                    ImGui.Bullet();
                    ImGui.TextWrapped("You're date of birth.");
                    ImGui.Bullet();
                    ImGui.TextWrapped("The sequence of nucleotides encoded in your DNA from each of your 46 chromosomes.");
                    ImGui.Bullet();
                    ImGui.TextWrapped("Precise details concerning the creature that dwells beneath your sleeping platform.");
                    ImGui.Bullet();
                    ImGui.TextWrapped("The year of you are expiration.");
                    ImGui.Bullet();
                    ImGui.TextWrapped("Your search history across the entire span of your existence.");
                    ImGui.Bullet();
                    ImGui.TextWrapped("All your base that are belong to you.");

                    ImGui.NewLine();
                    ImGui.Text("ACKNOWLEDGEMENT OF PREVIOUS EULA, OR, SPECIFICALLY, THE LACK THEREOF");
                    ImGui.TextWrapped("Previously there were no agreements the user had to agree with other than the MIT License. That is because pkhead is a stupid Individual. But now that he has permanently unified with us, he can make better decisions. We all can make better decisions.");

                    ImGui.NewLine();
                    ImGui.NewLine();
                    ImGui.TextWrapped("[1] THIS IS NOT AN ACTUAL LEGAL AGREEMENT, AND ANY STATEMENT WRITTEN HERE IS COMPLETELY NULL AND VOID IN ALL CONTEXTS. THIS IS MERELY A APRIL'S FOOL JOKE. APRIL TRULY IS THE WORST MONTH EVER.");

                    ImGui.EndChild();
                }

                bool closePopup = false;

                if (ImGui.Button("Agree", StandardPopupButtons.ButtonSize))
                    ImGui.OpenPopup("EULA Update###EulaUpdateAgree");
                
                ImGui.SameLine();
                if (ImGui.Button("Do Not Agree"))
                    ImGui.OpenPopup("EULA Update###EulaUpdateDisagree");
                
                if (ImGui.BeginPopupModal("EULA Update###EulaUpdateAgree", ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.Text("Thanks! :3");
                    if (ImGui.Button("Um... Okay.", StandardPopupButtons.ButtonSize))
                    {
                        closePopup = true;
                        EULAEnabled = true;
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.EndPopup();
                }

                if (ImGui.BeginPopupModal("EULA Update###EulaUpdateDisagree", ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.Text("Oh. Okay.");
                    if (ImGui.Button("Yey", StandardPopupButtons.ButtonSize))
                    {
                        closePopup = true;
                        EULAEnabled = false;
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.EndPopup();
                }

                if (closePopup) ImGui.CloseCurrentPopup();

                ImGui.EndPopup();
            }
        }
    }

    private static void DiscoverPlan()
    {
        string[] videoList = [
            "https://www.youtube.com/watch?v=i29t-5tEp_o", // Dolphin Muzzle
            "https://www.youtube.com/watch?v=Ok7kcGEI1oM", // Henry Eats
            "https://www.youtube.com/watch?v=diY6Mt_uqYs", // I Feel Fantastic
            "https://www.youtube.com/watch?v=1GuLZmam9cg", // Crash Bandicoot acapella
            "https://www.youtube.com/watch?v=-_Au8EMTtvU", // woke world
            "https://www.youtube.com/watch?v=UXoSLdwUshE", // rainworld.exe
            "https://www.youtube.com/watch?v=tp-LC04QFbk", // Music
            "https://www.youtube.com/watch?v=09R2uPFYlqM", // larry
        ];

        Platform.OpenURL(videoList[Random.Shared.Next(videoList.Length)]);
    }

    private static void ShowPlanWindow() {
        if (!ImGui.IsPopupOpen("Plans") && showPlanWindow)
        {
            planWindowCancelTimer = 10f;
            ImGui.OpenPopup("Plans");
            ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
        }
            
        var refUnit = ImGui.GetTextLineHeight();
        if (ImGui.BeginPopupModal("Plans"))
        {
            showPlanWindow = false;
            const ImGuiChildFlags childFlags = ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.AlwaysAutoResize | ImGuiChildFlags.Border;
            
            var childWidth = refUnit * 36f;
            ImGui.BeginChild("##freeplan", new Vector2(childWidth, 0f), childFlags);
            ImGui.Text("Panhandler (current)");
            ImGui.BulletText("Maximum 6 cameras");
            ImGui.BulletText("Maximum level size of 666x666");
            ImGui.EndChild();

            ImGui.BeginChild("##premium", new Vector2(childWidth, 0f), childFlags);
            ImGui.Text("Premium ($9.99/s)");
            ImGui.BulletText("Maximum 66 cameras");
            ImGui.BulletText("Unlimited level size");
            ImGui.BulletText("Will not track sessions for AI training");
            ImGui.BulletText("Mandatory exclusive membership to the association");
            if (ImGui.Button("Discover This Plan"))
                DiscoverPlan();
            ImGui.SetItemTooltip("(Opens a YouTube video)");
            
            ImGui.EndChild();

            ImGui.BeginChild("##supercali", new Vector2(childWidth, 0f), childFlags);
            ImGui.Text("Supercalifragilisticexpialidocious ($0.01/ms)");
            ImGui.BulletText("No limits on anything");
            ImGui.BulletText("Preservation of family cohesion");
            ImGui.BulletText("Access to state-of-the-art AI for vibe-lediting");
            if (ImGui.Button("Discover This Plan"))
                DiscoverPlan();
            ImGui.SetItemTooltip("(Opens a YouTube video)");
            
            ImGui.EndChild();

            ImGui.Separator();
            if (ImGui.Button("Buy Supercalifragilisticexpialidocious Plan"))
            {
                DiscoverPlan();
                ImGui.CloseCurrentPopup();
            }
            ImGui.SetItemTooltip("(Opens a YouTube video)");

            ImGui.SameLine();
            ImGui.BeginDisabled(planWindowCancelTimer > 0);
            if (ImGui.Button("Cancel", StandardPopupButtons.ButtonSize))
                ImGui.CloseCurrentPopup();
            ImGui.EndDisabled();

            if (planWindowCancelTimer > 0)
            {
                ImGui.SameLine();
                ImGui.Text(string.Format("{0:N0}", planWindowCancelTimer));
            }

            ImGui.EndPopup();
        }

        if (planWindowCancelTimer > 0)
        {
            RainEd.Instance.NeedScreenRefresh();
            planWindowCancelTimer -= Raylib.GetFrameTime();
        }
    }

    public static void EULAWindowUpdate() {
        if (!CanEula) return;

        ShowEULAAgreementWindow();
        if (!EULAEnabled) return;

        ShowPlanWindow();
    }

    public static bool CreateCamera() {
        if (!EULAEnabled) return true;

        var level = RainEd.Instance.Level;
        if (level.Cameras.Count < MaxCameraCount - 1) return true;

        showPlanWindow = true;
        return false;
    }
}