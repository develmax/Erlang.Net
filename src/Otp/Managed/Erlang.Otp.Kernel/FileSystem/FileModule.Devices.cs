// Ported from OTP-29.1.1 file.erl (Apache-2.0), Ericsson AB 1996-2026.
using System.Buffers.Binary;
using System.Numerics;

namespace Erlang;

public static partial class FileModule
{
    private static async ValueTask<Term> Device(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        Term handle = arguments[0];
        bool descriptor = Descriptor(handle, out Term module);
        bool pid = handle is Pid;
        bool io = pid || handle is Atom;
        bool valid = function switch
        {
            FileFunctionNames.Read => (descriptor || io) && Nonnegative(arguments[1]),
            FileFunctionNames.ReadLine or FileFunctionNames.Write => descriptor || io,
            FileFunctionNames.Pread => (descriptor || pid) && (arguments.Count == FileApiArity.Binary ? IsList(arguments[1]) : Nonnegative(arguments[2])),
            FileFunctionNames.Pwrite => (descriptor || pid) && (arguments.Count != FileApiArity.Binary || IsList(arguments[1])),
            _ => descriptor || pid
        };
        if (!valid)
        {
            if (function == FileFunctionNames.Allocate)
            {
                throw FileSource.Clause(
                    context,
                    function,
                    arguments,
                    FileSourceLocations.AllocateClause
                );
            }

            return BadArgument;
        }
        if (descriptor)
        {
            return await CallModule(
                context,
                module,
                function,
                arguments.ToArray()
            );
        }
        if (function is FileFunctionNames.Read or FileFunctionNames.ReadLine or FileFunctionNames.Write)
        {
            Term request;
            if (function == FileFunctionNames.Write)
            {
                Term data = await MakeBinary(context, arguments[1]);
                if (IsError(data))
                {
                    return data;
                }
                request = Term.Tuple(A(FileProtocol.PutChars), A(FileProtocol.Latin1), data);
            }
            else
            {
                request = function == FileFunctionNames.Read
                    ? Term.Tuple(
                        A(FileProtocol.GetChars),
                        A(FileProtocol.Latin1),
                        A(string.Empty),
                        arguments[1]
                    )
                    : Term.Tuple(A(FileProtocol.GetLine), A(FileProtocol.Latin1), A(string.Empty));
            }
            Term response = await Call(
                context,
                FileProtocol.Io,
                FileDependencyNames.IoRequestFunction,
                handle,
                request
            );

            return function != FileFunctionNames.Write && (IsList(response) || response is BitString { IsBinary: true }) ? Success(response) : response;
        }
        if (function is FileFunctionNames.Pread or FileFunctionNames.Pwrite && arguments.Count == FileApiArity.Binary)
        {
            return await PositionedBatch(
                context,
                function,
                handle,
                arguments[1]
            );
        }
        if (function == FileFunctionNames.IpreadS32buP32bu)
        {
            return await IndirectRead(
                context,
                handle,
                arguments[1],
                arguments[2]
            );
        }
        Term command = arguments.Count == FileApiArity.Unary ? A(function) : Term.Tuple(new[] { A(function) }.Concat(arguments.Skip(1)).ToArray());
        Term result = await Request(context, handle, command);

        return function == FileFunctionNames.Close && result.Equals(Error(A(FileProtocol.Terminated))) ? Ok : result;
    }

    private static async ValueTask<Term> PositionedBatch(
        ProcessContext context,
        string function,
        Term handle,
        Term requests
    )
    {
        var values = new List<Term>();
        BigInteger completed = BigInteger.Zero;
        while (requests is Cons cell)
        {
            if (cell.Head is not TupleTerm { Items.Count: FileTupleLayout.BatchEntryArity } pair || function == FileFunctionNames.Pread && !Nonnegative(pair.Items[1]))
            {
                return BadArgument;
            }
            Term result = await Device(context, function, [handle, pair.Items[0], pair.Items[1]]);
            if (function == FileFunctionNames.Pread)
            {
                if (IsSuccess(result))
                {
                    values.Add(((TupleTerm)result).Items[1]);
                }
                else if (result.Equals(A(FileProtocol.Eof)))
                {
                    values.Add(result);
                }
                else
                {
                    return result;
                }
            }
            else if (IsError(result))
            {
                return Error(Term.Tuple(new Integer(completed), ((TupleTerm)result).Items[1]));
            }
            else if (!result.Equals(Ok))
            {
                throw new ErlangException(Term.Tuple(A(ErlangErrorReasons.CaseClause), result));
            }
            completed++;
            requests = cell.Tail;
        }

        return requests is not Nil ? BadArgument : function == FileFunctionNames.Pread ? Success(Cons.From(values)) : Ok;
    }

