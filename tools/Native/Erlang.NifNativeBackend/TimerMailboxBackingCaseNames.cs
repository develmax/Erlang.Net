namespace Erlang.NifNativeBackend;

public static class TimerMailboxBackingCaseNames
{
    public const string OnHeap = "mailbox/actual-timer-on-heap-backing";
    public const string OffHeap = "mailbox/actual-timer-off-heap-backing";
    public const string Wrapped = "mailbox/actual-start-timer-wrapped-backing";
}
