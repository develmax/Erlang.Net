using System.Buffers;
using System.Globalization;
using System.Text;

namespace Erlang;

internal sealed class TermDisplayWriter(ProcessRuntime runtime)
{
    private readonly ArrayBufferWriter<byte> buffer = new();
    private readonly record struct Work(Term? Value, string? Token = null, bool ConsCell = false);

    private bool limited;
    private long remaining;
    private bool stopped;
    private CancellationToken cancellation;

    internal byte[] WriteLimited430(Term term, int precision, CancellationToken token)
    {
        limited = true;
        remaining = precision;
        cancellation = token;

        return Write(term);
    }

    private bool Consume430()
    {
        if (!limited) return true;
        cancellation.ThrowIfCancellationRequested();
        if (remaining-- > 0) return true;
        stopped = true;

        return false;
    }

    public byte[] Write(Term term)
    {
        var stack = new Stack<Work>();
        stack.Push(new(term));
        while (!stopped && stack.TryPop(out var work))
        {
            if (work.Token is not null)
            {
                Text(work.Token);
                continue;
            }
            if (work.ConsCell)
            {
                var cell = (Cons)work.Value!;
                if (cell.Tail is Cons)
                {
                    stack.Push(new(cell.Tail, ConsCell: true));
                    stack.Push(new(null, TermDisplaySyntax.Comma));
                }
                else if (cell.Tail is not Nil)
                {
                    stack.Push(new(cell.Tail));
                    stack.Push(new(null, TermDisplaySyntax.Bar));
                }
                stack.Push(new(cell.Head));
                continue;
            }
            if (!Consume430()) break;
            switch (work.Value)
            {
                case Nil:
                    Text(TermDisplaySyntax.EmptyList);
                    break;
                case Atom atom:
                    Atom(atom.Name);
                    break;
                case Integer integer:
                    Number(integer.Value);
                    break;
                case FloatTerm floating:
                    Float(floating.Value);
                    break;
                case Cons list when Printable(list):
                    String(list);
                    break;
                case Cons list:
                    Text(TermDisplaySyntax.OpenList);
                    stack.Push(new(null, TermDisplaySyntax.CloseList));
                    stack.Push(new(list, ConsCell: true));
                    break;
                case NativeRecordTerm record:
                    Text(NativeRecordSyntax.Prefix);
                    Atom(record.Module.Name);
                    Text(NativeRecordSyntax.ModuleSeparator);
                    Atom(record.Name.Name);
                    Text(NativeRecordSyntax.OpenFields);
                    stack.Push(new(null, NativeRecordSyntax.CloseFields));
                    for (int i = record.FieldNames.Count - 1; i >= 0; i--)
                    {
                        stack.Push(new(record.Values[i]));
                        stack.Push(new(null, NativeRecordSyntax.FieldAssignment));
                        stack.Push(new(record.FieldNames[i]));
                        if (i != 0)
                            stack.Push(new(null, NativeRecordSyntax.FieldSeparator));
                    }
                    break;
                case TupleTerm tuple:
                    Text(TermDisplaySyntax.OpenTuple);
                    stack.Push(new(null, TermDisplaySyntax.CloseTuple));
                    for (int i = tuple.Items.Count - 1; i >= 0; i--)
                    {
                        stack.Push(new(tuple.Items[i]));
                        if (i != 0)
                            stack.Push(new(null, TermDisplaySyntax.Comma));
                    }
                    break;
                case MapTerm map:
                    Text(TermDisplaySyntax.OpenMap);
                    stack.Push(new(null, TermDisplaySyntax.CloseTuple));
                    var entries = TermDisplayMapOrder.Entries(map);
                    for (int i = entries.Count - 1; i >= 0; i--)
                    {
                        stack.Push(new(entries[i].Value));
                        stack.Push(new(null, TermDisplaySyntax.Association));
                        stack.Push(new(entries[i].Key));
                        if (i != 0)
                            stack.Push(new(null, TermDisplaySyntax.Comma));
                    }
                    break;
                case BitString binary:
                    Binary(binary);
                    break;
                case Pid pid:
                    Text(Term.Text(ProcessIdentifierText.PidList(runtime, pid)));
                    break;
                case PortTerm port:
                    Text(Term.Text(ProcessIdentifierText.PortList(runtime, port)));
                    break;
                case ReferenceTerm reference:
                    Text(Term.Text(ReferenceText.ToList(runtime, reference)));
                    break;
                case FunctionTerm function:
                    Function(function);
                    break;
                default:
                    throw new NotSupportedException(TermDisplaySyntax.Unsupported);
            }
        }
        if (limited)
        {
            if (remaining <= 0) Text(InternalTextAuxiliaryNames430.Truncated);
        }
        else Byte(TermDisplayParameters.LineFeed);

        return buffer.WrittenSpan.ToArray();
    }

    private void Byte(byte value)
    {
        buffer.GetSpan(1)[0] = value;
        buffer.Advance(1);
    }

    private void Text(string text)
    {
        int count = Encoding.UTF8.GetByteCount(text);
        Encoding.UTF8.GetBytes(text, buffer.GetSpan(count));
        buffer.Advance(count);
    }

    private void Number(System.Numerics.BigInteger value) => Text(value.ToString(CultureInfo.InvariantCulture));

    private void Float(double value)
    {
        string text = value.ToString(TermDisplaySyntax.Scientific, CultureInfo.InvariantCulture);
        int exponent = text.IndexOf('e');
        int digits = exponent + TermDisplayParameters.ExponentPrefixLength;
        while (text.Length - digits > TermDisplayParameters.ExponentMinimumDigits && text[digits] == '0')
            digits++;
        Text(text[..(exponent + TermDisplayParameters.ExponentPrefixLength)] + text[digits..]);
    }

