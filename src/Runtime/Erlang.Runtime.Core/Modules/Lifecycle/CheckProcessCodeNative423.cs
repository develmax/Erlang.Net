namespace Erlang;

internal static class CheckProcessCodeNative423
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
        if (owner is null || module != CheckProcessCodeNames423.Module || function != CheckProcessCodeNames423.Function || arity != CheckProcessCodeNames423.Arity) return false;
        body = (context, arguments) =>
        {
            if (!ReferenceEquals(context.Runtime, owner) || arguments.Count != CheckProcessCodeNames423.Arity)
                throw new InvalidOperationException(CheckProcessCodeNames423.Foreign);
            owner.RequireBeamfileActor418(context);
            try
            {
                if (arguments[0] is not Atom name) throw new ErlangException(ErlangErrorReasons.BadArgument);
                bool executing = owner.Modules.IsExecutingOldCode(context, name.Name);
                owner.RequireBeamfileActor418(context);

                return ValueTask.FromResult<Term>(Term.A(executing ? CheckProcessCodeNames423.True : CheckProcessCodeNames423.False));
            }
            catch (ErlangException error)
            {
                throw context.CaptureNativeFailure(
                    error,
                    module,
                    function,
                    arguments
                );
            }
        };

        return true;
    }
}
