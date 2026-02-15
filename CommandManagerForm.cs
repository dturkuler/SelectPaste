using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Text.Json;
using System.IO;
using System.Text.Encodings.Web;

namespace SelectPaste
{
    public class CommandManagerForm : Form
    {
        private CommandPaletteForm paletteForm;
        private Program.AppSettings settings;
        private ListBox groupList;
        private ListBox commandList;
        private bool hasChanges = false;
        private TabControl detailsTabControl;
        private TabPage structuredTab;
        private TabPage rawJsonTab;
        private TextBox rawJsonEditor;
        
        // Structured Fields
        private ComboBox comboGroup;
        private TextBox txtLabel;
        private TextBox txtValue;
        private TextBox txtDescription;

        public CommandManagerForm(CommandPaletteForm palette, Program.AppSettings settings)
        {
            this.paletteForm = palette;
            this.settings = settings;
            this.Text = "Command & Group Manager";
            this.Size = new Size(950, 800);
            this.MinimumSize = new Size(800, 600);
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.LightGray;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.TopMost = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            InitializeComponents();
            LoadGroups();
        }

        private void InitializeComponents()
        {
            var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));

            // Left Panel (Groups)
            var groupPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            var lblGroups = new Label { Text = "Groups", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            groupList = new ListBox 
            { 
                Dock = DockStyle.Fill, 
                BackColor = Color.FromArgb(40, 40, 40), 
                ForeColor = Color.White, 
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            groupList.SelectedIndexChanged += GroupList_SelectedIndexChanged;

            var groupButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, FlowDirection = FlowDirection.LeftToRight };
            var btnAddGroup = CreateButton("Add", () => AddGroup());
            var btnEditGroup = CreateButton("Edit", () => EditGroup());
            var btnDelGroup = CreateButton("Del", () => DeleteGroup());
            groupButtons.Controls.AddRange(new Control[] { btnAddGroup, btnEditGroup, btnDelGroup });

            groupPanel.Controls.Add(groupList);
            groupPanel.Controls.Add(lblGroups);
            groupPanel.Controls.Add(groupButtons);

            // Right Panel (Commands)
            var commandPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            var lblCommands = new Label { Text = "Commands", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            
            var splitCommand = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 40 };
            
            commandList = new ListBox 
            { 
                Dock = DockStyle.Fill, 
                BackColor = Color.FromArgb(40, 40, 40), 
                ForeColor = Color.White, 
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                ScrollAlwaysVisible = true
            };
            commandList.SelectedIndexChanged += CommandList_SelectedIndexChanged;

            detailsTabControl = new TabControl { Dock = DockStyle.Fill };
            structuredTab = new TabPage("Structured") { BackColor = Color.FromArgb(30, 30, 30) };
            rawJsonTab = new TabPage("Raw JSON") { BackColor = Color.FromArgb(30, 30, 30) };

            // Structured Tab Layout
            var structLayout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, RowCount = 5, Padding = new Padding(10), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            structLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            structLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            
            structLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); // Group
            structLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); // Label
            structLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Value (2 lines)
            structLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Description (2 lines)
            structLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));  // Save Button

            structLayout.Controls.Add(new Label { Text = "Group:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            comboGroup = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White };
            structLayout.Controls.Add(comboGroup, 1, 0);

            structLayout.Controls.Add(new Label { Text = "Label:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            txtLabel = new TextBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White };
            structLayout.Controls.Add(txtLabel, 1, 1);

            structLayout.Controls.Add(new Label { Text = "Value:", AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Left }, 0, 2);
            txtValue = new TextBox { Dock = DockStyle.Fill, Multiline = true, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, ScrollBars = ScrollBars.Vertical };
            structLayout.Controls.Add(txtValue, 1, 2);

            structLayout.Controls.Add(new Label { Text = "Description:", AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Left }, 0, 3);
            txtDescription = new TextBox { Dock = DockStyle.Fill, Multiline = true, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, ScrollBars = ScrollBars.Vertical };
            structLayout.Controls.Add(txtDescription, 1, 3);

            var btnSaveCmd = CreateButton("Save Command", () => SaveCurrentCommand());
            btnSaveCmd.Anchor = AnchorStyles.Right;
            structLayout.Controls.Add(btnSaveCmd, 1, 4);
            
            var scrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            scrollPanel.Controls.Add(structLayout);
            structuredTab.Controls.Add(scrollPanel);

            // Raw JSON Tab Layout
            rawJsonEditor = new TextBox 
            { 
                Dock = DockStyle.Fill, 
                Multiline = true, 
                BackColor = Color.FromArgb(40, 40, 40), 
                ForeColor = Color.Lime, 
                Font = new Font("Consolas", 10),
                ScrollBars = ScrollBars.Both
            };
            var btnSaveRaw = CreateButton("Apply JSON", () => ApplyRawJson());
            var rawContainer = new Panel { Dock = DockStyle.Fill };
            rawContainer.Controls.Add(rawJsonEditor);
            var rawBottom = new Panel { Dock = DockStyle.Bottom, Height = 45 };
            rawBottom.Controls.Add(btnSaveRaw);
            rawJsonTab.Controls.Add(rawContainer);
            rawJsonTab.Controls.Add(rawBottom);

            detailsTabControl.TabPages.Add(structuredTab);
            detailsTabControl.TabPages.Add(rawJsonTab);
            detailsTabControl.Padding = new Point(0, 0);

            var commandButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, FlowDirection = FlowDirection.LeftToRight };
            var btnAddCmd = CreateButton("Add Cmd", () => AddCommand());
            var btnDelCmd = CreateButton("Delete Cmd", () => DeleteCommand());
            var btnMoveCmd = CreateButton("Move To...", () => MoveCommand());
            var btnClose = CreateButton("Close", () => {
                this.DialogResult = hasChanges ? DialogResult.OK : DialogResult.Cancel;
                this.Close();
            });
            commandButtons.Controls.AddRange(new Control[] { btnAddCmd, btnDelCmd, btnMoveCmd, btnClose });

            splitCommand.Panel1.Controls.Add(commandList);
            splitCommand.Panel2.Controls.Add(detailsTabControl);

            commandPanel.Controls.Add(splitCommand);
            commandPanel.Controls.Add(lblCommands);
            commandPanel.Controls.Add(commandButtons);

            mainLayout.Controls.Add(groupPanel, 0, 0);
            mainLayout.Controls.Add(commandPanel, 1, 0);
            this.Controls.Add(mainLayout);
        }

        private Button CreateButton(string text, Action onClick)
        {
            var btn = new Button 
            { 
                Text = text, 
                FlatStyle = FlatStyle.Flat, 
                BackColor = Color.FromArgb(60, 60, 60), 
                ForeColor = Color.White,
                AutoSize = true,
                Padding = new Padding(5)
            };
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void LoadGroups()
        {
            groupList.Items.Clear();
            comboGroup.Items.Clear();
            if (paletteForm.commandGroups == null || paletteForm.commandGroups.Count == 0)
            {
                // Fallback if somehow empty
                paletteForm.commandGroups = new List<CommandGroup> { new CommandGroup { name = "General" } };
            }

            foreach (var group in paletteForm.commandGroups)
            {
                if (group.name == "Favorites" || group.name == "System") continue;
                groupList.Items.Add(group.name);
                comboGroup.Items.Add(group.name);
            }
            if (groupList.Items.Count > 0) groupList.SelectedIndex = 0;
        }

        private void GroupList_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadCommandsForSelectedGroup();
        }

        private void LoadCommandsForSelectedGroup()
        {
            commandList.Items.Clear();
            if (groupList.SelectedItem == null) return;
            string groupName = groupList.SelectedItem.ToString();
            var group = paletteForm.commandGroups.FirstOrDefault(g => g.name == groupName);
            if (group != null)
            {
                foreach (var cmd in group.commands.OrderBy(c => c.label))
                {
                    commandList.Items.Add(cmd);
                }
            }
            if (commandList.Items.Count > 0) commandList.SelectedIndex = 0;
            else ClearCommandFields();
        }

        private void CommandList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (commandList.SelectedItem is CommandItem cmd)
            {
                comboGroup.SelectedItem = cmd.GroupName;
                txtLabel.Text = cmd.label;
                txtValue.Text = cmd.value;
                txtDescription.Text = cmd.description;
                
                var options = new JsonSerializerOptions { WriteIndented = false };
                rawJsonEditor.Text = JsonSerializer.Serialize(cmd, options);
            }
            else
            {
                ClearCommandFields();
            }
        }

        private void ClearCommandFields()
        {
            txtLabel.Text = "";
            txtValue.Text = "";
            txtDescription.Text = "";
            rawJsonEditor.Text = "";
        }

        private void AddGroup()
        {
            using (var dlg = new GroupEditDialog())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    if (paletteForm.commandGroups.Any(g => g.name.Equals(dlg.GroupName, StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show("Group already exists.");
                        return;
                    }
                    paletteForm.commandGroups.Add(new CommandGroup { name = dlg.GroupName, description = dlg.GroupDescription });
                    SaveAndRefresh();
                    LoadGroups();
                    groupList.SelectedItem = dlg.GroupName;
                }
            }
        }

        private void EditGroup()
        {
            if (groupList.SelectedItem == null) return;
            string oldName = groupList.SelectedItem.ToString();
            if (oldName == "General" && MessageBox.Show("Are you sure you want to rename the General group?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.No) return;

            var group = paletteForm.commandGroups.FirstOrDefault(g => g.name == oldName);
            if (group == null) return;

            using (var dlg = new GroupEditDialog(group.name, group.description))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    group.name = dlg.GroupName;
                    group.description = dlg.GroupDescription;
                    foreach (var cmd in group.commands) cmd.GroupName = group.name;
                    SaveAndRefresh();
                    LoadGroups();
                    groupList.SelectedItem = dlg.GroupName;
                }
            }
        }

        private void DeleteGroup()
        {
            if (groupList.SelectedItem == null) return;
            string groupName = groupList.SelectedItem.ToString();
            if (groupName == "General")
            {
                MessageBox.Show("Cannot delete the General group.");
                return;
            }

            var group = paletteForm.commandGroups.FirstOrDefault(g => g.name == groupName);
            if (group == null) return;

            var result = MessageBox.Show($"Delete group '{groupName}'?\nChoose 'Yes' to delete commands, 'No' to move them to 'General'.", "Delete Group", MessageBoxButtons.YesNoCancel);
            
            if (result == DialogResult.Yes)
            {
                paletteForm.commandGroups.Remove(group);
            }
            else if (result == DialogResult.No)
            {
                var general = paletteForm.commandGroups.FirstOrDefault(g => g.name == "General");
                if (general == null)
                {
                    general = new CommandGroup { name = "General" };
                    paletteForm.commandGroups.Add(general);
                }
                foreach (var cmd in group.commands)
                {
                    cmd.GroupName = "General";
                    general.commands.Add(cmd);
                }
                paletteForm.commandGroups.Remove(group);
            }
            else return;

            SaveAndRefresh();
            LoadGroups();
        }

        private void AddCommand()
        {
            if (groupList.SelectedItem == null) return;
            string groupName = groupList.SelectedItem.ToString();
            var group = paletteForm.commandGroups.FirstOrDefault(g => g.name == groupName);
            if (group == null) return;

            var newCmd = new CommandItem { label = "New Command", value = "", GroupName = groupName };
            group.commands.Add(newCmd);
            SaveAndRefresh();
            LoadCommandsForSelectedGroup();
            commandList.SelectedItem = newCmd;
        }

        private void DeleteCommand()
        {
            if (commandList.SelectedItem is CommandItem cmd)
            {
                if (MessageBox.Show($"Delete command '{cmd.label}'?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    var group = paletteForm.commandGroups.FirstOrDefault(g => g.name == cmd.GroupName);
                    group?.commands.Remove(cmd);
                    SaveAndRefresh();
                    LoadCommandsForSelectedGroup();
                }
            }
        }

        private void MoveCommand()
        {
            if (commandList.SelectedItem is CommandItem cmd)
            {
                var otherGroups = paletteForm.commandGroups
                    .Where(g => g.name != "Favorites" && g.name != "System" && g.name != cmd.GroupName)
                    .Select(g => g.name).ToList();

                if (otherGroups.Count == 0)
                {
                    MessageBox.Show("No other groups available.");
                    return;
                }

                using (var dlg = new MoveToGroupDialog(otherGroups))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        var oldGroup = paletteForm.commandGroups.FirstOrDefault(g => g.name == cmd.GroupName);
                        var newGroup = paletteForm.commandGroups.FirstOrDefault(g => g.name == dlg.SelectedGroup);
                        
                        oldGroup?.commands.Remove(cmd);
                        cmd.GroupName = dlg.SelectedGroup;
                        newGroup?.commands.Add(cmd);
                        
                        SaveAndRefresh();
                        LoadCommandsForSelectedGroup();
                    }
                }
            }
        }

        private void SaveCurrentCommand()
        {
            if (commandList.SelectedItem is CommandItem cmd)
            {
                string targetGroup = comboGroup.SelectedItem?.ToString() ?? cmd.GroupName;

                if (targetGroup != cmd.GroupName)
                {
                    var oldGroup = paletteForm.commandGroups.FirstOrDefault(g => g.name == cmd.GroupName);
                    var newGroup = paletteForm.commandGroups.FirstOrDefault(g => g.name == targetGroup);
                    
                    oldGroup?.commands.Remove(cmd);
                    cmd.GroupName = targetGroup;
                    newGroup?.commands.Add(cmd);
                }

                cmd.label = txtLabel.Text;
                cmd.value = txtValue.Text;
                cmd.description = txtDescription.Text;
                SaveAndRefresh();
                
                int index = commandList.SelectedIndex;
                LoadCommandsForSelectedGroup();
                if (index >= 0 && index < commandList.Items.Count) commandList.SelectedIndex = index;
            }
        }

        private void ApplyRawJson()
        {
            if (commandList.SelectedItem is CommandItem cmd)
            {
                try
                {
                    var updated = JsonSerializer.Deserialize<CommandItem>(rawJsonEditor.Text);
                    if (updated != null)
                    {
                        if (!string.IsNullOrEmpty(updated.GroupName) && updated.GroupName != cmd.GroupName)
                        {
                            var oldGroup = paletteForm.commandGroups.FirstOrDefault(g => g.name == cmd.GroupName);
                            var newGroup = paletteForm.commandGroups.FirstOrDefault(g => g.name == updated.GroupName);
                            
                            if (newGroup != null)
                            {
                                oldGroup?.commands.Remove(cmd);
                                cmd.GroupName = updated.GroupName;
                                newGroup.commands.Add(cmd);
                            }
                        }

                        cmd.label = updated.label;
                        cmd.value = updated.value;
                        cmd.description = updated.description;
                        SaveAndRefresh();
                        LoadCommandsForSelectedGroup();
                        MessageBox.Show("JSON applied successfully.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Invalid JSON: " + ex.Message);
                }
            }
        }

        private void SaveAndRefresh()
        {
            try
            {
                string exePath = AppDomain.CurrentDomain.BaseDirectory;
                string commandsPath = Path.Combine(exePath, settings.CommandFile);
                
                // Exclude system/favorites from persistent storage
                var persistentGroups = paletteForm.commandGroups
                    .Where(g => g.name != "Favorites" && g.name != "System")
                    .ToList();

                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                
                string json = JsonSerializer.Serialize(persistentGroups, options);
                File.WriteAllText(commandsPath, json);
                
                hasChanges = true; // Mark as changed for later reload
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save: " + ex.Message);
            }
        }
    }

    public class GroupEditDialog : Form
    {
        public string GroupName => txtName.Text;
        public string GroupDescription => txtDesc.Text;
        private TextBox txtName;
        private TextBox txtDesc;

        public GroupEditDialog(string name = "", string desc = "")
        {
            this.Text = string.IsNullOrEmpty(name) ? "Add Group" : "Edit Group";
            this.Size = new Size(300, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.TopMost = true;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), ColumnCount = 2, RowCount = 3 };
            layout.Controls.Add(new Label { Text = "Name:", AutoSize = true }, 0, 0);
            txtName = new TextBox { Text = name, Dock = DockStyle.Fill, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White };
            layout.Controls.Add(txtName, 1, 0);

            layout.Controls.Add(new Label { Text = "Description:", AutoSize = true }, 0, 1);
            txtDesc = new TextBox { Text = desc, Dock = DockStyle.Fill, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White };
            layout.Controls.Add(txtDesc, 1, 1);

            var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };
            
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            buttons.Controls.Add(btnCancel);
            buttons.Controls.Add(btnOk);
            layout.Controls.Add(buttons, 1, 2);

            this.Controls.Add(layout);
            this.AcceptButton = btnOk;
        }
    }

    public class MoveToGroupDialog : Form
    {
        public string SelectedGroup => comboGroups.SelectedItem?.ToString() ?? "";
        private ComboBox comboGroups;

        public MoveToGroupDialog(List<string> groups)
        {
            this.Text = "Move to Group";
            this.Size = new Size(300, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(30,30,30);
            this.ForeColor = Color.White;
            this.TopMost = true;

            var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            layout.Controls.Add(new Label { Text = "Select target group:", AutoSize = true });
            
            comboGroups = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240, BackColor = Color.FromArgb(50,50,50), ForeColor = Color.White };
            foreach (var g in groups) comboGroups.Items.Add(g);
            if (comboGroups.Items.Count > 0) comboGroups.SelectedIndex = 0;
            layout.Controls.Add(comboGroups);

            var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat };
            layout.Controls.Add(btnOk);

            this.Controls.Add(layout);
            this.AcceptButton = btnOk;
        }
    }
}
