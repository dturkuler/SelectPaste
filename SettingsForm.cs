using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;

namespace SelectPaste
{
    public partial class SettingsForm : Form
    {
        private Program.AppSettings settings;
        private Program.AppSettings originalSettings;
        private Action<Program.AppSettings>? onApply;

        private Label lblHotkey;
        private TextBox txtHotkey;
        private Label lblWindowSize;
        private NumericUpDown numWidth;
        private NumericUpDown numHeight;
        private Label lblFontSize;
        private NumericUpDown numFontSize;
        
        private Label lblColors;
        private ColorField groupLabelColor;
        private ColorField groupValueColor;
        private ColorField groupCategoryColor;

        private Label lblCommandFile;
        private TextBox txtCommandFile;
        private Button btnBrowseCommand;

        private Button btnSave;
        private Button btnApply;
        private Button btnCancel;

        public SettingsForm(Program.AppSettings currentSettings, Action<Program.AppSettings>? applyCallback = null)
        {
            this.settings = CloneSettings(currentSettings);
            this.originalSettings = currentSettings;
            this.onApply = applyCallback;

            InitializeComponent();
            LoadSettingsIntoUI();
        }

        private Program.AppSettings CloneSettings(Program.AppSettings s)
        {
            return new Program.AppSettings
            {
                hotkey = s.hotkey,
                WindowWidth = s.WindowWidth,
                WindowHeight = s.WindowHeight,
                WindowX = s.WindowX,
                WindowY = s.WindowY,
                FontSize = s.FontSize,
                LabelColor = s.LabelColor,
                ValueColor = s.ValueColor,
                CategoryColor = s.CategoryColor,
                CommandFile = s.CommandFile,
                ExtensionData = s.ExtensionData
            };
        }

        private void InitializeComponent()
        {
            this.Text = "SelectPaste Settings";
            this.Size = new Size(450, 550);
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 10);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.TopMost = true;

            int y = 20;
            int margin = 20;
            int labelWidth = 120;
            int controlWidth = 250;

            // Hotkey
            lblHotkey = CreateLabel("Global Hotkey:", margin, y);
            txtHotkey = new TextBox
            {
                Location = new Point(margin + labelWidth, y),
                Width = controlWidth,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true
            };
            txtHotkey.KeyDown += TxtHotkey_KeyDown;
            this.Controls.Add(lblHotkey);
            this.Controls.Add(txtHotkey);
            y += 40;

            // Window Size
            lblWindowSize = CreateLabel("Palette Size:", margin, y);
            numWidth = CreateNumeric(margin + labelWidth, y, 80, 200, 2000);
            numHeight = CreateNumeric(margin + labelWidth + 90, y, 80, 200, 2000);
            this.Controls.Add(lblWindowSize);
            this.Controls.Add(numWidth);
            this.Controls.Add(numHeight);
            y += 40;

            // Font Size
            lblFontSize = CreateLabel("Font Size:", margin, y);
            numFontSize = CreateNumeric(margin + labelWidth, y, 80, 8, 32);
            this.Controls.Add(lblFontSize);
            this.Controls.Add(numFontSize);
            y += 50;

            // Colors
            lblColors = new Label
            {
                Text = "THEME COLORS",
                Location = new Point(margin, y),
                Size = new Size(controlWidth, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.Gray
            };
            this.Controls.Add(lblColors);
            y += 30;

            groupLabelColor = new ColorField("Label Color", margin, y, this);
            y += 40;
            groupValueColor = new ColorField("Value Color", margin, y, this);
            y += 40;
            groupCategoryColor = new ColorField("Category Color", margin, y, this);
            y += 60;

            // Command File
            lblCommandFile = CreateLabel("Command File:", margin, y);
            txtCommandFile = new TextBox
            {
                Location = new Point(margin + labelWidth, y),
                Width = controlWidth - 40,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            btnBrowseCommand = new Button
            {
                Text = "...",
                Location = new Point(margin + labelWidth + controlWidth - 35, y),
                Size = new Size(35, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60)
            };
            btnBrowseCommand.Click += BtnBrowseCommand_Click;
            this.Controls.Add(lblCommandFile);
            this.Controls.Add(txtCommandFile);
            this.Controls.Add(btnBrowseCommand);
            y += 70;

            // Buttons
            btnSave = new Button
            {
                Text = "Save",
                Location = new Point(140, y),
                Size = new Size(90, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 120, 215)
            };
            btnSave.Click += (s, e) => SaveAndClose();

            btnApply = new Button
            {
                Text = "Apply",
                Location = new Point(240, y),
                Size = new Size(90, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60)
            };
            btnApply.Click += (s, e) => ApplyChanges();

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(340, y),
                Size = new Size(90, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60)
            };
            btnCancel.Click += (s, e) => this.Close();

            this.Controls.Add(btnSave);
            this.Controls.Add(btnApply);
            this.Controls.Add(btnCancel);
            
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        private Label CreateLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(110, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private NumericUpDown CreateNumeric(int x, int y, int width, int min, int max)
        {
            var num = new NumericUpDown
            {
                Location = new Point(x, y),
                Width = width,
                Minimum = min,
                Maximum = max,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            return num;
        }

        private void TxtHotkey_KeyDown(object? sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
            e.Handled = true;

            if (e.KeyCode == Keys.Escape) return;
            if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
            {
                txtHotkey.Text = "";
                return;
            }

            // Don't record just modifier keys
            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Menu || e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin)
                return;

            List<string> parts = new List<string>();
            if (e.Control) parts.Add("Ctrl");
            if (e.Alt) parts.Add("Alt");
            if (e.Shift) parts.Add("Shift");
            
            string keyName = e.KeyCode.ToString();
            // Handle common display names
            if (e.KeyCode == Keys.OemPeriod) keyName = ".";
            else if (e.KeyCode == Keys.Oemcomma) keyName = ",";
            else if (e.KeyCode == Keys.Oem1) keyName = ";";
            else if ((int)e.KeyCode == 191) keyName = "/"; // OemQuestion
            else if ((int)e.KeyCode == 219) keyName = "["; // OemOpenBrackets
            else if ((int)e.KeyCode == 221) keyName = "]"; // Oem6 / CloseBrackets
            
            parts.Add(keyName);
            txtHotkey.Text = string.Join(" + ", parts);
        }

        private void BtnBrowseCommand_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                ofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string file = ofd.FileName;
                    if (file.StartsWith(AppDomain.CurrentDomain.BaseDirectory))
                    {
                        file = Path.GetFileName(file);
                    }
                    txtCommandFile.Text = file;
                }
            }
        }

