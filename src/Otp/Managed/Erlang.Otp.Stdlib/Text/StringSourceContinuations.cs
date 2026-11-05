namespace Erlang;

internal sealed class StringSourceContinuations(ProcessContext context, string function, int arity) : IDisposable
{
    private readonly List<IDisposable> scopes = new();
    private int? previous;

    internal void Push(int line)
    {
        if (previous != line)
        {
            scopes.Add(StringSourceTrace.Scope(
                context,
                function,
                arity,
                line
            ));
        }
        previous = line;
    }

    public void Dispose()
    {
        for (int index = scopes.Count - 1; index >= 0; index--)
        {
            scopes[index].Dispose();
        }
    }
}
