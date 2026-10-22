namespace Erlang.HaltProbe;

internal static class HaltWindowsDumpNames437
{
    internal const string Command = "--internal-owned-parent-minidump";
    internal const string Kernel = "kernel32.dll";
    internal const string Host = "dotnet.exe";
    internal const string RuntimeRoot = "../../..";
    internal const string FxVersion = "--fx-version";
    internal const string Failure = "Owned external Windows dump helper failed";
    internal const string ParentRefused = "Dump target must be the actual live creating parent with matching creation time";
    internal const string InvalidDump = "Minidump does not identify the actual target parent";
    internal const string IdentitySuffix = ".identity.json";
    internal const uint ProcessSnapshot = 2;
    internal const uint QueryInformation = 0x0400;
    internal const uint VmRead = 0x0010;
    internal const uint MiscInfoStream = 15;
    internal const uint ProcessIdPresent = 1;
    internal const uint Signature = 0x504d444d;
    internal const int ArgumentCount = 4;
    internal const int MaxPath = 260;
    internal const int MillisecondsPerSecond = 1000;
    internal const int DumpHeaderBytes = 32;
    internal const int DirectoryEntryBytes = 12;
    internal const int MiscPrefixBytes = 12;
}
