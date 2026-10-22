namespace Erlang.Compiler;

internal sealed class SourceFunctionTailScope(ProcessContext context, SourceFunctionTailScope? previous) : IDisposable
{
    internal ProcessContext Context { get; } = context;
    internal SourceFunctionTailScope? Previous { get; } = previous;

    public void Dispose()
    {
        SourceFunctionTailDispatch.Restore(Previous);
    }
}
