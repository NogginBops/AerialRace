using AerialRace.Debugging;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace.DebugGui
{
    enum DialogMode
    {
        Select,
        Open,
        Save,
    }

    [Flags]
    enum FilterMode
    {
        Files = 0x01,
        Directories = 0x02,
    }

    enum DirPathBase
    {
        Computer,
        UserDir,
    }

    static class ImGuiInternal
    {
        // FIXME: While ImGui.NET doesn't expose internal functions we import them here.

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl, EntryPoint = "igArrowButtonEx")]
        public static unsafe extern byte ArrowButtonEx([MarshalAs(UnmanagedType.LPUTF8Str)] string strid, ImGuiDir dir, Vector2 sizearg, ImGuiButtonFlags flags);

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl, EntryPoint = "igGetFocusID")]
        public static extern int ImGuiGetFocusID();

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl, EntryPoint = "igGetFocusScopeID")]
        public static extern int GetFocusScopeID();

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl, EntryPoint = "igPushFocusScope")]
        public static extern void PushFocusScope(uint id);

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl, EntryPoint = "igPopFocusScope")]
        public static extern void PopFocusScope();
    }

    class ImGuiFileBrowser
    {
        // Adapted from: https://github.com/gallickgunner/ImGui-Addons

        public string SelectedFn = "";
        public string SelectedPath = "";
        public string? Ext;

        public DialogMode DialogMode;
        public FilterMode FilterMode = FilterMode.Files | FilterMode.Directories;

        public bool ShowInputbarCombobox = false;
        public bool ValidateFile = false;
        public bool ShowHidden = false;
        public bool FilterDirty = true;
        public bool IsAppearing = true;

        public bool IsDirectory;

        public int ColumnItemsLimit = 12;
        public int SelectedIndex = -1;
        public int SelectedExtIndex = 0;
        public float ExtBoxWidth = -1.0f;
        public float ColumnWidth = 280.0f;
        public Vector2 MinSize = new Vector2(500, 300);
        public Vector2 MaxSize;
        public Vector2 InputComboboxPos;
        public Vector2 InputComboboxSize;

        public string InvFileModalID = "Invalid File!";
        public string RepFileModalID = "Replace File?";
        public string InputFn = new string('\0', 256);

        public string ValidTypes = "";

        public List<string> ValidExts = new List<string>();
        public DirPathBase CurrentDirPathBase = DirPathBase.Computer;
        public List<string> CurrentDirList = new List<string>();
        public List<DirectoryInfo> SubDirs = new List<DirectoryInfo>();
        public List<FileInfo> SubFiles = new List<FileInfo>();
        public string CurrentPath = "";
        public string? ErrorTitle = "";
        public string? ErrorMessage = "";

        public string FilterInput = "";
        public List<string> Filter = new List<string>();
        public List<DirectoryInfo> FilteredDirs = new List<DirectoryInfo>();
        public List<FileInfo> FilteredFiles = new List<FileInfo>();
        public List<string> InputCBFilterFiles = new List<string>();

        public void ClearFileList()
        {
            // TODO: Clear filtered

            SubDirs.Clear();
            SubFiles.Clear();
            FilterDirty = true;
            SelectedIndex = -1;
        }

        public void CloseDialog()
        {
            ValidTypes = "";
            ValidExts.Clear();
            SelectedExtIndex = 0;
            SelectedIndex = -1;

            InputFn = "";
            Filter.Clear();

            ShowInputbarCombobox = false;
            ValidateFile = false;
            ShowHidden = false;
            IsDirectory = false;
            FilterDirty = true;
            IsAppearing = true;

            FilteredDirs.Clear();
            FilteredFiles.Clear();
            InputCBFilterFiles.Clear();

            SubDirs.Clear();
            SubFiles.Clear();

            ImGui.CloseCurrentPopup();
        }

        public void SetPath(string path)
        {
            if (StartsWithUserPath(path))
                CurrentDirPathBase = DirPathBase.UserDir;
            else CurrentDirPathBase = DirPathBase.Computer;
            CurrentPath = path;
        }

        public unsafe bool ShowFileDialog(string label, DialogMode mode, Vector2 szXY, string validTypes)
        {
            DialogMode = mode;
            ImGuiIOPtr io = ImGui.GetIO();
            MaxSize.X = io.DisplaySize.X;
            MaxSize.Y = io.DisplaySize.Y;
            ImGui.SetNextWindowSizeConstraints(MinSize, MaxSize);
            ImGui.SetNextWindowPos(io.DisplaySize * 0.5f, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(new Vector2(MathF.Max(szXY.X, MinSize.X), MathF.Max(szXY.Y, MinSize.Y)), ImGuiCond.Appearing);

            // Set Proper Filter Mode
            if (mode == DialogMode.Select)
            {
                FilterMode = FilterMode.Directories;
            }
            else
            {
                FilterMode = FilterMode.Files | FilterMode.Directories;
            }

            // FIXME: ref null
            bool b = true;
            if (ImGui.BeginPopupModal(label, ref b, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                bool showError = false;

                // If this is the initial run, read current directory and load data once.
                if (IsAppearing)
                {
                    //SelectedFn.Clear();
                    //SelectedPath.Clear();
                    if (mode != DialogMode.Select)
                    {
                        ValidTypes = validTypes;
                        SetValidExtTypes(validTypes);
                    }

                    /* 
                     * If current path is empty (can happen on Windows if user closes dialog while inside MyComputer.
                     * Since this is a virtual folder, path would be empty) load the drives on Windows else initialize the current path on Unix.
                     */
                    if (CurrentPath.Length == 0)
                    {
                        
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            showError = LoadWindowsDrives() == false;
                        }
                        else
                        {
                            // FIXME: Do something!
                        }
                    }
                    else
                    {
                        showError |= ReadDir(CurrentPath) == false;
                    }

                    IsAppearing = false;
                }

                showError |= RenderNavAndSearchBarRegion();
                showError |= RenderFileListRegion();
                showError |= RenderInputTextAndExtRegion();
                showError |= RenderButtonsAndCheckboxRegion();

                if (ValidateFile)
                {
                    ValidateFile = false;
                    bool check = DoValidateFile();

                    if (!check && DialogMode == DialogMode.Open)
                    {
                        ImGui.OpenPopup(InvFileModalID);
                        //SelectedFn.Clear();
                        //SelectedPath.Clear();
                    }
                    else if (!check && DialogMode == DialogMode.Save)
                    {
                        ImGui.OpenPopup(RepFileModalID);
                    }
                    else if (!check && DialogMode == DialogMode.Select)
                    {
                        //SelectedFn.Clear();
                        //SelectedPath.Clear();
                        showError = true;
                        ErrorTitle = "Invalid Directory";
                        ErrorMessage = "Invalid Directory Selected. Please make sure the directory exists.";
                    }

                    // If selected file passes through validation check, set path to the file and close file dialog
                    if (check)
                    {
                        SelectedPath = CurrentPath + SelectedFn;

                        if (DialogMode == DialogMode.Select)
                        {
                            SelectedPath += "/";
                        }

                        CloseDialog();
                    }
                }

                // We don't need to check as the modals will only be shown if OpenPopup is called
                ShowInvalidFileModal();
                if (ShowReplaceFileModal())
                {
                    CloseDialog();
                }

                //Show Error Modal if there was an error opening any directory
                if (showError)
                    ImGui.OpenPopup(ErrorTitle);
                ShowErrorModal();

                ImGui.EndPopup();

                return SelectedFn.Length > 0 && SelectedPath.Length > 0;
            }
            else
            {
                return false;
            }
        }

        bool RenderNavAndSearchBarRegion()
        {
            ImGuiStylePtr style = ImGui.GetStyle();
            bool showError = false;
            float frameHeight = ImGui.GetFrameHeight();
            float listItemHeight = ImGui.GetFontSize() + style.ItemSpacing.Y;

            Vector2 pwContentSize = ImGui.GetWindowSize() - style.WindowPadding * 2.0f;
            Vector2 swSize = new Vector2(ImGui.CalcTextSize("Random").X + 140, style.WindowPadding.Y * 2.0f + frameHeight);
            Vector2 swContentSize = swSize - style.WindowPadding * 2.0f;
            Vector2 nwSize = new Vector2(pwContentSize.X - style.ItemSpacing.X - swSize.X, swSize.Y);

            ImGui.BeginChild("##NavigationWindow", nwSize, true, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar);

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.882f, 0.745f, 0.078f, 1.0f));
            for (int i = 0; i < CurrentDirList.Count; i++)
            {
                if (ImGui.Button(CurrentDirList[i]))
                {
                    //If last button clicked, nothing happens
                    if (i != CurrentDirList.Count - 1)
                        showError |= OnNavigationButtonClick(i) == false;
                }

                // Draw Arrow Buttons
                if (i != CurrentDirList.Count - 1)
                {
                    ImGui.SameLine(0, 0);
                    float nextLabelWidth = ImGui.CalcTextSize(CurrentDirList[i+1]).X;

                    if (i + 1 < CurrentDirList.Count - 1)
                    {
                        nextLabelWidth += frameHeight + ImGui.CalcTextSize(">>").X;
                    }

                    if (ImGui.GetCursorPosX() + nextLabelWidth >= (nwSize.X - style.WindowPadding.X * 3.0f))
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1.0f, 1.0f, 1.0f, 0.01f));
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

                        //Render a drop down of navigation items on button press
                        if (ImGui.Button(">>")) ImGui.OpenPopup("##NavBarDropboxPopup");

                        if (ImGui.BeginPopup("##NavBarDropboxPopup"))
                        {
                            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.125f, 0.125f, 0.125f, 1.0f));

                            if (ImGui.ListBoxHeader("##NavBarDropBox", new Vector2(0, listItemHeight * 5)))
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.882f, 0.745f, 0.078f, 1.0f));
                                for (int j = i + 1; j < CurrentDirList.Count; j++)
                                {
                                    if (ImGui.Selectable(CurrentDirList[j], false) && j != CurrentDirList.Count - 1)
                                    {
                                        showError |= OnNavigationButtonClick(j) == false;
                                        ImGui.CloseCurrentPopup();
                                    }
                                }
                                
                                ImGui.PopStyleColor();
                                ImGui.ListBoxFooter();
                            }

                            ImGui.PopStyleColor();
                            ImGui.EndPopup();
                        }
                        ImGui.PopStyleColor(2);
                        break;
                    }
                    else
                    {
                        const ImGuiButtonFlags DisabledFlag = (ImGuiButtonFlags)(1 << 14);

                        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1.0f, 1.0f, 1.0f, 0.01f));
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
                        ImGuiInternal.ArrowButtonEx("##Right", ImGuiDir.Right, new Vector2(frameHeight, frameHeight), DisabledFlag);
                        ImGui.SameLine(0, 0);
                        ImGui.PopStyleColor(2);
                    }
                }
            }
            ImGui.PopStyleColor();
            ImGui.EndChild();

            ImGui.SameLine();
            ImGui.BeginChild("##SearchWindow", swSize, true, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar);

            // Render Search/Filter bar
            float markerWidth = ImGui.CalcTextSize("(?)").X + style.ItemSpacing.X;
            {
                // C# version of:
                // if (Filter.Draw("##SearchBar", swContentSize.X - markerWidth))

                ImGui.SetNextItemWidth(swContentSize.X - markerWidth);
                bool filterChanged = ImGui.InputText("##SearchBar", ref FilterInput, 256);
                if (filterChanged)
                {
                    // reparse the filter
                    Filter = new List<string>(FilterInput.Split(',', StringSplitOptions.RemoveEmptyEntries));
                }

                if (filterChanged || FilterDirty)
                {
                    FilterFiles(FilterMode);
                }
            }

            // If filter bar was focused clear selection
            if (ImGuiInternal.ImGuiGetFocusID() == ImGui.GetID("##SearchBar"))
            {
                SelectedIndex = -1;
            }

            ImGui.SameLine();
            ShowHelpMarker("Filter (inc, -exc)");

            ImGui.EndChild();
            return showError;
        }

        bool RenderFileListRegion()
        {
            ImGuiStylePtr style = ImGui.GetStyle();
            Vector2 pwSize = ImGui.GetWindowSize();
            bool showError = false;
            float listItemHeight = ImGui.CalcTextSize("").Y + style.ItemSpacing.Y;
            float inputBarYpos = pwSize.Y - ImGui.GetFrameHeightWithSpacing() * 2.5f - style.WindowPadding.Y;
            float windowHeight = inputBarYpos - ImGui.GetCursorPosY() - style.ItemSpacing.Y;
            float windowContentHeight = windowHeight - style.WindowPadding.Y * 2.0f;
            float minContentSize = pwSize.X - style.WindowPadding.X * 4.0f;

            if (windowContentHeight <= 0.0f)
                return showError;

            // Reinitialize the limit on number of selectables in one column based on height
            ColumnItemsLimit = (int)MathF.Max(1.0f, windowContentHeight / listItemHeight);
            int numCols = (int)MathF.Max(1.0f, MathF.Ceiling((FilteredDirs.Count + FilteredFiles.Count) / ColumnItemsLimit));

            //Limitation by ImGUI in 1.75. If columns are greater than 64 readjust the limit on items per column and recalculate number of columns
            if (numCols > 64)
            {
                int exceedItemsAmount = (numCols - 64) * ColumnItemsLimit;
                ColumnItemsLimit += (int)MathF.Ceiling(exceedItemsAmount / 64.0f);
                numCols = (int)MathF.Max(1.0f, MathF.Ceiling((FilteredDirs.Count + FilteredFiles.Count) / ColumnItemsLimit));
            }

            float contentWidth = numCols * ColumnWidth;
            if (contentWidth < minContentSize)
                contentWidth = 0;

            ImGui.SetNextWindowContentSize(new Vector2(contentWidth, 0));
            ImGui.BeginChild("##ScrollingRegion", new Vector2(0, windowHeight), true, ImGuiWindowFlags.HorizontalScrollbar);
            ImGui.Columns(numCols);


            //Output directories in yellow
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.882f, 0.745f, 0.078f, 1.0f));
            int items = 0;
            for (int i = 0; i < FilteredDirs.Count; i++)
            {
                var fileAttribs = FilteredDirs[i].Attributes;
                if (fileAttribs.HasFlag(FileAttributes.Hidden) == false /*|| fileAttribs.HasFlag(FileAttributes.System)*/ || ShowHidden)
                {
                    items++;
                    if (ImGui.Selectable(FilteredDirs[i].Name, SelectedIndex == i && IsDirectory, ImGuiSelectableFlags.AllowDoubleClick))
                    {
                        SelectedIndex = i;
                        IsDirectory = true;

                        // If dialog mode is SELECT then copy the selected dir name to the input text bar
                        if (DialogMode == DialogMode.Select)
                        {
                            InputFn = new string(FilteredDirs[i].Name);
                        }

                        if (ImGui.IsMouseDoubleClicked(0))
                        {
                            showError |= OnDirClick(i) == false;
                            break;
                        }
                    }

                    if ((items) % ColumnItemsLimit == 0)
                        ImGui.NextColumn();
                }
            }
            ImGui.PopStyleColor(1);

            //Output files
            for (int i = 0; i < FilteredFiles.Count; i++)
            {
                var fileAttribs = FilteredFiles[i].Attributes;
                if (fileAttribs.HasFlag(FileAttributes.Hidden) == false || ShowHidden)
                {
                    items++;
                    if (ImGui.Selectable(FilteredFiles[i].Name, SelectedIndex == i && !IsDirectory, ImGuiSelectableFlags.AllowDoubleClick))
                    {
                        //int len = filteredfiles[i]->name.length();
                        SelectedIndex = i;
                        IsDirectory = false;

                        // If dialog mode is OPEN/SAVE then copy the selected file name to the input text bar
                        InputFn = new string(FilteredFiles[i].Name);

                        if (ImGui.IsMouseDoubleClicked(0))
                        {
                            SelectedFn = FilteredFiles[i].Name;
                            ValidateFile = true;
                        }
                    }
                    if ((items) % ColumnItemsLimit == 0)
                        ImGui.NextColumn();
                }
            }
            ImGui.Columns(1);
            ImGui.EndChild();

            return showError;
        }

        bool RenderInputTextAndExtRegion()
        {
            string label = DialogMode == DialogMode.Save ? "Save As:" : "Open:";
            ImGuiStylePtr style = ImGui.GetStyle();
            ImGuiIOPtr io = ImGui.GetIO();

            Vector2 pwPos = ImGui.GetWindowPos();
            Vector2 pwContentSize = ImGui.GetWindowSize() - style.WindowPadding * 2.0f;
            Vector2 cursorPos = ImGui.GetCursorPos();

            if (ExtBoxWidth < 0.0)
                ExtBoxWidth = ImGui.CalcTextSize(".abc").X + 100;
            float labelWidth = ImGui.CalcTextSize(label).X + style.ItemSpacing.X;
            float frameHeightSpacing = ImGui.GetFrameHeightWithSpacing();
            float inputBarWidth = pwContentSize.X - labelWidth;
            if (DialogMode != DialogMode.Select)
                inputBarWidth -= (ExtBoxWidth + style.ItemSpacing.X);

            bool showError = false;
            ImGui.SetCursorPosY(pwContentSize.Y - frameHeightSpacing * 2.0f);

            // Render Input Text Bar label
            ImGui.TextUnformatted(label);
            ImGui.SameLine();

            // Render Input Text Bar
            InputComboboxPos = pwPos + ImGui.GetCursorPos();
            InputComboboxSize = new Vector2(inputBarWidth, 0);
            ImGui.PushItemWidth(inputBarWidth);
            // FIXME: This will probably not marshal the string correctly
            if (ImGui.InputTextWithHint("##FileNameInput", "Type a name...", InputFn, 256, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll))
            {
                if (InputFn.Length > 0)
                {
                    SelectedFn = new string(InputFn);
                    ValidateFile = true;
                }
            }
            ImGui.PopItemWidth();

            // If input bar was focused clear selection
            if (ImGui.IsItemEdited())
                SelectedIndex = -1;

            // If Input Bar is edited show a list of files or dirs matching the input text.
            if (ImGui.IsItemEdited() || ImGui.IsItemActivated())
            {
                // If dialogmode is OPEN/SAVE then filter from list of files..
                if (DialogMode == DialogMode.Open || DialogMode == DialogMode.Save)
                {
                    InputCBFilterFiles.Clear();
                    for (int i = 0; i < SubFiles.Count; i++)
                    {
                        if (SubFiles[i].Name.Contains(InputFn))
                            InputCBFilterFiles.Add(SubFiles[i].Name);
                    }
                }

                // If dialogmode == SELECT then filter from list of directories
                else if (DialogMode == DialogMode.Select)
                {
                    InputCBFilterFiles.Clear();
                    for (int i = 0; i < SubDirs.Count; i++)
                    {
                        if (SubDirs[i].Name.Contains(InputFn))
                            InputCBFilterFiles.Add(SubDirs[i].Name);
                    }
                }

                // If filtered list has any items show dropdown
                ShowInputbarCombobox = InputCBFilterFiles.Count > 0;
            }

            //Render Extensions and File Types DropDown
            if (DialogMode != DialogMode.Select)
            {
                ImGui.SameLine();
                RenderExtBox();
            }

            //Render a Drop Down of files/dirs (depending on mode) that have matching characters as the input text only.
            showError |= RenderInputComboBox();

            ImGui.SetCursorPos(cursorPos);
            return showError;
        }

        bool RenderButtonsAndCheckboxRegion()
        {
            Vector2 pwSize = ImGui.GetWindowSize();
            ImGuiStylePtr style = ImGui.GetStyle();
            bool showError = false;
            float frameHeight = ImGui.GetFrameHeight();
            float frameHeightSpacing = ImGui.GetFrameHeightWithSpacing();
            float opensaveButtonWidth = GetButtonSize("Open").X;     // Since both Open/Save are 4 characters long, width gonna be same.
            float selectCancelButtonWidth = GetButtonSize("Cancel").X;     // Since both Cacnel/Select have same number of characters, so same width.
            float buttonsXpos;

            if (DialogMode == DialogMode.Select)
                buttonsXpos = pwSize.X - opensaveButtonWidth - (2.0f * selectCancelButtonWidth) - (2.0f * style.ItemSpacing.X) - style.WindowPadding.X;
            else
                buttonsXpos = pwSize.X - opensaveButtonWidth - selectCancelButtonWidth - style.ItemSpacing.X - style.WindowPadding.X;

            ImGui.SetCursorPosY(pwSize.Y - frameHeightSpacing - style.WindowPadding.Y);

            //Render Checkbox
            float labelWidth = ImGui.CalcTextSize("Show Hidden Files and Folders").X + ImGui.GetCursorPosX() + frameHeight;
            bool showMarker = (labelWidth >= buttonsXpos);
            ImGui.Checkbox((showMarker) ? "##showHiddenFiles" : "Show Hidden Files and Folders", ref ShowHidden);
            if (showMarker)
            {
                ImGui.SameLine();
                ShowHelpMarker("Show Hidden Files and Folders");
            }

            //Render an Open Button (in OPEN/SELECT dialogmode) or Open/Save depending on what's selected in SAVE dialogmode
            ImGui.SameLine();
            ImGui.SetCursorPosX(buttonsXpos);
            if (DialogMode == DialogMode.Save)
            {
                // If directory selected and Input Text Bar doesn't have focus, render Open Button
                if (SelectedIndex != -1 && IsDirectory && ImGuiInternal.ImGuiGetFocusID() != ImGui.GetID("##FileNameInput"))
                {
                    if (ImGui.Button("Open"))
                        showError |= OnDirClick(SelectedIndex) == false;
                }
                else if (ImGui.Button("Save") && InputFn.Length > 0)
                {
                    SelectedFn = new string(InputFn);
                    ValidateFile = true;
                }
            }
            else
            {
                if (ImGui.Button("Open"))
                {
                    //It's possible for both to be true at once (user selected directory but input bar has some text. In this case we chose to open the directory instead of opening the file.
                    //Also note that we don't need to access the selected file through "selectedidx" since the if a file is selected, input bar will get populated with that name.
                    if (SelectedIndex >= 0 && IsDirectory)
                        showError |= OnDirClick(SelectedIndex) == false;
                    else if (InputFn.Length > 0)
                    {
                        SelectedFn = new string(InputFn);
                        ValidateFile = true;
                    }
                }

                //Render Select Button if in SELECT Mode
                if (DialogMode == DialogMode.Select)
                {
                    //Render Select Button
                    ImGui.SameLine();
                    if (ImGui.Button("Select"))
                    {
                        if (InputFn.Length > 0)
                        {
                            SelectedFn = new string(InputFn);
                            ValidateFile = true;
                        }
                    }
                }
            }

            //Render Cancel Button
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
                CloseDialog();

            return showError;
        }

        bool RenderInputComboBox()
        {
            bool showError = false;
            ImGuiStylePtr style = ImGui.GetStyle();
            uint inputid = ImGui.GetID("##FileNameInput");
            uint focusscopeid = ImGui.GetID("##InputBarComboBoxListScope");
            float frameheight = ImGui.GetFrameHeight();

            InputComboboxSize.Y = MathF.Min(
                (InputCBFilterFiles.Count + 1) * frameheight + style.WindowPadding.Y * 2.0f,
                8 * ImGui.GetFrameHeight() + style.WindowPadding.Y * 2.0f);

            // FIXME!! ImGui.GetCurrentContext()->ActiveIdIsAlive
            if (ShowInputbarCombobox && (ImGuiInternal.GetFocusScopeID() == focusscopeid /*|| ImGui.GetCurrentContext()->ActiveIdIsAlive == inputid*/))
            {
                ImGuiWindowFlags popupFlags = ImGuiWindowFlags.NoTitleBar |
                                              ImGuiWindowFlags.NoResize |
                                              ImGuiWindowFlags.NoMove |
                                              ImGuiWindowFlags.NoFocusOnAppearing |
                                              ImGuiWindowFlags.NoScrollbar |
                                              ImGuiWindowFlags.NoSavedSettings;

                ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.125f, 0.125f, 0.125f, 1.0f));
                ImGui.SetNextWindowBgAlpha(1.0f);
                ImGui.SetNextWindowPos(InputComboboxPos + new Vector2(0, ImGui.GetFrameHeightWithSpacing()));
                ImGui.PushClipRect(Vector2.Zero, ImGui.GetIO().DisplaySize, false);

                ImGui.BeginChild("##InputBarComboBox", InputComboboxSize, true, popupFlags);

                Vector2 listboxsize = InputComboboxSize - ImGui.GetStyle().WindowPadding * 2.0f;
                if (ImGui.ListBoxHeader("##InputBarComboBoxList", listboxsize))
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
                    ImGuiInternal.PushFocusScope(focusscopeid);
                    foreach (var element in InputCBFilterFiles)
                    {
                        const ImGuiSelectableFlags NoHoldingActiveID = (ImGuiSelectableFlags)(1 << 20);
                        const ImGuiSelectableFlags SelectOnClick = (ImGuiSelectableFlags)(1 << 21);
                        if (ImGui.Selectable(element, false, NoHoldingActiveID | SelectOnClick))
                        {
                            InputFn = new string(element);
                            ShowInputbarCombobox = false;
                        }
                    }
                    ImGuiInternal.PopFocusScope();
                    ImGui.PopStyleColor(1);
                    ImGui.ListBoxFooter();
                }
                ImGui.EndChild();
                ImGui.PopStyleColor(2);
                ImGui.PopClipRect();
            }
            return showError;
        }

        void RenderExtBox()
        {
            ImGui.PushItemWidth(ExtBoxWidth);
            if(ImGui.BeginCombo("##FileTypes", ValidExts[SelectedExtIndex]))
            {
                for(int i = 0; i < ValidExts.Count; i++)
                {
                    if(ImGui.Selectable(ValidExts[i], SelectedExtIndex == i))
                    {
                        SelectedExtIndex = i;
                        if(DialogMode == DialogMode.Save)
                        {
                            string name = InputFn;
                            int idx = name.LastIndexOf(".");
                            if(idx == -1)
                                idx = InputFn.Length;
                            InputFn += ValidExts[SelectedExtIndex];
                        }
                        FilterFiles(FilterMode.Files);
                    }
                }
                ImGui.EndCombo();
            }
            Ext = ValidExts[SelectedExtIndex];
            ImGui.PopItemWidth();
        }

        bool OnNavigationButtonClick(int idx)
        {
            string newPath = "";

            // First Button corresponds to virtual folder Computer which lists all logical drives (hard disks and removables) and "/" on Unix
            if (idx == 0)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    switch (CurrentDirPathBase)
                    {
                        case DirPathBase.Computer:
                            {
                                if (!LoadWindowsDrives())
                                    return false;
                                CurrentPath = "";
                                CurrentDirList.Clear();
                                CurrentDirList.Add("Computer");
                            }
                            return true;
                        case DirPathBase.UserDir:
                            {
                                var userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                                newPath = userPath + "/";
                                if (ReadDir(newPath))
                                {
                                    CurrentPath = newPath;
                                    CurrentDirList.Clear();
                                    CurrentDirPathBase = DirPathBase.Computer;
                                    ParsePathTabs(CurrentPath);
                                    return true;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        default: throw new Exception();
                    }
                    
                    //return true;
                }
                else
                {
                    newPath = "/";
                }
            }
            else
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Clicked on a drive letter?
                    if (CurrentDirPathBase == DirPathBase.Computer && idx == 1)
                        newPath = CurrentPath.Substring(0, 3);
                    else
                    {
                        if (CurrentDirPathBase == DirPathBase.UserDir)
                        {
                            newPath += Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/";
                        }
                        // Start from i=1 since at 0 lies "MyComputer" which is only virtual and shouldn't be read by readDIR
                        for (int i = 1; i <= idx; i++)
                            newPath += CurrentDirList[i] + "/";
                    }
                }
                else
                {
                    // Since UNIX absolute paths start at "/", we handle this separately to avoid adding a double slash at the beginning
                    newPath += CurrentDirList[0];
                    for (int i = 1; i <= idx; i++)
                        newPath += CurrentDirList[i] + "/";
                }
            }

            if (ReadDir(newPath))
            {
                CurrentDirList.RemoveRange(idx + 1, CurrentDirList.Count - (idx + 1));
                CurrentPath = newPath;
                return true;
            }
            else
            {
                return false;
            }
        }

        bool OnDirClick(int idx)
        {
            string name;
            string newPath = CurrentPath;
            bool drivesShown = false;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                drivesShown = CurrentDirList.Count == 1 && CurrentDirList.Last() == "Computer";
            }

            name = FilteredDirs[idx].Name;

            if (name == "..")
            {
                newPath = newPath[0..^1]; // Remove trailing '/'
                newPath = newPath.Substring(0, newPath.LastIndexOf('/') + 1); // Also include a trailing '/'
            }
            else
            {
                // Remember we displayed drives on Windows as *Local/Removable Disk: X* hence we need last char only
                if (drivesShown)
                    name = name[^1] + ":";
                newPath += name + "/";
            }

            bool startsWithUserPath = StartsWithUserPath(newPath);
            if (CurrentDirPathBase == DirPathBase.Computer && startsWithUserPath)
            {
                CurrentDirPathBase = DirPathBase.UserDir;
                CurrentDirList.Clear();
                CurrentDirList.Add(Environment.UserName);
            }

            if (ReadDir(newPath))
            {
                if (name == "..")
                {
                    CurrentDirList.RemoveAt(CurrentDirList.Count - 1);

                    if (startsWithUserPath == false)
                    {
                        CurrentDirPathBase = DirPathBase.Computer;
                        CurrentDirList.Clear();
                        ParsePathTabs(newPath);
                    }
                }
                else
                {
                    CurrentDirList.Add(name);
                }

                CurrentPath = newPath;
                return true;
            }
            else
            {
                return false;
            }
        }

        bool ReadDir(string dirPath)
        {
            /* If the current directory doesn't exist, and we are opening the dialog for the first time, reset to defaults to avoid looping of showing error modal.
             * An example case is when user closes the dialog in a folder. Then deletes the folder outside. On reopening the dialog the current path (previous) would be invalid.
             */
            DirectoryInfo dir = new DirectoryInfo(dirPath);
            if (dir.Exists == false && IsAppearing)
            {
                CurrentDirList.Clear();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    CurrentPath = dirPath = "./";
                }
                else
                {
                    InitCurrentPath();
                    dirPath = CurrentPath;
                }

                dir = new DirectoryInfo(dirPath);
            }

            if (dir.Exists)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // If we are on Windows and current path is relative then get absolute path from dirent structure
                    if (CurrentDirList.Count == 0 && dirPath == "./")
                    {
                        CurrentPath = dir.FullName;
                        if (StartsWithUserPath(CurrentPath))
                            CurrentDirPathBase = DirPathBase.UserDir;
                        else CurrentDirPathBase = DirPathBase.Computer;

                        //Create a vector of each directory in the file path for the filepath bar. Not Necessary for linux as starting directory is "/"
                        ParsePathTabs(CurrentPath);
                    }
                }

                // store all the files and directories within directory and clear previous entries
                ClearFileList();
                foreach (var entry in dir.GetFileSystemInfos("", SearchOption.TopDirectoryOnly))
                {
                    if (entry is FileInfo file)
                    {
                        SubFiles.Add(file);
                    }
                    else if (entry is DirectoryInfo directory)
                    {
                        SubDirs.Add(directory);
                    }
                }
                SubDirs.Sort((d1, d2) => string.Compare(d1.Name, d2.Name));
                SubFiles.Sort((f1, f2) => string.Compare(f1.Name, f2.Name));

                //Initialize Filtered dirs and files
                FilterFiles(FilterMode);
            }
            else
            {
                ErrorTitle = "Error!";
                ErrorMessage = "Error opening directory! Make sure the directory exists and you have the proper rights to access the directory.";
                return false;
            }
            return true;
        }

        bool PassFilter(string str)
        {
            foreach (var filterStr in Filter)
            {
                if (filterStr[0] == '-')
                {
                    if (str.Contains(filterStr.Substring(1), StringComparison.InvariantCultureIgnoreCase))
                        return false;
                }
                else
                {
                    if (str.Contains(filterStr, StringComparison.InvariantCultureIgnoreCase))
                        return true;
                }
            }

            if (Filter.Count == 0)
            {
                return true;
            }

            return false;
        }

        void FilterFiles(FilterMode filterMode)
        {
            FilterDirty = false;
            if (FilterMode.HasFlag(FilterMode.Directories))
            {
                FilteredDirs.Clear();
                for (int i = 0; i < SubDirs.Count; ++i)
                {
                    if (PassFilter(SubDirs[i].Name))
                        FilteredDirs.Add(SubDirs[i]);
                }
            }
            if (FilterMode.HasFlag(FilterMode.Files))
            {
                FilteredFiles.Clear();
                for (int i = 0; i < SubFiles.Count; ++i)
                {
                    if (ValidExts[SelectedExtIndex] == "*.*")
                    {
                        if (PassFilter(SubFiles[i].Name))
                            FilteredFiles.Add(SubFiles[i]);
                    }
                    else
                    {
                        if (PassFilter(SubFiles[i].Name) && SubFiles[i].Name.Contains(ValidExts[SelectedExtIndex]))
                            FilteredFiles.Add(SubFiles[i]);
                    }
                }
            }
        }

        void ShowHelpMarker(string desc)
        {
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
                ImGui.TextUnformatted(desc);
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }

        unsafe void ShowErrorModal()
        {
            Vector2 windowSize = new Vector2(260, 0);
            ImGui.SetNextWindowSize(windowSize);

            // FIXME: ref null
            bool b = true;
            if (ImGui.BeginPopupModal(ErrorTitle, ref b, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize))
            {
                // FIXME: TextWrapped!!
                //ImGui.TextWrapped("%s", ErrorMessage);
                ImGui.TextUnformatted(ErrorMessage);

                ImGui.Separator();
                ImGui.SetCursorPosX(windowSize.X / 2.0f - GetButtonSize("OK").X / 2.0f);
                if (ImGui.Button("OK", GetButtonSize("OK")))
                    ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }
        }

        unsafe bool ShowReplaceFileModal()
        {
            Vector2 windowSize = new Vector2(250, 0);
            ImGui.SetNextWindowSize(windowSize);
            bool retval = false;

            // FIXME: ref null
            bool b = true;
            if (ImGui.BeginPopupModal(RepFileModalID, ref b, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize))
            {
                float frameheight = ImGui.GetFrameHeightWithSpacing();

                string text = "A file with the following filename already exists. Are you sure you want to replace the existing file?";
                // FIXME: TextWrapped!!
                //ImGui.TextWrapped("%s", text.cstr());
                ImGui.TextUnformatted(text);

                ImGui.Separator();

                float buttonswidth = GetButtonSize("Yes").X + GetButtonSize("No").X + ImGui.GetStyle().ItemSpacing.X;
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetWindowWidth() / 2.0f - buttonswidth / 2.0f - ImGui.GetStyle().WindowPadding.X);

                if (ImGui.Button("Yes", GetButtonSize("Yes")))
                {
                    SelectedPath = CurrentPath + SelectedFn;
                    ImGui.CloseCurrentPopup();
                    retval = true;
                }

                ImGui.SameLine();
                if (ImGui.Button("No", GetButtonSize("No")))
                {
                    SelectedFn = "";
                    SelectedPath = "";
                    ImGui.CloseCurrentPopup();
                    retval = false;
                }
                ImGui.EndPopup();
            }
            return retval;
        }

        unsafe void ShowInvalidFileModal()
        {
            ImGuiStylePtr style = ImGui.GetStyle();
            string text = "Selected file either doesn't exist or is not supported. Please select a file with the following extensions...";
            // FIXME: ImGui.CalcTextSize hideTextAfterDoubleHash and wrapWidth!!
            //Vector2 textSize = ImGui.CalcTextSize(text, nullptr, true, 350 - style.WindowPadding.X * 2.0f);
            Vector2 textSize = ImGui.CalcTextSize(text);
            Vector2 buttonSize = GetButtonSize("OK");

            float frameHeight = ImGui.GetFrameHeightWithSpacing();
            float cwContentHeight = ValidExts.Count * frameHeight;
            float cwHeight = MathF.Min(4.0f * frameHeight, cwContentHeight);
            Vector2 windowSize = new Vector2(350, 0);
            ImGui.SetNextWindowSize(windowSize);

            // FIXME: ref null
            bool b = true;
            if (ImGui.BeginPopupModal(InvFileModalID, ref b, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize))
            {

                // FIXME: TextWrapped
                //ImGui.TextWrapped("%s", text);
                ImGui.TextUnformatted(text);

                ImGui.BeginChild("##SupportedExts", new Vector2(0, cwHeight), true);
                for (int i = 0; i < ValidExts.Count; i++)
                    // FIXME: ImGui.BulletText(fmt, ...) varargs!!
                    ImGui.BulletText(ValidExts[i]);
                ImGui.EndChild();

                ImGui.SetCursorPosX(windowSize.X / 2.0f - buttonSize.X / 2.0f);
                if (ImGui.Button("OK", buttonSize))
                    ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }
        }

        void SetValidExtTypes(string extTypes)
        {
            /* Initialize a list of files extensions that are valid.
             * If the user chooses a file that doesn't match the extensions in the
             * list, we will show an error modal...
             */
            string maxStr = "";
            ValidExts.Clear();
            foreach (var ext in extTypes.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (maxStr.Length < ext.Length)
                {
                    maxStr = ext;
                }

                ValidExts.Add(ext);
            }

            float minwidth = ImGui.CalcTextSize(".abc").X + 100;
            ExtBoxWidth = MathF.Max(minwidth, ImGui.CalcTextSize(maxStr).X);
        }

        bool DoValidateFile()
        {
            bool match = false;

            // If there is an item selected, check if the selected file name (the input filename, in other words) matches the selection.
            if (SelectedIndex >= 0)
            {
                if (DialogMode == DialogMode.Select)
                    match = (FilteredDirs[SelectedIndex].Name == SelectedFn);
                else
                    match = (FilteredFiles[SelectedIndex].Name == SelectedFn);
            }

            // If the input filename doesn't match we need to explicitly find the input filename..
            if (!match)
            {
                if (DialogMode == DialogMode.Select)
                {
                    for (int i = 0; i < SubDirs.Count; i++)
                    {
                        if (SubDirs[i].Name == SelectedFn)
                        {
                            match = true;
                            break;
                        }
                    }

                }
                else
                {
                    for (int i = 0; i < SubFiles.Count; i++)
                    {
                        if (SubFiles[i].Name == SelectedFn)
                        {
                            match = true;
                            break;
                        }
                    }
                }
            }

            // If file doesn't match, return true on SAVE mode (since file doesn't exist, hence can be saved directly) and return false on other modes (since file doesn't exist so cant open/select)
            if (!match) return (DialogMode == DialogMode.Save);

            
            if (DialogMode == DialogMode.Save)
            {
                // If file matches, return false on SAVE, we need to show a replace file modal
                return false;
            }
            else if (DialogMode == DialogMode.Select)
            {
                // Return true on SELECT, no need to validate extensions
                return true;
            }
            else
            {
                // If list of extensions has all types, no need to validate.
                foreach (var ext in ValidExts)
                {
                    if (ext == "*.*")
                        return true;
                }
                int idx = SelectedFn.LastIndexOf('.');
                string fileExt = idx == -1 ? "" : SelectedFn.Substring(idx, SelectedFn.Length - idx);
                return ValidExts.Contains(fileExt);
            }
        }

        Vector2 GetButtonSize(string buttonText)
        {
            return ImGui.CalcTextSize(buttonText) + ImGui.GetStyle().FramePadding * 2.0f;
        }

        void ParsePathTabs(string path)
        {
            if (CurrentDirPathBase == DirPathBase.UserDir)
            {
                Debug.Assert(StartsWithUserPath(path));

                string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                CurrentDirList.Add(Environment.UserName);
                path = path[userPath.Length..];

                // If there is more stuff in the path remove the leading /
                if (path.StartsWith(Path.DirectorySeparatorChar) ||
                    path.StartsWith(Path.AltDirectorySeparatorChar))
                    path = path[1..];
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                CurrentDirList.Add("Computer");
            }
            else
            {
                CurrentDirList.Add("/");
            }

            if (path.Length > 2 && path[1] == ':')
            {
                CurrentDirList.Add(path[..2]);
                path = path[3..];
            }

            string rest = path;
            while (rest.Length > 0)
            {
                int index = rest.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
                if (index == -1)
                {
                    CurrentDirList.Add(rest);
                    break;
                }
                CurrentDirList.Add(rest[..index]);
                rest = rest[(index + 1)..];
            }
        }

        bool LoadWindowsDrives()
        {
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                if (drive.DriveType == DriveType.Removable)
                {
                    SubDirs.Add(drive.RootDirectory);
                }
                else if (drive.DriveType == DriveType.Fixed)
                {
                    SubDirs.Add(drive.RootDirectory);
                }
            }
            return true;
        }

        void InitCurrentPath()
        {
            CurrentPath = new DirectoryInfo("./").FullName;
            if (StartsWithUserPath(CurrentPath))
                CurrentDirPathBase = DirPathBase.UserDir;
            else CurrentDirPathBase = DirPathBase.Computer;
            ParsePathTabs(CurrentPath);
        }

        bool StartsWithUserPath(string path)
        {
            var userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return path.StartsWith(userPath);
        }
    }
}
