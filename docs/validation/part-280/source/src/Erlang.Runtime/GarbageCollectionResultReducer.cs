namespace Erlang;

internal static class GarbageCollectionResultReducer
{
    internal static async ValueTask<Term> GatherAsync(
        ProcessContext context,
        Term reference,
        int expectedReplies
    )
    {
        if (reference is not ReferenceTerm)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentOutOfRangeException.ThrowIfNegative(expectedReplies);
        Term collections = Term.I(GarbageCollectionResultNames.InitialCollections);
        Term reclaimed = Term.I(GarbageCollectionResultNames.InitialReclaimed);
        for (int remaining = expectedReplies; remaining > 0; remaining--)
        {
            var statistics = await context.ReceiveAsync<Term>(message =>
            {
                if (message is TupleTerm { Items.Count: GarbageCollectionResultNames.ReplyArity } reply &&
                    reply.Items[GarbageCollectionResultNames.ReferenceIndex].Equals(reference) &&
                    reply.Items[GarbageCollectionResultNames.StatisticsIndex] is TupleTerm
                    { Items.Count: GarbageCollectionResultNames.StatisticsArity } values)
                    return values;

                return null;
            });
            var values = ((TupleTerm)statistics!).Items;
            collections = CoreModules.Arithmetic(ErlangOperators.Plus,
                values[GarbageCollectionResultNames.CollectionsIndex], collections);
            reclaimed = CoreModules.Arithmetic(ErlangOperators.Plus,
                values[GarbageCollectionResultNames.ReclaimedIndex], reclaimed);
        }

        return Term.Tuple(collections, reclaimed, Term.I(GarbageCollectionResultNames.ReservedResult));
    }
}
