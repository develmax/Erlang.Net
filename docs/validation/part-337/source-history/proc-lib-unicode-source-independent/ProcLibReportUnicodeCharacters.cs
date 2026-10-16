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
        while (true)
        {
            if (input is Cons list)
            {
                if (IsCodePoint(list.Head))
                    return new(input, false);
                if (list.Tail is Nil)
                {
                    input = list.Head;
                    continue;
                }

                return NestedCodePoint(list.Head, list.Tail);
            }
            if (input is Nil)
                return new(input, false);
            if (input is BitString { IsBinary: true } binary)
            {
                byte[] bytes = binary.ToArray();
                if (bytes.Length == 0)
                    return new(Nil.Value, false);
                if (!Decode(bytes, out int scalar, out int consumed))
                    return new(binary, true);

                return new(new Cons(Term.I(scalar), Tail(bytes, consumed)), false);
            }

            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        }
    }

    private static CodePointResult NestedCodePoint(Term input, Term continuation)
    {
        while (true)
        {
            if (input is Cons list)
            {
                if (IsCodePoint(list.Head))
                    return new(new Cons(list.Head, list.Tail is Nil
                        ? ContinueSingle(continuation, ProcLibReportUnicodeParameters.FirstContinuation)
                        : Continue(list.Tail, continuation, ProcLibReportUnicodeParameters.FirstContinuation)), false);
                if (list.Tail is not Nil)
                    continuation = new Cons(list.Tail, continuation);
                input = list.Head;
                continue;
            }
            if (input is Nil)
                return CodePoint(continuation);
            if (input is BitString { IsBinary: true } binary)
            {
                byte[] bytes = binary.ToArray();
                if (bytes.Length == 0)
                    return CodePoint(continuation);
                if (!Decode(bytes, out int scalar, out int consumed))
                    return new(new Cons(binary, continuation), true);

                return new(new Cons(Term.I(scalar), new Cons(Tail(bytes, consumed), continuation)), false);
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
