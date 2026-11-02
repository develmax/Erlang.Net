using System.Collections.Frozen;

namespace Erlang;

internal sealed partial record BeamLinkedCode
{
    private FrozenDictionary<int, TupleTerm>? loaderMapKeys565;

    internal void BindNativeLoaderMapKeys802(FrozenDictionary<int, TupleTerm> actual, ModuleLiteralDomain800 domain)
    {
        FrozenDictionary<int, TupleTerm>? previous = Volatile.Read(ref loaderMapKeys565);
        if (previous is null || previous.Count != actual.Count || previous.Keys.Any(key => !actual.ContainsKey(key))
            || actual.Values.Any(value => !domain.IsPreparedLiteral800(value)))
            throw new InvalidOperationException(ModuleLiteral800Names.Invalid);
        Volatile.Write(ref loaderMapKeys565, actual);
    }

    internal FrozenDictionary<int, TupleTerm> LoaderMapKeys565
    {
        get
        {
            if (Volatile.Read(ref loaderMapKeys565) is { } existing)
                return existing;
            FrozenDictionary<int, TupleTerm> prepared = BeamLoaderMapKeys565.Capture(this);

            return Interlocked.CompareExchange(ref loaderMapKeys565, prepared, null) ?? prepared;
        }
    }
}