    private static async ValueTask<Term> IndirectRead(
        ProcessContext context,
        Term handle,
        Term position,
        Term maximum
    )
    {
        if (maximum is Atom)
        {
            maximum = Term.I(FileIndirectReadLayout.MaximumReadSize);
        }
        if (!Nonnegative(maximum))
        {
            return BadArgument;
        }
        if (((Integer)maximum).Value >= FileIndirectReadLayout.SizeExclusiveLimit)
        {
            return Error(A(FileProtocol.Invalid));
        }
        Term headerResult = await Device(context, FileFunctionNames.Pread, [handle, position, Term.I(FileIndirectReadLayout.HeaderSizeBytes)]);
        if (!IsSuccess(headerResult))
        {
            return headerResult;
        }
        Term header = ((TupleTerm)headerResult).Items[1];
        if (IsList(header))
        {
            header = await Call(
                context,
                FileProtocol.Erlang,
                FileProtocol.ListToBinary,
                header
            );
        }
        if (header is not BitString { IsBinary: true } binary)
        {
            throw FileSource.Clause(
                context,
                FileFunctionNames.IpreadS32buP32bu2,
                [handle, header, maximum],
                FileSourceLocations.IndirectHeaderClause
            );
        }
        byte[] bytes = binary.ToArray();
        if (bytes.Length != FileIndirectReadLayout.HeaderSizeBytes)
        {
            return A(FileProtocol.Eof);
        }
        uint size = BinaryPrimitives.ReadUInt32BigEndian(bytes);
        uint offset = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(FileIndirectReadLayout.WordSizeBytes));
        if (size == 0)
        {
            return Success(Term.Tuple(Term.I(size), Term.I(offset), A(FileProtocol.Eof)));
        }
        if (size > ((Integer)maximum).Value)
        {
            return A(FileProtocol.Eof);
        }
        Term data = await Device(context, FileFunctionNames.Pread, [handle, Term.I(offset), Term.I(size)]);
        if (IsSuccess(data))
        {
            data = ((TupleTerm)data).Items[1];
        }
        else if (!data.Equals(A(FileProtocol.Eof)))
        {
            return data;
        }

        return Success(Term.Tuple(Term.I(size), Term.I(offset), data));
    }

    private static async ValueTask<Term> Copy(
        ProcessContext context,
        Term source,
        Term destination,
        Term length
    )
    {
        if (!ValidLength(length))
        {
            return BadArgument;
        }
        bool sourceHandle = IsHandle(source);
        bool destinationHandle = IsHandle(destination);
        if (sourceHandle && destinationHandle)
        {
            if (Descriptor(source, out Term sourceModule) && Descriptor(destination, out Term destinationModule) && sourceModule.Equals(destinationModule))
            {
                return await CallModule(
                    context,
                    sourceModule,
                    FileFunctionNames.Copy,
                    source,
                    destination,
                    length
                );
            }

            return await CopyOpened(
                context,
                source,
                destination,
                length
            );
        }
        if (!sourceHandle && !NameOptions(source, out _, out _))
        {
            source = Term.Tuple(source, Nil.Value);
        }
        if (!destinationHandle && !NameOptions(destination, out _, out _))
        {
            destination = Term.Tuple(destination, Nil.Value);
        }
        if (!sourceHandle && !destinationHandle)
        {
            _ = NameOptions(source, out Term sourceName, out Term sourceOptions);
            _ = NameOptions(destination, out Term destinationName, out Term destinationOptions);

            return await CheckedServer(
                context,
                FileFunctionNames.Copy,
                await FileName(context, sourceName),
                sourceOptions,
                await FileName(context, destinationName),
                destinationOptions,
                length
            );
        }
        Term operand = sourceHandle ? destination : source;
        _ = NameOptions(operand, out Term name, out Term options);
        name = await FileName(context, name);
        if (IsError(name))
        {
            return name;
        }
        Term opened = await Open(context, name, new Cons(A(sourceHandle ? FileProtocol.Write : FileProtocol.Read), options));
        if (!IsSuccess(opened))
        {
            return opened;
        }
        Term handle = ((TupleTerm)opened).Items[1];
        Term copied = await CopyOpened(
            context,
            sourceHandle ? source : handle,
            sourceHandle ? handle : destination,
            length
        );
        Term closed = await Device(context, FileFunctionNames.Close, [handle]);

        return sourceHandle && IsSuccess(copied) && !closed.Equals(Ok) ? closed : copied;
    }

    private static bool NameOptions(Term value, out Term name, out Term options)
    {
        if (value is TupleTerm { Items.Count: FileTupleLayout.NameOptionsArity } pair && IsList(pair.Items[1]))
        {
            name = pair.Items[0];
            options = pair.Items[1];

            return true;
        }
        name = value;
        options = Nil.Value;

        return false;
    }

    private static async ValueTask<Term> CopyOpened(
        ProcessContext context,
        Term source,
        Term destination,
        Term length
    )
    {
        BigInteger? remaining = length is Integer integer ? integer.Value : null;
        BigInteger copied = BigInteger.Zero;
        while (remaining is null || remaining > 0)
        {
            BigInteger count = remaining is null ? FileTransferLimits.CopyChunkSize : BigInteger.Min(remaining.Value, FileTransferLimits.CopyChunkSize);
            Term read = await Device(context, FileFunctionNames.Read, [source, new Integer(count)]);
            if (read.Equals(A(FileProtocol.Eof)))
            {
                return Success(new Integer(copied));
            }
            if (!IsSuccess(read))
            {
                return read;
            }
            Term data = ((TupleTerm)read).Items[1];
            BigInteger size = data is BitString binary ? binary.BitLength / FileTransferLimits.BitsPerByte : Cons.Items(data).Count();
            Term written = await Device(context, FileFunctionNames.Write, [destination, data]);
            if (!written.Equals(Ok))
            {
                return written;
            }
            copied += size;
            if (size < count)
            {
                return Success(new Integer(copied));
            }
            if (remaining is not null)
            {
                remaining -= size;
            }
        }

        return Success(new Integer(copied));
    }
}
