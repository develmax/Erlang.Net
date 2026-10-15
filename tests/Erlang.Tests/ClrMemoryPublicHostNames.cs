using System.Reflection;

namespace Erlang.Tests;

internal static class ClrMemoryPublicHostNames
{
    internal const string Test = "host/clr-observed-payload-memory";
    internal const string Failure = "CLR observed payload memory contract failed.";
    internal const string Memory = "memory";
    internal const string Total = "total";
    internal const string Processes = "processes";
    internal const string ProcessesUsed = "processes_used";
    internal const string CapturedProcesses = "Processes";
    internal const string System = "system";
    internal const string Atom = "atom";
    internal const string AtomUsed = "atom_used";
    internal const string Binary = "binary";
    internal const string ObservedBinary = "Binary";
    internal const string TrackCode = "Code";
    internal const string FromOwned = "FromOwnedBytes";
    internal const string Code = "code";
    internal const string Ets = "ets";
    internal const string Maximum = "maximum";
    internal const string Unknown = "memory317_unknown";
    internal const string DuplicateAtom = "memory317_duplicate_atom_storage";
    internal const string RegistryType = "Erlang.ObservedPayloadStorage";
    internal const string Entries = "entries";
    internal const string Lookup = "TryGetValue";
    internal const string Bytes = "Bytes";
    internal const string Kind = "Kind";
    internal const string ByteView = "ByteView";
    internal const string Backing = "bytes";
    internal const string Slots = "processSlots";
    internal const string SlotBacking = "slots";
    internal const string Allocated = "AllocatedPayloadBytes";
    internal const string Used = "UsedPayloadBytes";
    internal const string NativeDomain = "nativeAllocationDomain";
    internal const string Capture = "CaptureMemoryPayloadAsync";
    internal const string Native = "Native";
    internal const string BeamResource = ".beam_fixture_constant.beam";
    internal const string Descriptor = "Descriptor";
    internal const string Functions = "Functions";
    internal const string Items = "Items";
    internal const string Bodies = "Bodies";
    internal const string LinkedCode = "Code";
    internal const string Structure = "Structure";
    internal const string Chunks = "Chunks";
    internal const string Payload = "Payload";
    internal const string Gate = "gate";
    internal const string Snapshot = "Snapshot";
    internal const string Available = "available";
    internal const string Kill = "kill";
    internal const string AsTask = "AsTask";
    internal const string Result = "Result";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    internal static readonly string[] Order = [Total, Processes, ProcessesUsed, System, Atom, AtomUsed, Binary, Code, Ets];
}
