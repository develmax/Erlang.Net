namespace Erlang;

internal static class MatchSavedCompiler634
{
    internal static bool TryCompile634(
        ProcessContext context,
        Term specification,
        MatchSpecDialect dialect,
        out MatchSavedProgram634? result
    )
    {
        result = null;
        if (dialect != MatchSpecDialect.Table || specification is not Cons { Tail: Nil } list
            || list.Head is not TupleTerm { Items.Count: 3 } clause
            || clause.Items[0] is not Atom { Name: MatchSpecNames.Wildcard }
            || clause.Items[1] is not Nil || clause.Items[2] is not Cons { Tail: Nil } body
            || body.Head is not TupleTerm { Items.Count: 2 } constant
            || constant.Items[0] is not Atom { Name: MatchSpecNames.Constant })
            return false;
        context.Runtime.RequireSavedMatchActor634(context);
        context.Cancellation.ThrowIfCancellationRequested();
        if (!MatchSavedFragment604.TryCapture604(
            context.Runtime,
            constant.Items[1],
            context.Cancellation,
            out var fragment
        ))
            return false;
        MatchSavedProgram634? pending = null;
        Exception? primary = null;
        try
        {
            context.Cancellation.ThrowIfCancellationRequested();
            pending = new(context.Runtime, fragment!);
            context.Runtime.PublishSavedMatch634(context, pending);
            result = pending;
            pending = null;

            return true;
        }
        catch (Exception error)
        {
            primary = error;
            throw;
        }
        finally
        {
            // A completed transfer closes the old wrapper; a failed constructor
            // leaves it responsible for the actual fragment. Both settle here.
            Exception? programFailure = null, fragmentFailure = null;
            try
            {
                pending?.Close634().GetAwaiter().GetResult();
            }
            catch (Exception error)
            {
                programFailure = error;
            }
            try
            {
                fragment!.Close604().GetAwaiter().GetResult();
            }
            catch (Exception error)
            {
                fragmentFailure = error;
            }
            // Summarize only after BOTH deterministic cleanup attempts. In
            // particular an allocation failure cannot precede fragment release.
            if (programFailure is not null || fragmentFailure is not null)
            {
                var cleanup = new List<Exception>();
                if (primary is not null)
                    cleanup.Add(primary);
                if (programFailure is not null)
                    cleanup.Add(programFailure);
                if (fragmentFailure is not null)
                    cleanup.Add(fragmentFailure);
                throw new AggregateException(MatchSavedNames634.Cleanup, cleanup);
            }
        }
    }

    internal static async Task<MatchSpecResult> Run634(MatchSavedProgram634 program, ProcessContext context)
    {
        using var reader = program.Acquire634();
        // Exercise genuine custody across an asynchronous dispatch boundary.
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        context.Cancellation.ThrowIfCancellationRequested();
        Term result = reader.CopyResult634(context.Runtime, context.Cancellation);

        return new(
            true,
            result,
            false,
            false
        );
    }
}
