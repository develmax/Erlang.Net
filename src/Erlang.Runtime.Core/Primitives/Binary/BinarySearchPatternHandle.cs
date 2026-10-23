namespace Erlang;

internal sealed class BinarySearchPatternHandle : IDisposable
{
    private readonly ReferenceResourceLease<BinarySearchPattern>? lease;
    internal BinarySearchPattern Pattern { get; }

    internal BinarySearchPatternHandle(BinarySearchPattern pattern, ReferenceResourceLease<BinarySearchPattern>? lease = null)
    {
        Pattern = pattern;
        this.lease = lease;
    }

    public void Dispose() => lease?.Dispose();
}
