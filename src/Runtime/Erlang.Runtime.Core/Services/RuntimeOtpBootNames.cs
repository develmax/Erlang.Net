namespace Erlang;

internal static class RuntimeOtpBootNames
{
    internal const string NotifyWhenStarted = "notify_when_started";
    internal const string OriginalInitRequired = "OTP boot requires a selected BEAM init:boot/1 entry";
    internal const string AlreadyStarted = "The runtime initializer name has already been transferred";
    internal const string EndedBeforeReady = "The original OTP initializer ended before reporting startup";
    internal const string InvalidArguments = "OTP boot arguments must be byte-aligned binaries";
    internal const string FailureSeparator = ": ";
}
