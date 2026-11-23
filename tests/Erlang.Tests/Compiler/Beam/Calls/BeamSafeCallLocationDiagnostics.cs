namespace Erlang.Tests;

internal static class BeamSafeCallLocationDiagnostics
{
    internal const string LineMismatch =
        "A call location was retained for a nonthrowing call or lost before a throwing call on the same source line.";
}
