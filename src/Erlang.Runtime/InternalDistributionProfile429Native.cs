namespace Erlang;

internal static class InternalDistributionProfile429Native
{
    internal static bool TryCapture(
        ProcessRuntime? owner,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        body = null!;
        if (owner is null || module != InternalDistributionProfile429Names.Module || function != InternalDistributionProfile429Names.Function || arity != 0) return false;
        body = (context, arguments) =>
        {
            if (!ReferenceEquals(context.Runtime, owner) || arguments.Count != 0) throw new InvalidOperationException(InternalDistributionProfile429Names.Foreign);

            return ValueTask.FromResult(owner.DistributionProfile429(context));
        };

        return true;
    }
}
