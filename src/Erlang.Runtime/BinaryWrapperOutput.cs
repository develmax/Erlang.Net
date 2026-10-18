namespace Erlang;

internal static class BinaryWrapperOutput
{
    internal static async ValueTask<BitString> Flatten(ProcessContext context, Term value)
    {
        if (value is Cons { Head: BitString { IsBinary: true } single, Tail: Nil })
            return single;
        using var output = new MemoryStream();
        var pending = new Stack<(Term Value, bool Tail)>();
        pending.Push((value, true));
        while (pending.TryPop(out var item))
        {
            await context.ReduceAsync();
            switch (item.Value)
            {
                case Nil:
                    break;
                case Cons cell:
                    pending.Push((cell.Tail, true));
                    pending.Push((cell.Head, false));
                    break;
                case Integer number when !item.Tail && number.Value >= 0 && number.Value <= byte.MaxValue:
                    if (output.Length == BinaryWrapperSource.MaximumBinaryBytes)
                        throw new ErlangException(ErlangErrorReasons.SystemLimit);
                    output.WriteByte((byte)number.Value);
                    break;
                case BitString { IsBinary: true } segment:
                    if (output.Length + segment.ByteLength > BinaryWrapperSource.MaximumBinaryBytes)
                        throw new ErlangException(ErlangErrorReasons.SystemLimit);
                    for (int offset = 0; offset < segment.ByteLength; offset += BinaryWrapperSource.ByteChunk)
                    {
                        await context.ReduceAsync();
                        int count = Math.Min(BinaryWrapperSource.ByteChunk, segment.ByteLength - offset);
                        output.Write(segment.ByteSpan.Slice(offset, count));
                    }
                    break;
                default:
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
        }

        return BitString.FromOwnedBytes(output.ToArray());
    }
}
