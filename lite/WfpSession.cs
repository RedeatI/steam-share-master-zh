using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SteamShareMasterLite {
    // Dynamic WFP sessions remove their filters when this process exits.
    internal sealed class WfpSession : IDisposable {
        static readonly Guid ConnectV4 = new Guid("c38d57d1-05a7-4c33-904f-7fbceee60e82");
        static readonly Guid ConnectV6 = new Guid("4a72393b-319f-44bc-84c3-ba54dcb3b6b4");
        static readonly Guid AppId = new Guid("d78e1e87-8644-4ea5-9437-d809ecefc971");
        static readonly Guid Flags = new Guid("632ce23b-5167-435c-86d7-e903684aa80c");
        static readonly Guid Universal = new Guid("eebecc03-ced4-4380-819a-2734397b2b74");
        IntPtr engine;

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        struct DisplayData { [MarshalAs(UnmanagedType.LPWStr)] public string name; [MarshalAs(UnmanagedType.LPWStr)] public string description; }
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        struct Session { public Guid key; public DisplayData display; public uint flags; public uint timeout; public uint processId; public IntPtr sid; public IntPtr username; public int kernelMode; }
        [StructLayout(LayoutKind.Sequential)]
        struct Blob { public uint size; public IntPtr data; }
        [StructLayout(LayoutKind.Sequential)]
        struct Value { public uint type; public IntPtr value; }
        [StructLayout(LayoutKind.Sequential)]
        struct Condition { public Guid fieldKey; public uint matchType; public Value conditionValue; }
        [StructLayout(LayoutKind.Sequential)]
        struct ActionData { public uint type; public Guid key; }
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        struct Filter {
            public Guid key; public DisplayData display; public uint flags; public IntPtr providerKey;
            public Blob providerData; public Guid layerKey; public Guid subLayerKey; public Value weight;
            public uint conditionCount; public IntPtr conditions; public ActionData action; public Guid context;
            public IntPtr reserved; public ulong filterId; public Value effectiveWeight;
        }

        [DllImport("fwpuclnt.dll", CharSet=CharSet.Unicode)] static extern uint FwpmEngineOpen0(string server, uint authentication, IntPtr identity, ref Session session, out IntPtr handle);
        [DllImport("fwpuclnt.dll")] static extern uint FwpmEngineClose0(IntPtr handle);
        [DllImport("fwpuclnt.dll")] static extern uint FwpmTransactionBegin0(IntPtr handle, uint flags);
        [DllImport("fwpuclnt.dll")] static extern uint FwpmTransactionCommit0(IntPtr handle);
        [DllImport("fwpuclnt.dll")] static extern uint FwpmTransactionAbort0(IntPtr handle);
        [DllImport("fwpuclnt.dll", CharSet=CharSet.Unicode)] static extern uint FwpmGetAppIdFromFileName0(string path, out IntPtr appId);
        [DllImport("fwpuclnt.dll")] static extern void FwpmFreeMemory0(ref IntPtr pointer);
        [DllImport("fwpuclnt.dll", CharSet=CharSet.Unicode)] static extern uint FwpmFilterAdd0(IntPtr handle, ref Filter filter, IntPtr security, out ulong id);

        static void Check(uint result, string operation) {
            if (result != 0) throw new InvalidOperationException(operation + "失败（0x" + result.ToString("X8") + "）： " + new Win32Exception(unchecked((int)result)).Message);
        }
        internal bool Active { get { return engine != IntPtr.Zero; } }
        internal void Block(IEnumerable<string> paths) {
            if (Active) throw new InvalidOperationException("Steam 已处于断网状态。");
            var session = new Session { key = Guid.NewGuid(), flags = 1, display = new DisplayData { name = "Steam Share Master 临时规则" } };
            IntPtr handle;
            Check(FwpmEngineOpen0(null, 10, IntPtr.Zero, ref session, out handle), "打开 WFP");
            bool transaction = false;
            try {
                Check(FwpmTransactionBegin0(handle, 0), "开始 WFP 事务");
                transaction = true;
                int count = 0;
                foreach (string path in paths) {
                    Add(handle, path, ConnectV4);
                    Add(handle, path, ConnectV6);
                    count++;
                }
                if (count == 0) throw new InvalidOperationException("没有找到需要阻断的程序。");
                Check(FwpmTransactionCommit0(handle), "提交 WFP 规则");
                transaction = false;
                engine = handle;
            } catch {
                if (transaction) FwpmTransactionAbort0(handle);
                FwpmEngineClose0(handle);
                throw;
            }
        }
        static void Add(IntPtr handle, string path, Guid layer) {
            IntPtr blob = IntPtr.Zero;
            IntPtr conditionPtr = IntPtr.Zero;
            try {
                Check(FwpmGetAppIdFromFileName0(path, out blob), "识别程序 " + path);
                var condition = new Condition { fieldKey = AppId, matchType = 0, conditionValue = new Value { type = 12, value = blob } };
                // Only block non-loopback traffic. Steam uses loopback for its UI transport.
                var nonLoopback = new Condition { fieldKey = Flags, matchType = 8, conditionValue = new Value { type = 3, value = new IntPtr(1) } };
                int conditionSize = Marshal.SizeOf(typeof(Condition));
                conditionPtr = Marshal.AllocHGlobal(conditionSize * 2);
                Marshal.StructureToPtr(condition, conditionPtr, false);
                Marshal.StructureToPtr(nonLoopback, IntPtr.Add(conditionPtr, conditionSize), false);
                var filter = new Filter {
                    key = Guid.NewGuid(), display = new DisplayData { name = "Steam Share Master 临时断网" },
                    layerKey = layer, subLayerKey = Universal, weight = new Value { type = 0 },
                    conditionCount = 2, conditions = conditionPtr, action = new ActionData { type = 0x1001 }
                };
                ulong id;
                Check(FwpmFilterAdd0(handle, ref filter, IntPtr.Zero, out id), "添加 WFP 规则");
            } finally {
                if (conditionPtr != IntPtr.Zero) Marshal.FreeHGlobal(conditionPtr);
                if (blob != IntPtr.Zero) FwpmFreeMemory0(ref blob);
            }
        }
        public void Dispose() {
            if (engine != IntPtr.Zero) {
                uint result = FwpmEngineClose0(engine);
                Check(result, "清理 WFP 规则");
                engine = IntPtr.Zero;
            }
        }
    }
}
