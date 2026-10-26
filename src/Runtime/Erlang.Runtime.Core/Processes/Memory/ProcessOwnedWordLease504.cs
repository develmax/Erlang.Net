namespace Erlang;

internal sealed class ProcessOwnedWordLease504(
    ProcessOwnedWordArena504 owner,
    long identity,
    nint pointer,
    int words
) : IDisposable
{
    internal long Identity { get; } = identity;
    internal nint Pointer { get; set; } = pointer;
    internal int Words { get; } = words;

    internal object? Read(int index) => owner.Read(this, index);

    internal void Write(int index, object? value) => owner.Write(this, index, value);

    public void Dispose() => owner.Release(this);
}
