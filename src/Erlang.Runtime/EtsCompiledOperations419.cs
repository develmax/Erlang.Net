namespace Erlang;

internal static class EtsCompiledOperations419
{
    internal static Term Compile(ProcessContext context, Term specification)
    {
        MatchSpecCompilation compiled = MatchSpecCompiler.Compile(specification, MatchSpecDialect.Table);
        if (compiled.Program is null) throw new ErlangException(ErlangErrorReasons.BadArgument);

        return context.Runtime.PublishMatchSpec419(context, new(compiled.Program));
    }

    private static EtsCompiledProgram419? Read(ProcessContext context, Term argument)
    {
        if (argument is not ReferenceTerm reference) return null;
        using var lease = context.Runtime.ReferenceResources.AcquireMatchSpec419(reference);
        if (lease is null)
        {
            context.Runtime.RequireMatchSpecActor419(context);

            return null;
        }
        try
        {
            return lease.Value;
        }
        catch (ObjectDisposedException)
        {
            // Runtime close can race acquisition. Never turn a canceled/closed
            // actor into a fabricated guest invalid-program response.
            context.Runtime.RequireMatchSpecActor419(context);
            throw;
        }
    }

    internal static Term IsCompiled(ProcessContext context, Term argument)
        => CoreModules.Boolean(Read(context, argument) is not null);

    internal static async ValueTask<Term> Run(
        ProcessContext context,
        Term list,
        Term resource,
        Term accumulator
    )
    {
        if (list is not Cons && list is not Nil) throw new ErlangException(ErlangErrorReasons.BadArgument);
        var owned = Read(context, resource) ?? throw new ErlangException(ErlangErrorReasons.BadArgument);
        MatchSpecProgram program = owned.Program;
        Term entryList = list, entryAccumulator = accumulator;
        Term cursor = list, result = accumulator;
        int reductions = 0;
        while (cursor is Cons cell)
        {
            context.Cancellation.ThrowIfCancellationRequested();
            if (++reductions > EtsCompiledNames419.ContextReductions)
            {
                // Native BIF_TRAP3 restarts with the unprocessed list and the
                // already reversed accumulator; keep these exact error args.
                entryList = cursor;
                entryAccumulator = result;
                reductions = 0;
                await Task.Yield();
                continue;
            }
            MatchSpecResult match = program.RunNative419(context, cell.Head);
            if (match.Matched) result = new Cons(match.Value, result);
            cursor = cell.Tail;
        }
        if (cursor is not Nil) throw new EtsCompiledRunFailure419(entryList, resource, entryAccumulator);

        return result;
    }
}
