internal static class MfaCompatibilityNames
{
    public const int SchemaVersion = 1;
    public const int Sha256Length = 64;
    public const string File = "mfa-compatibility.json";
    public const string Report = "API_COMPATIBILITY.md";
    public const string Verified = "Verified compatible";
    public const string Pending = "Needs verification";
    public const string Different = "Known differences";
    public const string OriginalComparison = "OriginalOtpComparison";
    public const string Audit = "CompatibilityAudit";
    public const string SourceReview = "SourceReview";
    public const string LocalValidation = "LocalValidation";
    public const string SourceBindingScope = "Audited current implementation source binding";
    public const string Invalid = "Invalid baseline-bound MFA compatibility ledger or evidence";
    public const string RegistrationStatus = "Implemented";
    public const string Title = "# API compatibility";
    public const string PendingAudit = "Pending audit";
    public const string BaselineLabel = "Pinned baseline";
    public const string CommandsLinkLabel = "Every command, evidence and limits";
    public const string Heading = "## Compatibility of registered APIs";
    public const string Method = "Compatibility is tracked per command in an explicit, baseline-bound audited ledger. Direct registry tests establish API presence only. Verified requires an audit of the command's complete observable semantic contract on the supported Erlang term model, actual original OTP comparisons, and no known or unresolved semantic gaps. Selected-case or subcontract-only evidence remains Needs verification. Verification does not claim identical internal VM layout, timings or performance; APIs exposing resource values or options still require their complete observable contracts.";
    public const string TableHeader = "| Command | Compatibility | Audited scope | Evidence | Unresolved limits |";
    public const string TableSeparator = "| --- | --- | --- | --- | --- |";
}
