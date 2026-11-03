namespace Erlang;

// Exact zero-edge value proof only; grants no runtime/image/frame authority.
internal static class ImmediateTermRoot161
{
    internal static bool IsEmpty(Term value)
        => value is Nil || value is Integer && ProcessTermHeap506.Storage(value) is null
            && ProcessTermHeap506.IsImmediateInteger559(value);
}
