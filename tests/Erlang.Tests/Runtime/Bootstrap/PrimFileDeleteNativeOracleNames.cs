namespace Erlang.Tests;

internal static class PrimFileDeleteNativeOracleNames
{
    internal const string Ok = "ok";
    internal const string Error = "error";
    internal const string Source = "unlink-races.c";
    internal const string Executable = "unlink-races";
    internal const string Compiler = "cc";
    internal const string Receipt = "native-unlink-oracle.json";
    internal const string OneSuccess = "OneSuccess";
    internal const string BothSuccess = "BothSuccess";
    internal const string RoundCount = "Rounds";
    internal const string SequentialVerified = "SequentialVerified";
    internal const string Missing = "enoent";
    internal const string OriginalLeaf = "OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5:erts/emulator/nifs/unix/unix_prim_file.c:efile_del_file";
    internal const string StartFailed = "Cannot start the native unlink oracle";
    internal const string Failed = "Native unlink oracle failed: ";
    internal const string InvalidCapture = "Native unlink oracle round accounting differs";
    internal const int Rounds = 2048;
}
