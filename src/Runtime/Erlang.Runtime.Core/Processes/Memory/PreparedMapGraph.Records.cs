namespace Erlang;

internal sealed partial class PreparedMapGraph517
{
    private readonly Dictionary<NativeRecordTerm, PreparedHeapRecord> records = new(ReferenceEqualityComparer.Instance);

    internal PreparedHeapRecord? FindRecord(NativeRecordTerm source) => records.GetValueOrDefault(source);

    private void CaptureRecord(ProcessRuntime runtime, Term source, Stack<Term> pending)
    {
        if (source is not NativeRecordTerm record || runtime.IsSharingLiteral425(record)) return;
        var plan = new PreparedHeapRecord(record);
        records.Add(record, plan);
        pending.Push(plan.Definition);
    }
}
