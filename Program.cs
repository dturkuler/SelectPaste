using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;

namespace SelectPaste
{
    public static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Modifiers
        const uint MOD_ALT = 0x0001;
        const uint MOD_CONTROL = 0x0002;
        const uint MOD_SHIFT = 0x0004;
        const uint MOD_WIN = 0x0008;
        const uint MOD_NOREPEAT = 0x4000;

        const int HOTKEY_ID = 1;

        [STAThread]
        static void Main()
        {
            EnsureSingleInstance();
            ApplicationConfiguration.Initialize();

            // Load settings EARLY to get the hotkey string
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            string settingsPath = Path.Combine(exePath, "settings.json");
            
            AppSettings settings = new AppSettings();
            if (File.Exists(settingsPath))
            {
                try 
                {
                   var options = new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip, PropertyNameCaseInsensitive = true };
                   settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(settingsPath), options) ?? new AppSettings();
                }
                catch { }
            }

            // Check if argument 'silent' is passed to skip popup
            bool silent = false;
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && args.Contains("--silent"))
            {
                silent = true;
            }

            if (!silent)
            {
                // Startup Info Popup
                DialogResult result = MessageBox.Show(
                    $"Welcome to SelectPaste v{GetVersion()}!\n\n" +
                    $"This application runs in the background. Press {settings.hotkey} to open the command palette.\n" +
                    $"You can access the config folder or quit from the system tray icon.\n\n" +
                    $"Do you want to continue?",
                    "SelectPaste",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information
                );

                if (result == DialogResult.No)
                {
                    return; // Quit app
                }
            }
            
            try 
            {
                Application.Run(new HiddenContext(settings, !silent));
            }
            catch
            {
                // Silent fail in production
            }
        }

        private static void EnsureSingleInstance()
        {
            Process current = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(current.ProcessName);
            
            foreach (Process process in processes)
            {
                if (process.Id != current.Id)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(3000); // Wait up to 3 seconds for it to die
                    }
                    catch { }
                }
            }
        }

        private static string GetVersion()
        {
            // Use InformationalVersion to get the semantic version (e.g. 1.0.1) set in .csproj
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var attr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            
            // Allow for "1.0.1+commit_hash" standard format, splitting to just get "1.0.1"
            if (attr?.InformationalVersion != null)
            {
                var version = attr.InformationalVersion;
                int plusIndex = version.IndexOf('+');
                return plusIndex > 0 ? version.Substring(0, plusIndex) : version;
            }
            
            return assembly.GetName().Version?.ToString(3) ?? "1.0.0";
        }

        public class AppSettings
        {
            public string hotkey { get; set; } = "Shift + Alt + ."; // Default
            public int WindowWidth { get; set; } = 600;
            public int WindowHeight { get; set; } = 400;
            public int? WindowX { get; set; } = null;
            public int? WindowY { get; set; } = null;
            
            // Styling
            public float FontSize { get; set; } = 10f;
            public string LabelColor { get; set; } = "#FFFFFF";    // White
            public string ValueColor { get; set; } = "#888888";    // Gray
            public string CategoryColor { get; set; } = "#FFA500"; // Orange
            
            // Command Profile
            public string CommandFile { get; set; } = "commands.json";

            // Preserve unknown fields (like _help)
            [JsonExtensionData]
            public Dictionary<string, JsonElement>? ExtensionData { get; set; }
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                string exePath = AppDomain.CurrentDomain.BaseDirectory;
                string settingsPath = Path.Combine(exePath, "settings.json");
                
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                
                string json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(settingsPath, json);
            }
            catch { }
        }

        class HiddenContext : ApplicationContext
        {
            private CommandPaletteForm? paletteForm;
            private InvisibleWindow? hotkeyWindow;
            private NotifyIcon trayIcon;
            private AppSettings settings;

            public HiddenContext(AppSettings settings, bool startWithPalette = false)
            {
                this.settings = settings;
                // Initialize Tray Icon
                trayIcon = new NotifyIcon();
                
                // Try to use app.ico if exists in base directory, otherwise use system icon
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(iconPath))
                {
                    try { trayIcon.Icon = new Icon(iconPath); } catch { trayIcon.Icon = SystemIcons.Application; }
                }
                else
                {
                    trayIcon.Icon = SystemIcons.Application; 
                }

                trayIcon.Text = $"SelectPaste ({settings.hotkey})";
                trayIcon.Visible = true;

                // Tray Menu
                ContextMenuStrip menu = new ContextMenuStrip();
                
                ToolStripMenuItem configItem = new ToolStripMenuItem("Config Folder");
                configItem.Click += (s, e) => Process.Start("explorer.exe", AppDomain.CurrentDomain.BaseDirectory);

                ToolStripMenuItem aboutItem = new ToolStripMenuItem("About");
                aboutItem.Click += (s, e) => ShowAbout();
                
                ToolStripMenuItem quitItem = new ToolStripMenuItem("Quit");
                quitItem.Click += (s, e) => ExitApp();

                menu.Items.Add(configItem);
                menu.Items.Add(aboutItem);
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(quitItem);
                
                trayIcon.ContextMenuStrip = menu;

                (uint fsModifiers, uint vk) = ParseHotkey(settings.hotkey);

                hotkeyWindow = new InvisibleWindow();
                hotkeyWindow.HotkeyPressed += OnHotkeyPressed;
                
                if (!RegisterHotKey(hotkeyWindow.Handle, HOTKEY_ID, fsModifiers, vk))
                {
                    int err = Marshal.GetLastWin32Error();
                    MessageBox.Show($"Could not register hotkey '{settings.hotkey}' (Error {err}).\nCheck settings.json.");
                }
                if (startWithPalette)
                {
                    System.Windows.Forms.Timer startTimer = new System.Windows.Forms.Timer { Interval = 100 };
                    startTimer.Tick += (s, e) => {
                        startTimer.Stop();
                        startTimer.Dispose();
                        ShowPalette();
                    };
                    startTimer.Start();
                }
            }

            private void ShowAbout()
            {
                var version = Program.GetVersion();
                var description = "A keyboard-centric command palette for pasting text.";
                var repo = "https://github.com/dturkuler/SelectPaste"; // Ideally read from assembly attributes
                
                MessageBox.Show(
                    $"SelectPaste v{version}\n\n{description}\n\nRepository: {repo}",
                    "About SelectPaste",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }

            private void ExitApp()
            {
                trayIcon.Visible = false;
                UnregisterHotKey(hotkeyWindow.Handle, HOTKEY_ID);
                Application.Exit();
            }

            private (uint modifiers, uint key) ParseHotkey(string hotkeyString)
            {
                uint modifiers = 0;
                uint key = 0;

                var parts = hotkeyString.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in parts)
                {
                    string p = part.ToLower();
                    if (p == "ctrl" || p == "control") modifiers |= MOD_CONTROL;
                    else if (p == "alt") modifiers |= MOD_ALT;
                    else if (p == "shift") modifiers |= MOD_SHIFT;
                    else if (p == "win" || p == "windows") modifiers |= MOD_WIN;
                    else
                    {
                        // Parse key
                        key = ParseKey(p);
                    }
                }

                return (modifiers, key);
            }

            private uint ParseKey(string keyString)
            {
                // Handle special cases
                if (keyString == ".") return 0xBE;
                if (keyString == ",") return 0xBC;
                if (keyString == ";") return 0xBA;
                if (keyString == "/") return 0xBF;
                if (keyString == "[") return 0xDB;
                if (keyString == "]") return 0xDD;
                if (keyString == "enter") return 0x0D;
                if (keyString == "space") return 0x20;
                if (keyString == "tab") return 0x09;
                if (keyString == "esc" || keyString == "escape") return 0x1B;
                if (keyString == "backspace") return 0x08;
                if (keyString == "delete") return 0x2E;
                
                if (keyString.StartsWith("f") && keyString.Length > 1 && int.TryParse(keyString.Substring(1), out int fNum))
                {
                    if (fNum >= 1 && fNum <= 12) return (uint)(0x70 + fNum - 1);
                }

                if (keyString.Length == 1)
                {
                   char c = keyString.ToUpper()[0];
                   return (uint)c;
                }

                return 0;
            }

            private void OnHotkeyPressed(object? sender, EventArgs e)
            {
                ShowPalette();
            }

            private void ShowPalette()
            {
                if (paletteForm == null || paletteForm.IsDisposed)
                {
                    paletteForm = new CommandPaletteForm(settings);
                }

                DialogResult result = paletteForm.ShowDialog();
                
                // Save window state even if cancelled
                Program.SaveSettings(settings);

                if (result == DialogResult.OK)
                {
                    string textToInject = paletteForm.SelectedValue;
                    if (!string.IsNullOrEmpty(textToInject))
                    {
                        System.Threading.Thread.Sleep(50);
                        InputInjector.InjectText(textToInject);
                    }
                }
            }
            
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                   if (hotkeyWindow != null)
                   {
                       UnregisterHotKey(hotkeyWindow.Handle, HOTKEY_ID);
                       hotkeyWindow.Dispose();
                   }
                   if (trayIcon != null)
                   {
                       trayIcon.Visible = false;
                       trayIcon.Dispose();
                   }
                }
                base.Dispose(disposing);
            }
        }

        class InvisibleWindow : Form
        {
            public event EventHandler? HotkeyPressed;
            const int WM_HOTKEY = 0x0312;

            public InvisibleWindow()
            {
                this.ShowInTaskbar = false;
                this.WindowState = FormWindowState.Minimized;
                this.FormBorderStyle = FormBorderStyle.None;
                this.CreateHandle();
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_HOTKEY)
                {
                    if (m.WParam.ToInt32() == HOTKEY_ID)
                    {
                        HotkeyPressed?.Invoke(this, EventArgs.Empty);
                    }
                }
                base.WndProc(ref m);
            }
        }
    }
}
