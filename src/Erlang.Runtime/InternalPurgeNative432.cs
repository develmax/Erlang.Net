namespace Erlang;

internal static class InternalPurgeNative432
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
        if (owner is null || module != CodePurgerNames432.Internal || function != CodePurgerNames432.Function || arity != 2) return false;
        body = async (context, arguments) =>
        {
            if (!ReferenceEquals(context.Runtime, owner) || arguments.Count != 2) throw new InvalidOperationException(CodePurgerNames432.Foreign);
            try
            {
                // Native source checks the designated process before either argument.
                owner.RequireCodePurger432(context);
                if (arguments[0] is not Atom name || arguments[1] is not Atom operation)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);

                return await owner.CodePurge432(context, name.Name, operation.Name);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error && !error.ExplicitStackTrace)
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
