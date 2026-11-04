using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Erlang;

internal static class PrimFilePosixOperations
{
    internal static void Admit()
    {
        if (PrimFilePosixNative.Abi() != Marshal.SizeOf<PrimFilePosixInfo>())
        {
            throw new PlatformNotSupportedException(PrimFilePosixNames.AbiMismatch);
        }
    }

    private static Term Ok(Term value) => Term.Tuple(Term.A(CommonAtomNames.Ok), value);

    private static Term Error(int error) => Term.Tuple(Term.A(CommonAtomNames.Error), Term.A(PrimFileUnixErrno.Error(error, OperatingSystem.IsMacOS())));

    private static Term Invalid => Term.Tuple(Term.A(CommonAtomNames.Error), Term.A(PrimFileDeleteNames.Invalid));

    private static Term Status(int error) => error == 0 ? Term.A(CommonAtomNames.Ok) : Error(error);

    private static int Fd(PrimFileDataResource file) => checked((int)file.Handle.DangerousGetHandle());

    private static long Number(Term term) => term is Integer n && n.Value >= long.MinValue && n.Value <= long.MaxValue ? (long)n.Value : throw new ErlangException(ErlangErrorReasons.BadArgument);

    private static int Count(Term term)
    {
        long value = Number(term);
        if (value < 0 || value > int.MaxValue) throw new ErlangException(ErlangErrorReasons.BadArgument);

        return (int)value;
    }

    internal static Term GetCwd(CancellationToken cancellation)
    {
        int capacity = PrimFilePosixNames.InitialBufferBytes;
        while (true)
        {
            cancellation.ThrowIfCancellationRequested();
            byte[] bytes = new byte[capacity];
            int error = PrimFilePosixNative.Cwd(bytes, capacity, out int length);
            if (error == 0) return Ok(new BitString(bytes.AsSpan(0, length).ToArray()));
            if (error != PrimFilePosixNames.RangeError) return Error(error);
            capacity = checked(capacity * 2);
        }
    }

