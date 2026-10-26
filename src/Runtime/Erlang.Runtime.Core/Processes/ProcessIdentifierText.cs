using System.Globalization;

namespace Erlang;

internal static class ProcessIdentifierText
{
    private static ulong Channel(ProcessRuntime runtime, string node) => node == runtime.Node ? ProcessIdentifierTextParameters.LocalChannel : (ulong)runtime.AtomNames.IndexOf(node);

    public static Term PidList(ProcessRuntime runtime, Pid pid) => Term.String(string.Create(
        CultureInfo.InvariantCulture,
        $"{ProcessIdentifierTextSyntax.PidOpening}{Channel(runtime, pid.Node)}{ProcessIdentifierTextSyntax.Separator}{pid.Id & ProcessIdentifierTextParameters.StoredPidNumberMask}{ProcessIdentifierTextSyntax.Separator}{pid.Id >> ProcessIdentifierTextParameters.StoredPidSerialShift}{ProcessIdentifierTextSyntax.Closing}"
    ));

    public static Term PortList(ProcessRuntime runtime, PortTerm port) => Term.String(string.Create(
        CultureInfo.InvariantCulture,
        $"{ProcessIdentifierTextSyntax.PortOpening}{Channel(runtime, port.Node)}{ProcessIdentifierTextSyntax.Separator}{port.Id}{ProcessIdentifierTextSyntax.Closing}"
    ));

    public static Pid Pid(ProcessRuntime runtime, Term value)
    {
        var parsed = Parse(value, true);
        if (parsed.Channel != ProcessIdentifierTextParameters.LocalChannel || parsed.Number > ProcessIdentifierTextParameters.MaximumLocalPidNumber || parsed.Serial > ProcessIdentifierTextParameters.MaximumLocalPidSerial)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return new(runtime.LocalIdentity, parsed.Number | (parsed.Serial << ProcessIdentifierTextParameters.StoredPidSerialShift));
    }

    public static PortTerm Port(ProcessRuntime runtime, Term value)
    {
        var parsed = Parse(value, false);
        if (parsed.Channel != ProcessIdentifierTextParameters.LocalChannel || parsed.Number > ProcessIdentifierTextParameters.MaximumLocalPortNumber)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return new(runtime.LocalIdentity, parsed.Number);
    }

    private static ParsedProcessIdentifierText Parse(Term value, bool pid)
    {
        Span<byte> buffer = stackalloc byte[ProcessIdentifierTextParameters.PidMaximumInputBytes];
        int limit = pid ? ProcessIdentifierTextParameters.PidMaximumInputBytes : ProcessIdentifierTextParameters.PortMaximumInputBytes;
        int count = 0;
        Term tail = value;
        while (tail is Cons cell)
        {
            if (count == limit || cell.Head is not Integer number || number.Value < byte.MinValue || number.Value > byte.MaxValue)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            buffer[count++] = (byte)number.Value;
            tail = cell.Tail;
        }
        if (tail is not Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        ReadOnlySpan<byte> text = buffer[..count];
        int zero = text.IndexOf(ProcessIdentifierTextSyntax.NullTerminator);
        if (zero >= 0)
            text = text[..zero];
        ReadOnlySpan<byte> prefix = pid ? ProcessIdentifierTextSyntax.PidPrefix : ProcessIdentifierTextSyntax.PortPrefix;
        if (!text.StartsWith(prefix))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        int position = prefix.Length;
        ulong channel = ReadUnsigned(text, ref position, ProcessIdentifierTextSyntax.Separator);
        ulong id = ReadUnsigned(text, ref position, pid ? ProcessIdentifierTextSyntax.Separator : ProcessIdentifierTextSyntax.Closing);
        ulong serial = pid ? ReadUnsigned(text, ref position, ProcessIdentifierTextSyntax.Closing) : 0;
        if (position != text.Length)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return new(pid ? channel : (uint)channel, id, serial);
    }

    private static ulong ReadUnsigned(ReadOnlySpan<byte> text, ref int position, char delimiter)
    {
        int first = position;
        ulong value = 0;
        while (position < text.Length && text[position] is >= ProcessIdentifierTextSyntax.LowestDigit and <= ProcessIdentifierTextSyntax.HighestDigit)
        {
            value = unchecked(value * ProcessIdentifierTextParameters.DecimalRadix + (ulong)(text[position] - ProcessIdentifierTextSyntax.LowestDigit));
            position++;
        }
        if (position == first || position == text.Length || text[position++] != delimiter)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return value;
    }
}
