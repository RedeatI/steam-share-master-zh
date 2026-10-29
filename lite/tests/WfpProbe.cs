using System;
using System.Net;
using System.Net.Sockets;
static class WfpProbe {
    static int Main(string[] args) {
        try {
            IPAddress address = IPAddress.Parse(args[0]);
            if (args.Length > 2 && args[2] == "udp") {
                using (var udp = new UdpClient(address.AddressFamily)) {
                    udp.Connect(address, Int32.Parse(args[1]));
                    udp.Send(new byte[] { 83, 83, 77 }, 3);
                    return 0;
                }
            }
            using (var client = new TcpClient(address.AddressFamily)) {
                var pending = client.BeginConnect(address, Int32.Parse(args[1]), null, null);
                if (!pending.AsyncWaitHandle.WaitOne(3000)) return 2;
                client.EndConnect(pending);
                return 0;
            }
        } catch { return 2; }
    }
}
