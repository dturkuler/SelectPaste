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

        public override string ToString()
        {
            return label;
        }
    }

    public class CommandGroup
    {
        public string name { get; set; } = "General";
        public string description { get; set; } = ""; // Added Group Tooltip
        public List<CommandItem> commands { get; set; } = new List<CommandItem>();
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
            
            UpdateList(commandGroups[currentGroupIndex].commands);
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

        private void UpdateList(List<CommandItem> items)
        {
            resultMap.Items.Clear();
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
                if (commandGroups.Count > 0)
                    UpdateList(commandGroups[currentGroupIndex].commands);
            }
            else
            {
                 if (commandGroups.Count > 0)
                 {
                     var allCommands = commandGroups.SelectMany(g => g.commands).ToList();
                     var filtered = allCommands.Where(c => 
                        c.label.ToLower().Contains(query) || 
                        c.description.ToLower().Contains(query)
                     ).ToList();
                     UpdateList(filtered);
                 }
            }
        }
        
        private void ResultMap_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (resultMap.SelectedItem is CommandItem item)
            {
                string tip = item.value;
                if (!string.IsNullOrEmpty(item.description))
                {
                    tip = $"{item.description}\nValue: {item.value}";
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
                e.SuppressKeyPress = true;
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
            LoadCommands(); 
            searchBox.Text = "";
        }
    }
}
