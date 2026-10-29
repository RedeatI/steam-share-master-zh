using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SteamShareMasterLite {
    static class Program {
        [STAThread]
        static void Main() {
            bool created;
            using (var mutex = new Mutex(true, @"Local\SteamShareMasterLiteWfp", out created)) {
                if (!created) {
                    MessageBox.Show("软件已经运行，请在系统托盘中打开。", "Steam Share Master");
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
        }
    }
    sealed class MainForm : Form {
        readonly string config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "steam-share-master", "steam_path.txt");
        readonly Label pathLabel = new Label();
        readonly TextBox log = new TextBox();
        readonly Button block = new Button();
        readonly Button unblock = new Button();
        readonly NotifyIcon tray = new NotifyIcon();
        readonly WindowsSwitchOperations operations = new WindowsSwitchOperations();
        string steamPath;
        bool quitting;
        bool busy;

        public MainForm() {
            Text = "Steam Share Master 精简版";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(440, 300);
            MinimumSize = new Size(456, 339);
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Color.FromArgb(45, 45, 45);
            ForeColor = Color.White;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            block.Text = "断开 Steam 网络";
            unblock.Text = "恢复 Steam 网络";
            block.SetBounds(16, 18, 198, 40);
            unblock.SetBounds(225, 18, 198, 40);
            block.BackColor = Color.Firebrick;
            unblock.BackColor = Color.SeaGreen;
            block.FlatStyle = unblock.FlatStyle = FlatStyle.Flat;
            Button select = new Button();
            select.Text = "选择 Steam 程序位置";
            select.SetBounds(16, 68, 407, 36);
            select.BackColor = Color.FromArgb(65, 65, 65);
            select.FlatStyle = FlatStyle.Flat;
            pathLabel.SetBounds(16, 114, 407, 42);
            pathLabel.AutoEllipsis = true;
            log.SetBounds(16, 163, 407, 110);
            log.Multiline = true;
            log.ReadOnly = true;
            log.ScrollBars = ScrollBars.Vertical;
            log.BackColor = Color.FromArgb(25, 25, 25);
            log.ForeColor = Color.LightGreen;
            Controls.AddRange(new Control[] {block, unblock, select, pathLabel, log});
            steamPath = FindSteam();
            ShowPath();
            select.Click += delegate { SelectSteam(); };
            block.Click += async delegate { await SwitchSteam(true); };
            unblock.Click += async delegate { await SwitchSteam(false); };
            var menu = new ContextMenuStrip();
            menu.Items.Add("显示主窗口", null, delegate { Show(); WindowState = FormWindowState.Normal; Activate(); });
            menu.Items.Add("断开 Steam 网络", null, async delegate { await SwitchSteam(true); });
            menu.Items.Add("恢复 Steam 网络", null, async delegate { await SwitchSteam(false); });
            menu.Items.Add("退出", null, delegate {
                if (busy) { WriteLog("请等待当前操作完成后再退出。"); return; }
                quitting = true; tray.Visible = false; Close();
            });
            tray.Icon = Icon;
            tray.Text = "Steam Share Master 精简版";
            tray.ContextMenuStrip = menu;
            tray.Visible = true;
            tray.DoubleClick += delegate { Show(); WindowState = FormWindowState.Normal; Activate(); };
            FormClosing += delegate(object sender, FormClosingEventArgs e) { if (!quitting && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); } };
            FormClosed += delegate { operations.Dispose(); tray.Dispose(); };
        }
        string FindSteam() {
            try {
                if (File.Exists(config)) {
                    string saved = File.ReadAllText(config, Encoding.UTF8).Trim();
                    if (File.Exists(saved)) return saved;
                }
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam")) {
                    string exe = key == null ? null : key.GetValue("SteamExe") as string;
                    if (!String.IsNullOrEmpty(exe) && File.Exists(exe)) { SavePath(exe); return exe; }
                }
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam")) {
                    string dir = key == null ? null : key.GetValue("InstallPath") as string;
                    string exe = String.IsNullOrEmpty(dir) ? null : Path.Combine(dir, "steam.exe");
                    if (exe != null && File.Exists(exe)) { SavePath(exe); return exe; }
                }
            } catch (Exception ex) { WriteLog("自动查找路径失败：" + ex.Message); }
            return "";
        }
        void SavePath(string value) {
            Directory.CreateDirectory(Path.GetDirectoryName(config));
            File.WriteAllText(config, value, new UTF8Encoding(false));
        }
        void ShowPath() { pathLabel.Text = "Steam 路径：" + (String.IsNullOrEmpty(steamPath) ? "未找到，请手动选择" : steamPath); }
        void SelectSteam() {
            if (busy || operations.IsBlocked) {
                WriteLog("请先恢复 Steam 网络，再更改程序位置。");
                return;
            }
            using (var dialog = new OpenFileDialog()) {
                dialog.Title = "选择 Steam.exe";
                dialog.Filter = "Steam 程序 (steam.exe)|steam.exe|可执行文件 (*.exe)|*.exe";
                if (dialog.ShowDialog() == DialogResult.OK) {
                    steamPath = dialog.FileName;
                    try { SavePath(steamPath); } catch (Exception ex) { WriteLog("保存路径失败：" + ex.Message); }
                    ShowPath();
                }
            }
        }
        void WriteLog(string text) { log.AppendText(text + Environment.NewLine); }
        async Task SwitchSteam(bool disconnect) {
            if (busy) return;
            if (!File.Exists(steamPath)) { WriteLog("找不到 Steam，请先选择 Steam.exe。"); return; }
            if (MessageBox.Show("此操作会强制关闭并重启 Steam。请先保存并退出游戏。是否继续？", "确认操作", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            busy = true;
            block.Enabled = unblock.Enabled = false;
            try {
                string exe = steamPath;
                WriteLog(disconnect ? "正在断开 Steam 网络..." : "正在恢复 Steam 网络...");
                string result = await Task.Run(() => NetworkSwitch.Execute(operations, exe, disconnect));
                WriteLog(result);
            } catch (Exception ex) { WriteLog("操作失败：" + ex.Message); }
            finally { busy = false; block.Enabled = unblock.Enabled = true; }
        }
    }
}
