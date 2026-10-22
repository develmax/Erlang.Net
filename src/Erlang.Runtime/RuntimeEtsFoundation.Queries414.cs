namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    internal Term CoreMatchObject414(ProcessContext context, Term identifier, Term pattern)
        => CoreSelect409(context, identifier, Term.List(Term.Tuple(
            pattern, Nil.Value, Term.List(Term.A(MatchSpecNames.Whole)))));

    internal Term CoreSelectCount414(ProcessContext context, Term identifier, Term specification)
    {
        // Same native table-dialect evaluation; count only the actual atom true.
        // Existing CoreSelect409 releases ALL storage leases before mutable HOST guard evaluation.
        Term result = CoreSelect409(context, identifier, specification);
        long count = 0;
        while (result is Cons cell)
        {
            context.Cancellation.ThrowIfCancellationRequested();
            if (cell.Head is Atom { Name: ErlangBooleanAtoms.True }) count++;
            result = cell.Tail;
        }
        if (result is not Nil) throw new InvalidOperationException(EtsQueryNames414.Signature);

        return Term.I(count);
    }

    internal Term CoreSelectReverse414(ProcessContext context, Term identifier, Term specification)
        => CoreSelect409(
            context,
            identifier,
            specification,
            reverse: true
        );
}
