namespace Erlang.Tests;

internal static class ProcessExitCheckDiagnostics
{
    public const string WrongReply = "Exit signal did not return true";
    public const string WrongCompletion = "Pending alias kill did not terminate its child with killed";
    public const string AliveChild = "A child remained alive after admitted kill";
    public const string InvalidPendingCount = "Pending signal cleanup left mailbox entries or an invalid count";
}
