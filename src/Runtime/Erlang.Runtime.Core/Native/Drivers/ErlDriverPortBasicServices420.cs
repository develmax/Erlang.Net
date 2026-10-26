using System.Runtime.InteropServices;

namespace Erlang;

internal sealed class ErlDriverPortBasicServices420
{
    private readonly Func<nint, ErlDriverPortAdmission420> acquire;
    private readonly IErlDriverPortHost420 host;
    private readonly ErlDriverTermTokens420 tokens;
    private readonly ErlDriverCallbackTypes420.erl_errno_id errno;
    private readonly ErlDriverServiceFault420 fault;

    internal ErlDriverPortBasicServices420(
        Func<nint, ErlDriverPortAdmission420> acquire,
        IErlDriverPortHost420 host,
        ErlDriverTermTokens420 tokens,
        IReadOnlyDictionary<string, Delegate> primitives,
        ErlDriverServiceFault420 fault
    )
    {
        this.acquire = acquire;
        this.host = host;
        this.tokens = tokens;
        errno = (ErlDriverCallbackTypes420.erl_errno_id)primitives[nameof(ErlDriverCallbackTypes420.erl_errno_id)];
        this.fault = fault;
        Callbacks = new Dictionary<string, Delegate>(StringComparer.Ordinal)
        {
            [nameof(ErlDriverCallbackTypes420.driver_failure_atom)] = new ErlDriverCallbackTypes420.driver_failure_atom(FailureAtom),
            [nameof(ErlDriverCallbackTypes420.driver_failure_posix)] = new ErlDriverCallbackTypes420.driver_failure_posix(FailurePosix),
            [nameof(ErlDriverCallbackTypes420.driver_failure)] = new ErlDriverCallbackTypes420.driver_failure(Failure),
            [nameof(ErlDriverCallbackTypes420.driver_failure_eof)] = new ErlDriverCallbackTypes420.driver_failure_eof(FailureEof),
            [nameof(ErlDriverCallbackTypes420.driver_exit)] = new ErlDriverCallbackTypes420.driver_exit(Exit),
            [nameof(ErlDriverCallbackTypes420.set_busy_port)] = new ErlDriverCallbackTypes420.set_busy_port(SetBusy),
            [nameof(ErlDriverCallbackTypes420.set_port_control_flags)] = new ErlDriverCallbackTypes420.set_port_control_flags(SetControl),
            [nameof(ErlDriverCallbackTypes420.get_port_flags)] = new ErlDriverCallbackTypes420.get_port_flags(Flags),
            [nameof(ErlDriverCallbackTypes420.erl_drv_busy_msgq_limits)] = new ErlDriverCallbackTypes420.erl_drv_busy_msgq_limits(Limits),
            [nameof(ErlDriverCallbackTypes420.erl_drv_consume_timeslice)] = new ErlDriverCallbackTypes420.erl_drv_consume_timeslice(Consume),
            [nameof(ErlDriverCallbackTypes420.driver_lock_driver)] = new ErlDriverCallbackTypes420.driver_lock_driver(LockDriver)
        };
    }

    internal IReadOnlyDictionary<string, Delegate> Callbacks { get; }

    private T Use<T>(nint port, Func<ErlDriverPort420, T> operation, T invalid)
    {
        try
        {
            var admission = acquire(port);
            using var lease = admission.Lease;
            if (lease is null || admission.Status < 1) return invalid;

            return operation(lease.Port);
        }
        catch (Exception error)
        {
            fault.Record(error);

            return invalid;
        }
    }

    private int Fail(
        nint port,
        Func<Term> reason,
        bool eof,
        bool unlink
    )
        => Use(
            port,
            owner =>
        {
            int result = host.Failure(
                owner.Identity,
                reason(),
                eof,
                unlink
            );
            // Do not synchronously send ClosePort back through the callback's
            // parent request: that would wait on this same callback gate.
            if (result == 0 && (!eof || !owner.SoftEof)) owner.MarkClosing();

            return result;
        },
            -1
        );

    private Term Errno(int error) => tokens.Resolve(tokens.Atom(errno(error)));

    private int FailureAtom(nint port, nint name) => Fail(
        port,
        () => tokens.Resolve(tokens.Atom(name)),
        false,
        false
    );

    private int FailurePosix(nint port, int error) => Fail(
        port,
        () => Errno(error),
        false,
        false
    );

    private int Failure(nint port, int code) => Fail(
        port,
        () => Term.I(code),
        code == 0,
        false
    );

    private int FailureEof(nint port) => Fail(
        port,
        () => Nil.Value,
        true,
        false
    );

    private int Exit(nint port, int error)
        => Fail(
            port,
            () => error == 0 ? Term.A(ProcessExitReasons.Normal) : Errno(error),
            false,
            true
        );

    private void SetBusy(nint port, int busy)
        => Use(port, owner =>
 {
     host.SetBusy(owner.Identity, busy != 0);

     return true;
 }, false);

    private void SetControl(nint port, int flags)
        => Use(port, owner =>
 {
     owner.ControlFlags = flags;

     return true;
 }, false);

    private int Flags(nint port) => Use(port, owner => owner.OpenFlags, 0);

    private int Consume(nint port, int percent) => Use(port, owner => host.ConsumeTimeslice(owner.Identity, percent), -1);

    private int LockDriver(nint port) => Use(port, owner => host.LockDriver(owner.Identity), -1);

    private static ulong? ReadLimit(nint pointer) => pointer == nint.Zero ? null : unchecked((ulong)Marshal.ReadInt64(pointer));

    private void Limits(nint port, nint low, nint high)
    {
        try
        {
            ErlDriverBusyLimits420 limits = Use(
                port,
                owner => host.BusyLimits(owner.Identity, ReadLimit(low), ReadLimit(high)),
                new ErlDriverBusyLimits420(ulong.MaxValue, ulong.MaxValue)
            );
            if (low != nint.Zero) Marshal.WriteInt64(low, unchecked((long)limits.Low));
            if (high != nint.Zero) Marshal.WriteInt64(high, unchecked((long)limits.High));
        }
        catch (Exception error)
        {
            fault.Record(error);
        }
    }
}
