// Native ram_file_drv protocol from OTP-29.1.1 ram_file_drv.c, Apache-2.0.
// Copyright Ericsson AB 1997-2026. Original Erlang ram_file remains unchanged.
using System.Buffers.Binary;
using static Erlang.RamFileDriverProtocol;

namespace Erlang;

internal sealed class RamFilePortProcess(bool binary) : PortChildProcess
{
    private readonly MemoryStream contents = new();
    private long position;
    private int mode;

    public override bool HasChild => false;
    public override int Id => 0;
    public override int ExitCode => 0;
    public override Stream Input => Stream.Null;
    public override Stream Output => Stream.Null;

    public override Task WaitForExitAsync(CancellationToken cancellation) => Task.CompletedTask;

    public override void Dispose() => contents.Dispose();

    internal int ResponseBytes { get; private set; }

    internal Term? Command(ReadOnlySpan<byte> command)
    {
        if (command.Length == 0)
        {
            return null;
        }
        byte operation = command[0];
        ReadOnlySpan<byte> data = command[1..];
        try
        {
            switch (operation)
            {
                case Open:
                    if (data.Length < WordBytes)
                    {
                        return Error(Invalid);
                    }
                    mode = BinaryPrimitives.ReadInt32BigEndian(data);
                    contents.SetLength(0);
                    contents.Write(data[WordBytes..]);
                    position = 0;

                    return Number(0);
                case Sync:
                case DataSync:
                case Advise:
                case Allocate:
                    return mode == 0 ? Error(BadDescriptor) : Header(OkResponse);
                case Read:
                case Pread:
                    {
                        if ((mode & ReadMode) == 0)
                        {
                            return Error(BadDescriptor);
                        }
                        if (data.Length != (operation == Pread ? WordBytes * 2 : WordBytes))
                        {
                            return Error(Invalid);
                        }
                        long offset = operation == Pread ? BinaryPrimitives.ReadInt32BigEndian(data) : position;
                        int count = BinaryPrimitives.ReadInt32BigEndian(operation == Pread ? data[WordBytes..] : data);
                        if (offset < 0 || count < 0)
                        {
                            return Error(Invalid);
                        }
                        int size = offset >= contents.Length ? 0 : (int)Math.Min(count, contents.Length - offset);
                        byte[] payload = contents.GetBuffer().AsSpan(size == 0 ? 0 : checked((int)offset), size).ToArray();
                        if (operation == Read)
                        {
                            position += size;
                        }

                        return Data(payload);
                    }
                case Write:
                case Pwrite:
                    {
                        if ((mode & WriteMode) == 0)
                        {
                            return Error(BadDescriptor);
                        }
                        if (operation == Pwrite && data.Length < WordBytes)
                        {
                            return Error(Invalid);
                        }
                        long offset = operation == Pwrite ? BinaryPrimitives.ReadInt32BigEndian(data) : position;
                        ReadOnlySpan<byte> payload = operation == Pwrite ? data[WordBytes..] : data;
                        if (offset < 0)
                        {
                            return Error(Invalid);
                        }
                        if (offset + payload.Length > int.MaxValue)
                        {
                            return Error(NoMemory);
                        }
                        contents.Position = offset;
                        contents.Write(payload);
                        if (operation == Write)
                        {
                            position = offset + payload.Length;
                        }

                        return Number(payload.Length);
                    }
                case Seek:
                    {
                        if (mode == 0)
                        {
                            return Error(BadDescriptor);
                        }
                        if (data.Length != WordBytes * 2)
                        {
                            return Error(Invalid);
                        }
                        int offset = BinaryPrimitives.ReadInt32BigEndian(data);
                        int origin = BinaryPrimitives.ReadInt32BigEndian(data[WordBytes..]);
                        long next = origin switch
                        {
                            Beginning => offset,
                            Current => position + offset,
                            End => contents.Length + offset,
                            _ => -1
                        };
                        if (next < 0)
                        {
                            return Error(Invalid);
                        }
                        position = next;

                        return Number(position);
                    }
                case Truncate:
                    if ((mode & WriteMode) == 0)
                    {
                        return Error(Denied);
                    }
                    if (position > int.MaxValue)
                    {
                        return Error(NoMemory);
                    }
                    contents.SetLength(position);

                    return Header(OkResponse);
                case Get: return Data(contents.ToArray());
                case Size: return Number(contents.Length);
                default: return null;
            }
        }
        catch (OutOfMemoryException)
        {
            return Error(NoMemory);
        }
        catch (ArgumentException)
        {
            return Error(Invalid);
        }
    }

    private Term Header(byte response)
    {
        ResponseBytes = 1;

        return Term.List(Term.I(response));
    }

    private Term Number(long number)
    {
        var header = new byte[ResponseHeaderBytes];
        header[0] = NumberResponse;
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(1), unchecked((uint)number));
        ResponseBytes = header.Length;

        return Cons.From(header.Select(value => Term.I(value)));
    }

    private Term Data(byte[] payload)
    {
        var header = new byte[ResponseHeaderBytes];
        header[0] = DataResponse;
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(1), checked((uint)payload.Length));
        ResponseBytes = header.Length + payload.Length;
        Term tail = binary ? BitString.FromOwnedBytes(payload) : Cons.From(payload.Select(value => Term.I(value)));

        return Cons.From(header.Select(value => Term.I(value)), tail);
    }

    private Term Error(string reason)
    {
        ResponseBytes = 1 + reason.Length;

        return new Cons(Term.I(ErrorResponse), Term.String(reason));
    }
}
