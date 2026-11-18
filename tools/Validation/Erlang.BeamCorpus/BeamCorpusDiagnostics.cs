internal static class BeamCorpusDiagnostics
{
    internal const string Usage =
        "Usage: Erlang.BeamCorpus <OTP-29.1.1 installation> <report.json> [public-key-asn1-headers [otp-source-headers]] [--modules-file <path>] [--jobs <count>]";
    internal const string GenerateAsn1Usage =
        "       Erlang.BeamCorpus --generate-asn1 <schema.asn1> <output-directory>";
    internal const string InvalidModuleSelection =
        "The module selection must contain at least one path and no duplicates.";

    internal static string ModuleSelectionReadFailed(string reason) =>
        $"Could not read module selection: {reason}";

    internal static string UnknownModules(IEnumerable<string> modules) =>
        "The reference installation does not contain: " + string.Join(", ", modules.Order(StringComparer.Ordinal));
}
