namespace Erlang;

internal static class BeamOnLoadMetadata
{
    internal static void RequireSupported(BeamPreparedMetadata metadata)
    {
        Check(metadata.Descriptor.Attributes ?? Nil.Value);
        Check(metadata.Descriptor.Compilation);
    }

    private static void Check(Term value)
    {
        while (value is Cons cell)
        {
            if (cell.Head is not TupleTerm { Items.Count: 2 } pair || pair.Items[0] is not Atom)
                throw new NotSupportedException(BeamOnLoadDiagnostics.Metadata);
            value = cell.Tail;
        }
        if (value is not Nil)
            throw new NotSupportedException(BeamOnLoadDiagnostics.Metadata);
    }
}
