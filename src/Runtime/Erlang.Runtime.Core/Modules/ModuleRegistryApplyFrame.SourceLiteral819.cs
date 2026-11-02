namespace Erlang;

internal sealed partial class ModuleRegistryApplyFrame
{
    private List<PreparedSourceLiteralView819>? sourceLiteralPublications819;

    internal void TrackSourceLiteral819(PreparedSourceLiteralView819 view)
    {
        if (!ReferenceEquals(root, this) || exited || thread != Environment.CurrentManagedThreadId)
            throw new InvalidOperationException(SourceLiteral819Names.Invalid);
        (sourceLiteralPublications819 ??= new()).Add(view);
    }

    private void ReleaseSourceRoot819()
    {
        Exception? primary = null;
        try
        {
            adopted?.Dispose();
        }
        catch (Exception error)
        {
            primary = error;
        }
        adopted = null;
        try
        {
            ReleaseSourceLiterals819();
        }
        catch (Exception cleanup)
        {
            if (primary is not null) throw new AggregateException(primary, cleanup);
            throw;
        }
        if (primary is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(primary).Throw();
    }

    private void ReleaseSourceLiterals819()
    {
        List<PreparedSourceLiteralView819>? views = sourceLiteralPublications819;
        sourceLiteralPublications819 = null;
        if (views is not null) registry.SettleSourceLiteralPublications819(views);
    }
}
