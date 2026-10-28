namespace Erlang;

internal sealed unsafe class NativeResourceBinaryStorage524 : INativeBinaryStorage524
{
    private readonly NativeResourcePayload524 payload;
    private readonly nint data;
    public object ResourceIdentity524 => payload;
    public nint Pointer524
    {
        get
        {
            payload.ValidateRead524();

            return data;
        }
    }
    public int DataLength524 { get; }
    public long AllocatedBytes524 => payload.AllocationBytes;
    public long ReferencedBytes524 => payload.ReferencedBytes527;

    internal NativeResourceBinaryStorage524(NativeResourcePayload524 payload, nint data, nuint bytes)
    {
        if (bytes > int.MaxValue / BitStringLayout.BitsPerByte || payload.AllocationBytes > int.MaxValue || bytes != 0 && data == 0)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
        payload.ValidateRead524();
        this.payload = payload;
        this.data = data;
        DataLength524 = checked((int)bytes);
    }

    public ReadOnlySpan<byte> Bytes524
    {
        get
        {
            payload.ValidateRead524();

            return new ReadOnlySpan<byte>((void*)data, DataLength524);
        }
    }

    public IDisposable RetainRoot524() => payload.RetainRoot524();
}
