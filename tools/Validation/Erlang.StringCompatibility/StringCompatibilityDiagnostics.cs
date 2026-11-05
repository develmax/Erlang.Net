internal static class StringCompatibilityDiagnostics
{
    internal const string Usage = "Usage: Erlang.StringCompatibility <repository> <corpus-directory> <managed|beam>";
    internal const string UnknownBackend = "Unknown string backend";
    internal const string ExportMismatch = "The C# export surface differs from pinned OTP";
    internal const string PublicationRefused = "Cannot publish explicit Unicode dependencies";
    internal const string ResourceControls = "Compiled pattern comparison accepted an altered stack, binding or query";
    internal const string TrailingData = "Original string corpus contains trailing ETF data";

    internal static string ExecutionCase(int index, string function) => $"String corpus execution failed at case {index}: {function}";
}
