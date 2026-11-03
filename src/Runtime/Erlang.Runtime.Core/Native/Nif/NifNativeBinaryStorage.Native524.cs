namespace Erlang;

internal sealed unsafe partial class NifNativeBinaryStorage
{
    private IDisposable? nativeBorrow524;
    private BitString? nativeBorrowView524;

    private NifNativeBinaryStorage(BitString view, nint address, IDisposable lease)
    {
        pointer = address;
        size = (nuint)view.ByteLength;
        nativeBorrow524 = lease;
        nativeBorrowView524 = view;
    }

    internal static NifNativeBinaryStorage? Borrow524(BitString view)
    {
        if (!view.BorrowNativeBinary524(out nint pointer, out IDisposable? lease)) return null;
        try
        {
            return new(view, pointer, lease!);
        }
        catch
        {
            lease!.Dispose();
            throw;
        }
    }

    private bool ReleaseBorrowed524()
    {
        IDisposable? lease = nativeBorrow524;
        if (lease is null) return false;
        nativeBorrow524 = null;
        nativeBorrowView524 = null;
        pointer = 0;
        lease.Dispose();

        return true;
    }
}
