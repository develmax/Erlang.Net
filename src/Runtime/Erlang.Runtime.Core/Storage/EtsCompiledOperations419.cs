namespace Erlang;

internal static class EtsCompiledOperations419
{
    internal static Term Compile(ProcessContext context, Term specification)
    {
        MatchSpecCompilation compiled = MatchSpecCompiler.Compile(specification, MatchSpecDialect.Table);
        if (compiled.Program is null) throw new ErlangException(ErlangErrorReasons.BadArgument);

        return context.Runtime.PublishMatchSpec419(context, new(compiled.Program));
    }

    private static (ReferenceResourceLease<EtsCompiledProgram419> Lease, EtsCompiledProgram419 Payload)? Read(ProcessContext context, Term argument)
    {
        if (argument is not ReferenceTerm reference) return null;
        var lease = context.Runtime.ReferenceResources.AcquireMatchSpec419(reference);
        if (lease is null)
        {
            context.Runtime.RequireMatchSpecActor419(context);

            return null;
        }
        bool transferred = false;
        try
        {
            // Capture exactly once at the old read point. Re-reading Value in
            // the caller would add a different registry-close race window.
            EtsCompiledProgram419 payload = lease.Value;
            transferred = true;

            return (lease, payload);
        }
        catch (ObjectDisposedException)
        {
            // Runtime close can race acquisition. Never turn a canceled/closed
            // actor into a fabricated guest invalid-program response.
            context.Runtime.RequireMatchSpecActor419(context);
            throw;
        }
        finally
        {
            if (!transferred)
                lease.Dispose();
        }
    }

    internal static Term IsCompiled(ProcessContext context, Term argument)
    {
        var read = Read(context, argument);
        using var lease = read?.Lease;

        return CoreModules.Boolean(read is not null);
    }

    internal static async ValueTask<Term> Run(
        ProcessContext context,
        Term list,
        Term resource,
        Term accumulator
    )
    {
        if (list is not Cons && list is not Nil) throw new ErlangException(ErlangErrorReasons.BadArgument);
        var dispatch = Read(context, resource) ?? throw new ErlangException(ErlangErrorReasons.BadArgument);
        using var lease = dispatch.Lease;
        using var execution = dispatch.Payload.Executable.Acquire638(context);
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
            MatchSpecResult match = execution.RunCompiled638(context, cell.Head);
            if (match.Matched) result = new Cons(match.Value, result);
            cursor = cell.Tail;
        }
        if (cursor is not Nil) throw new EtsCompiledRunFailure419(entryList, resource, entryAccumulator);

        return result;
    }
}
