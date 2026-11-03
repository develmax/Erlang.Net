namespace Erlang;

internal sealed class MatchSavedReader634(MatchSavedProgram634 owner) : IDisposable
{
    private readonly object gate = new();
    private MatchSavedProgram634? selected = owner;

    internal bool Owns634(MatchSavedProgram634 candidate) => ReferenceEquals(Volatile.Read(ref selected), candidate);

    internal Term CopyResult634(ProcessRuntime runtime, CancellationToken cancellation)
    {
        lock (gate)
            return Require634().CopyResult634(this, runtime, cancellation);
    }

    internal nint TextAddress634()
    {
        lock (gate)
            return Require634().TextAddress634(this);
    }

    internal nint FragmentAddress634()
    {
        lock (gate)
            return Require634().FragmentAddress634(this);
    }

    private MatchSavedProgram634 Require634()
        => selected ?? throw new ObjectDisposedException(MatchSavedNames634.Closed);

    public void Dispose()
    {
        MatchSavedProgram634? release;
        lock (gate)
        {
            release = selected;
            selected = null;
        }
        release?.ReleaseReader634();
    }
}
