using System.Runtime.CompilerServices;

namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly ConditionalWeakTable<BeamLinkedCode, DebugSharingLiteralMarker425> loaderMapImages565 = new();
    private readonly ConditionalWeakTable<TupleTerm, DebugSharingLiteralMarker425> loaderMapKeys565 = new();

    internal void RegisterLoaderMapKeys565(BeamLinkedCode code)
    {
        if (loaderMapImages565.TryGetValue(code, out _))
            return;
        _ = loaderMapImages565.GetValue(code, RegisterLoaderMapImage565);
    }

    private DebugSharingLiteralMarker425 RegisterLoaderMapImage565(BeamLinkedCode code)
    {
        foreach (TupleTerm keys in code.LoaderMapKeys565.Values)
        {
            loaderMapKeys565.GetValue(keys, static _ => new());
            MarkPreparedLiterals425([keys]);
        }

        return new();
    }

    internal bool IsLoaderMapKeys565(Term value)
        => value is TupleTerm { OwnedStorage506: null } keys && loaderMapKeys565.TryGetValue(keys, out _);
}
