internal static class DetsCompatibilityDiagnostics
{
    internal const string Usage = "Expected repository, original capture directory and a fresh DETS output directory";
    internal const string ExistingOutput = "DETS output already exists";
    internal const string ForeignRead = "C# failed to read the original DETS file";
    internal const string PartialInsert = "DETS insert_new admitted a partial batch";
    internal const string Admission = "DETS oracle admission differs from the pinned provider";
    internal const string OracleIdentity = "DETS oracle executable identity changed";
    internal const string OracleRead = "OTP could not reopen the C# DETS images";
    internal const string Mismatch = "Bidirectional DETS file interchange differs";
    internal const string Passed = "PASS 6 DETS file interchange cases, including incremental growth";
}
