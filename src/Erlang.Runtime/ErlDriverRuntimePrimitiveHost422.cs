namespace Erlang;

// Lives in the actual parent VM. The dedicated child's proxy forwards to this
// owner, so driver and guest APIs see the same clock, unique-now sequence and
// private Windows environment tree. No machine-wide environment is mutated.
internal sealed class ErlDriverRuntimePrimitiveHost422 : IErlDriverPrimitiveHost422
{
    private readonly ProcessRuntime runtime;
    private readonly RuntimeOsEnvironmentDriverLease422? environmentLease;
    public ErlDriverSystemInfo422 SystemInfo { get; }

    internal ErlDriverRuntimePrimitiveHost422(ProcessRuntime runtime, ErlDriverSystemInfo422 systemInfo)
    {
        this.runtime = runtime;
        SystemInfo = systemInfo;
    }

    internal ErlDriverRuntimePrimitiveHost422(ProcessRuntime runtime, ErlDriverSystemInfo422 systemInfo,
        RuntimeOsEnvironmentDriverLease422 environmentLease) : this(runtime, systemInfo)
    {
        if (!environmentLease.IsOwnedBy(runtime.OsEnvironment))
            throw new ArgumentException(DriverParentWireNames422.Foreign, nameof(environmentLease));
        this.environmentLease = environmentLease;
    }

    public long GetNowMicroseconds()
    {
        var value = (TupleTerm)ErlangClockState.Timestamp(true);
        var mega = ((Integer)value.Items[0]).Value;
        var seconds = ((Integer)value.Items[1]).Value;
        var micro = ((Integer)value.Items[2]).Value;

        return checked((long)((mega * ErlangClockParameters.SecondsPerMegaSecond + seconds)
            * ErlangClockParameters.MicrosecondsPerSecond + micro));
    }

    public long Clock(bool offset, int unit)
    {
        long frequency = unit switch
        {
            ErlDriverPrimitiveNames422.Second => 1,
            ErlDriverPrimitiveNames422.Millisecond => ErlangClockParameters.MillisecondsPerSecond,
            ErlDriverPrimitiveNames422.Microsecond => ErlangClockParameters.MicrosecondsPerSecond,
            ErlDriverPrimitiveNames422.Nanosecond => ErlangClockParameters.NanosecondsPerSecond,
            _ => 0
        };
        if (frequency == 0) return ErlDriverPrimitiveNames422.TimeError;
        var value = offset ? ErlangClockState.OffsetNanoseconds : ErlangClockState.MonotonicNanoseconds;

        return checked((long)ErlangClockState.Floor(value * frequency, ErlangClockParameters.NanosecondsPerSecond));
    }

    public string? ReadEnvironment(byte[] nativeKey) => environmentLease is null
        ? Environment.ReadDriver422(nativeKey) : environmentLease.Read(nativeKey);

    public int WriteEnvironment(byte[] nativeKey, byte[] nativeValue) => environmentLease is null
        ? Environment.WriteDriver422(nativeKey, nativeValue) : environmentLease.Write(nativeKey, nativeValue);

    private RuntimeOsEnvironment Environment => runtime.OsEnvironment
        ?? throw new PlatformNotSupportedException(ErlDriverPrimitiveNames422.InvalidLayout);
}
