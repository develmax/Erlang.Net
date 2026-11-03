namespace Erlang;

public sealed class ProcessRuntimeOptions
{
    internal bool OwnedTermsPilot506 { get; init; }

    public RuntimeNativeMetadataImage? NativeMetadataImage { get; init; }

    public int DirtyIoSchedulerCount { get; init; } = DirtyNativePoolParameters522.DefaultIoWorkers;

    public int MaximumAtoms { get; init; } = AtomTableLimits.MaximumNames;

    public RuntimeWarningMapping WarningMapping { get; }

    public RuntimePrintableRange PrintableRange { get; }

    public ProcessRuntimeOptions(RuntimeWarningMapping warningMapping = RuntimeWarningMapping.Warning)
        : this(warningMapping, RuntimePrintableRange.Latin1)
    {
    }

    public ProcessRuntimeOptions(
        RuntimeWarningMapping warningMapping,
        RuntimePrintableRange printableRange
    )
    {
        if (warningMapping is not (RuntimeWarningMapping.Warning or RuntimeWarningMapping.Info or RuntimeWarningMapping.Error))
            throw new ArgumentOutOfRangeException(nameof(warningMapping));
        if (printableRange is not (RuntimePrintableRange.Latin1 or RuntimePrintableRange.Unicode))
            throw new ArgumentOutOfRangeException(nameof(printableRange));
        WarningMapping = warningMapping;
        PrintableRange = printableRange;
    }
}
