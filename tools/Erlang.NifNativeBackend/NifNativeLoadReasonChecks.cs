namespace Erlang.NifNativeBackend;

internal static class NifNativeLoadReasonChecks
{
    internal static void AssertOriginal(string caseName, Term result)
    {
        if (!NifNativeLoadReasonExpected.Cases.TryGetValue(caseName, out var expected))
            return;
        Term required = Term.Tuple(
            Term.A(NifNativeElevenNames.Error),
            Term.Tuple(Term.A(expected.Category), Term.String(expected.Reason))
        );
        NifNativeHarness.Require(result.Equals(required));
    }
}
