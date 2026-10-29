using System;
using System.ComponentModel;
using SteamShareMasterLite;

sealed class FakeOperations : ISwitchOperations {
    public string calls = "";
    public string firewallIssue;
    public bool failRule;
    public bool failStop;
    public string CheckActiveFirewall() { calls += "check;"; return firewallIssue; }
    public void ChangeRule(string exe, bool disconnect) {
        calls += "rule;";
        if (failRule) throw new InvalidOperationException("拒绝访问");
    }
    public void StopSteam() { calls += "stop;"; if (failStop) throw new InvalidOperationException("无法关闭"); }
    public void StartSteam(string exe) { calls += "start;"; }
}
static class Tests {
    static void Expect(bool value, string message) { if (!value) throw new Exception(message); }
    static void Main() {
        var f = new FakeOperations { firewallIssue = "防火墙关闭" };
        Expect(NetworkSwitch.Execute(f, "steam.exe", true) == "防火墙关闭", "disabled firewall message");
        Expect(f.calls == "check;", "disabled firewall must not touch Steam or rules");
        f = new FakeOperations { failRule = true };
        try { NetworkSwitch.Execute(f, "steam.exe", true); throw new Exception("missing rule failure"); }
        catch (InvalidOperationException ex) { Expect(ex.Message == "拒绝访问", "rule diagnostics"); }
        Expect(f.calls == "check;rule;", "failed rule must not stop Steam");
        f = new FakeOperations();
        NetworkSwitch.Execute(f, "steam.exe", true);
        Expect(f.calls == "check;rule;stop;start;", "disconnect order");
        f = new FakeOperations();
        NetworkSwitch.Execute(f, "steam.exe", false);
        Expect(f.calls == "rule;stop;start;", "restore while firewall disabled");
        f = new FakeOperations { failStop = true };
        Expect(NetworkSwitch.Execute(f, "steam.exe", false).Contains("手动启动"), "restart failure explanation");
        bool exited = false;
        NetworkSwitch.StopProcess(() => { exited = true; throw new InvalidOperationException(); }, () => exited, () => {});
        exited = false;
        NetworkSwitch.StopProcess(() => { exited = true; throw new Win32Exception(); }, () => exited, () => {});
        try {
            NetworkSwitch.StopProcess(() => { throw new Win32Exception(); }, () => false, () => {});
            throw new Exception("live process failure must propagate");
        } catch (Win32Exception) {}
        string actual = new WindowsSwitchOperations().CheckActiveFirewall();
        Console.WriteLine("Firewall inspection: " + (actual ?? "enabled"));
        var ruleCheck = typeof(WindowsSwitchOperations).GetMethod("RuleExists", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Console.WriteLine("Application firewall rule exists: " + ruleCheck.Invoke(null, null));
        var run = typeof(WindowsSwitchOperations).GetMethod("RunNetsh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        try {
            run.Invoke(null, new object[] { "advfirewall firewall show rule name=SSM-Lite-Diagnostic-Rule-Not-Installed" });
            throw new Exception("expected missing rule failure");
        } catch (System.Reflection.TargetInvocationException ex) {
            Expect(ex.InnerException.Message.Contains("代码 1"), "netsh exit code missing");
            Expect(ex.InnerException.Message.Length > 25, "netsh diagnostic missing");
        }
        Console.WriteLine("All network-switch tests passed.");
    }
}
