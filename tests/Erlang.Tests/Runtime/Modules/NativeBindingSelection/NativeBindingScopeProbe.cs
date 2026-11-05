namespace Erlang.Tests;

internal sealed class NativeBindingScopeProbe : IDisposable
{
    internal int Entered { get; private set; }
    internal int Disposed { get; private set; }

    internal IDisposable Enter()
    {
        Entered++;

        return this;
    }

    public void Dispose() => Disposed++;
}
