namespace Erlang.Tests;

internal static class SequenceExitHostParameters
{
    public const string Test = "runtime/seq-exit-asynchronous-linked-token-snapshots";
    public const string Failure = "Linked EXIT token snapshot or clock contract failed";
    public const string Parent = "parent";
    public const string AfterExits = "after_exits";
    public const string Clock = "clock";
    public const int Children = 64;
    public const int ChildLastCount = 3;
    public const int ChildSerial = 10;
    public const int ParentSerial = 200;
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
}
