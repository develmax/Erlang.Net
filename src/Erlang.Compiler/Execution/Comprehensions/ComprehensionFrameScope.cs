namespace Erlang.Compiler;

internal sealed class ComprehensionFrameScope(IReadOnlyList<IDisposable> scopes) : IDisposable
{
    public void Dispose()
    {
        for (int index = scopes.Count - 1; index >= 0; index--)
        {
            scopes[index].Dispose();
        }
    }
}
