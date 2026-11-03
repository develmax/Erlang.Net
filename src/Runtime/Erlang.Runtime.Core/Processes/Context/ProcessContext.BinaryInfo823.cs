namespace Erlang;

public sealed partial class ProcessContext
{
    // Internal process-heap inventory only. Attached fragments are a separate
    // acceptance gate before this key can enter public ProcessInfoAtoms.
    internal Term CaptureBinaryInfo823()
    {
        Runtime.RequireBinaryInfoCapture822();
        NativeBinaryInfoRow822[] rows = termHeap506?.CaptureNativeBinaryInfo822() ?? [];

        return Cons.From(rows.Select(row => Term.Tuple(
            new Integer(row.Identifier),
            new Integer(row.OriginalSize),
            Term.I(row.References)
        )));
    }
}
