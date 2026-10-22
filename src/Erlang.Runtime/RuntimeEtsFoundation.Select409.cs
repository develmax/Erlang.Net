namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    internal Term CoreSelect409(
        ProcessContext context,
        Term identifier,
        Term specification,
        bool reverse = false
    )
    {
        RuntimeEtsReadLease?[] leases;
        RuntimeEtsCoreOptions407 metadata;
        lock (gate)
        {
            var table = CoreResolve407(context, identifier, false);
            if (specification is Nil)
                return Nil.Value;
            metadata = coreOptions407.GetValue(table, _ => throw new InvalidOperationException());
            leases = new RuntimeEtsReadLease?[CoreRows420(table).Count()];
            int index = 0;
            try
            {
                foreach (var slot in CoreRows420(table))
                {
                    context.Cancellation.ThrowIfCancellationRequested();
                    leases[index++] = slot.Payload.Acquire();
                }
            }
            catch (Exception original)
            {
                EtsSelectRows409.Release(leases, original);
                throw;
            }
        }

        MatchSpecProgram program;
        TupleTerm[] rows;
        Exception? failure = null;
        try
        {
            context.Cancellation.ThrowIfCancellationRequested();
            MatchSpecCompilation compiled;
            try
            {
                compiled = MatchSpecCompiler.Compile(specification, MatchSpecDialect.Table);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
            {
                throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            }
            program = compiled.Program ?? throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            rows = EtsSelectRows409.Materialize(leases, context.Cancellation);
        }
        catch (Exception error)
        {
            failure = error;
            throw;
        }
        finally
        {
            EtsSelectRows409.Release(leases, failure);
        }

        // Every storage lease has been released before evaluating any program.
        // MatchSpecProgram handles guard failure/body EXIT and first-clause wins.
        bool ordered = metadata.Kind == EtsCoreTableKind407.OrderedSet;
        if (ordered)
        {
            rows = EtsSelectRows409.Order(rows, metadata.KeyPosition, context.Cancellation);
            // The tree kernel visits opposite the output direction and prepends.
            if (!reverse) Array.Reverse(rows);
        }
        Term orderedResult = Nil.Value;
        var unorderedResult = ordered ? null : new List<Term>();
        foreach (var row in rows)
        {
            context.Cancellation.ThrowIfCancellationRequested();
            MatchSpecResult selected = program.Run(context, row, MatchSpecRunMode.Production);
            if (!selected.Matched) continue;
            if (ordered) orderedResult = new Cons(selected.Value, orderedResult);
            else unorderedResult!.Add(selected.Value);
        }

        return ordered ? orderedResult : Cons.From(unorderedResult!);
    }
}
