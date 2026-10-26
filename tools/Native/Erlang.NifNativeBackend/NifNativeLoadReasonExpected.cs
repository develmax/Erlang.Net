namespace Erlang.NifNativeBackend;

// Independent literals captured from original OTP run39013935267; never call production reason builders here.
internal static class NifNativeLoadReasonExpected
{
    internal static readonly IReadOnlyDictionary<string, (string Category, string Reason)> Cases =
        new Dictionary<string, (string Category, string Reason)>(StringComparer.Ordinal)
        {
            ["alpha_duplicate"] = ("bad_lib", "Duplicate NIF entry for native_entry_alpha:identity/1"),
            ["beta_undeclared"] = ("bad_lib", "Function not declared as nif native_entry_beta:ordinary/0"),
            ["beta_missing"] = ("bad_lib", "Function not found native_entry_beta:absent_entry/0"),
            ["module_mismatch"] = ("bad_lib", "Library module name 'native_entry_alpha' does not match calling module 'native_entry_beta'"),
            ["load_veto"] = ("load", "Library load-call unsuccessful (1)."),
        };
}
