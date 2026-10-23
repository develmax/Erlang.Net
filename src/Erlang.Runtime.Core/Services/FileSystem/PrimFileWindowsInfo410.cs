using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Erlang;

internal static class PrimFileWindowsInfo410
{
    internal static Term Run(Term term, Term follow, CancellationToken cancellation)
    {
        PrimFileWindowsOperations410.Require(cancellation);
        if (follow is not Integer number || number.Value < int.MinValue || number.Value > int.MaxValue)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (!PrimFileWindowsPaths410.Marshal(term, out string path, out string error))
            return PrimFileWindowsErrors410.Tuple(error);

        return Read(path, number.Value != 0, cancellation);
    }

    internal static ulong SizeFromHandle(SafeFileHandle handle, CancellationToken cancellation)
    {
        uint required = PrimFileWindowsNative410.FinalPath(
            handle,
            null,
            0,
            0
        );
        if (required == 0) return 0;
        var resolved = new char[checked((int)required)];
        uint actual = PrimFileWindowsNative410.FinalPath(
            handle,
            resolved,
            required,
            0
        );
        if (actual == 0 || actual >= required) return 0;
        if (PrimFileWindowsNative410.Information(handle, out var info)) return info.Size;
        string path = new string(resolved, 0, (int)actual);
        if (PrimFileWindowsPaths410.Root(path)
            && Read(path, false, cancellation) is TupleTerm { Items.Count: 14 } tuple
            && tuple.Items[1] is Integer size && size.Value >= 0 && size.Value <= ulong.MaxValue)
            return (ulong)size.Value;

        return 0;
    }

    private static bool NameSurrogate(string path)
    {
        using var handle = PrimFileWindowsNative410.Open(
            path,
            PrimFileWindowsNames410.GenericRead,
            PrimFileWindowsNames410.ShareAll,
            IntPtr.Zero,
            PrimFileWindowsNames410.OpenExisting,
            PrimFileWindowsNames410.OpenReparse | PrimFileWindowsNames410.BackupSemantics,
            IntPtr.Zero
        );
        if (handle.IsInvalid) return false;
        var data = new byte[24];
        bool success = PrimFileWindowsNative410.DeviceControl(
            handle,
            PrimFileWindowsNames410.GetReparsePoint,
            IntPtr.Zero,
            0,
            data,
            (uint)data.Length,
            out _,
            IntPtr.Zero
        );
        int error = Marshal.GetLastPInvokeError();

        return (success || error == PrimFileWindowsNames410.MoreData)
            && (BinaryPrimitives.ReadUInt32LittleEndian(data) & PrimFileWindowsNames410.NameSurrogate) != 0;
    }

    private static Term Read(string path, bool follow, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        uint attributes = PrimFileWindowsNative410.Attributes(path);
        int error = Marshal.GetLastPInvokeError();
        PrimFileInformation410 info = default;
        bool link = false;
        long access = 0, write = 0, creation = 0;
        bool root = PrimFileWindowsPaths410.Root(path);
        if (attributes == PrimFileWindowsNames410.InvalidAttributes)
        {
            if (!root || error != PrimFileWindowsNames410.BadNetPath) return PrimFileWindowsErrors410.Last(error);
            info.Attributes = PrimFileWindowsNames410.DirectoryAttribute;
        }
        else if (root)
            info.Attributes = attributes;
        else
        {
            if ((attributes & PrimFileWindowsNames410.ReparseAttribute) != 0) link = NameSurrogate(path);
            uint flags = PrimFileWindowsNames410.BackupSemantics;
            if (!follow && link) flags |= PrimFileWindowsNames410.OpenReparse;
            using var handle = PrimFileWindowsNative410.Open(
                path,
                PrimFileWindowsNames410.GenericRead,
                PrimFileWindowsNames410.ShareAll,
                IntPtr.Zero,
                PrimFileWindowsNames410.OpenExisting,
                flags,
                IntPtr.Zero
            );
            error = Marshal.GetLastPInvokeError();
            if (handle.IsInvalid) return PrimFileWindowsErrors410.Last(error);
            if (follow && link)
            {
                uint required = PrimFileWindowsNative410.FinalPath(
                    handle,
                    null,
                    0,
                    0
                );
                error = Marshal.GetLastPInvokeError();
                if (required == 0) return PrimFileWindowsErrors410.Last(error);
                var resolved = new char[checked((int)required)];
                uint actual = PrimFileWindowsNative410.FinalPath(
                    handle,
                    resolved,
                    required,
                    0
                );
                error = Marshal.GetLastPInvokeError();
                if (actual == 0 || actual >= required) return PrimFileWindowsErrors410.Last(error);
                // Match actual followed handle path, then query that target without
                // following another link. The source does the same recursion.
                handle.Dispose();

                return Read(new string(resolved, 0, (int)actual), false, cancellation);
            }
            _ = PrimFileWindowsNative410.Information(handle, out info);
            write = Epoch(info.Write);
            access = Epoch(info.Access);
            creation = Epoch(info.Creation);
            if (write == -PrimFileWindowsNames410.EpochDifference) write = 0;
            if (access == -PrimFileWindowsNames410.EpochDifference) access = write;
            if (creation == -PrimFileWindowsNames410.EpochDifference) creation = write;
        }
        string type;
        uint mode;
        if (link)
        {
            type = PrimFileWindowsNames410.Symlink;
            mode = 0x8000;
        }
        else if ((info.Attributes & PrimFileWindowsNames410.DirectoryAttribute) != 0)
        {
            type = PrimFileWindowsNames410.Directory;
            mode = 0x4000 | 0x40;
        }
        else
        {
            type = PrimFileWindowsNames410.Regular;
            mode = 0x8000;
            if (Executable(path)) mode |= 0x40;
        }
        bool writable = (info.Attributes & PrimFileWindowsNames410.ReadOnlyAttribute) == 0;
        mode |= writable ? 0x100u | 0x80u : 0x100u;
        mode |= (mode & 0x1C0) >> 3;
        mode |= (mode & 0x1C0) >> 6;

        return Term.Tuple(
            Term.A(PrimFileWindowsNames410.FileInfo),
            new Integer(info.Size),
            Term.A(type),
            Term.A(writable ? PrimFileWindowsNames410.ReadWrite : PrimFileWindowsNames410.ReadOnly),
            Term.I(Clamp(access)),
            Term.I(Clamp(write)),
            Term.I(Clamp(creation)),
            Term.I(mode),
            Term.I(Math.Max(1u, info.Links)),
            Term.I(PrimFileWindowsPaths410.Drive(path)),
            Term.I(0),
            Term.I(0),
            Term.I(0),
            Term.I(0)
        );
    }

    private static long Epoch(PrimFileTime410 time)
    {
        ulong bits = time.Bits == ulong.MaxValue ? 0 : time.Bits;

        return (long)(bits / PrimFileWindowsNames410.TicksPerSecond) - PrimFileWindowsNames410.EpochDifference;
    }

    private static long Clamp(long value)
        => Math.Clamp(value, PrimFileWindowsNames410.MinimumTime, PrimFileWindowsNames410.MaximumTime);

    private static bool Executable(string path)
        => path.EndsWith(PrimFileWindowsNames410.Executable, StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(PrimFileWindowsNames410.Command, StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(PrimFileWindowsNames410.Batch, StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(PrimFileWindowsNames410.Com, StringComparison.OrdinalIgnoreCase);
}
