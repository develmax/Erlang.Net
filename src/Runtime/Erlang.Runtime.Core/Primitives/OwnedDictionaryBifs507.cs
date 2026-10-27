namespace Erlang;

internal static class OwnedDictionaryBifs507
{
    internal static Term Get(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        var scope = context.CurrentOwned506;
        if (scope is not null && context.Dictionary.TryReadGuest507(
            scope,
            arguments[0],
            false,
            out Term value
        ))
            return value;

        return ErlangInfallibleBifValues.Get(context, arguments[0]);
    }

    internal static Term Put(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        var scope = context.CurrentOwned506;
        if (scope is not null && context.Dictionary.TryPutGuest507(
            scope,
            arguments[0],
            arguments[1],
            out Term previous
        ))
            return previous;
        Term fallback = context.Dictionary.GetValueOrDefault(arguments[0], ProcessDictionary507Atoms.Undefined);
        context.Dictionary[arguments[0]] = arguments[1];

        return fallback;
    }

    internal static Term Erase(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        var scope = context.CurrentOwned506;
        if (scope is not null && context.Dictionary.TryReadGuest507(
            scope,
            arguments[0],
            true,
            out Term previous
        ))
            return previous;
        Term fallback = context.Dictionary.GetValueOrDefault(arguments[0], ProcessDictionary507Atoms.Undefined);
        context.Dictionary.Remove(arguments[0]);

        return fallback;
    }

    internal static Term All(ProcessContext context, bool erase)
    {
        var scope = context.CurrentOwned506;
        if (scope is not null && context.Dictionary.TrySnapshotGuest507(scope, erase, out Term result))
            return result;
        Term fallback = Cons.From(context.Dictionary.Select(entry => Term.Tuple(entry.Key, entry.Value)));
        if (erase)
            context.Dictionary.Clear();

        return fallback;
    }
}
