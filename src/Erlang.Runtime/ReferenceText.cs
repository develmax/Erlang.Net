using System.Globalization;
using System.Text;

namespace Erlang;

internal static class ReferenceText
{
    public static Term ToList(ProcessRuntime runtime, ReferenceTerm reference)
    {
        if (reference is IReferenceResourceSerialization resource)
            resource.SaveReferenceIdentity();
        ulong channel = reference.Node == runtime.Node ? ReferenceTextParameters.LocalChannel : (ulong)AtomNameRegistry.IndexOf(reference.Node);
        var text = new StringBuilder(ReferenceTextSyntax.Opening);
        text.Append(channel.ToString(CultureInfo.InvariantCulture));
        for (int index = reference.Words.Count - 1; index >= 0; index--)
        {
            text.Append(ReferenceTextSyntax.Separator);
            text.Append(reference.Words[index].ToString(CultureInfo.InvariantCulture));
        }
        text.Append(ReferenceTextSyntax.Closing);

        return Term.String(text.ToString());
    }

    public static ReferenceTerm FromList(ProcessRuntime runtime, Term value)
    {
        Span<byte> buffer = stackalloc byte[ReferenceTextParameters.MaximumInputBytes];
        int count = 0;
        Term tail = value;
        while (tail is Cons cell)
        {
            if (count == buffer.Length || cell.Head is not Integer number || number.Value < byte.MinValue || number.Value > byte.MaxValue)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            buffer[count++] = (byte)number.Value;
            tail = cell.Tail;
        }
        if (tail is not Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        ReadOnlySpan<byte> text = buffer[..count];
        int zero = text.IndexOf(ReferenceTextSyntax.NullTerminator);
        if (zero >= 0)
            text = text[..zero];
        if (!text.StartsWith(ReferenceTextSyntax.Prefix))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        int position = ReferenceTextSyntax.Prefix.Length;
        Span<uint> fields = stackalloc uint[ReferenceTextParameters.MaximumFields];
        int fieldCount = 0;
        bool closed = false;
        while (fieldCount < fields.Length)
        {
            int first = position;
            ulong number = 0;
            while (position < text.Length && text[position] is >= ReferenceTextSyntax.LowestDigit and <= ReferenceTextSyntax.HighestDigit)
            {
                number = unchecked(number * ReferenceTextParameters.DecimalRadix + (ulong)(text[position] - ReferenceTextSyntax.LowestDigit));
                position++;
            }
            if (position == first || number > uint.MaxValue || position == text.Length)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            fields[fieldCount++] = (uint)number;
            byte delimiter = text[position++];
            if (delimiter == ReferenceTextSyntax.Closing)
            {
                closed = true;
                break;
            }
            if (delimiter != ReferenceTextSyntax.Separator)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        if (!closed || position != text.Length || fieldCount < 2 || fields[0] != ReferenceTextParameters.LocalChannel)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        int wordCount = fieldCount - 1;
        if (wordCount != ReferenceIdentityLayout.LocalWords && wordCount != ReferenceNumberLayout.PidWords)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var words = new uint[wordCount];
        for (int index = 0; index < words.Length; index++)
            words[index] = fields[fieldCount - index - 1];
        if (words[0] > ReferenceIdentityLayout.LocalFirstWordMask || (words[1] & ReferenceNumberLayout.DomainMask) > runtime.ReferenceDomainCapacity)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (wordCount == ReferenceNumberLayout.PidWords && (words[1] & ProcessAliasLayout.PidMarker) != 0)
        {
            if ((words[3] & ReferenceNumberLayout.PidTagMask) != ProcessAliasLayout.PidTag)
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return new(runtime.Node, words);
        }

        return runtime.ReferenceResources.Rehydrate(new(runtime.Node, words.Take(ReferenceIdentityLayout.LocalWords)));
    }
}
