using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SteamShareMasterLite {
    interface ISwitchOperations {
        string CheckActiveFirewall();
        void ChangeRule(string exe, bool disconnect);
        void StopSteam();
        void StartSteam(string exe);
    }

    static class NetworkSwitch {
        internal static string Execute(ISwitchOperations ops, string exe, bool disconnect) {
            if (disconnect) {
                string issue = ops.CheckActiveFirewall();
                if (!String.IsNullOrEmpty(issue)) return issue;
            }
            // Do not close Steam until the firewall operation has succeeded.
            ops.ChangeRule(exe, disconnect);
            try {
                ops.StopSteam();
                ops.StartSteam(exe);
            } catch (Exception ex) {
                return "防火墙规则已更新，但 Steam 未能自动重启：" + ex.Message + "。请手动启动 Steam。";
            }
            return disconnect ? "已断开 Steam 网络，正在启动 Steam。" : "已恢复 Steam 网络，正在启动 Steam。";
        }

        internal static void StopProcess(Action kill, Func<bool> hasExited, Action wait) {
            try {
                if (!hasExited()) kill();
                wait();
            } catch (InvalidOperationException) {
                // Steam may exit while its helper processes are being enumerated.
                if (!hasExited()) throw;
            } catch (Win32Exception) {
                if (!hasExited()) throw;
            }
        }
    }

    sealed class WindowsSwitchOperations : ISwitchOperations {
        const string Rule = "SteamShareMasterLite";
        static object Policy() {
            Type type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            if (type == null) throw new InvalidOperationException("无法访问 Windows 防火墙服务。");
            return Activator.CreateInstance(type);
        }
        public string CheckActiveFirewall() {
            object policy = Policy();
            Type t = policy.GetType();
            int active = (int)t.InvokeMember("CurrentProfileTypes", BindingFlags.GetProperty, null, policy, null);
            if (active == 0) return "无法确定当前网络的防火墙配置，Steam 保持原状。";
            foreach (int profile in new[] {1, 2, 4}) {
                if ((active & profile) != 0) {
                    bool enabled = (bool)t.InvokeMember("FirewallEnabled", BindingFlags.GetProperty, null, policy, new object[] {profile});
                    if (!enabled) return "当前网络有 Windows 防火墙配置处于关闭状态，断网规则无法保证生效。请先启用对应的防火墙配置；Steam 保持原状。";
                }
            }
            return null;
        }
        static bool RuleExists() {
            object policy = Policy();
            object rules = policy.GetType().InvokeMember("Rules", BindingFlags.GetProperty, null, policy, null);
            foreach (object rule in (System.Collections.IEnumerable)rules) {
                string name = (string)rule.GetType().InvokeMember("Name", BindingFlags.GetProperty, null, rule, null);
                if (String.Equals(name, Rule, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
        public void ChangeRule(string exe, bool disconnect) {
            if (RuleExists()) RunNetsh("advfirewall firewall delete rule name=\"" + Rule + "\"");
            if (disconnect) {
                RunNetsh("advfirewall firewall add rule name=\"" + Rule + "\" dir=out action=block program=\"" + exe + "\" enable=yes");
                if (!RuleExists()) throw new InvalidOperationException("防火墙没有保存断网规则。");
            } else if (RuleExists()) {
                throw new InvalidOperationException("防火墙断网规则仍存在，未恢复 Steam 网络。");
            }
        }
        static void RunNetsh(string args) {
            using (var p = Process.Start(new ProcessStartInfo("netsh.exe", args) {
                UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true, RedirectStandardError = true
            })) {
                Task<string> stdout = p.StandardOutput.ReadToEndAsync();
                Task<string> stderr = p.StandardError.ReadToEndAsync();
                p.WaitForExit();
                Task.WaitAll(stdout, stderr);
                if (p.ExitCode != 0) {
                    string detail = (stdout.Result + " " + stderr.Result).Trim();
                    throw new InvalidOperationException("防火墙操作失败（代码 " + p.ExitCode + "）" +
                        (detail.Length == 0 ? "，请检查管理员权限和防火墙服务。" : "：" + detail));
                }
            }
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
    }
}