        private void LoadSettingsIntoUI()
        {
            txtHotkey.Text = settings.hotkey;
            numWidth.Value = settings.WindowWidth;
            numHeight.Value = settings.WindowHeight;
            numFontSize.Value = (decimal)settings.FontSize;
            
            groupLabelColor.Hex = settings.LabelColor;
            groupValueColor.Hex = settings.ValueColor;
            groupCategoryColor.Hex = settings.CategoryColor;
            
            txtCommandFile.Text = settings.CommandFile;
        }

        private void SyncSettingsFromUI()
        {
            settings.hotkey = txtHotkey.Text;
            settings.WindowWidth = (int)numWidth.Value;
            settings.WindowHeight = (int)numHeight.Value;
            settings.FontSize = (float)numFontSize.Value;
            
            settings.LabelColor = groupLabelColor.Hex;
            settings.ValueColor = groupValueColor.Hex;
            settings.CategoryColor = groupCategoryColor.Hex;
            
            settings.CommandFile = txtCommandFile.Text;
        }

        private void ApplyChanges()
        {
            SyncSettingsFromUI();
            
            // Update the original object reference properties
            originalSettings.hotkey = settings.hotkey;
            originalSettings.WindowWidth = settings.WindowWidth;
            originalSettings.WindowHeight = settings.WindowHeight;
            originalSettings.FontSize = settings.FontSize;
            originalSettings.LabelColor = settings.LabelColor;
            originalSettings.ValueColor = settings.ValueColor;
            originalSettings.CategoryColor = settings.CategoryColor;
            originalSettings.CommandFile = settings.CommandFile;

            onApply?.Invoke(originalSettings);
        }

        private void SaveAndClose()
        {
            ApplyChanges();
            Program.SaveSettings(originalSettings);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // Inner helper class for color fields
        private class ColorField
        {
            private TextBox txtHex;
            private Panel pnlPreview;
            private Button btnPick;
            private Label lblName;
            private Form parent;

            public string Hex
            {
                get => txtHex.Text;
                set 
                {
                    txtHex.Text = value;
                    UpdatePreview();
                }
            }

            public ColorField(string name, int x, int y, Form parent)
            {
                lblName = new Label { Text = name, Location = new Point(x, y), Size = new Size(110, 25), TextAlign = ContentAlignment.MiddleLeft };
                this.parent = parent;
                
                txtHex = new TextBox
                {
                    Location = new Point(x + 120, y),
                    Width = 80,
                    BackColor = Color.FromArgb(45, 45, 45),
                    ForeColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle
                };
                txtHex.TextChanged += (s, e) => UpdatePreview();

                pnlPreview = new Panel
                {
                    Location = new Point(x + 210, y),
                    Size = new Size(25, 25),
                    BorderStyle = BorderStyle.FixedSingle
                };

                btnPick = new Button
                {
                    Text = "Pick",
                    Location = new Point(x + 245, y),
                    Size = new Size(50, 25),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(60, 60, 60),
                    Font = new Font("Segoe UI", 8)
                };
                btnPick.Click += BtnPick_Click;

                parent.Controls.Add(lblName);
                parent.Controls.Add(txtHex);
                parent.Controls.Add(pnlPreview);
                parent.Controls.Add(btnPick);
            }

            private void UpdatePreview()
            {
                try
                {
                    pnlPreview.BackColor = ColorTranslator.FromHtml(txtHex.Text);
                }
                catch { pnlPreview.BackColor = Color.Transparent; }
            }

            private void BtnPick_Click(object? sender, EventArgs e)
            {
                using (ColorDialog cd = new ColorDialog())
                {
                    try { cd.Color = ColorTranslator.FromHtml(txtHex.Text); } catch { }
                    if (cd.ShowDialog(parent) == DialogResult.OK)
                    {
                        txtHex.Text = "#" + (cd.Color.ToArgb() & 0x00FFFFFF).ToString("X6");
                    }
                }
            }
        }
    }
}
