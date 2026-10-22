namespace Erlang;

internal sealed class RuntimeOsEnvironmentDriverLease422 : IDisposable
{
    private RuntimeOsEnvironment? owner;

    internal RuntimeOsEnvironmentDriverLease422(RuntimeOsEnvironment owner) => this.owner = owner;

    internal bool IsOwnedBy(RuntimeOsEnvironment? expected)
        => expected is not null && ReferenceEquals(Volatile.Read(ref owner), expected);

    internal string? Read(byte[] key)
    {
        RuntimeOsEnvironment selected = Volatile.Read(ref owner)
            ?? throw new ObjectDisposedException(nameof(RuntimeOsEnvironmentDriverLease422));

        return selected.ReadDriverLeased422(this, key);
    }

    internal int Write(byte[] key, byte[] value)
    {
        RuntimeOsEnvironment selected = Volatile.Read(ref owner)
            ?? throw new ObjectDisposedException(nameof(RuntimeOsEnvironmentDriverLease422));

        return selected.WriteDriverLeased422(this, key, value);
    }

    public void Dispose()
    {
        RuntimeOsEnvironment? selected = Interlocked.Exchange(ref owner, null);
        selected?.ReleaseDriverLease422();
    }
}
