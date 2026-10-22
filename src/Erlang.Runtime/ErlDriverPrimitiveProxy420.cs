namespace Erlang;

// Native callbacks are synchronous. The dedicated control reader remains free
// while this physical driver thread waits for the real parent state operation.
internal sealed class ErlDriverPrimitiveProxy420 : IErlDriverPrimitiveHost422
{
    private readonly ErlDriverHostPeer420 peer;
    public ErlDriverSystemInfo422 SystemInfo { get; }

    internal ErlDriverPrimitiveProxy420(ErlDriverHostPeer420 peer, ErlDriverSystemInfo422 systemInfo)
    {
        this.peer = peer;
        SystemInfo = systemInfo;
    }

    private ErlDriverHostFrame420 Request(ErlDriverPrimitiveCommand420 command, byte[]? name = null, byte[]? data = null)
        => peer.RequestAsync(
            ErlDriverHostOperation420.PrimitiveHost,
            0,
            0,
            0,
            (uint)command,
            name,
            data,
            CancellationToken.None
        ).AsTask().GetAwaiter().GetResult();

    public long GetNowMicroseconds()
    {
        var result = Request(ErlDriverPrimitiveCommand420.GetNow);
        if (result.NativeResult != 0) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);

        return ErlDriverPrimitiveWire420.Decode<long>(result.Data);
    }

    public long Clock(bool offset, int unit)
    {
        var result = Request(ErlDriverPrimitiveCommand420.Clock,
            data: ErlDriverPrimitiveWire420.Encode(new ErlDriverClockRequest420(offset, unit)));
        if (result.NativeResult != 0) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);

        return ErlDriverPrimitiveWire420.Decode<long>(result.Data);
    }

    public string? ReadEnvironment(byte[] nativeKey)
    {
        var result = Request(ErlDriverPrimitiveCommand420.ReadEnvironment, nativeKey);
        if (result.NativeResult == -1 && result.Data is null) return null;
        if (result.NativeResult != 0) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);

        return ErlDriverPrimitiveWire420.Decode<string>(result.Data);
    }

    public int WriteEnvironment(byte[] nativeKey, byte[] nativeValue)
    {
        var result = Request(ErlDriverPrimitiveCommand420.WriteEnvironment, nativeKey, nativeValue);
        if (result.NativeResult is < -1 or > 1 || result.Data is not null)
            throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);

        return result.NativeResult;
    }
}
