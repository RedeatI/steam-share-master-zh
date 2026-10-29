using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace SteamShareMasterLite {
    interface ISwitchOperations {
        bool IsBlocked { get; }
        void ChangeRule(string exe, bool disconnect);
        void StopSteam();
        void StartSteam(string exe);
    }
    static class NetworkSwitch {
        internal static string Execute(ISwitchOperations ops, string exe, bool disconnect) {
            if (ops.IsBlocked == disconnect)
                return disconnect ? "Steam 已处于断网状态。" : "Steam 当前没有被本软件断网。";
            ops.ChangeRule(exe, disconnect);
            try {
                ops.StopSteam();
                ops.StartSteam(exe);
            } catch (Exception ex) {
                return "网络规则已更新，但 Steam 未能自动重启：" + ex.Message + "。请手动启动 Steam。";
            }
            return disconnect ? "已断开 Steam 网络，正在启动 Steam。退出本软件会自动恢复网络。" : "已恢复 Steam 网络，正在启动 Steam。";
        }
        internal static void StopProcess(Action kill, Func<bool> hasExited, Action wait) {
            try {
                if (!hasExited()) kill();
                wait();
            } catch (InvalidOperationException) {
                if (!hasExited()) throw;
            } catch (Win32Exception) {
                if (!hasExited()) throw;
            }
        }
    }
    sealed class WindowsSwitchOperations : ISwitchOperations, IDisposable {
        readonly WfpSession session = new WfpSession();
        public bool IsBlocked { get { return session.Active; } }
        public void ChangeRule(string exe, bool disconnect) {
            if (!disconnect) { session.Dispose(); return; }
            var paths = new List<string> { Path.GetFullPath(exe) };
            string root = Path.GetDirectoryName(exe);
            foreach (string version in new[] { "cef.win64", "cef.win7x64" }) {
                string helper = Path.Combine(root, "bin", "cef", version, "steamwebhelper.exe");
                if (File.Exists(helper)) paths.Add(helper);
            }
            session.Block(paths);
        }
        public void StopSteam() {
            foreach (var name in new[] {"steam", "steamwebhelper"}) {
                foreach (var process in Process.GetProcessesByName(name)) {
                    using (process) {
                        NetworkSwitch.StopProcess(() => process.Kill(), () => {
                            try { return process.HasExited; } catch (InvalidOperationException) { return true; }
                        }, () => { if (!process.WaitForExit(5000)) throw new TimeoutException("Steam 进程未能及时关闭"); });
                    }
                }
            }
        }
        public void StartSteam(string exe) {
            Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(exe) });
        }
        public void Dispose() { session.Dispose(); }
    }
}