    private static bool Lower(int c) => c is >= 'a' and <= 'z' || c >= TermDisplayParameters.LatinLowerStart && c <= TermDisplayParameters.ByteMaximum && c != TermDisplayParameters.Division;

    private static bool AlphaNumeric(int c) => Lower(c) || c is >= 'A' and <= 'Z' or >= '0' and <= '9' || c >= TermDisplayParameters.LatinUpperStart && c <= TermDisplayParameters.LatinUpperEnd && c != TermDisplayParameters.Multiplication;

    private void Atom(string name)
    {
        if (limited) remaining -= Encoding.UTF8.GetByteCount(name);
        var runes = name.EnumerateRunes().ToArray();
        bool quote = runes.Length == 0 || !Lower(runes[0].Value) || runes.Skip(1).Any(r => !AlphaNumeric(r.Value) && r.Value != '_');
        if (quote)
            Text(TermDisplaySyntax.AtomQuote);
        foreach (var rune in runes)
        {
            string? escape = rune.Value switch
            {
                '\'' => TermDisplaySyntax.EscapeAtomQuote,
                '\\' => TermDisplaySyntax.EscapeSlash,
                '\n' => TermDisplaySyntax.NewLine,
                '\f' => TermDisplaySyntax.FormFeed,
                '\t' => TermDisplaySyntax.Tab,
                '\r' => TermDisplaySyntax.CarriageReturn,
                '\b' => TermDisplaySyntax.Backspace,
                '\v' => TermDisplaySyntax.VerticalTab,
                _ => null
            };
            if (escape is not null)
                Text(escape);
            else if (rune.Value < TermDisplayParameters.Space || rune.Value is >= TermDisplayParameters.ExtendedControlStart and <= TermDisplayParameters.ExtendedControlEnd)
                Text(TermDisplaySyntax.Escape + Convert.ToString(rune.Value, TermDisplayParameters.OctalRadix).PadLeft(TermDisplayParameters.OctalDigits, '0'));
            else
                Text(rune.ToString());
        }
        if (quote)
            Text(TermDisplaySyntax.AtomQuote);
    }

    private bool Printable(Cons list)
    {
        Term remaining = list;
        while (remaining is Cons cell)
        {
            if (limited) cancellation.ThrowIfCancellationRequested();
            if (cell.Head is not Integer integer || integer.Value < 0 || integer.Value > TermDisplayParameters.ByteMaximum)
                return false;
            int c = (int)integer.Value;
            bool control = c < TermDisplayParameters.Space || c == TermDisplayParameters.Delete || c is >= TermDisplayParameters.ExtendedControlStart and <= TermDisplayParameters.ExtendedControlEnd;
            if (control && c is not (' ' or '\n' or '\t' or '\r'))
                return false;
            remaining = cell.Tail;
        }

        return remaining is Nil;
    }

    private void String(Cons list)
    {
        Text(TermDisplaySyntax.Quote);
        Term remaining = list;
        while (remaining is Cons cell)
        {
            if (limited) cancellation.ThrowIfCancellationRequested();
            if (!Consume430()) return;
            byte value = (byte)((Integer)cell.Head).Value;
            if (value == '\n')
                Text(TermDisplaySyntax.NewLine);
            else
            {
                if (value == '"')
                    Text(TermDisplaySyntax.Escape);
                Byte(value);
            }
            remaining = cell.Tail;
        }
        Text(TermDisplaySyntax.Quote);
    }

    private void Binary(BitString binary)
    {
        ReadOnlySpan<byte> bytes = binary.ByteSpan;
        int tailBits = binary.BitLength % TermDisplayParameters.BitsPerByte;
        bool quoted = bytes.Length != 0 && tailBits == 0;
        foreach (byte value in bytes)
            quoted &= value >= TermDisplayParameters.Space && value < TermDisplayParameters.Delete;
        Text(TermDisplaySyntax.OpenBinary);
        if (quoted)
            Text(TermDisplaySyntax.Quote);
        for (int i = 0; i < bytes.Length; i++)
        {
            if (quoted)
            {
                if (bytes[i] == '"')
                    Text(TermDisplaySyntax.Escape);
                Byte(bytes[i]);
            }
            else
            {
                if (i != 0)
                    Text(TermDisplaySyntax.Comma);
                bool tail = tailBits != 0 && i == bytes.Length - 1;
                Number(tail ? bytes[i] >> (TermDisplayParameters.BitsPerByte - tailBits) : bytes[i]);
                if (tail)
                {
                    Text(TermDisplaySyntax.Colon);
                    Number(tailBits);
                }
            }
            if (i < binary.BitLength / TermDisplayParameters.BitsPerByte && !Consume430()) return;
        }
        if (quoted)
            Text(TermDisplaySyntax.Quote);
        Text(TermDisplaySyntax.CloseBinary);
    }

    private void Function(FunctionTerm function)
    {
        if (function.ExternalIdentity is { } external)
        {
            Text(TermDisplaySyntax.ExternalFunOpening);
            Atom(external.Module.Name);
            Text(TermDisplaySyntax.Colon);
            Atom(external.Function.Name);
            Text(TermDisplaySyntax.Slash);
            Number(external.Arity);
        }
        else if (function.LocalIdentity is { } local)
        {
            Text(TermDisplaySyntax.LocalFunOpening);
            Text(local.Module.Name);
            Text(TermDisplaySyntax.LocalFunSeparator);
            Number(local.OldIndex);
            Text(TermDisplaySyntax.LocalFunSeparator);
            Number(local.OldUnique);
            Text(TermDisplaySyntax.LocalFunClosing);
        }
        else
            throw new NotSupportedException(TermDisplaySyntax.Unsupported);
    }
}
