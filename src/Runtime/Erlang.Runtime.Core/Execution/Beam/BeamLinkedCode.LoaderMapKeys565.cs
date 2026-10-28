using System.Collections.Frozen;

namespace Erlang;

internal sealed partial record BeamLinkedCode
{
    private FrozenDictionary<int, TupleTerm>? loaderMapKeys565;

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
