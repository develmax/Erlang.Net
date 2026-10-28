namespace Erlang;

public sealed partial class BitString
{
    internal object? NativeResourceIdentity524 => backing.NativeIdentity524;

    internal IDisposable? RetainNativeRoot524() => backing.RetainNative524();

    internal static BitString FromNativeStorage524(INativeBinaryStorage524 storage)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(storage.DataLength524);
        int bits = checked(storage.DataLength524 * BitStringLayout.BitsPerByte);

        // enif_make_resource_binary always wraps refc bits, even empty/small.
        return new BitString(new BinaryBacking(storage), 0, bits);
    }

    internal BitString CloneNativeView524() => new(backing, bitOffset, BitLength);

    internal bool BorrowNativeBinary524(out nint pointer, out IDisposable? lease)
    {
        pointer = 0;
        lease = null;
        if (!IsBinary || bitOffset % BitStringLayout.BitsPerByte != 0 || backing.NativeStorage524 is not { } native)
            return false;
        IDisposable retained = native.RetainRoot524();
        try
        {
            pointer = checked(native.Pointer524 + (nint)(bitOffset / BitStringLayout.BitsPerByte));
            lease = retained;

            return true;
        }
        catch
        {
            retained.Dispose();
            throw;
        }
    }
}
