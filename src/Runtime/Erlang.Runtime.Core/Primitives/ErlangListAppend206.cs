namespace Erlang;

internal static class ErlangListAppend206
{
    internal static async ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Term left = arguments[0], right = arguments[1];
        if (left is Nil) return right;
        if (left is not Cons) throw new ErlangException(ErlangErrorReasons.BadArgument);
        List<Term>? heads = right is Nil ? null : new();
        int cellsPerReduction = heads is null
            ? ErlangListAppend206Names.ValidateCellsPerReduction : ErlangListAppend206Names.CopyCellsPerReduction;
        Term cursor = left;
        while (true)
        {
            await context.ChargeAsync(0);
            int reductions = context.RemainingListTraversalReductions;
            int remaining = reductions * cellsPerReduction;
            int initial = remaining;
            while (cursor is Cons cell && remaining != 0)
            {
                heads?.Add(cell.Head);
                cursor = cell.Tail;
                remaining--;
            }
            if (cursor is Cons)
            {
                await context.ChargeAsync(reductions);
                continue;
            }
            await context.ChargeAsync((initial - remaining) / cellsPerReduction);
            if (cursor is not Nil) throw new ErlangException(ErlangErrorReasons.BadArgument);
            break;
        }
        // Nil RHS still required full LHS validation, but must keep its spine.
        if (heads is null) return left;
        Term result = right;
        int index = heads.Count;
        while (index > 0)
        {
            await context.ChargeAsync(0);
            int remaining = context.RemainingListTraversalReductions * ErlangListAppend206Names.CopyCellsPerReduction;
            while (index > 0 && remaining-- > 0)
                result = new Cons(heads[--index], result);
            // This CLR reverse-build pass adds no second reduction debit.
            // Genuine argument frames retain every native head/tail across yields.
            if (index > 0) await context.YieldExecution();
        }
        await context.ChargeAsync(0);
        OwnedTermInvocation506? invocation = context.CurrentOwned506;
        if (invocation?.BindingKind507 == OwnedBindingKind507.LocalListAppend)
        {
            if (invocation.IsDisposed || !invocation.CanBorrow507 || !invocation.OwnsContext507(context)
                || !RuntimeActorExecutionTurn.IsOwnedBy(context))
                throw new InvalidOperationException(OwnedBoundary507Names.PublicationChanged);

            // Both registry and captured primitive bridges publish only actual
            // local graphs. Adopt fresh cons scaffolding here before they return.
            return invocation.Frame.AdoptGuestInto507(invocation.ResultSlot507, result);
        }

        return result;
    }
}
