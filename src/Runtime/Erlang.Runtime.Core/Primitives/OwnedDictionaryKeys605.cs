namespace Erlang;

internal static class OwnedDictionaryKeys605
{
    internal static Term All(ProcessContext context)
    {
        if (context.CurrentOwned506 is { } invocation
            && context.Dictionary.TryKeysGuest605(
                invocation,
                null,
                false,
                out Term result
            ))
            return result;

        return Cons.From(context.Dictionary.Keys);
    }

    internal static Term Matching(ProcessContext context, Term expectedValue)
    {
        if (context.CurrentOwned506 is { } invocation
            && context.Dictionary.TryKeysGuest605(
                invocation,
                expectedValue,
                true,
                out Term result
            ))
            return result;

        return Cons.From(context.Dictionary.Where(pair => pair.Value.Equals(expectedValue)).Select(pair => pair.Key));
    }
}
