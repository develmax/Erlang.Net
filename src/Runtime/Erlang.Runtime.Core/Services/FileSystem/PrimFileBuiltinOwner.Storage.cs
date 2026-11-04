// Native file operations needed by pinned OTP DETS; derived from prim_file_nif.c,
// Apache-2.0, Ericsson AB. Erlang library wrappers remain unchanged.
using System.Buffers.Binary;

namespace Erlang;

internal sealed partial class PrimFileBuiltinOwner
{
    private readonly List<WeakReference<PrimFileDataResource>> fileResources = new();

    private IEnumerable<KeyValuePair<(string Name, int Arity), ErlangFunction>> StorageBindings()
    {
        foreach (var signature in PrimFileBuiltinNames.StorageSignatures)
        {
            string operation = signature.Name;
            yield return new(signature, (context, arguments) => Invoke(context, () => Storage(context, operation, arguments)));
        }
    }

    private Term Storage(ProcessContext context, string operation, IReadOnlyList<Term> arguments)
    {
        try
        {
            if (operation == PrimFileBuiltinNames.Delete)
            {
                return PrimFileDeleteOperations.Delete(arguments[0], context.Cancellation);
            }
            if (operation == PrimFileBuiltinNames.Rename)
            {
                if (!PrimFileWindowsPaths410.Marshal(arguments[0], out string path, out string error))
                {
                    return PrimFileWindowsErrors410.Tuple(error);
                }
                if (!PrimFileWindowsPaths410.Marshal(arguments[1], out string target, out error))
                {
                    return PrimFileWindowsErrors410.Tuple(error);
                }
                File.Move(path, target, overwrite: true);

                return Term.A(CommonAtomNames.Ok);
            }
            if (operation == PrimFileBuiltinNames.Open)
            {
                if (!PrimFileWindowsPaths410.Marshal(arguments[0], out string path, out string error))
                {
                    return PrimFileWindowsErrors410.Tuple(error);
                }
                Term[] modes = Cons.Items(arguments[1]).ToArray();
                if (Directory.Exists(path))
                {
                    return PrimFileWindowsErrors410.Tuple(PrimFileWindowsNames410.IsDirectory);
                }

                bool Has(string name) => modes.Any(term => term is Atom atom && atom.Name == name);
                bool read = Has(PrimFileBuiltinNames.ReadMode) || !Has(PrimFileBuiltinNames.WriteMode) && !Has(PrimFileBuiltinNames.AppendMode);
                bool write = Has(PrimFileBuiltinNames.WriteMode) || Has(PrimFileBuiltinNames.AppendMode);
                FileAccess access = read && write ? FileAccess.ReadWrite : write ? FileAccess.Write : FileAccess.Read;
                FileMode mode = Has(PrimFileBuiltinNames.ExclusiveMode) ? FileMode.CreateNew : read && !write ? FileMode.Open
                    : Has(PrimFileBuiltinNames.AppendMode) || read ? FileMode.OpenOrCreate : FileMode.Create;
                var stream = new FileStream(
                    path,
                    mode,
                    access,
                    FileShare.ReadWrite | FileShare.Delete,
                    bufferSize: 1
                );
                var resource = new PrimFileDataResource(stream, path, Has(PrimFileBuiltinNames.AppendMode));
                runtime.RegisterRawFile(context, resource);
                lock (fileResources)
                {
                    fileResources.RemoveAll(weak => !weak.TryGetTarget(out _));
                    fileResources.Add(new(resource));
                }

                return PrimFileWindowsErrors410.Ok(runtime.ReferenceResources.CreateFile(resource));
            }
            if (arguments[0] is not ReferenceTerm reference)
            {
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            using var lease = runtime.ReferenceResources.AcquireFile(reference);
            if (lease is null)
            {
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            var file = lease.Value;
            lock (file.Gate)
            {
                if (file.Closed || file.IsClosing)
                {
                    return PrimFileWindowsErrors410.Tuple(PrimFileWindowsErrors410.BadDescriptor);
                }
                var stream = file.Stream;
                switch (operation)
                {
                    case PrimFileBuiltinNames.Close:
                    case PrimFileBuiltinNames.DelayedClose:
                        file.Dispose();

                        return Term.A(CommonAtomNames.Ok);
                    case PrimFileBuiltinNames.Seek:
                        long offset = Long(arguments[2]);
                        SeekOrigin origin = arguments[1] switch
                        {
                            Atom { Name: PrimFileBuiltinNames.Beginning } => SeekOrigin.Begin,
                            Atom { Name: PrimFileBuiltinNames.Current } => SeekOrigin.Current,
                            Atom { Name: PrimFileBuiltinNames.EndOfFile } => SeekOrigin.End,
                            _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
                        };

                        return PrimFileWindowsErrors410.Ok(Term.I(stream.Seek(offset, origin)));
                    case PrimFileBuiltinNames.Sync:
                        _ = Long(arguments[1]);
                        stream.Flush(flushToDisk: true);

                        return Term.A(CommonAtomNames.Ok);
                    case PrimFileBuiltinNames.Truncate:
                        if (!stream.CanWrite)
                        {
                            return PrimFileWindowsErrors410.Tuple(PrimFileWindowsNames410.Denied);
                        }
                        stream.SetLength(stream.Position);

                        return Term.A(CommonAtomNames.Ok);
                    case PrimFileBuiltinNames.Read:
                        return Read(stream, Size(arguments[1]));
                    case PrimFileBuiltinNames.Pread:
                        {
                            long position = stream.Position;
                            try
                            {
                                stream.Position = Long(arguments[1]);

                                return Read(stream, Size(arguments[2]));
                            }
                            finally
                            {
                                stream.Position = position;
                            }
                        }
                    case PrimFileBuiltinNames.Write:
                    case PrimFileBuiltinNames.Pwrite:
                        {
                            if (!stream.CanWrite)
                            {
                                return PrimFileWindowsErrors410.Tuple(PrimFileWindowsErrors410.BadDescriptor);
                            }
                            long position = stream.Position;
                            bool positioned = operation == PrimFileBuiltinNames.Pwrite;
                            try
                            {
                                if (positioned)
                                {
                                    stream.Position = Long(arguments[1]);
                                }
                                else if (file.Append)
                                {
                                    stream.Position = stream.Length;
                                }
                                foreach (BitString bytes in ErlangIoData.Segments(arguments[positioned ? 2 : 1], false, false))
                                {
                                    stream.Write(bytes.ByteSpan);
                                }

                                return Term.A(CommonAtomNames.Ok);
                            }
                            finally
                            {
                                if (positioned)
                                {
                                    stream.Position = position;
                                }
                            }
                        }
                    case PrimFileBuiltinNames.Indirect:
                        {
                            long pointer = Long(arguments[1]);
                            long max = Long(arguments[2]);
                            if (pointer < 0 || max >= PrimFileStorageLayout.MaximumIndirectReadBytes)
                            {
                                return PrimFileWindowsErrors410.Tuple(PrimFileWindowsNames410.Invalid);
                            }
                            long position = stream.Position;
                            try
                            {
                                stream.Position = pointer;
                                var header = new byte[PrimFileStorageLayout.IndirectHeaderBytes];
                                if (stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) != header.Length)
                                {
                                    return Term.A(PrimFileBuiltinNames.EndOfFile);
                                }
                                uint size = BinaryPrimitives.ReadUInt32BigEndian(header);
                                uint address = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(PrimFileStorageLayout.IndirectPointerOffset));
                                if (size > max)
                                {
                                    return Term.A(PrimFileBuiltinNames.EndOfFile);
                                }
                                stream.Position = address;
                                Term data = size == 0 ? Term.A(PrimFileBuiltinNames.EndOfFile) : Read(stream, checked((int)size));
                                Term payload = data is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0] is Atom { Name: CommonAtomNames.Ok }
                                    ? tuple.Items[1] : data;

                                return PrimFileWindowsErrors410.Ok(Term.Tuple(Term.I(size), Term.I(address), payload));
                            }
                            finally
                            {
                                stream.Position = position;
                            }
                        }
                    case PrimFileBuiltinNames.HandleInfo:
                        return PrimFileWindowsInfo410.ReadHandle(stream.SafeFileHandle, file.Path, context.Cancellation);
                    case PrimFileBuiltinNames.Handle:
                        return new BitString(BitConverter.GetBytes(stream.SafeFileHandle.DangerousGetHandle().ToInt64()));
                    default: throw new ErlangException(ErlangErrorReasons.BadArgument);
                }
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or OverflowException)
        {
            return PrimFileWindowsErrors410.Tuple(error is UnauthorizedAccessException ? PrimFileWindowsNames410.Denied
                : error is ArgumentException or OverflowException ? PrimFileWindowsNames410.Invalid
                : error is NotSupportedException ? PrimFileWindowsErrors410.BadDescriptor
                : PrimFileWindowsErrors410.Map(error.HResult & PrimFileBuiltinNames.Win32ErrorCodeMask));
        }
    }

    private static long Long(Term term) => term is Integer value && value.Value >= long.MinValue && value.Value <= long.MaxValue
        ? (long)value.Value : throw new ErlangException(ErlangErrorReasons.BadArgument);

    private static int Size(Term term)
    {
        long value = Long(term);
        if (value < 0 || value > int.MaxValue)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }

        return (int)value;
    }

    private static Term Read(FileStream stream, int count)
    {
        if (!stream.CanRead)
        {
            return PrimFileWindowsErrors410.Tuple(PrimFileWindowsErrors410.BadDescriptor);
        }
        var bytes = new byte[count];
        int read = stream.ReadAtLeast(bytes, count, throwOnEndOfStream: false);
        if (read == 0 && count != 0)
        {
            return Term.A(PrimFileBuiltinNames.EndOfFile);
        }

        return PrimFileWindowsErrors410.Ok(new BitString(bytes.AsSpan(0, read)));
    }
}
