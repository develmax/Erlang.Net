internal static class MfaCompatibilityIndexNames
{
    internal const string File = "API_COMPATIBILITY_INDEX.md";
    internal const string Title = "# API compatibility index";
    internal const string Baseline = "Baseline";
    internal const string Introduction = "Registration and passing selected tests do not establish full compatibility. This index separates every registered command by its audited status. The [complete ledger report](" + MfaCompatibilityNames.Report + ") records scope, evidence and remaining checks for each command.";
    internal const string Summary = "**{0} commands 100% compatible; {1} with known differences; {2} needing verification for 100% compatibility.**";
    internal const string Scope = "100% compatible applies to the complete observable contract on the supported Erlang term model. It does not assert identical VM internals, timing or performance. Known differences require implementation changes and verification before promotion; pending commands require a complete contract audit and original OTP comparisons.";
    internal const string TableHeader = "| Command | Details |";
    internal const string TableSeparator = "| --- | --- |";
    internal const string Details = "Scope, evidence and remaining checks";
    internal const string VerifiedAnchor = "100-compatible";
    internal const string DifferentAnchor = "known-differences";
    internal const string PendingAnchor = "needs-verification-for-100-compatibility";
}
