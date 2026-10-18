namespace Erlang;

internal sealed class BinarySearchWork(ProcessContext context)
{
    private int pending;

    internal ValueTask Step()
    {
        if (++pending < BinarySearchParameters.WorkPerReduction)
            return ValueTask.CompletedTask;
        pending = 0;
        return context.ChargeAsync(1);
    }
}
