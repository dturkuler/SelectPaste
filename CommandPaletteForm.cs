using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Text.Json;
using System.IO;

namespace SelectPaste
{
    public class CommandItem
    {
        public string label { get; set; } = "";
        public string value { get; set; } = "";
        public string description { get; set; } = ""; 
        
        // Metadata for display/sorting
        public string GroupName { get; set; } = "";
        public int UsageCount { get; set; } = 0;

        // Formatted properties for ListBox DisplayMember
        public string FullDisplay => $"[{GroupName.ToUpper()}] {label} -> {ShortValue}";
        public string TabDisplay => $"{label} -> {ShortValue}";

        private string ShortValue => (value?.Length > 40) ? value.Substring(0, 37) + "..." : (value ?? "");

        public override string ToString() => FullDisplay;
    }

    public class CommandGroup
    {
        public string name { get; set; } = "General";
        public string description { get; set; } = ""; 
        public List<CommandItem> commands { get; set; } = new List<CommandItem>();
    }

    // New class to handle usage statistics
    public class UsageManager
    {
        private string filePath;
        private Dictionary<string, int> usageStats;

        public UsageManager(string usageFilePath)
        {
            filePath = usageFilePath;
            Load();
        }

        private void Load()
        {
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    usageStats = JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
                }
                catch
                {
                    usageStats = new Dictionary<string, int>();
                }
            }
            else
            {
                usageStats = new Dictionary<string, int>();
            }
        }

        public void Increment(string commandValue)
        {
            if (usageStats.ContainsKey(commandValue))
                usageStats[commandValue]++;
            else
                usageStats[commandValue] = 1;

            Save();
        }

        public int GetUsage(string commandValue)
        {
            return usageStats.ContainsKey(commandValue) ? usageStats[commandValue] : 0;
        }

        private void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(usageStats);
                File.WriteAllText(filePath, json);
            }
            catch { }
        }
    }

    public class CommandPaletteForm : Form
    {
        private TextBox searchBox;
        private ListBox resultMap;
        private Panel tabViewport; // Viewport to hide scrollbars
        private FlowLayoutPanel tabHeaderPanel; // Content that scrolls
        private Label closeButton; 
        private ToolTip toolTip;

        internal List<CommandGroup> commandGroups = new List<CommandGroup>();
        private int currentGroupIndex = 0;
        internal UsageManager usageManager;
        
        public string SelectedValue { get; private set; } = "";

        private Program.AppSettings settings;

        public CommandPaletteForm(Program.AppSettings settings)
        {
            this.settings = settings;
            this.FormBorderStyle = FormBorderStyle.None;
            // Restore Size
            this.Size = new Size(settings.WindowWidth, settings.WindowHeight);
            this.MinimumSize = new Size(300, 200); 

            // Restore Position
            if (settings.WindowX.HasValue && settings.WindowY.HasValue)
            {
                this.StartPosition = FormStartPosition.Manual;
                this.Location = new Point(settings.WindowX.Value, settings.WindowY.Value);
            }
            else
            {
                this.StartPosition = FormStartPosition.CenterScreen;
            }

            this.BackColor = Color.FromArgb(30, 30, 30);
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.KeyPreview = true; 
            this.DoubleBuffered = true; 

            // Initialize Usage Manager with checking existing setting
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            // Determine usage file name based on command file
            // Convention: [name].json -> [name]_usage.json
            string commandFile = settings.CommandFile;
            string usageFile = "usage.json"; // Default fallback
            
            if (commandFile.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                 string baseName = Path.GetFileNameWithoutExtension(commandFile);
                 usageFile = $"{baseName}_usage.json";
            }
            
            usageManager = new UsageManager(Path.Combine(exePath, usageFile));

            // Initialize ToolTip
            toolTip = new ToolTip();
            toolTip.AutoPopDelay = 5000;
            toolTip.InitialDelay = 500;
            toolTip.ReshowDelay = 100;
            toolTip.ShowAlways = true;

            InitializeComponents();
            LoadCommands();
            
            this.KeyDown += Form_KeyDown;
            this.FormClosing += CommandPaletteForm_FormClosing;
        }

        private void CommandPaletteForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Update settings before closing
            settings.WindowWidth = this.Width;
            settings.WindowHeight = this.Height;
            settings.WindowX = this.Location.X;
            settings.WindowY = this.Location.Y;
        }

        private void InitializeComponents()
        {
            int padding = 10;
            int headerHeight = 30;
            int searchBoxHeight = (int)(settings.FontSize * 3); // Approx height for TextBox

            // Tabs Header
            // Tabs Header Viewport (The "Window")
            tabViewport = new Panel
            {
                Location = new Point(padding, 5),
                Size = new Size(this.ClientSize.Width - padding - 40, 30),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = false // We handle scrolling manually
            };

            // Tabs Header Content (The actual tabs)
            tabHeaderPanel = new FlowLayoutPanel
            {
                Location = Point.Empty,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            // Allow dragging from header
            tabViewport.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); } };
            tabHeaderPanel.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); } };
            tabViewport.Controls.Add(tabHeaderPanel);

            // Close Button "X"
            closeButton = new Label
            {
                Text = "X",
                Location = new Point(this.ClientSize.Width - 20, 5),
                Size = new Size(15, 20),
                ForeColor = Color.IndianRed,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            closeButton.Click += (s, e) => CloseForm();
            closeButton.MouseEnter += (s, e) => closeButton.ForeColor = Color.Red;
            closeButton.MouseLeave += (s, e) => closeButton.ForeColor = Color.IndianRed;

            searchBox = new TextBox
            {
                Location = new Point(padding, 40),
                Width = this.ClientSize.Width - (padding * 2),
                Font = new Font("Segoe UI", settings.FontSize + 2),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            searchBox.TextChanged += SearchBox_TextChanged;
            searchBox.KeyDown += SearchBox_KeyDown;

            resultMap = new ListBox
            {
                Location = new Point(padding, 80),
                Width = this.ClientSize.Width - (padding * 2),
                Height = this.ClientSize.Height - 80 - padding,
                Font = new Font("Segoe UI", settings.FontSize),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.LightGray,
                BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = (int)(settings.FontSize * 2)
            };
            resultMap.DrawItem += ResultMap_DrawItem;
            resultMap.DoubleClick += ResultMap_DoubleClick;
            resultMap.KeyDown += ResultMap_KeyDown;
            resultMap.SelectedIndexChanged += ResultMap_SelectedIndexChanged;

            this.Controls.Add(tabViewport);
            this.Controls.Add(closeButton);
            this.Controls.Add(searchBox);
            this.Controls.Add(resultMap);
        }

        private void ResultMap_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            
            CommandItem? item = resultMap.Items[e.Index] as CommandItem;
            if (item == null) return;

            // Background
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(60, 60, 60)), e.Bounds);
            }
            else
            {
                e.Graphics.FillRectangle(new SolidBrush(resultMap.BackColor), e.Bounds);
            }

            // Text Rendering
            float x = e.Bounds.X + 5;
            float y = e.Bounds.Y + (e.Bounds.Height - e.Font!.Height) / 2;

            // 1. Category (Breadcrumb)
            if (resultMap.DisplayMember == "FullDisplay" && !string.IsNullOrEmpty(item.GroupName))
            {
                string category = $"[{item.GroupName.ToUpper()}] ";
                using (var brush = new SolidBrush(HexToColor(settings.CategoryColor)))
                {
                    e.Graphics.DrawString(category, e.Font, brush, x, y);
                    x += e.Graphics.MeasureString(category, e.Font).Width;
                }
            }

            // 2. Label
            using (var brush = new SolidBrush(HexToColor(settings.LabelColor)))
            {
                e.Graphics.DrawString(item.label, e.Font, brush, x, y);
                x += e.Graphics.MeasureString(item.label, e.Font).Width;
            }

            // 3. Separator " -> "
            using (var brush = new SolidBrush(Color.DimGray))
            {
                string sep = " -> ";
                e.Graphics.DrawString(sep, e.Font, brush, x, y);
                x += e.Graphics.MeasureString(sep, e.Font).Width;
            }

            // 4. Value (Shortened)
            using (var brush = new SolidBrush(HexToColor(settings.ValueColor)))
            {
                string shortVal = (item.value?.Length > 40) ? item.value.Substring(0, 37) + "..." : (item.value ?? "");
                e.Graphics.DrawString(shortVal, e.Font, brush, x, y);
            }
        }

        private Color HexToColor(string hex)
        {
            try
            {
                return ColorTranslator.FromHtml(hex);
            }
            catch
            {
                return Color.White;
            }
        }

        #region Resizing and Moving Logic
        private const int WM_NCHITTEST = 0x84;
        private const int HT_CLIENT = 0x1;
        private const int HT_CAPTION = 0x2;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);
        private const int SB_HORZ = 0;
        private const int SB_VERT = 1;
        private const int SB_BOTH = 3;
        private const int WM_NCLBUTTONDOWN = 0xA1;

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_NCHITTEST)
            {
                Point pos = new Point(m.LParam.ToInt32());
                pos = this.PointToClient(pos);

                const int grip = 10; // Grip size for resizing 

                if (pos.X <= grip && pos.Y <= grip) m.Result = (IntPtr)HTTOPLEFT;
                else if (pos.X >= this.ClientSize.Width - grip && pos.Y <= grip) m.Result = (IntPtr)HTTOPRIGHT;
                else if (pos.X <= grip && pos.Y >= this.ClientSize.Height - grip) m.Result = (IntPtr)HTBOTTOMLEFT;
                else if (pos.X >= this.ClientSize.Width - grip && pos.Y >= this.ClientSize.Height - grip) m.Result = (IntPtr)HTBOTTOMRIGHT;
                else if (pos.X <= grip) m.Result = (IntPtr)HTLEFT;
                else if (pos.X >= this.ClientSize.Width - grip) m.Result = (IntPtr)HTRIGHT;
                else if (pos.Y <= grip) m.Result = (IntPtr)HTTOP;
                else if (pos.Y >= this.ClientSize.Height - grip) m.Result = (IntPtr)HTBOTTOM;
                // Allow dragging window from the tab empty space
                else if (pos.Y < 40) m.Result = (IntPtr)HT_CAPTION;
            }
        }
        #endregion

        private void LoadCommands()
        {
            try
            {
                // Ensure we read from the executable's directory
                string exePath = AppDomain.CurrentDomain.BaseDirectory;
                string commandsPath = Path.Combine(exePath, settings.CommandFile);

                if (File.Exists(commandsPath))
                {
                    string json = File.ReadAllText(commandsPath);
                    try 
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        commandGroups = JsonSerializer.Deserialize<List<CommandGroup>>(json, options) ?? new List<CommandGroup>();
                        
                        // Assign Group Names to items and Load Usage Counts
                        foreach (var group in commandGroups)
                        {
                            foreach (var cmd in group.commands)
                            {
                                cmd.GroupName = group.name; // Set Context
                                cmd.UsageCount = usageManager.GetUsage(cmd.value); // Load Usage
                            }
                        }

                        if (commandGroups.Count == 0 || (commandGroups.Count > 0 && commandGroups[0].commands == null))
                        {
                            throw new Exception("Fallback to flat list");
                        }
                    }
                    catch
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var flatList = JsonSerializer.Deserialize<List<CommandItem>>(json, options) ?? new List<CommandItem>();
                        commandGroups = new List<CommandGroup> 
                        { 
                            new CommandGroup { name = "All Commands", commands = flatList } 
                        };
                         // Assign Group Names to items for flat list
                        foreach (var group in commandGroups)
                        {
                            foreach (var cmd in group.commands)
                            {
                                cmd.GroupName = "General";
                                cmd.UsageCount = usageManager.GetUsage(cmd.value);
                            }
                        }
                    }

                    if (commandGroups.Count == 0)
                    {
                         commandGroups.Add(new CommandGroup { name = "Empty", commands = new List<CommandItem>() });
                    }

                    // --- Inject Favorites Group ---
                    var allCommandsForFavs = commandGroups.SelectMany(g => g.commands).ToList();
                    var favoriteCommands = allCommandsForFavs
                        .Where(c => c.UsageCount > 0)
                        .GroupBy(c => c.value) // Unique by value
                        .Select(g => g.OrderByDescending(x => x.UsageCount).First())
                        .OrderByDescending(c => c.UsageCount)
                        .Take(15)
                        .Select(c => new CommandItem { 
                            label = c.label, 
                            value = c.value, 
                            description = c.description, 
                            GroupName = c.GroupName, 
                            UsageCount = c.UsageCount 
                        }) // Create copies
                        .ToList();

                    if (favoriteCommands.Count > 0)
                    {
                        commandGroups.Insert(0, new CommandGroup 
                        { 
                            name = "Favorites", 
                            description = "Most frequently used commands",
                            commands = favoriteCommands 
                        });
                    }

                    // --- Inject Profile Switcher ---
                    // Helper command to switch profiles
                    var systemGroup = new CommandGroup 
                    { 
                        name = "System", 
                        commands = new List<CommandItem> 
                        {
                            new CommandItem 
                            { 
                                label = "Switch Profile", 
                                value = "::SWITCH_PROFILE::", 
                                description = $"Current: {settings.CommandFile}",
                                GroupName = "System"
                            },
                            new CommandItem 
                            { 
                                label = "Manage Commands", 
                                value = "::MANAGE_COMMANDS::", 
                                description = "Open the Command & Group Manager",
                                GroupName = "System"
                            },
                            new CommandItem 
                            { 
                                label = "Settings", 
                                value = "::SETTINGS::", 
                                description = "Application configuration",
                                GroupName = "System"
                            }
                        }
                    };
                    commandGroups.Add(systemGroup);
                    // ------------------------------
                    // ------------------------------
                    
                    SelectGroup(0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading commands.json: {ex.Message}");
            }
        }

        private void SelectGroup(int index)
        {
            if (index < 0) index = commandGroups.Count - 1;
            if (index >= commandGroups.Count) index = 0;

            currentGroupIndex = index;
            UpdateTabsUI();
            
            if (!string.IsNullOrEmpty(searchBox.Text))
            {
                searchBox.Text = ""; 
            }
            
            // Sort by Usage only for Favorites, otherwise Alphabetical
            List<CommandItem> sortedCommands;
            if (commandGroups[currentGroupIndex].name == "Favorites")
            {
                sortedCommands = commandGroups[currentGroupIndex].commands
                    .OrderByDescending(c => c.UsageCount)
                    .ThenBy(c => c.label, StringComparer.OrdinalIgnoreCase) // Case-insensitive alphabetical tie-breaker
                    .ToList();
            }
            else
            {
                sortedCommands = commandGroups[currentGroupIndex].commands
                    .OrderBy(c => c.label, StringComparer.OrdinalIgnoreCase) // Case-insensitive alphabetical
                    .ToList();
            }

            UpdateList(sortedCommands, showBreadcrumbs: false); // No need for "GIT > Push" inside Git tab
            searchBox.Focus(); 
        }

        private void UpdateTabsUI()
        {
            tabHeaderPanel.Controls.Clear();
            if (commandGroups.Count <= 1) return;

            Label? activeLabel = null;
            for (int i = 0; i < commandGroups.Count; i++)
            {
                var group = commandGroups[i];
                Label lbl = new Label
                {
                    Text = group.name.ToUpper(),
                    AutoSize = true,
                    Padding = new Padding(5),
                    Margin = new Padding(0, 0, 10, 0),
                    Font = new Font("Segoe UI", 9, i == currentGroupIndex ? FontStyle.Bold : FontStyle.Regular),
                    ForeColor = i == currentGroupIndex ? Color.DeepSkyBlue : Color.Gray,
                    Cursor = Cursors.Hand
                };
                int tempIndex = i; 
                lbl.Click += (s, e) => SelectGroup(tempIndex);
                
                // Add tooltip to tab
                if (!string.IsNullOrEmpty(group.description))
                {
                    toolTip.SetToolTip(lbl, group.description);
                }
                else
                {
                    toolTip.SetToolTip(lbl, $"Switch to {group.name} group");
                }

                tabHeaderPanel.Controls.Add(lbl);
                if (i == currentGroupIndex) activeLabel = lbl;
            }

            tabHeaderPanel.PerformLayout();
            if (activeLabel != null)
            {
                // Manual Scroll-into-view logic (Carousel)
                int labelLeft = activeLabel.Left;
                int labelRight = activeLabel.Right;
                int viewportWidth = tabViewport.Width;
                int currentScrollX = -tabHeaderPanel.Left;

                if (labelLeft < currentScrollX)
                {
                    // Scroll Left to show label
                    tabHeaderPanel.Left = -labelLeft;
                }
                else if (labelRight > currentScrollX + viewportWidth)
                {
                    // Scroll Right to show label
                    tabHeaderPanel.Left = -(labelRight - viewportWidth);
                }
            }
        }

        private void UpdateList(List<CommandItem> items, bool showBreadcrumbs)
        {
            resultMap.Items.Clear();
            resultMap.DisplayMember = showBreadcrumbs ? "FullDisplay" : "TabDisplay";
            foreach (var item in items)
            {
                resultMap.Items.Add(item);
            }
            if (resultMap.Items.Count > 0)
            {
                resultMap.SelectedIndex = 0; 
            }
        }

        private void SearchBox_TextChanged(object? sender, EventArgs e)
        {
            string query = searchBox.Text.ToLower();
            
            if (string.IsNullOrWhiteSpace(query))
            {
                // Reset to current group view
                if (commandGroups.Count > 0)
                {
                    List<CommandItem> groupCmds;
                    if (commandGroups[currentGroupIndex].name == "Favorites")
                    {
                        groupCmds = commandGroups[currentGroupIndex].commands
                            .OrderByDescending(c => c.UsageCount)
                            .ThenBy(c => c.label)
                            .ToList();
                    }
                    else
                    {
                        groupCmds = commandGroups[currentGroupIndex].commands
                            .OrderBy(c => c.label)
                            .ToList();
                    }
                    UpdateList(groupCmds, showBreadcrumbs: false);
                }
            }
            else
            {
                     // Global Search across ALL groups
                     if (commandGroups.Count > 0)
                     {
                         var allCommands = commandGroups
                            .Where(g => g.name != "Favorites") // Exclude duplicates
                            .SelectMany(g => g.commands)
                            .ToList();
                         
                          var filtered = allCommands.Where(c => 
                             c.label.ToLower().Contains(query) || 
                             c.value.ToLower().Contains(query)
                          )
                          .OrderByDescending(c => c.UsageCount) // Frequency Sort
                          .ThenBy(c => c.label, StringComparer.OrdinalIgnoreCase) // Case-insensitive alphabetical
                          .ToList();

                          UpdateList(filtered, showBreadcrumbs: true);
                      }
             }
         }
        
        private void ResultMap_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (resultMap.SelectedItem is CommandItem item)
            {
                string usageText = item.UsageCount > 0 ? $"\nUsed: {item.UsageCount} times" : "";
                string tip = item.value;
                if (!string.IsNullOrEmpty(item.description))
                {
                    tip = $"{item.GroupName} > {item.label}\n{item.description}\nValue: {item.value}{usageText}";
                }
                else
                {
                     tip = $"{item.GroupName} > {item.label}\nValue: {item.value}{usageText}";
                }
                toolTip.SetToolTip(resultMap, tip);
            }
        }

        private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                if (resultMap.Items.Count > 0)
                {
                    resultMap.Focus();
                    if (resultMap.SelectedIndex < resultMap.Items.Count - 1)
                        resultMap.SelectedIndex++;
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Right)
            {
                if (searchBox.SelectionStart == searchBox.Text.Length)
                {
                    SelectGroup(currentGroupIndex + 1);
                    e.Handled = true;
                }
            }
            else if (e.KeyCode == Keys.Left)
            {
                if (searchBox.SelectionStart == 0)
                {
                    SelectGroup(currentGroupIndex - 1);
                    e.Handled = true;
                }
            }
            else if (e.KeyCode == Keys.Enter)
            {
                ConfirmSelection();
                e.Handled = true;
                e.SuppressKeyPress = true; // Prevent ding sound
            }
            else if (e.KeyCode == Keys.Escape)
            {
                CloseForm();
            }
        }

        private void ResultMap_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ConfirmSelection();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                CloseForm();
            }
            else if (e.KeyCode == Keys.Up && resultMap.SelectedIndex == 0)
            {
                searchBox.Focus();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Left)
            {
                SelectGroup(currentGroupIndex - 1);
                e.Handled = true;
                searchBox.Focus();
            }
            else if (e.KeyCode == Keys.Right)
            {
                SelectGroup(currentGroupIndex + 1);
                e.Handled = true;
                searchBox.Focus();
            }
        }

        private void ResultMap_DoubleClick(object? sender, EventArgs e)
        {
            ConfirmSelection();
        }

        private void ConfirmSelection()
        {
            if (resultMap.SelectedItem is CommandItem item)
            {
                if (item.value.StartsWith("::LOAD_PROFILE::"))
                {
                    string newProfile = item.value.Replace("::LOAD_PROFILE::", "");
                    settings.CommandFile = newProfile;
                    // Do NOT save settings here? Or should we?
                    // Program.SaveSettings(settings); // Maybe save so it persists
                    
                    // Reload everything
                    // Re-init Usage Manager
                    string exePath = AppDomain.CurrentDomain.BaseDirectory;
                    string usageFile = "usage.json";
                    if (newProfile.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                         string baseName = Path.GetFileNameWithoutExtension(newProfile);
                         usageFile = $"{baseName}_usage.json";
                    }
                    usageManager = new UsageManager(Path.Combine(exePath, usageFile));
                    
                    LoadCommands();
                    searchBox.Text = "";
                    searchBox.Focus();
                    return;
                }

                // Track Usage!
                usageManager.Increment(item.value);
                
                SelectedValue = item.value;
                
                if (SelectedValue == "::SWITCH_PROFILE::")
                {
                    this.DialogResult = DialogResult.None;
                    SwitchProfile();
                    return;
                }

                if (SelectedValue == "::MANAGE_COMMANDS::")
                {
                    this.DialogResult = DialogResult.None;
                    ShowManager();
                    return;
                }

                if (SelectedValue == "::SETTINGS::")
                {
                    this.DialogResult = DialogResult.None;
                    ShowSettings();
                    return;
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void ShowManager()
        {
            var manager = new CommandManagerForm(this, settings);
            if (manager.ShowDialog(this) == DialogResult.OK)
            {
                LoadCommands(); // Refresh UI
            }
        }

        private void ShowSettings()
        {
            // We use the same context logic as Program.cs if possible, 
            // but since palette is already open, we can just trigger it.
            // However, Program.cs owns the RegisterHotKey and TrayIcon.
            // Let's assume we can trigger the "ShowSettings" via a callback or just launch it here.
            // Launching it here is simpler for layout/theme refresh.
            
            string oldHotkey = settings.hotkey;
            var form = new SettingsForm(settings, (s) => {
                RefreshTheme();
                // Note: Hotkey update is handled by Program.cs if we can pass it up,
                // but here we just need the UI to look right.
            });
            
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                // If hotkey changed, we might need to alert the user it requires app restart or handle it?
                // Actually the Program.cs ShowSettings handles it.
                // Let's just refresh theme for now.
                RefreshTheme();
            }
        }

        public void RefreshTheme()
        {
            this.Size = new Size(settings.WindowWidth, settings.WindowHeight);
            
            // Update fonts
            searchBox.Font = new Font("Segoe UI", settings.FontSize + 2);
            resultMap.Font = new Font("Segoe UI", settings.FontSize);
            resultMap.ItemHeight = (int)(settings.FontSize * 2);

            // Redraw list to apply colors
            resultMap.Invalidate();
        }

        private void SwitchProfile()
        {
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            var candidates = Directory.GetFiles(exePath, "*.json");
            
            var profiles = new List<CommandItem>();
            foreach (var file in candidates)
            {
                string filename = Path.GetFileName(file);
                // Filter out non-command files
                if (filename.Equals("settings.json", StringComparison.OrdinalIgnoreCase)) continue;
                if (filename.EndsWith("_usage.json", StringComparison.OrdinalIgnoreCase)) continue;
                if (filename.EndsWith(".deps.json", StringComparison.OrdinalIgnoreCase)) continue;
                if (filename.EndsWith(".runtimeconfig.json", StringComparison.OrdinalIgnoreCase)) continue;
                if (filename.EndsWith(".sourcelink.json", StringComparison.OrdinalIgnoreCase)) continue;
                
                profiles.Add(new CommandItem 
                { 
                    label = filename, 
                    value = $"::LOAD_PROFILE::{filename}", 
                    description = "Load this command profile",
                    GroupName = "Profiles"
                });
            }

            // Temporarily replace the list with profiles
            // We can just show them in the list box
            UpdateList(profiles, showBreadcrumbs: false);
            searchBox.Text = ""; // Clear search
            searchBox.Focus();
            
            // Hack: Hijack the selection logic for the next enter
            // We need to change the state or just handle ::LOAD_PROFILE:: in ConfirmSelection
        }
        
        private void Form_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                CloseForm();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }
        
        private void CloseForm()
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            searchBox.Focus();
            // Reload commands to get fresh usage stats if changed?
            // Usually LoadCommands is enough, but reloading usage ensures sync
            LoadCommands(); 
            searchBox.Text = "";
        }
    }
}
