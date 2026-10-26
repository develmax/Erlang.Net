using System.Numerics;
using System.Text;

namespace Erlang.Otp;

internal static class ProcLibReportControlText
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static Term Unicode(Term value) => Convert(value, true);

    public static Term Latin1(Term value, BigInteger limit)
    {
        if (limit.Sign < 0)
            return Convert(value, false);
        BigInteger prefix = BigInteger.Max(limit - ProcLibReportControlNames.EllipsisLength, BigInteger.Zero);
        var output = new List<Term>();
        using var characters = Latin1Characters(value).GetEnumerator();
        while (prefix > 0 && characters.MoveNext())
        {
            output.Add(Term.I(characters.Current));
            prefix--;
        }
        var lookahead = new List<Term>();
        while (lookahead.Count < ProcLibReportControlNames.EllipsisLookahead && characters.MoveNext())
            lookahead.Add(Term.I(characters.Current));
        if (lookahead.Count == ProcLibReportControlNames.EllipsisLookahead)
            output.AddRange(Cons.Items(Term.String(ProcLibReportControlNames.Ellipsis)));
        else
            output.AddRange(lookahead);

        return Cons.From(output);
    }

    private static Term Convert(Term value, bool unicode)
    {
        var work = new Stack<(Term Value, bool Combine, bool Scalar)>();
        var results = new Stack<Term>();
        work.Push((value, false, false));
        while (work.TryPop(out var item))
        {
            if (item.Combine)
            {
                Term tail = results.Pop();
                Term head = results.Pop();
                results.Push(new Cons(head, tail));
                continue;
            }
            switch (item.Value)
            {
                case Nil:
                    results.Push(Nil.Value);
                    break;
                case Cons list:
                    work.Push((list, true, false));
                    work.Push((list.Tail, false, false));
                    work.Push((list.Head, false, true));
                    break;
                case Integer integer when item.Scalar && integer.Value.Sign >= 0 &&
                    (unicode || integer.Value <= ProcLibReportControlNames.Latin1Maximum):
                    results.Push(integer);
                    break;
                case BitString { IsBinary: true } binary:
                    byte[] bytes = binary.ToArray();
                    Term converted;
                    try
                    {
                        converted = unicode ? Term.String(StrictUtf8.GetString(bytes)) : Cons.From(bytes.Select(item => (Term)Term.I(item)));
                    }
                    catch (DecoderFallbackException)
                    {
                        converted = Cons.From(bytes.Select(item => (Term)Term.I(item)));
                    }
                    results.Push(converted);
                    break;
                default:
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
        }

        return results.Pop();
    }

    public static IEnumerable<int> Characters(Term text)
    {
        var pending = new Stack<Term>();
        pending.Push(text);
        while (pending.TryPop(out Term? item))
        {
            switch (item)
            {
                case Nil:
                    break;
                case Cons list:
                    pending.Push(list.Tail);
                    pending.Push(list.Head);
                    break;
                case Integer integer when integer.Value >= 0 && integer.Value <= int.MaxValue:
                    yield return (int)integer.Value;
                    break;
                default:
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
        }
    }

    private static IEnumerable<int> Latin1Characters(Term text)
    {
        var pending = new Stack<(Term Value, bool Scalar)>();
        pending.Push((text, false));
        while (pending.TryPop(out var item))
        {
            switch (item.Value)
            {
                case Nil:
                    break;
                case Cons list:
                    pending.Push((list.Tail, false));
                    pending.Push((list.Head, true));
                    break;
                case Integer integer when item.Scalar && integer.Value >= 0 && integer.Value <= ProcLibReportControlNames.Latin1Maximum:
                    yield return (int)integer.Value;
                    break;
                case BitString { IsBinary: true } binary:
                    foreach (byte character in binary.ToArray())
                        yield return character;
                    break;
                default:
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
        }
    }
}
