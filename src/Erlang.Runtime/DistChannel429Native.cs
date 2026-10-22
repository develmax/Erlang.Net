namespace Erlang;

internal static class DistChannel429Native
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
        if (owner is null || module != DistChannel429Names.Module || function != DistChannel429Names.Function || arity != DistChannel429Names.Arity) return false;
        body = (caller, arguments) =>
        {
            if (!ReferenceEquals(caller.Runtime, owner) || arguments.Count != DistChannel429Names.Arity) throw new InvalidOperationException(DistChannel429Names.Foreign);

            return ValueTask.FromResult(owner.CreateDistChannel429(
                caller,
                arguments[0],
                arguments[1],
                arguments[2]
            ));
        };

        return true;
    }
}
