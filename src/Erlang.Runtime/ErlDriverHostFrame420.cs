namespace Erlang;

// Handles identify actual live host objects inside one child session. They are
// never interchangeable with native addresses or guest PID/reference values.
internal sealed record ErlDriverHostFrame420(
    int Version,
    string Session,
    ErlDriverHostKind420 Kind,
    ErlDriverHostOperation420 Operation,
    ulong Request,
    ulong Driver,
    ulong Port,
    ulong Actor,
    uint Command,
    int NativeResult,
    byte[]? Name,
    byte[]? Data,
    ulong EventSequence = 0,
    bool SchedulerThread = false
);
