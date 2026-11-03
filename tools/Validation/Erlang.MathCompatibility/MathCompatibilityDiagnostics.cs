internal static class MathCompatibilityDiagnostics
{
    internal const string Usage = "Usage: MathCompatibility <managed|beam> <oracle-directory> <report.json>";
    internal const string Exports = "Selected math backend exports differ from pinned OTP declarations";
    internal const string ImproperList = "Oracle contains an improper list";
    internal const string Count = "Oracle numeric case count differs from the complete declared corpus";
    internal const string Ownership = "Selected math backend ownership or identity changed";
    internal const string OracleIdentity = "Original math oracle identity is incomplete or changed";
    internal const string StackMismatch = "Original values/errors/full stacks differ from the selected math backend";
}
