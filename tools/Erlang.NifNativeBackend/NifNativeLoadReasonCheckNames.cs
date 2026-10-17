namespace Erlang.NifNativeBackend;

internal static class NifNativeLoadReasonCheckNames
{
    internal const string Mode = "--managed-load-reason-controls";
    internal const string Test = "runtime/native-entry-original-diagnostic-reasons";
    internal const string Pass = "Managed native-load reason controls passed; no NIF library loaded or callback invoked.";
    internal const string AlphaModule = "native_entry_alpha";
    internal const string BetaModule = "native_entry_beta";
    internal const string MissingFunction = "absent_entry";
    internal const string DuplicateCase = "alpha_duplicate";
    internal const string UndeclaredCase = "beta_undeclared";
    internal const string MissingCase = "beta_missing";
    internal const string MismatchCase = "module_mismatch";
    internal const string VetoCase = "load_veto";
    internal const string QuotedModule = "node@host";
    internal const string ControlModule = "mod\n@host";
    internal const string QuotedMissingReason = "Function not found 'node@host':absent_entry/0";
    internal const string ControlMismatchReason = "Library module name 'native_entry_alpha' does not match calling module ''mod\\n@host''";
    internal const string UnsignedArityReason = "Function not found native_entry_beta:absent_entry/4294967295";
    internal const string NegativeVetoReason = "Library load-call unsuccessful (-7).";
    internal const int VetoCode = 1;
    internal const int NegativeVetoCode = -7;
    internal const int SingleEntry = 1;
    internal const int DuplicateEntries = 2;
    internal const int NeverInvokedFunctionAddress = 1;
    internal const uint IdentityArity = 1;
}
