namespace Erlang;

public static class DistributionCapabilityProfile429
{
    // Capabilities of this actual stateless pass-through codec and its current
    // runtime signal/output owners. These are deliberately not ERTS defaults.
    public const ulong Mandatory = DistributionPassThroughFlags.Mandatory;
    public const ulong Unsupported = DistributionPassThroughFlags.Unsupported;
    public const ulong Preferred = Mandatory | DistributionPassThroughFlags.ProcessMonitors
        | DistributionPassThroughFlags.SendSender | DistributionPassThroughFlags.BigSequenceTraceLabels
        | DistributionPassThroughFlags.AlternativeActionSignal | DistributionPassThroughFlags.ExitPayload
        | DistributionPassThroughFlags.Alias | DistributionPassThroughFlags.Spawn
        | DistributionPassThroughFlags.NativeRecords | DistributionCapabilityWire429.UnicodeIo
        | DistributionCapabilityWire429.SmallAtomTags | DistributionCapabilityWire429.Mandatory25Digest;
    public const ulong Addable = Preferred;
    // Removing an already unsupported transport capability is permitted. It is
    // never advertised in Preferred/Addable and cannot be enabled by validation.
    public const ulong Rejectable = Unsupported;
    // There is no cross-packet atom cache in the actual pass-through codec.
    public const ulong StrictOrder = 0;

    public static bool HasMandatory(ulong flags) => (flags & Mandatory) == Mandatory;

    public static bool HasUnsupported(ulong flags) => (flags & Unsupported) != 0;

    public static bool SupportsNegotiation(ulong flags) => HasMandatory(flags) && !HasUnsupported(flags);
}
