namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    internal async ValueTask<Term> CoreDeleteAll412(ProcessContext context, Term identifier, Term holder)
    {
        Task retirement;
        long count;
        lock (gate)
        {
            var table = CoreResolve407(context, identifier, true);
            // Both pinned hash/tree native holder extractors return their argument unchanged.
            // A non-undefined argument is the source's already-completed continuation path.
            if (holder is not Atom { Name: CommonAtomNames.Undefined }) return holder;
            count = CoreRows420(table).Count();
            context.Cancellation.ThrowIfCancellationRequested();
            var next = new Dictionary<long, RuntimeEtsObjectSlot>();
            var order = new LinkedList<RuntimeEtsObjectSlot>();
            var change420 = CoreStageIndex420(
                table,
                order,
                CoreRows420(table).Select(slot => slot.Payload),
                context.Cancellation
            );
            var prepared = CorePrepareRetirement407(context.Self, change420.Retired);
            CorePublishIndex420(table, change420);
            retirement = CoreBeginRetirement407(prepared);
        }
        await retirement.ConfigureAwait(false);

        return Term.I(count);
    }
}
