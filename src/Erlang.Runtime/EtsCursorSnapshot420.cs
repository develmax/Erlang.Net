namespace Erlang;

internal sealed class EtsCursorSnapshot420(
    EtsCoreTableKind407 kind,
    Term handle,
    int slots,
    EtsCursorRow420[] rows,
    int[] scanSlots
)
{
    internal EtsCoreTableKind407 Kind { get; } = kind;
    internal Term Handle { get; } = handle;
    internal int SlotExtent { get; } = slots;
    internal EtsCursorRow420[] Rows { get; } = rows;
    internal int[] ScanSlots { get; } = scanSlots;
}
