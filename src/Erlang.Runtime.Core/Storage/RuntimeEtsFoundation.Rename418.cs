namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    internal Term CoreRename418(ProcessContext context, Term identifier, Term requestedName)
    {
        lock (gate)
        {
            // Native lookup/access precede malformed new-name badarg.
            var table = CoreResolve407(context, identifier, true);
            if (requestedName is not Atom nextName)
                throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            if (!table.Named)
            {
                context.Cancellation.ThrowIfCancellationRequested();
                table.Name = nextName;

                return identifier;
            }
            // Source insert_named_tab refuses every existing entry, including self.
            if (names.TryGetValue(nextName.Name, out var previous) && IsTableVisible419(previous))
                throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            // All potentially allocating namespace preparation precedes publication.
            var next = new Dictionary<string, RuntimeEtsFoundationTable>(names, StringComparer.Ordinal);
            next.Remove(table.Name.Name);
            next[nextName.Name] = table;
            context.Cancellation.ThrowIfCancellationRequested();
            names = next;
            table.Name = nextName;

            return nextName;
        }
    }
}
