namespace Erlang;

internal sealed class ListUniqueMergeScope : IDisposable
{
    private readonly List<IDisposable> scopes = new();

    internal ListUniqueMergeScope(ProcessContext context, IEnumerable<ListUniqueMergeContinuation> continuations)
    {
        foreach (var continuation in continuations.Reverse())
            scopes.Add(ListUniqueMergeTrace.Enter(context, continuation));
    }

    public void Dispose()
    {
        for (int index = scopes.Count - 1; index >= 0; index--)
            scopes[index].Dispose();
    }
}
