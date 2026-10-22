namespace Erlang.Tests;

internal static class NetKernel400HostNames
{
    internal const string ModuleName = "net_kernel";
    internal const string Function = "dflag_unicode_io";
    internal const string Invariant = "Original net_kernel native contract invariant failed";
    internal const string ImageHash = "17DACC9AD0EAEC67F9C1FB1D82C9B9B10C950EBD3F0BB4E5D6CC23B8AF0136CD";
    internal const string Remote = "net_kernel400_host_remote@localhost";
    internal const string Local = "net_kernel400_host_local@localhost";
    internal const string CaptureDeferredFailure = "CaptureDeferredReasonFailure";
    internal const string RuntimeGate = "gate";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
    internal const ulong UnicodeFlag = 0x1000;
    internal const string ImageResource = "Erlang.Tests.NetKernelNative400.net_kernel.beam";
    internal const string ImportsResource = "Erlang.Tests.NetKernelNative400.imports.json";
    internal const string Test = "host/net-kernel400-original-primitive-owner-and-peer-flags";
    internal const string ModuleInfo = "module_info";
    internal const string Admission = "AdmitObservedLocalNode";
    internal const string ProviderType = "Erlang.NetKernelNative";
    internal const string Capture = "TryCapture";
    internal const string Query = "DflagUnicodeIo";
    internal const string WrongModule = "foreign_module";
    internal const string WrongFunction = "not_a_native_primitive";
    internal const string ForeignRuntime = "Original net_kernel primitive belongs to another runtime";
    internal const string ImportsHash = "6711E3220772F583C2FC57AD935009FE7D74E2621880BCFD615E36CA6077BC9A";
}
