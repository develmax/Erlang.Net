namespace Erlang;

internal static class InternalSystemCheckNative431
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
        if (owner is null || module != InternalSystemCheckNames431.Module || function != InternalSystemCheckNames431.Function || arity != 1) return false;
        body = (context, arguments) =>
        {
            if (!ReferenceEquals(context.Runtime, owner) || arguments.Count != 1) throw new InvalidOperationException(InternalSystemCheckNames431.Foreign);
            owner.RequireInternalPortActor416(context);
            if (arguments[0] is not Atom { Name: InternalSystemCheckNames431.Schedulers })
                throw context.CaptureNativeFailure(
                    new ErlangException(ErlangErrorReasons.BadArgument),
                    module,
                    function,
                    arguments
                );

            return owner.SystemCheckSchedulers431(context);
        };

        return true;
    }
}
