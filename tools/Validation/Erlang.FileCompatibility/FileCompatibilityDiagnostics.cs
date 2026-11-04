internal static class FileCompatibilityDiagnostics
{
    internal const string Usage = "Usage: Erlang.FileCompatibility <repository> <captured-directory> Managed|Beam [original.etf|runtime-gates.etf] [row-index]";
    internal const string Corpus = "Unknown file corpus";
    internal const string Publication = "File probe image publication failed";
    internal const string OnLoad = "File probe on_load failed: ";
    internal const string Api = "File API differs from pinned OTP";
    internal const string Owner = "File source owner differs from requested backend";
    internal const string Server = "File server: ";
    internal const string Provenance = "File reference provenance is incomplete or uses a different OTP baseline";
    internal const string Digest = "File reference source or captured bytes changed after admission";

    internal static string Results(
        string backend,
        int compared,
        int differences,
        int skipped
    )
        => $"{backend}: {compared} observations, {differences} differences; {skipped} skipped rows.";
}
