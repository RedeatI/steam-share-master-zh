using System;
using System.IO;
using System.Threading;
using SteamShareMasterLite;
static class WfpSessionHolder {
    static int Main(string[] args) {
        try {
            using (var session = new WfpSession()) {
                session.Block(new[] { Path.GetFullPath(args[0]) });
                File.WriteAllText(args[1], "ready");
                Thread.Sleep(120000);
            }
            return 0;
        } catch (Exception ex) {
            File.WriteAllText(args[1], "error: " + ex.Message);
            return 1;
        }
    }
}
