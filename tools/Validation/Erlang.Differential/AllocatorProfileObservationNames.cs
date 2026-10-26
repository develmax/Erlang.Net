namespace Erlang.Differential;

internal static class AllocatorProfileObservationNames
{
    internal const string Command = "--allocator-profile";
    internal const string BaselineVersion = "29.1.1";
    internal const string PinnedCommit = "ad05823719d77c8faee87348ea39513d4e2f99c5";
    internal const string NoShell = "-noshell";
    internal const string NoInput = "-noinput";
    internal const string Evaluate = "-eval";
    internal const string NativeBackend = "libc";
    internal const string TrimThreshold = "tt";
    internal const string TopPad = "tp";
    internal const string Failure = "Allocator profile observation did not match its declared native and CLR contracts.";
    internal const string Timeout = "Original allocator profile process timed out.";
    internal const string VersionCommand = "{ok,V}=file:read_file(filename:join([code:root_dir(),\"releases\",erlang:system_info(otp_release),\"OTP_VERSION\"])), io:format(\"~s\",[V]), halt().";
    internal const string NativeExpectation = "libc backend, enabled true, instance0/options shape; optional nonnegative tt then tp retained";
    internal const string SdkExpectation = "dotnet_native_memory backend, enabled true, instance0/options shape; empty tuning tail";
    internal const string CommitVerification = "This mode verifies OTP_VERSION; exact source commit is verified separately by the workflow build gate.";
    internal const int OutcomeSize = 2;
    internal const int OutcomeValue = 1;
    internal const int ObservationSize = 7;
    internal const int Tag = 0;
    internal const int AtomEnabled = 1;
    internal const int AtomBackend = 2;
    internal const int AtomTail = 3;
    internal const int ListEnabled = 4;
    internal const int ListBackend = 5;
    internal const int ListTail = 6;
    internal const int OptionSize = 2;
    internal const int OptionKey = 0;
    internal const int OptionValue = 1;
    internal const int MaximumTuningOptions = 2;
    internal const int FirstTuningOption = 0;
    internal const int SecondTuningOption = 1;
}
