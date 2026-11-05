internal static class UnicodeUtilValidationNames
{
    internal const string Usage = "Usage: Erlang.UnicodeUtilCompatibility repository corpus-directory Managed|Beam core|long|tables|conformance|probe";
    internal const string Failure = "Unicode utility pinned OTP comparison failed";
    internal const string Observer = "unicode_util_port_cases";
    internal const string Module = "unicode_util";
    internal const int Maximum = 0x10ffff;
    internal const int Batch = 32;
    internal const int NormalizationColumns = 5;
    internal static readonly string[] Forms = ["nfc", "nfd", "nfkc", "nfkd"];
    internal static readonly int[,] NormalizationExpected = { { 1, 1, 1, 3, 3 }, { 2, 2, 2, 4, 4 }, { 3, 3, 3, 3, 3 }, { 4, 4, 4, 4, 4 } };
    internal static readonly TimeSpan Deadline = TimeSpan.FromMinutes(5);
}
