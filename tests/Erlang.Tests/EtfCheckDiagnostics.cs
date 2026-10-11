namespace Erlang.Tests;

internal static class EtfCheckDiagnostics
{
    public const string IoVecBytes = "ETF iovec fragments differ from flat encoding";
    public const string Size = "ETF counting stream differs from plain encoding";
    public const string BorrowedBinary = "ETF iovec copied a large immutable binary";
    public const string ImmutableFragment = "ETF fragment mutated through public access";
    public const string CompressedBoundary = "ETF used count crossed the first zlib member";
    public const string StrictTrailing = "Strict ETF API accepted trailing packet bytes";
    public const string TruncatedCompression = "ETF accepted a truncated packet";
    public const string DecodedEnvironment = "ETF restoration reused the source callback environment";
    public const string TargetEnvironment = "ETF restoration reused the target callback environment";
    public const string LocalDomain = "ETF local packet failed in its runtime";
    public const string OtherLocalDomain = "ETF local packet accepted by another runtime";
    public const string ReferenceEquality = "Zero-padded reference equality or hash differs";
    public const string ReferenceMutable = "Reference words exposed mutable storage";
    public const string ReferenceRoundtrip = "Five-word reference lost data in ETF";
    public const string ReferenceTruncation = "Legacy reference ID silently truncated";
    public const string ReferenceAllocation = "Reference allocation failed at the first-word rollover";
}
