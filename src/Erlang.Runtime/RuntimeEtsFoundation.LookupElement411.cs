namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    internal Term CoreLookupElement411(
        ProcessContext context,
        Term identifier,
        Term key,
        Term position,
        bool hasDefault,
        Term defaultValue
    )
    {
        Term copiedRows;
        EtsCoreTableKind407 kind;
        int index;
        lock (gate)
        {
            // Native ordering: table ownership/protection precedes index checks.
            var table = CoreResolve407(context, identifier, false);
            if (position is not Integer number || number.Value < 1 || number.Value > RuntimeEtsCoreNames407.MaximumSmall64)
                throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            index = unchecked((int)(long)number.Value);
            kind = coreOptions407.GetValue(table, _ => throw new InvalidOperationException()).Kind;
            // Existing kernel acquires/materializes/disposes each real read lease
            // under the same reentrant gate. This is an atomic detached snapshot.
            copiedRows = CoreLookup407(context, identifier, key);
        }

        context.Cancellation.ThrowIfCancellationRequested();
        if (copiedRows is Nil)
        {
            if (hasDefault) return defaultValue;
            throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.BadKey);
        }
        var result = new List<Term>();
        foreach (Term term in Cons.Items(copiedRows))
        {
            context.Cancellation.ThrowIfCancellationRequested();
            var row = (TupleTerm)term;
            if (index == 0)
                throw new NotSupportedException(RuntimeEtsLookupNames411.HeaderProjection);
            if (index < 0 || index > row.Items.Count)
                throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            Term value = row.Items[index - 1];
            if (kind is EtsCoreTableKind407.Set or EtsCoreTableKind407.OrderedSet)
                return value;
            result.Add(value);
        }

        return Cons.From(result);
    }
}
