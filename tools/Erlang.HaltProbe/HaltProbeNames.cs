namespace Erlang.HaltProbe;

internal static class HaltProbeNames
{
    public const string Source = "--source";
    public const string Module = "--module";
    public const string Entry = "--entry";
    public const string Backend = "--backend";
    public const string Compiled = "compiled";
    public const string Interpreted = "interpreted";
    public const string Evidence = "--evidence";
    public const string Dump = "--dump";
    public const string DefaultEntry = "run";
    public const string RequestObserved = "halt-request-observed";
    public const string Returned = "entry-returned";
    public const string Infrastructure = "probe-infrastructure-failure";
    public const string Numeric = "numeric-exit";
    public const string Abort = "os-abort";
    public const string CrashDump = "clr-minidump";
    public const string WindowsRuntime = "ucrtbase.dll";
    public const string UnixRuntime = "libc";
    public const string WindowsDumpLibrary = "dbghelp.dll";
    public const string DumpFormatGap = "CLR/OS minidump; not a BEAM crash dump";
    public const int InfrastructureExit = 125;
    public const int AbnormalEntryExit = 1;
    public const int NormalDump = 0;
    public const uint CallReportFault = 2;
    public const string LinuxShell = "/bin/sh";
    public const string DumpGateScript = "read -r token && exec \"$@\"";
    public const string DumpGate = "halt-owned-dump-gate";
    public const string CreateDump = "createdump";
    public const string NormalDumpOption = "--normal";
    public const string DumpNameOption = "--name";
    public const int LinuxSetPtracer = 0x59616d61;
}
