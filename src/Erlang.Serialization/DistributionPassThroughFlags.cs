namespace Erlang;

public static class DistributionPassThroughFlags
{
    public const ulong ProcessMonitor = 0x08;
    public const ulong NamedProcessMonitor = 0x20;
    public const ulong ProcessMonitors = ProcessMonitor | NamedProcessMonitor;
    public const ulong ExtendedReferences = 0x04;
    public const ulong FunctionTags = 0x10;
    public const ulong NewFunctionTags = 0x80;
    public const ulong ExtendedPidsPorts = 0x100;
    public const ulong ExportPointer = 0x200;
    public const ulong BitBinaries = 0x400;
    public const ulong NewFloats = 0x800;
    public const ulong AtomCacheHeader = 0x2000;
    public const ulong Utf8Atoms = 0x10000;
    public const ulong MapTag = 0x20000;
    public const ulong BigCreation = 0x40000;
    public const ulong SendSender = 0x80000;
    public const ulong ExitPayload = 0x400000;
    public const ulong Fragments = 0x800000;
    public const ulong Handshake23 = 0x1000000;
    public const ulong UnlinkId = 0x2000000;
    public const ulong V4NodeContainer = 0x4UL << 32;
    public const ulong NativeRecords = 0x40UL << 32;
    public const ulong Mandatory = ExtendedReferences | FunctionTags | NewFunctionTags |
        ExtendedPidsPorts | ExportPointer | BitBinaries | NewFloats | Utf8Atoms |
        MapTag | BigCreation | Handshake23 | UnlinkId | V4NodeContainer;
    public const ulong Unsupported = AtomCacheHeader | Fragments | SendSender;
}