    internal static bool Open(
        Term term,
        Term modes,
        out PrimFileDataResource? resource,
        out Term result
    )
    {
        resource = null;
        result = Invalid;
        byte[]? path = PrimFileUnixPaths.Marshal(term);
        if (path is null) return false;
        Term[] requested = Cons.Items(modes).ToArray();

        bool Has(string name) => requested.Any(item => item is Atom atom && atom.Name == name);
        bool append = Has(PrimFileBuiltinNames.AppendMode);
        bool write = Has(PrimFileBuiltinNames.WriteMode) || append;
        bool read = Has(PrimFileBuiltinNames.ReadMode) || !write;
        int flags = (read ? PrimFilePosixNames.ReadFlag : 0) | (write ? PrimFilePosixNames.WriteFlag : 0)
            | (append ? PrimFilePosixNames.AppendFlag : 0) | (Has(PrimFileBuiltinNames.ExclusiveMode) ? PrimFilePosixNames.ExclusiveFlag : 0)
            | (Has(PrimFilePosixNames.SyncMode) ? PrimFilePosixNames.SyncFlag : 0);
        int error = PrimFilePosixNative.Open(path, flags, out int fd);
        if (error != 0)
        {
            result = Error(error);

            return false;
        }
        var handle = new SafeFileHandle((nint)fd, ownsHandle: true);
        try
        {
            var stream = new FileStream(
                handle,
                read && write ? FileAccess.ReadWrite : write ? FileAccess.Write : FileAccess.Read,
                bufferSize: 1,
                isAsync: false
            );
            resource = new PrimFileDataResource(stream, Encoding.UTF8.GetString(path.AsSpan(0, Array.IndexOf(path, (byte)0))), append);
            result = Term.A(CommonAtomNames.Ok);

            return true;
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    internal static Term PathOperation(string operation, Term term, Term? target)
    {
        byte[]? path = PrimFileUnixPaths.Marshal(term);
        byte[]? destination = target is null ? null : PrimFileUnixPaths.Marshal(target);
        if (path is null || target is not null && destination is null) return Invalid;
        int kind = operation == PrimFileBuiltinNames.Rename ? PrimFilePosixNames.RenameOperation
            : operation == PrimFileBuiltinNames.MakeDirectory ? PrimFilePosixNames.MkdirOperation : PrimFilePosixNames.RmdirOperation;

        return Status(PrimFilePosixNative.Path(kind, path, destination));
    }

    internal static Term ReadInfo(Term term, Term follow, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        byte[]? path = PrimFileUnixPaths.Marshal(term);
        if (path is null) return Invalid;
        long mode = Number(follow);
        if (mode < int.MinValue || mode > int.MaxValue) throw new ErlangException(ErlangErrorReasons.BadArgument);
        int error = PrimFilePosixNative.Stat(
            path,
            -1,
            (int)mode,
            out var info
        );

        return error == 0 ? Info(info) : Error(error);
    }

    private static Term Info(PrimFilePosixInfo info)
    {
        string kind = info.Type switch
        {
            PrimFilePosixNames.DirectoryType => PrimFilePosixNames.Directory,
            PrimFilePosixNames.RegularType => PrimFilePosixNames.Regular,
            PrimFilePosixNames.SymlinkType => PrimFilePosixNames.Symlink,
            PrimFilePosixNames.DeviceType => PrimFilePosixNames.Device,
            _ => PrimFilePosixNames.Other
        };
        string access = info.Access switch
        {
            1 => PrimFilePosixNames.ReadAccess,
            2 => PrimFilePosixNames.WriteAccess,
            3 => PrimFilePosixNames.ReadWriteAccess,
            _ => PrimFilePosixNames.NoAccess
        };

        return Term.Tuple(
            Term.A(PrimFilePosixNames.FileInfoRecord),
            Term.I(info.Size),
            Term.A(kind),
            Term.A(access),
            Term.I(info.ATime),
            Term.I(info.MTime),
            Term.I(info.CTime),
            Term.I(info.Mode),
            new Integer(unchecked((ulong)info.Links)),
            new Integer(unchecked((ulong)info.Dev)),
            new Integer(unchecked((ulong)info.RDev)),
            new Integer(unchecked((ulong)info.Inode)),
            Term.I(info.Uid),
            Term.I(info.Gid)
        );
    }

    internal static Term ReadFile(Term path, CancellationToken cancellation)
    {
        if (!Open(
            path,
            Term.List(Term.A(PrimFileBuiltinNames.ReadMode)),
            out var file,
            out Term error
        )) return error;
        using (file!)
        using (var output = new MemoryStream())
        {
            byte[] buffer = new byte[PrimFilePosixNames.ReadBufferBytes];
            while (true)
            {
                cancellation.ThrowIfCancellationRequested();
                int status = PrimFilePosixNative.Read(
                    Fd(file!),
                    buffer,
                    buffer.Length,
                    0,
                    0,
                    out int actual
                );
                if (status != 0) return Error(status);
                if (actual == 0) return Ok(new BitString(output.ToArray()));
                output.Write(buffer, 0, actual);
            }
        }
    }

    internal static Term ListDir(Term term, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        byte[]? path = PrimFileUnixPaths.Marshal(term);
        if (path is null) return Invalid;
        int error = PrimFilePosixNative.OpenDir(path, out nint handle);
        if (error != 0) return Error(error);
        try
        {
            var names = new List<Term>();
            byte[] buffer = new byte[PrimFilePosixNames.InitialBufferBytes];
            while (true)
            {
                cancellation.ThrowIfCancellationRequested();
                error = PrimFilePosixNative.ReadDir(
                    handle,
                    buffer,
                    buffer.Length,
                    out int length
                );
                if (error == PrimFilePosixNames.RangeError)
                {
                    buffer = new byte[checked(buffer.Length * 2)];
                    continue;
                }
                if (error != 0) return Error(error);
                if (length < 0) return Ok(Cons.From(names));
                if (length == 1 && buffer[0] == (byte)'.' || length == 2 && buffer[0] == (byte)'.' && buffer[1] == (byte)'.') continue;
                names.Add(new BitString(buffer.AsSpan(0, length).ToArray()));
            }
        }
        finally
        {
            PrimFilePosixNative.CloseDir(handle);
        }
    }

    private static Term Read(
        int fd,
        int count,
        bool positioned = false,
        long offset = 0
    )
    {
        if (count == 0) return Ok(new BitString(Array.Empty<byte>()));
        byte[] buffer = new byte[count];
        int error = PrimFilePosixNative.Read(
            fd,
            buffer,
            count,
            positioned ? 1 : 0,
            offset,
            out int actual
        );

        return error != 0 ? Error(error) : actual == 0 ? Term.A(PrimFileBuiltinNames.EndOfFile) : Ok(new BitString(buffer.AsSpan(0, actual).ToArray()));
    }

    internal static Term Storage(PrimFileDataResource file, string operation, IReadOnlyList<Term> arguments)
    {
        int fd = Fd(file);
        switch (operation)
        {
            case PrimFileBuiltinNames.Close:
            case PrimFileBuiltinNames.DelayedClose:
                file.Dispose();

                return Term.A(CommonAtomNames.Ok);
            case PrimFileBuiltinNames.Seek:
                int origin = arguments[1] switch
                {
                    Atom { Name: PrimFileBuiltinNames.Beginning } => 0,
                    Atom { Name: PrimFileBuiltinNames.Current } => 1,
                    Atom { Name: PrimFileBuiltinNames.EndOfFile } => 2,
                    _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
                };
                int error = PrimFilePosixNative.Seek(
                    fd,
                    Number(arguments[2]),
                    origin,
                    out long position
                );

                return error == 0 ? Ok(Term.I(position)) : Error(error);
            case PrimFileBuiltinNames.Sync:
                return Status(PrimFilePosixNative.Sync(fd, Number(arguments[1]) != 0 ? 1 : 0));
            case PrimFileBuiltinNames.Truncate:
                return RuntimeFileOperations.TruncatePosix(file.Handle);
            case PrimFileBuiltinNames.Read:
                return Read(fd, Count(arguments[1]));
            case PrimFileBuiltinNames.Pread:
                return Read(
                    fd,
                    Count(arguments[2]),
                    true,
                    Number(arguments[1])
                );
            case PrimFileBuiltinNames.Write:
            case PrimFileBuiltinNames.Pwrite:
                bool positioned = operation == PrimFileBuiltinNames.Pwrite;
                long offset = positioned ? Number(arguments[1]) : 0;
                foreach (BitString segment in ErlangIoData.Segments(arguments[positioned ? 2 : 1], false, false))
                {
                    byte[] bytes = segment.ToArray();
                    error = PrimFilePosixNative.Write(
                        fd,
                        bytes,
                        bytes.Length,
                        positioned ? 1 : 0,
                        offset
                    );
                    if (error != 0) return Error(error);
                    offset += bytes.Length;
                }

                return Term.A(CommonAtomNames.Ok);
            case PrimFileBuiltinNames.HandleInfo:
                error = PrimFilePosixNative.Stat(
                    null,
                    fd,
                    1,
                    out var info
                );

                return error == 0 ? Info(info) : Error(error);
            case PrimFileBuiltinNames.Handle:
                return new BitString(BitConverter.GetBytes(fd));
            case PrimFileBuiltinNames.Indirect:
                long pointer = Number(arguments[1]);
                long maximum = Number(arguments[2]);
                if (pointer < 0 || maximum < 0 || maximum >= PrimFileStorageLayout.MaximumIndirectReadBytes) return Invalid;
                byte[] header = new byte[PrimFileStorageLayout.IndirectHeaderBytes];
                error = PrimFilePosixNative.Read(
                    fd,
                    header,
                    header.Length,
                    1,
                    pointer,
                    out int actual
                );
                if (error != 0) return Error(error);
                if (actual != header.Length) return Term.A(PrimFileBuiltinNames.EndOfFile);
                uint size = BinaryPrimitives.ReadUInt32BigEndian(header);
                uint address = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(PrimFileStorageLayout.IndirectPointerOffset));
                if (size > maximum) return Term.A(PrimFileBuiltinNames.EndOfFile);
                Term data = size == 0 ? Term.A(PrimFileBuiltinNames.EndOfFile) : Read(
                    fd,
                    checked((int)size),
                    true,
                    address
                );
                if (data is TupleTerm { Items.Count: 2 } pair && pair.Items[0].Equals(Term.A(CommonAtomNames.Ok))) data = pair.Items[1];

                return Ok(Term.Tuple(Term.I(size), Term.I(address), data));
            default: throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
    }
}
