using System.Buffers;
using System.Text;

namespace Erlang;

// Literal state-machine port of cp/cpl and the three lazy continuation stages.
// Retains OTP's unflattened tail shape, including malformed data beyond the lookahead.
internal static class UnicodeUtilTraversal
{
    private enum State
    {
        Cp,
        Cpl,
        Continue,
        Continue2,
        Continue3,
        One,
        One2,
        One3,
        NoBinary,
        NoBinaryList
    }

    internal static async ValueTask<Term> ReadAt(
        ProcessContext context,
        Term input,
        string function,
        int arity,
        int line
    )
    {
        using var source = UnicodeUtilSource.Scope(
            context,
            function,
            arity,
            line
        );

        return await Cp(context, input);
    }

    internal static bool Codepoint(Term value, out int cp)
    {
        if (value is Integer integer && integer.Value >= 0 && integer.Value <= UnicodeUtilNames.Maximum)
        {
            cp = (int)integer.Value;

            return true;
        }
        cp = 0;

        return false;
    }

    internal static async ValueTask<Term> Cp(ProcessContext context, Term input, bool noBinary = false)
    {
        State state = noBinary ? State.NoBinary : State.Cp;
        Term value = input;
        Term rest = Nil.Value;
        var prefix = new List<Term>();
        int work = 0;

        Term Done(Term result)
        {
            for (int i = prefix.Count - 1; i >= 0; i--) result = new Cons(prefix[i], result);

            return result;
        }
        while (true)
        {
            if (++work == UnicodeUtilNames.WorkSlice)
            {
                await context.BumpReductionsAsync(work);
                work = 0;
            }
            switch (state)
            {
                case State.Cp:
                case State.NoBinary:
                    if (value is Cons cpCell)
                    {
                        bool accepted = state == State.NoBinary ? cpCell.Head is Integer : Codepoint(cpCell.Head, out _);
                        if (accepted) return Done(value);
                        if (cpCell.Tail is Nil)
                        {
                            value = cpCell.Head;
                            continue;
                        }
                        rest = cpCell.Tail;
                        value = cpCell.Head;
                        state = state == State.NoBinary ? State.NoBinaryList : State.Cpl;
                        continue;
                    }
                    if (value is Nil) return Done(value);
                    if (state == State.NoBinary) return Done(Term.A(UnicodeUtilNames.BinaryFound));
                    if (value is BitString { IsBinary: true } binary)
                    {
                        if (binary.BitLength == 0) return Done(Nil.Value);

                        return Done(Decode(binary, out Term cp, out Term tail) ? new Cons(cp, tail) : Error(binary));
                    }
                    throw UnicodeUtilSource.Clause(
                        context,
                        "cp",
                        [value],
                        UnicodeUtilSourceLines.Cp
                    );
                case State.Cpl:
                case State.NoBinaryList:
                    if (value is Cons list)
                    {
                        bool accepted = state == State.NoBinaryList ? list.Head is Integer : Codepoint(list.Head, out _);
                        if (accepted)
                        {
                            prefix.Add(list.Head);
                            if (list.Tail is Nil)
                            {
                                value = rest;
                                state = State.One;
                            }
                            else
                            {
                                value = list.Tail;
                                state = State.Continue;
                            }
                            continue;
                        }
                        if (list.Tail is not Nil) rest = new Cons(list.Tail, rest);
                        value = list.Head;
                        continue;
                    }
                    if (value is Nil)
                    {
                        value = rest;
                        state = state == State.NoBinaryList ? State.NoBinary : State.Cp;
                        continue;
                    }
                    if (state == State.NoBinaryList) return Done(Term.A(UnicodeUtilNames.BinaryFound));
                    if (value is BitString { IsBinary: true } part)
                    {
                        if (part.BitLength == 0)
                        {
                            value = rest;
                            state = State.Cp;
                            continue;
                        }

                        return Done(Decode(part, out Term cp, out Term tail) ? new Cons(cp, new Cons(tail, rest)) : Error(new Cons(part, rest)));
                    }
                    throw UnicodeUtilSource.Clause(
                        context,
                        "cpl",
                        [value, rest],
                        UnicodeUtilSourceLines.Cpl
                    );
                case State.Continue:
                case State.Continue2:
                case State.Continue3:
                    if (value is Cons cont)
                    {
                        if (cont.Head is Integer)
                        {
                            prefix.Add(cont.Head);
                            if (state == State.Continue3)
                            {
                                return Done(cont.Tail is Nil ? rest : new Cons(cont.Tail, rest));
                            }
                            value = cont.Tail;
                            state = state == State.Continue ? State.Continue2 : State.Continue3;
                            continue;
                        }
                        if (cont.Tail is not Nil) rest = new Cons(cont.Tail, rest);
                        value = cont.Head;
                        continue;
                    }
                    if (value is Nil)
                    {
                        value = rest;
                        state = state == State.Continue ? State.One : state == State.Continue2 ? State.One2 : State.One3;
                        continue;
                    }

                    return Done(new Cons(value, rest));
                default:
                    if (value is Cons one)
                    {
                        if (one.Head is Integer)
                        {
                            if (state == State.One3) return Done(value);
                            prefix.Add(one.Head);
                            value = one.Tail;
                            state = state == State.One ? State.One2 : State.One3;
                            continue;
                        }
                        if (one.Tail is Nil)
                        {
                            value = one.Head;
                            continue;
                        }
                        rest = one.Tail;
                        value = one.Head;
                        state = state == State.One ? State.Continue : state == State.One2 ? State.Continue2 : State.Continue3;
                        continue;
                    }

                    return Done(value);
            }
        }
    }

    internal static bool Decode(BitString binary, out Term cp, out Term rest)
    {
        OperationStatus status = Rune.DecodeFromUtf8(binary.ByteSpan, out Rune rune, out int consumed);
        cp = Term.I(rune.Value);
        rest = status == OperationStatus.Done ? binary.SliceBits(consumed * UnicodeUtilNames.BitsPerByte,
            binary.BitLength - consumed * UnicodeUtilNames.BitsPerByte) : binary;

        return status == OperationStatus.Done;
    }

    internal static Term Error(Term remainder) => Term.Tuple(Term.A(ErlangExceptionClasses.Error), remainder);
}
