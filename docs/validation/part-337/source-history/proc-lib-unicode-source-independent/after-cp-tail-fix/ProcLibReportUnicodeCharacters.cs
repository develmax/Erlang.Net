using System.Buffers;
using System.Text;

namespace Erlang.Otp;

internal static class ProcLibReportUnicodeCharacters
{
    public static bool TryRead(Term input, out int character, out Term remainder)
    {
        var result = CodePoint(input);
        if (result.IsError)
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadArgument), result.Value));
        if (result.Value is Nil)
        {
            character = 0;
            remainder = Nil.Value;

            return false;
        }
        if (result.Value is not Cons { Head: Integer integer } list)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        character = (int)integer.Value;
        remainder = list.Tail;

        return true;
    }

    private static CodePointResult CodePoint(Term input)
    {
        bool nested = false;
        Term continuation = Nil.Value;
        while (true)
        {
            if (input is Cons list)
            {
                if (IsCodePoint(list.Head))
                {
                    if (!nested)
                        return new(input, false);

                    return new(new Cons(list.Head, list.Tail is Nil
                        ? ContinueSingle(continuation, ProcLibReportUnicodeParameters.FirstContinuation)
                        : Continue(list.Tail, continuation, ProcLibReportUnicodeParameters.FirstContinuation)), false);
                }
                if (list.Tail is not Nil)
                {
                    continuation = nested ? new Cons(list.Tail, continuation) : list.Tail;
                    nested = true;
                }
                input = list.Head;
                continue;
            }
            if (input is Nil)
            {
                if (!nested)
                    return new(input, false);
                input = continuation;
                continuation = Nil.Value;
                nested = false;
                continue;
            }
            if (input is BitString { IsBinary: true } binary)
            {
                byte[] bytes = binary.ToArray();
                if (bytes.Length == 0)
                {
                    if (!nested)
                        return new(Nil.Value, false);
                    input = continuation;
                    continuation = Nil.Value;
                    nested = false;
                    continue;
                }
                if (!Decode(bytes, out int scalar, out int consumed))
                    return new(nested ? new Cons(binary, continuation) : binary, true);
                Term remainder = Tail(bytes, consumed);
                if (nested)
                    remainder = new Cons(remainder, continuation);

                return new(new Cons(Term.I(scalar), remainder), false);
            }

            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        }
    }

    private static Term Continue(Term input, Term continuation, int stage)
    {
        while (true)
        {
            if (input is Cons list)
            {
                if (list.Head is Integer)
                {
                    if (stage == ProcLibReportUnicodeParameters.LastContinuation)
                        return new Cons(list.Head, list.Tail is Nil ? continuation : new Cons(list.Tail, continuation));

                    return new Cons(list.Head, Continue(list.Tail, continuation, stage + 1));
                }
                if (list.Tail is not Nil)
                    continuation = new Cons(list.Tail, continuation);
                input = list.Head;
                continue;
            }
            if (input is Nil)
                return ContinueSingle(continuation, stage);

            return new Cons(input, continuation);
        }
    }

    private static Term ContinueSingle(Term input, int stage)
    {
        while (true)
        {
            if (input is not Cons list)
                return input;
            if (list.Head is Integer)
                return stage == ProcLibReportUnicodeParameters.LastContinuation
                    ? input
                    : new Cons(list.Head, ContinueSingle(list.Tail, stage + 1));
            if (list.Tail is not Nil)
                return Continue(list.Head, list.Tail, stage);
            input = list.Head;
        }
    }

    private static bool IsCodePoint(Term value)
        => value is Integer integer && integer.Value >= 0 && integer.Value <= ProcLibReportCharacterParameters.UnicodeMaximum;

    private static bool Decode(byte[] bytes, out int character, out int consumed)
    {
        OperationStatus status = Rune.DecodeFromUtf8(bytes, out Rune rune, out consumed);
        character = rune.Value;

        return status == OperationStatus.Done;
    }

    private static BitString Tail(byte[] bytes, int consumed)
        => new(bytes.AsSpan(consumed));

    private readonly record struct CodePointResult(Term Value, bool IsError);
}
