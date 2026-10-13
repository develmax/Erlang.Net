namespace Erlang;

[Flags]
internal enum ProcessTraceFlags
{
    None = 0,
    Send = 1,
    Receive = 2,
    Call = 4,
    Arity = 8,
    Timestamp = 16,
    MonotonicTimestamp = 32,
    StrictMonotonicTimestamp = 64
}
