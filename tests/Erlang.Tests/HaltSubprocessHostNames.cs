namespace Erlang.Tests;

internal static class HaltSubprocessHostNames
{
    public const string Test = "runtime/halt-dedicated-raw-process-numeric-errors-abort-and-real-dump";
    public const string Probe = "tools/Erlang.HaltProbe/bin/Debug/net10.0/Erlang.HaltProbe.dll";
    public const string Artifacts = "artifacts/halt-subprocess-cases";
    public const string Report = "artifacts/halt-subprocess-host-results.json";
    public const string Dump = "requested-clr.dmp";
    public const string Failure = "Dedicated halt process contract failed";
    public const string ContractSeparator = ":";
    public const string BackendSeparator = "/";
    public const string FailureListSeparator = ",";
    public const int HeaderBytes = 4;
    public const int OwnedProcessBudgetSeconds = 300;
    public const int OuterHostBudgetSeconds = 330;
    public static readonly byte[] WindowsDumpHeader = [0x4d, 0x44, 0x4d, 0x50];
    public static readonly byte[] LinuxDumpHeader = [0x7f, 0x45, 0x4c, 0x46];
}
