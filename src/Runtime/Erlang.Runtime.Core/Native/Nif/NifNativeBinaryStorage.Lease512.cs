namespace Erlang;

internal sealed unsafe partial class NifNativeBinaryStorage
{
    internal NifBinaryPayloadLease512 RetainPayload512() => new(this);

    internal void CopyPayload512(nuint offset, Span<byte> destination)
    {
        lock (gate)
        {
            if (pointer == 0)
                throw new InvalidOperationException(NifNativeBinaryNames.ExpiredStorage);
            if (offset > size || (nuint)destination.Length > size - offset)
                throw new InvalidOperationException(NifNativeBinaryNames.SubBinaryRange);
            new ReadOnlySpan<byte>((byte*)pointer + offset, destination.Length).CopyTo(destination);
        }
    }
}
