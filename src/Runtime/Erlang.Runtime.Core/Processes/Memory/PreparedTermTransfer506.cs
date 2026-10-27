namespace Erlang;

internal sealed class PreparedTermTransfer506(ProcessTermBorrow506 borrow, Term portable) : IDisposable
{
    internal Term Portable { get; } = portable;

    public void Dispose() => borrow.Dispose();
}
