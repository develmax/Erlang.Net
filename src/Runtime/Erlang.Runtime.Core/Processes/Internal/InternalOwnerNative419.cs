namespace Erlang;

internal static class InternalOwnerNative419
{
    internal static bool TryCapture(
        ProcessRuntime? owner,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        if (owner is null || module != InternalOwnerNames419.Module || ((function, arity) switch
        {
            (InternalOwnerNames419.ErasePersistent, 0) => false,
            (InternalOwnerNames419.ProcessesNext, 1) => false,
            (InternalOwnerNames419.Breakpoint, 3) => !owner.HasDisabledDebugger413,
            _ => true
        }))
        {
            body = null!;

            return false;
        }
        body = async (context, arguments) =>
        {
            if (!ReferenceEquals(context.Runtime, owner) || !ReferenceEquals(context.Runtime.Modules, owner.Modules) || arguments.Count != arity)
                throw new InvalidOperationException(InternalOwnerNames419.Foreign);
            owner.RequireInternalActor419(context);
            if (function == InternalOwnerNames419.ErasePersistent)
                return await owner.PersistentTerms.EraseAll419(context).ConfigureAwait(false);
            if (function == InternalOwnerNames419.Breakpoint)
            {
                owner.RequireDisabledDebuggerActor413(context);
                throw context.CaptureNativeFailure(
                    new ErlangException(ErlangErrorReasons.UndefinedFunction),
                    module,
                    function,
                    arguments
                );
            }
            if (arguments[0] is not Integer cursor || cursor.Value < 0 || cursor.Value > int.MaxValue)
                throw context.CaptureNativeFailure(
                    new ErlangException(ErlangErrorReasons.BadArgument),
                    module,
                    function,
                    arguments
                );
            ProcessTableBatch? batch;
            try
            {
                batch = owner.ReadNativeProcessBatch419(context, (int)cursor.Value);
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
            if (batch is null) return Term.A(ProcessTableNames.None);
            Term result = Term.Tuple(Term.I(batch.Cursor), Term.List(batch.Processes.Cast<Term>().ToArray()));
            await context.BumpReductionsAsync((batch.Cursor - (int)cursor.Value) / ProcessTableParameters.NativeIndexesPerReduction + 1);

            return result;
        };

        return true;
    }
}
