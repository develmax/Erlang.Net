namespace Erlang.Tests;

internal static class ComparisonOriginal482Names
{
    internal const string ComparisonTest = "oracle/compiled-module/comparison482-eight-bifs-whole-values-and-stacks";
    internal const string CrashdumpTest = "oracle/compiled-module/comparison482-original-crashdump-whole-values-and-stacks";
    internal const string AdmissionTest = "compiler/comparison482/original-crashdump-image-publication";
    internal const string Prefix = "Erlang.Tests.ComparisonOriginal482.";
    internal const string ComparisonObserver = "otp_frame_comparisons482_observer";
    internal const string ComparisonObserverImage = "otp_frame_comparisons482_observer.beam";
    internal const string ComparisonImage = "otp_frame_comparisons482.beam";
    internal const string ComparisonGolden = "comparison-values.etf";
    internal const string ComparisonDirectory = "comparison";
    internal const string CrashdumpObserver = "otp_crashdump482_observer";
    internal const string CrashdumpObserverImage = "otp_crashdump482_observer.beam";
    internal const string CrashdumpImage = "crashdump_viewer.beam";
    internal const string CrashdumpGolden = "crashdump-values.etf";
    internal const string CrashdumpDirectory = "crashdump";
    internal const string ListsImage = "lists.beam";
    internal const string Worker = "worker";
    internal const string Manifest = "fixture.json";
    internal const string ManifestFiles = "Files";
    internal const string Managed = "managed-values.etf";
    internal const string Output = "artifacts/library482/permanent";
    internal const string Failure = "Comparison BIF frames,original crashdump values,full stacks or actual image ownership differ";
    internal const int ComparisonOutcomes = 160;
    internal const int CrashdumpOutcomes = 20;
    internal static readonly string[] Imports = ["base64", "binary", "cdv_wx", "crashdump_viewer", "erlang", "erts_debug", "ets", "file", "filelib", "gb_trees", "gen_server", "io", "io_lib", "lists", "maps", "observer_lib", "otp_crashdump482_observer", "otp_frame_comparisons482", "otp_frame_comparisons482_observer", "proplists", "re", "string", "ttb", "unicode"];
}
