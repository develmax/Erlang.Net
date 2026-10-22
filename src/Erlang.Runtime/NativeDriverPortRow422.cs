namespace Erlang;

// Genuine native backend state, never an invented OS process or fd stream.
// The child owns data pointers; this row owns VM links, option state, bytes and
// the pending physical acknowledgement. It survives logical close until ACK.
internal sealed class NativeDriverPortRow422(
    ErlDriverPortIdentity420 native,
    PortTerm identity,
    Pid connected,
    PortOpenOptions options
)
{
    internal ErlDriverPortIdentity420 Native { get; } = native;
    internal PortTerm Identity { get; } = identity;
    internal Pid Connected = connected;
    internal PortOpenOptions Options { get; } = options;
    internal HashSet<Pid> Links { get; } = new() { connected };
    internal PortLineBuffer? Lines = options.LineBytes == 0 ? null : new(options.LineBytes);
    internal Term Data = Term.A(ProcessRegistryAtoms.Undefined);
    internal long InputBytes;
    internal long OutputBytes;
    internal ulong QueuedMessageBytes;
    internal bool ManualBusy;
    internal bool MessageBusy;
    internal bool CheckMessageBusy;
    internal bool MessageLimitsEnabled = options.MessageLowWatermark != ulong.MaxValue;
    internal ulong MessageLow = options.MessageLowWatermark;
    internal ulong MessageHigh = options.MessageHighWatermark;
    internal DriverPortTurn422? Turn;
    internal Task NativeTurns = Task.CompletedTask;
    internal bool Closing;
    internal bool StopRequested;
    internal Term Reason = Term.A(ProcessExitReasons.Normal);
    internal bool PhysicallyStopped;
    internal bool OpenResultKnown;
    internal bool OpenSucceeded;
    internal bool NativeReferenceReleased;
    internal int CleanupStatus;
    internal byte[] Command = [];
    internal bool DriverSoftBusy;
    internal Task Commands = Task.CompletedTask;
    internal TaskCompletionSource Writable = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource Stopped { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}
