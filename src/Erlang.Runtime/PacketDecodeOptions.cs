namespace Erlang;

internal readonly record struct PacketDecodeOptions(uint PacketSize, uint LineLength, byte Delimiter)
{
    public static PacketDecodeOptions Parse(Term options, PacketDecodeType type)
    {
        uint size = 0, length = 0;
        byte delimiter = PacketDecodeParameters.DefaultLineDelimiter;
        Term tail = options;
        while (tail is Cons cell)
        {
            if (cell.Head is not TupleTerm { Items.Count: 2 } tuple || tuple.Items[0] is not Atom key || tuple.Items[1] is not Integer value || value.Value < uint.MinValue || value.Value > uint.MaxValue)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            uint parsed = (uint)value.Value;
            switch (key.Name)
            {
                case PacketDecodeOptionNames.PacketSize:
                    size = parsed;
                    break;
                case PacketDecodeOptionNames.LineLength:
                    length = parsed;
                    break;
                case PacketDecodeOptionNames.LineDelimiter when type == PacketDecodeType.Line && parsed <= byte.MaxValue:
                    delimiter = (byte)parsed;
                    break;
                default:
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            tail = cell.Tail;
        }
        if (tail is not Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return new(size, length, delimiter);
    }
}
