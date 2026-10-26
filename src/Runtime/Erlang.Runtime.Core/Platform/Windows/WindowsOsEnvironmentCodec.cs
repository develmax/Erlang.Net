using System.Text;

namespace Erlang;

internal static class WindowsOsEnvironmentCodec
{
    internal static string Read(Term value, bool key, CancellationToken cancellation)
    {
        var result = new StringBuilder();
        var pending = new Stack<(Term Value, bool Element)>();
        pending.Push((value, false));
        while (pending.TryPop(out var part))
        {
            cancellation.ThrowIfCancellationRequested();
            switch (part.Value)
            {
                case Nil: break;
                case Cons cell:
                    pending.Push((cell.Tail, false));
                    pending.Push((cell.Head, true));
                    break;
                case Integer number when part.Element && number.Value > 0 && number.Value <= char.MaxValue:
                    result.Append((char)(int)number.Value);
                    break;
                default: throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
        }
        string text = result.ToString();
        // Pinned converter permits '=' only at the first Windows code unit.
        // Empty names pass its actual pointer-based length test; do not invent
        // a nonempty-name check from the public type documentation.
        if (key && text.AsSpan(Math.Min(1, text.Length)).Contains('='))
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return text;
    }

    internal static Term Write(string value, CancellationToken cancellation = default)
    {
        Term result = Nil.Value;
        for (int index = value.Length - 1; index >= 0; index--)
        {
            cancellation.ThrowIfCancellationRequested();
            result = new Cons(Term.I(value[index]), result);
        }

        return result;
    }
}
