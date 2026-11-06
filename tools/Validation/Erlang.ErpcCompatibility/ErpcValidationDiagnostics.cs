namespace Erlang.ErpcValidation;

internal static class ErpcValidationDiagnostics
{
    internal const string Usage = "Expected repository, oracle directory, Managed|Beam|Connected";
    internal const string Publication = "Observer publication failed";
    internal const string Exports = "Export parity failed";
    internal const string Owner = "Selected owner is incorrect";
    internal const string Response = "Observer did not reply";
    internal const string Source = "Erpc source or original reference changed after capture";
    internal const string Capture = "Erpc capture identity differs";
    internal const string Baseline = "Erpc capture does not match the pinned reference";
}
