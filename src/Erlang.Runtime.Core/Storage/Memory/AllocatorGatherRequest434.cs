namespace Erlang;

internal sealed class AllocatorGatherRequest434
{
    internal ProcessContext? Origin { get; private set; }
    internal Pid? Recipient { get; private set; }
    internal ReferenceTerm? Reference { get; private set; }
    internal string Allocator { get; }
    internal int Width { get; }
    internal ulong Start { get; }
    internal int Flags { get; }
    internal bool Carriers { get; }
    internal RuntimeSchedulerAuxiliaryBatch420? Batch { get; set; }
    internal TaskCompletionSource PhysicalDone { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal AllocatorGatherRequest434(
        ProcessContext origin,
        ReferenceTerm reference,
        string allocator,
        int width,
        ulong start,
        int flags,
        bool carriers
    )
    {
        Origin = origin;
        Recipient = origin.Self;
        Reference = reference;
        Allocator = allocator;
        Width = width;
        Start = start;
        Flags = flags;
        Carriers = carriers;
    }

    internal void Clear()
    {
        Origin = null;
        Recipient = null;
        Reference = null;
        Batch = null;
    }
}
