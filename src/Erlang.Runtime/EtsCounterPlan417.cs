namespace Erlang;

internal static class EtsCounterPlan417
{
    internal static TupleTerm Apply(
        TupleTerm value,
        long keyPosition,
        Term updates,
        CancellationToken cancellation,
        out Term result,
        out bool any
    )
    {
        bool returnList = updates is not (Integer or TupleTerm);
        Term rest = updates switch
        {
            Integer increment => Term.List(Term.Tuple(Term.I(keyPosition + 1), increment)),
            TupleTerm tuple => Term.List(tuple),
            _ => updates
        };
        var operations = new List<TupleTerm>();
        while (rest is Cons cell)
        {
            cancellation.ThrowIfCancellationRequested();
            if (cell.Head is not TupleTerm operation || operation.Items.Count is not (2 or 4))
                throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            if (operation.Items.Count == 4 && (operation.Items[2] is not Integer || operation.Items[3] is not Integer))
                throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            if (operation.Items[0] is not Integer position
                || position.Value < -RuntimeEtsCoreNames407.MaximumSmall64 - 1
                || position.Value > RuntimeEtsCoreNames407.MaximumSmall64
                || operation.Items[1] is not Integer)
                throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            if (position.Value == keyPosition) throw new EtsMutationFailure414(EtsMutationNames414.KeyPosition);
            if (position.Value < 1 || position.Value > value.Items.Count)
                throw new EtsMutationFailure414(EtsMutationNames414.Position);
            if (value.Items[(int)position.Value - 1] is not Integer)
                throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            operations.Add(operation);
            rest = cell.Tail;
        }
        if (rest is not Nil) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
        Term[] fields = value.Items.ToArray();
        var results = new List<Term>(operations.Count);
        foreach (TupleTerm operation in operations)
        {
            cancellation.ThrowIfCancellationRequested();
            int index = (int)((Integer)operation.Items[0]).Value - 1;
            var increment = ((Integer)operation.Items[1]).Value;
            var updated = ((Integer)fields[index]).Value + increment;
            Term counter = new Integer(updated);
            if (operation.Items.Count == 4)
            {
                var threshold = ((Integer)operation.Items[2]).Value;
                if (increment.Sign < 0 ? updated < threshold : updated > threshold)
                    counter = operation.Items[3];
            }
            fields[index] = counter;
            results.Add(counter);
        }
        result = returnList ? Cons.From(results) : results[0];
        any = operations.Count != 0;

        return new TupleTerm(fields);
    }
}
