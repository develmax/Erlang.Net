namespace Erlang.Tests;

internal static class ErlangHashCheckParameters
{
    public const int BinaryBytes = 4 * 1024 * 1024;
    public const int AllocationBudgetBytes = 4096;
    public const int TupleDepth = 20000;
    public const int MapDepth = 5000;
    public const long FullRange = 4294967296;
}
