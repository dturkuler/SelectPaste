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

        // Custom ToString to handle contextual breadcrumbs and value preview
        public override string ToString()
        {
            string groupPrefix = !string.IsNullOrEmpty(GroupName) ? $"[{GroupName.ToUpper()}] " : "";
            
            // Preview the value (shortened if too long)
            string valuePreview = value ?? "";
            if (valuePreview.Length > 40) valuePreview = valuePreview.Substring(0, 37) + "...";
            
            return $"{groupPrefix}{label} -> {valuePreview}";
        }
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

        public UsageManager()
        {
            filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "usage.json");
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
        private FlowLayoutPanel tabHeaderPanel;
        private Label closeButton; 
        private ToolTip toolTip;

        private List<CommandGroup> commandGroups = new List<CommandGroup>();
        private int currentGroupIndex = 0;
        private UsageManager usageManager;
        
        public string SelectedValue { get; private set; } = "";

        public CommandPaletteForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(600, 400); 
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.KeyPreview = true; 

            // Initialize Usage Manager
            usageManager = new UsageManager();

            // Initialize ToolTip
            toolTip = new ToolTip();
            toolTip.AutoPopDelay = 5000;
            toolTip.InitialDelay = 500;
            toolTip.ReshowDelay = 100;
            toolTip.ShowAlways = true;

            InitializeComponents();
            LoadCommands();
            
            this.KeyDown += Form_KeyDown;
        }

        private void InitializeComponents()
        {
            // Tabs Header
            tabHeaderPanel = new FlowLayoutPanel
            {
                Location = new Point(10, 5),
                Size = new Size(540, 30), 
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            // Close Button "X"
            closeButton = new Label
            {
                Text = "X",
                Location = new Point(570, 5),
                Size = new Size(20, 20),
                ForeColor = Color.IndianRed,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            closeButton.Click += (s, e) => CloseForm();
            closeButton.MouseEnter += (s, e) => closeButton.ForeColor = Color.Red;
            closeButton.MouseLeave += (s, e) => closeButton.ForeColor = Color.IndianRed;

            searchBox = new TextBox
            {
                Location = new Point(10, 40),
                Width = 580,
                Font = new Font("Segoe UI", 12),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            searchBox.TextChanged += SearchBox_TextChanged;
            searchBox.KeyDown += SearchBox_KeyDown;

            resultMap = new ListBox
            {
                Location = new Point(10, 80),
                Width = 580,
                Height = 310,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.LightGray,
                BorderStyle = BorderStyle.None
            };
            resultMap.DoubleClick += ResultMap_DoubleClick;
            resultMap.KeyDown += ResultMap_KeyDown;
            resultMap.SelectedIndexChanged += ResultMap_SelectedIndexChanged;

            this.Controls.Add(tabHeaderPanel);
            this.Controls.Add(closeButton);
            this.Controls.Add(searchBox);
            this.Controls.Add(resultMap);
        }

        private void LoadCommands()
        {
            try
            {
                // Ensure we read from the executable's directory
                string exePath = AppDomain.CurrentDomain.BaseDirectory;
                string commandsPath = Path.Combine(exePath, "commands.json");

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
            
            // When selecting a specific group tab, sort by Usage, but keep it deterministic
            var sortedCommands = commandGroups[currentGroupIndex].commands
                .OrderByDescending(c => c.UsageCount)
                .ThenBy(c => c.label)
                .ToList();

            UpdateList(sortedCommands, showBreadcrumbs: false); // No need for "GIT > Push" inside Git tab
            searchBox.Focus(); 
        }

        private void UpdateTabsUI()
        {
            tabHeaderPanel.Controls.Clear();
            if (commandGroups.Count <= 1) return;

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
            }
        }

        private void UpdateList(List<CommandItem> items, bool showBreadcrumbs)
        {
            resultMap.Items.Clear();
            foreach (var item in items)
            {
                // Temporarily override ToString logic via flag? 
                // Alternatively, just rely on GroupName being present.
                // Since we want "Clean" list in tabs, and "Breadcrumbs" in Global Search.
                
                // Hack: We can clear GroupName temporarily if we don't want breadcrumbs,
                // but that mutates state. Better: ListBox uses ToString().
                // Let's modify CommandItem.ToString() to use a static flag or just ALWAYS show breadcrumbs?
                // The user specifically asked: "When I search ... show Git > Push".
                // This implies Breadcrumbs are mostly for Search context or All context.
                // Let's keep it simple: ToString always shows GroupName if present.
                // But in Tab view, maybe we want it cleaner? 
                // Let's stick to showing [GROUP] Item. It provides clarity.
                
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
                    var groupCmds = commandGroups[currentGroupIndex].commands
                        .OrderByDescending(c => c.UsageCount)
                        .ToList();
                    UpdateList(groupCmds, showBreadcrumbs: false);
                }
            }
            else
            {
                 // Global Search across ALL groups
                 if (commandGroups.Count > 0)
                 {
                     var allCommands = commandGroups.SelectMany(g => g.commands).ToList();
                     
                     var filtered = allCommands.Where(c => 
                        c.label.ToLower().Contains(query) || 
                        c.description.ToLower().Contains(query) ||
                        c.GroupName.ToLower().Contains(query) // Contextual match
                     )
                     .OrderByDescending(c => c.UsageCount) // Frequency Sort
                     .ThenBy(c => c.label.Length) // Shortest match first
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
                // Track Usage!
                usageManager.Increment(item.value);
                
                SelectedValue = item.value;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
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
