using System;
using System.ComponentModel;
using SteamShareMasterLite;

sealed class FakeOperations : ISwitchOperations {
    public string calls = "";
    public bool blocked, failRule, failStop;
    public bool IsBlocked { get { return blocked; } }
    public void ChangeRule(string exe, bool disconnect) {
        calls += "rule;";
        if (failRule) throw new InvalidOperationException("拒绝访问");
        blocked = disconnect;
    }
    public void StopSteam() { calls += "stop;"; if (failStop) throw new InvalidOperationException("无法关闭"); }
    public void StartSteam(string exe) { calls += "start;"; }
}
static class Tests {
    static void Expect(bool value, string message) { if (!value) throw new Exception(message); }
    static void Main() {
        var f = new FakeOperations { failRule = true };
        try { NetworkSwitch.Execute(f, "steam.exe", true); throw new Exception("missing rule failure"); }
        catch (InvalidOperationException ex) { Expect(ex.Message == "拒绝访问", "rule diagnostics"); }
        Expect(f.calls == "rule;", "failed rule must not stop Steam");
        f = new FakeOperations();
        NetworkSwitch.Execute(f, "steam.exe", true);
        Expect(f.calls == "rule;stop;start;", "disconnect order");
        f.calls = "";
        NetworkSwitch.Execute(f, "steam.exe", true);
        Expect(f.calls == "", "duplicate block must not restart Steam");
        NetworkSwitch.Execute(f, "steam.exe", false);
        Expect(f.calls == "rule;stop;start;", "restore order");
        f.calls = "";
        NetworkSwitch.Execute(f, "steam.exe", false);
        Expect(f.calls == "", "duplicate restore must not restart Steam");
        f = new FakeOperations { blocked = true, failStop = true };
        Expect(NetworkSwitch.Execute(f, "steam.exe", false).Contains("手动启动"), "restart failure explanation");
        bool exited = false;
        NetworkSwitch.StopProcess(() => { exited = true; throw new InvalidOperationException(); }, () => exited, () => {});
        exited = false;
        NetworkSwitch.StopProcess(() => { exited = true; throw new Win32Exception(); }, () => exited, () => {});
        try {
            NetworkSwitch.StopProcess(() => { throw new Win32Exception(); }, () => false, () => {});
            throw new Exception("live process failure must propagate");
        } catch (Win32Exception) {}
        Console.WriteLine("All network-switch tests passed.");
    }
}
