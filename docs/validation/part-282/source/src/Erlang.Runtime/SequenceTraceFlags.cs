namespace Erlang;

[Flags]
internal enum SequenceTraceFlags : long
{
    None = 0,
    Send = 1 << 0,
    Receive = 1 << 1,
    Print = 1 << 2,
    Timestamp = 1 << 3,
    StrictMonotonicTimestamp = 1 << 4,
    MonotonicTimestamp = 1 << 5
}
