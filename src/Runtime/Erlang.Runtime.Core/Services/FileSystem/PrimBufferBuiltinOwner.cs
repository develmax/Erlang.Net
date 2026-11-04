// Native buffer primitives for unchanged OTP-29.1.1 prim_buffer.erl.
// Derived from OTP prim_buffer_nif.c, Apache-2.0, Ericsson AB 2017-2026.
namespace Erlang;

internal sealed class PrimBufferBuiltinOwner(ProcessRuntime runtime) : IAsyncDisposable
{
    private int disposed;

    internal IReadOnlyList<KeyValuePair<(string Name, int Arity), ErlangFunction>> Bindings()
        => PrimBufferBuiltinNames.Signatures.Select(signature =>
        {
            string function = signature.Name;

            return new KeyValuePair<(string, int), ErlangFunction>(signature,
                (context, arguments) => ValueTask.FromResult(Invoke(context, function, arguments)));
        }).ToArray();

    private Term Invoke(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        if (!ReferenceEquals(context.Runtime, runtime))
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        context.Cancellation.ThrowIfCancellationRequested();
        runtime.ThrowIfHalted();
        if (function == PrimBufferBuiltinNames.New)
        {
            return runtime.ReferenceResources.CreateBuffer(new PrimBufferDataResource());
        }
        if (arguments[0] is not ReferenceTerm reference)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        using var lease = runtime.ReferenceResources.AcquireBuffer(reference);
        if (lease is null)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        var buffer = lease.Value;
        if (function == PrimBufferBuiltinNames.Lock)
        {
            return Term.A(Interlocked.CompareExchange(ref buffer.Locked, 1, 0) == 0 ? PrimBufferBuiltinNames.LockAcquired : PrimBufferBuiltinNames.LockBusy);
        }
        if (function == PrimBufferBuiltinNames.Unlock)
        {
            if (Interlocked.CompareExchange(ref buffer.Locked, 0, 1) == 0)
            {
                throw new ErlangException(PrimBufferBuiltinNames.LockOrderViolation);
            }

            return Term.A(CommonAtomNames.Ok);
        }
        lock (buffer.Gate)
        {
            switch (function)
            {
                case PrimBufferBuiltinNames.Size: return Term.I(buffer.Size);
                case PrimBufferBuiltinNames.Write:
                    Term remainingInput = arguments[1];
                    do
                    {
                        var values = new List<BitString>();
                        for (int index = 0; index < PrimBufferBuiltinNames.InspectionVectorElements && remainingInput is Cons item; index++)
                        {
                            if (item.Head is not BitString { IsBinary: true } value)
                            {
                                throw new ErlangException(ErlangErrorReasons.BadArgument);
                            }
                            values.Add(value);
                            remainingInput = item.Tail;
                        }
                        if (remainingInput is not Cons and not Nil)
                        {
                            throw new ErlangException(ErlangErrorReasons.BadArgument);
                        }
                        buffer.Write(values);
                    }
                    while (remainingInput is not Nil);

                    return Term.A(CommonAtomNames.Ok);
                case PrimBufferBuiltinNames.Peek:
                    buffer.FlushAccumulator();
                    if (buffer.Segments.Count == 0)
                    {
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    }
                    BitString head = buffer.Segments.Peek();

                    return head.SliceBits(buffer.HeadOffset * BitStringLayout.BitsPerByte, head.BitLength - buffer.HeadOffset * BitStringLayout.BitsPerByte);
                case PrimBufferBuiltinNames.Read:
                case PrimBufferBuiltinNames.Skip:
                    if (arguments[1] is not Integer count || count.Value < 0 || count.Value > ulong.MaxValue)
                    {
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    }
                    buffer.FlushAccumulator();
                    if (count.Value > buffer.Size || count.Value > int.MaxValue)
                    {
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    }
                    int remaining = (int)count.Value;
                    byte[]? bytes = function == PrimBufferBuiltinNames.Read ? new byte[remaining] : null;
                    int destination = 0;
                    while (remaining != 0)
                    {
                        BitString segment = buffer.Segments.Peek();
                        int size = Math.Min(remaining, segment.BitLength / BitStringLayout.BitsPerByte - buffer.HeadOffset);
                        if (bytes is not null)
                        {
                            segment.ByteSpan.Slice(buffer.HeadOffset, size).CopyTo(bytes.AsSpan(destination));
                        }
                        remaining -= size;
                        destination += size;
                        buffer.HeadOffset += size;
                        buffer.Size -= size;
                        if (buffer.HeadOffset == segment.BitLength / BitStringLayout.BitsPerByte)
                        {
                            buffer.Segments.Dequeue();
                            buffer.HeadOffset = 0;
                        }
                    }

                    return bytes is null ? Term.A(CommonAtomNames.Ok) : new BitString(bytes);
                case PrimBufferBuiltinNames.Find:
                    if (arguments[1] is not Integer needle || needle.Value < 0 || needle.Value > byte.MaxValue)
                    {
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    }
                    buffer.FlushAccumulator();
                    long offset = 0;
                    int skip = buffer.HeadOffset;
                    foreach (BitString segment in buffer.Segments)
                    {
                        int index = segment.ByteSpan[skip..].IndexOf((byte)needle.Value);
                        if (index >= 0)
                        {
                            return Term.Tuple(Term.A(CommonAtomNames.Ok), Term.I(offset + index));
                        }
                        offset += segment.BitLength / BitStringLayout.BitsPerByte - skip;
                        skip = 0;
                    }

                    return Term.A(PrimBufferBuiltinNames.NotFound);
                default: throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        Interlocked.Exchange(ref disposed, 1);

        return ValueTask.CompletedTask;
    }
}
