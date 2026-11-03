namespace Erlang.Tests;

internal static class OrdsetsCompatibilityNames
{
    internal const string ManagedCase = "runtime/ordsets/managed-whole-otp-corpus";
    internal const string BeamCase = "runtime/ordsets/original-whole-otp-corpus";
    internal const string Module = "ordsets";
    internal const string Lists = "lists";
    internal const string Observer = "ordsets_compatibility_observer";
    internal const string Entry = "run";
    internal const string Prefix = "Erlang.Tests.OrdsetsCompatibility.";
    internal const string Image = "ordsets_compatibility_observer.beam";
    internal const string Golden = "original.etf";
    internal const string Worker = "ordsets_core_worker";
    internal const string Erlang = "erlang";
    internal const string ModuleInfo = "module_info";
    internal const string ImageExtension = ".beam";
    internal const string Provenance = "provenance.json";
    internal const string PinnedCommit = "ad05823719d77c8faee87348ea39513d4e2f99c5";
    internal const string FailureDirectory = "artifacts/ordsets-verification/failures";
    internal const string ReceiptDirectory = "artifacts/ordsets-verification/receipts";
    internal const string Failure = "Ordsets whole OTP corpus differs";
    internal const int ExportCount = 21;
    internal const int ObservationCount = 12247;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(75);
}
