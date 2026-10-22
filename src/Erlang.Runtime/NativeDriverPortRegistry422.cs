namespace Erlang;

internal sealed partial class NativeDriverPortRegistry422 : IErlDriverPortHost420, IErlDriverPortTaskParent420
{
    private readonly ProcessRuntime runtime;
    private readonly object gate;
    private readonly RuntimeDdllParent422 drivers;
    private readonly DriverDispatcherLease422 dispatcher;
    private readonly Dictionary<ErlDriverPortIdentity420, NativeDriverPortRow422> native = new();
    private readonly Dictionary<PortTerm, NativeDriverPortRow422> ports = new();

    internal NativeDriverPortRegistry422(
        ProcessRuntime runtime,
        object gate,
        RuntimeDdllParent422 drivers,
        DriverDispatcherLease422 dispatcher
    )
    {
        this.runtime = runtime;
        this.gate = gate;
        this.drivers = drivers;
        this.dispatcher = dispatcher;
    }

    private NativeDriverPortRow422? Find(ErlDriverPortIdentity420 identity)
        => native.GetValueOrDefault(identity);

    public int Failure(
        ErlDriverPortIdentity420 identity,
        Term reason,
        bool eof,
        bool unlink
    )
    {
        lock (gate)
        {
            var row = Find(identity);
            if (row is null || row.PhysicallyStopped) return -1;
            if (unlink)
            {
                row.Links.Remove(row.Connected);
                runtime.RemoveDriverConnectedLink422(row.Connected, row.Identity);
            }
            if (eof)
                foreach (Term line in NativeDriverPortOutput422.Flush(row)) Deliver(row, line);
            if (eof && row.Options.Eof && !row.Closing)
            {
                runtime.SendPortNotification(row.Connected, Term.Tuple(row.Identity, Term.A(PortNames.Eof)));

                return 0;
            }
            // Staging only. Child stops after its current callback; do not send
            // ClosePort recursively from this reverse callback.
            row.Closing = true;
            row.Reason = eof ? Term.A(ProcessExitReasons.Normal) : reason;
            row.Writable.TrySetResult();

            return 0;
        }
    }

    public void SetBusy(ErlDriverPortIdentity420 identity, bool busy)
    {
        lock (gate)
        {
            var row = Find(identity);
            if (row is null || row.PhysicallyStopped) return;
            row.ManualBusy = busy;
            if (!busy && !row.MessageBusy) row.Writable.TrySetResult();
            if (busy && row.Writable.Task.IsCompleted)
                row.Writable = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    public ErlDriverBusyLimits420 BusyLimits(ErlDriverPortIdentity420 identity, ulong? lowValue, ulong? highValue)
    {
        lock (gate)
        {
            var row = Find(identity);
            if (row is null || row.PhysicallyStopped || !row.MessageLimitsEnabled)
                return new(ulong.MaxValue, ulong.MaxValue);
            ulong low = lowValue ?? 0, high = highValue ?? 0;
            if (low == ulong.MaxValue || high == ulong.MaxValue)
            {
                row.MessageLimitsEnabled = false;
                row.MessageBusy = false;
                row.CheckMessageBusy = false;
                if (!row.ManualBusy) row.Writable.TrySetResult();

                return new(low, high); // exact source in/out pointer values on disable.
            }
            bool written = false;
            if (low == 0) low = row.MessageLow;
            else
            {
                if (row.MessageHigh < low) row.MessageHigh = low;
                row.MessageLow = low;
                written = true;
            }
            if (high == 0) high = row.MessageHigh;
            else
            {
                if (low > high) row.MessageLow = low = high;
                row.MessageHigh = high;
                written = true;
            }
            if (written)
            {
                if (row.QueuedMessageBytes > high)
                {
                    row.MessageBusy = true;
                    if (row.Writable.Task.IsCompleted) row.Writable = new(TaskCreationOptions.RunContinuationsAsynchronously);
                }
                else if (row.QueuedMessageBytes < low) row.CheckMessageBusy = true;
            }

            return new(low, high);
        }
    }

    public int ConsumeTimeslice(ErlDriverPortIdentity420 identity, int percent)
    {
        lock (gate)
        {
            var row = Find(identity);
            if (row is null || row.PhysicallyStopped) return -1;
            var turn = row.Turn ?? throw new NotSupportedException(DriverPortDispatchNames422.OutsidePortTurn);

            return turn.Consume(percent);
        }
    }

    public int LockDriver(ErlDriverPortIdentity420 identity)
    {
        lock (gate)
            return Find(identity) is { PhysicallyStopped: false } ? drivers.LockDriver(identity.Driver) : -1;
    }

    public Task Dispatch(ErlDriverPortIdentity420 identity, Func<Task> callback)
    {
        lock (gate)
            if (Find(identity) is not { PhysicallyStopped: false })
                throw new InvalidDataException(DriverParentWireNames422.Invalid);

        // Closing ports remain eligible for async_free/stop cleanup until ACK.
        return DispatchTurn(identity, callback);
    }

    private void Deliver(NativeDriverPortRow422 row, Term data)
        => runtime.SendPortNotification(row.Connected,
            Term.Tuple(row.Identity, Term.Tuple(Term.A(PortNames.Data), data)));

    internal void Output(ErlDriverPortIdentity420 identity, ErlDriverOutput420 output)
    {
        lock (gate)
        {
            var row = Find(identity) ?? throw new InvalidDataException(DriverParentWireNames422.Invalid);
            if (row.PhysicallyStopped) throw new InvalidDataException(DriverParentWireNames422.Invalid);
            if (row.Closing) return; // pinned driver_output returns success, discards closing data.
            long count = checked(output.Header.LongLength + (output.Kind == ErlDriverOutputKind420.Vector
                ? output.Segments!.Sum(segment => segment.LongLength) : output.Body.LongLength));
            row.InputBytes = checked(row.InputBytes + count);
            foreach (Term data in NativeDriverPortOutput422.Data(row, output)) Deliver(row, data);
        }
    }
}
