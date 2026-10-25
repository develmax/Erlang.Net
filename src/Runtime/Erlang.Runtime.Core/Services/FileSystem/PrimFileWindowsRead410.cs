using System.Runtime.InteropServices;

namespace Erlang;

internal static class PrimFileWindowsRead410
{
    internal static unsafe Term Run(Term term, CancellationToken cancellation)
    {
        PrimFileWindowsOperations410.Require(cancellation);
        if (!PrimFileWindowsPaths410.Marshal(term, out string path, out string error))
            return PrimFileWindowsErrors410.Tuple(error);
        using var handle = PrimFileWindowsNative410.Open(
            path,
            PrimFileWindowsNames410.GenericRead,
            PrimFileWindowsNames410.ShareAll,
            IntPtr.Zero,
            PrimFileWindowsNames410.OpenExisting,
            PrimFileWindowsNames410.Normal,
            IntPtr.Zero
        );
        int last = Marshal.GetLastPInvokeError();
        if (handle.IsInvalid)
        {
            uint attributes = PrimFileWindowsNative410.Attributes(path);
            if (attributes != PrimFileWindowsNames410.InvalidAttributes
                && (attributes & PrimFileWindowsNames410.DirectoryAttribute) != 0)
                return PrimFileWindowsErrors410.Tuple(PrimFileWindowsNames410.IsDirectory);

            return PrimFileWindowsErrors410.Last(last);
        }
        ulong known = PrimFileWindowsInfo410.SizeFromHandle(handle, cancellation);
        if (known > (ulong)(int.MaxValue / 8)) throw new NotSupportedException(PrimFileWindowsLimits410.BinaryCapacity);
        int size = (int)known;
        var bytes = new byte[size == 0 ? PrimFileWindowsNames410.UnknownReadBuffer : size];
        int used = 0;
        for (; ; )
        {
            cancellation.ThrowIfCancellationRequested();
            int block = 0;
            int requested = bytes.Length - used;
            fixed (byte* start = bytes)
            {
                while (block < requested)
                {
                    cancellation.ThrowIfCancellationRequested();
                    bool success = PrimFileWindowsNative410.Read(
                        handle,
                        start + used + block,
                        (uint)(requested - block),
                        out uint read,
                        IntPtr.Zero
                    );
                    last = Marshal.GetLastPInvokeError();
                    if (!success && last == PrimFileWindowsLimits410.BrokenPipe) break;
                    if (!success && last != PrimFileWindowsLimits410.HandleEof)
                        return PrimFileWindowsErrors410.Last(last);
                    if (read == 0) break;
                    block += checked((int)read);
                }
            }
            used += block;
            if (block < bytes.Length - used || used == size) break;
            long next = (long)used * 2;
            if (next > int.MaxValue / 8) throw new NotSupportedException(PrimFileWindowsLimits410.BinaryCapacity);
            Array.Resize(ref bytes, (int)next);
        }
        if (used != bytes.Length) Array.Resize(ref bytes, used);

        return PrimFileWindowsErrors410.Ok(new BitString(bytes));
    }
}
