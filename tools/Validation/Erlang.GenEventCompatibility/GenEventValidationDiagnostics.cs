internal static class GenEventValidationDiagnostics
{
    internal const string Usage = "Usage: Erlang.GenEventCompatibility repository corpus Managed|Beam [case|depth=N]";
    internal const string BackendOwnerMismatch = "gen_event backend owner mismatch";
    internal const string ExportMismatch = "gen_event export inventory mismatch";
    internal const string BootEnded = "Original init/code_server terminated before gen_event checks";
    internal const string DisposalCase = "runtime-disposal";
    internal const string ChangedInputCase = "input-changed";

    internal static string Publication(string module, object status) => module + ": " + status;

    internal static string Difference(string name, object actual) => name + ": DIFFERENCE " + actual;

    internal static string Passed(string name) => name + ": pass";

    internal static string Failed(string name, Exception error) => name + ": " + error;
}
