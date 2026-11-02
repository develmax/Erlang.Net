namespace Erlang;

public sealed partial class ProcessContext
{
    internal bool TryReadExecutingSourceConstant820(object node, object? templateOwner, out Term value)
    {
        for (SourceExecution? current = sourceExecution.Value; current is not null; current = current.Parent)
            if ((templateOwner is null || ReferenceEquals(current.Owner, templateOwner))
                && current.Image.SourceLiteralView819 is { } view)
            {
                ModuleLiteralLease800 lease = current.Literals800 ?? throw new InvalidOperationException(SourceLiteral819Names.Invalid);
                if (view.TryReadConstant820(
                    node,
                    current.Image,
                    lease,
                    out value
                )) return true;
            }
        value = null!;

        return false;
    }
}
