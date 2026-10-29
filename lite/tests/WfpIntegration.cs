using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using SteamShareMasterLite;
static class WfpIntegration {
    static int Probe(string exe, IPAddress address, int port) {
        using (var p = Process.Start(new ProcessStartInfo(exe, address + " " + port) { UseShellExecute = false, CreateNoWindow = true })) {
            if (!p.WaitForExit(6000)) { p.Kill(); throw new Exception("Probe timed out"); }
            return p.ExitCode;
        }
    }
    static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    static bool ProbeUdp(string exe, IPAddress address, UdpClient listener) {
        int port = ((IPEndPoint)listener.Client.LocalEndPoint).Port;
        using (var p = Process.Start(new ProcessStartInfo(exe, address + " " + port + " udp") { UseShellExecute = false, CreateNoWindow = true })) {
            if (!p.WaitForExit(6000)) { p.Kill(); throw new Exception("UDP probe timed out"); }
        }
        try {
            listener.Client.ReceiveTimeout = 1200;
            IPEndPoint sender = null;
            byte[] packet = listener.Receive(ref sender);
            return packet.Length == 3;
        } catch (SocketException) { return false; }
    }
    static int Main(string[] args) {
        var log = new System.Text.StringBuilder();
        try {
            var target = Path.GetFullPath(args[0]);
            var control = Path.GetFullPath(args[1]);
            var listener4 = new TcpListener(IPAddress.Loopback, 0);
            var listener6 = new TcpListener(IPAddress.IPv6Loopback, 0);
            var udp4 = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
            var udp6 = new UdpClient(new IPEndPoint(IPAddress.IPv6Loopback, 0));
            listener4.Start(40);
            listener6.Start(40);
            int port4 = ((IPEndPoint)listener4.LocalEndpoint).Port;
            int port6 = ((IPEndPoint)listener6.LocalEndpoint).Port;
            IPAddress remote = IPAddress.Parse("1.1.1.1");
            try {
                Expect(Probe(target, IPAddress.Loopback, port4) == 0, "IPv4 baseline");
                Expect(Probe(target, IPAddress.IPv6Loopback, port6) == 0, "IPv6 baseline");
                Expect(ProbeUdp(target, IPAddress.Loopback, udp4), "UDP IPv4 baseline");
                Expect(ProbeUdp(target, IPAddress.IPv6Loopback, udp6), "UDP IPv6 baseline");
                bool remoteAvailable = Probe(target, remote, 443) == 0 && Probe(control, remote, 443) == 0;
                log.AppendLine(remoteAvailable ? "Remote TCP baseline available." : "Remote TCP baseline unavailable; remote test skipped.");
                using (var failed = new WfpSession()) {
                    try {
                        failed.Block(new[] { target, target + ".missing" });
                        throw new Exception("Expected invalid application path to fail.");
                    } catch (InvalidOperationException) {}
                    Expect(!failed.Active, "Failed transaction must close session");
                    Expect(Probe(target, IPAddress.Loopback, port4) == 0, "Failed transaction left IPv4 rule");
                    Expect(Probe(target, IPAddress.IPv6Loopback, port6) == 0, "Failed transaction left IPv6 rule");
                    log.AppendLine("Failed transaction cleaned up all filters.");
                }
                using (var session = new WfpSession()) {
                    session.Block(new[] { target });
                    log.AppendLine("WFP filters added.");
                    Expect(Probe(target, IPAddress.Loopback, port4) != 0, "IPv4 block");
                    Expect(Probe(target, IPAddress.IPv6Loopback, port6) != 0, "IPv6 block");
                    Expect(Probe(control, IPAddress.Loopback, port4) == 0, "Control IPv4 unaffected");
                    Expect(Probe(control, IPAddress.IPv6Loopback, port6) == 0, "Control IPv6 unaffected");
                    Expect(!ProbeUdp(target, IPAddress.Loopback, udp4), "UDP IPv4 block");
                    Expect(!ProbeUdp(target, IPAddress.IPv6Loopback, udp6), "UDP IPv6 block");
                    Expect(ProbeUdp(control, IPAddress.Loopback, udp4), "Control UDP IPv4 unaffected");
                    Expect(ProbeUdp(control, IPAddress.IPv6Loopback, udp6), "Control UDP IPv6 unaffected");
                    log.AppendLine("UDP target blocked and control unaffected, IPv4 and IPv6.");
                    if (remoteAvailable) {
                        Expect(Probe(target, remote, 443) != 0, "Remote target block");
                        Expect(Probe(control, remote, 443) == 0, "Remote control unaffected");
                        log.AppendLine("Remote target blocked and remote control unaffected.");
                    }
                    log.AppendLine("Target blocked and control unaffected, IPv4 and IPv6.");
                }
                Expect(Probe(target, IPAddress.Loopback, port4) == 0, "IPv4 restored");
                Expect(Probe(target, IPAddress.IPv6Loopback, port6) == 0, "IPv6 restored");
                Expect(ProbeUdp(target, IPAddress.Loopback, udp4), "UDP IPv4 restored");
                Expect(ProbeUdp(target, IPAddress.IPv6Loopback, udp6), "UDP IPv6 restored");
                if (remoteAvailable) Expect(Probe(target, remote, 443) == 0, "Remote TCP restored");
                log.AppendLine("Target connectivity restored after WFP session closed.");
                string ready = args[2] + ".ready";
                if (File.Exists(ready)) throw new Exception("Crash-test signal already exists.");
                using (var child = Process.Start(new ProcessStartInfo(args[3], target + " " + ready) { UseShellExecute = false, CreateNoWindow = true })) {
                    try {
                        for (int i = 0; i < 100 && !File.Exists(ready) && !child.HasExited; i++) System.Threading.Thread.Sleep(100);
                        Expect(File.Exists(ready) && File.ReadAllText(ready) == "ready", "Crash-test session did not start.");
                        Expect(Probe(target, IPAddress.Loopback, port4) != 0, "Crash-test block");
                        child.Kill();
                        child.WaitForExit();
                        bool restoredAfterCrash = false;
                        for (int i = 0; i < 20; i++) {
                            if (Probe(target, IPAddress.Loopback, port4) == 0) { restoredAfterCrash = true; break; }
                            System.Threading.Thread.Sleep(250);
                        }
                        Expect(restoredAfterCrash, "Crash-test cleanup");
                        log.AppendLine("Target connectivity restored after session owner was terminated.");
                    } finally {
                        if (!child.HasExited) { child.Kill(); child.WaitForExit(); }
                    }
                }
            } finally { listener4.Stop(); listener6.Stop(); udp4.Close(); udp6.Close(); }
            File.WriteAllText(args[2], log.ToString());
            return 0;
        } catch (Exception ex) {
            File.WriteAllText(args[2], log + "FAIL: " + ex.ToString());
            return 1;
        }
    }
}
