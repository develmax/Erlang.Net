namespace Erlang;

// OTP's lazy grapheme state machine. The undecoded source tail is retained at breaks.
internal static class UnicodeUtilGraphemes
{
    private enum State
    {
        Extend,
        Pictograph,
        PictographZwj,
        Indic,
        HangulL,
        HangulV,
        HangulT
    }
    private static UnicodeUtilData Data => UnicodeUtilData.Instance;

    internal static async ValueTask<Term> Gc(ProcessContext context, Term input)
    {
        while (true)
        {
            if (input is Nil) return input;
            if (input is Cons cell)
            {
                if (cell.Tail is Nil && UnicodeUtilTraversal.Codepoint(cell.Head, out _)) return input;
                if (cell.Head.Equals(Term.I('\r')))
                {
                    Term next = await UnicodeUtilTraversal.ReadAt(
                        context,
                        cell.Tail,
                        "gc",
                        1,
                        UnicodeUtilSourceLines.GcCrRead
                    );

                    return next is Cons lf && lf.Head.Equals(Term.I('\n'))
                        ? new Cons(Term.List(Term.I('\r'), Term.I('\n')), lf.Tail) : input;
                }
                if (UnicodeUtilTraversal.Codepoint(cell.Head, out int cp))
                {
                    if (cp <= UnicodeUtilNames.Latin1Maximum)
                    {
                        if (cell.Tail is Cons following && Latin1(following.Head)) return input;
                        Term next = await UnicodeUtilTraversal.Cp(context, cell.Tail, noBinary: true);
                        if (next is Cons plain && Latin1(plain.Head)) return new Cons(cell.Head, next);

                        return await Run(context, next is Atom ? cell.Tail : next, cp);
                    }

                    return await Run(context, cell.Tail, cp);
                }
            }
            if (input is BitString { IsBinary: true } binary)
            {
                if (binary.BitLength == 0) return Nil.Value;
                if (UnicodeUtilTraversal.Decode(binary, out Term first, out Term remaining))
                {
                    int cp = (int)((Integer)first).Value;
                    if (cp <= UnicodeUtilNames.Latin1Maximum && cp != '\r'
                        && UnicodeUtilTraversal.Decode((BitString)remaining, out Term following, out _) && Latin1(following))
                        return new Cons(first, remaining);

                    return await Run(context, remaining, cp);
                }
            }
            Term decoded = await UnicodeUtilTraversal.ReadAt(
                context,
                input,
                "gc",
                1,
                UnicodeUtilSourceLines.GcRead
            );
            if (decoded is TupleTerm) return decoded;
            input = decoded;
        }
    }

    private static async ValueTask<Term> Run(ProcessContext context, Term rest, int first)
    {
        var sources = new Stack<IDisposable>();
        try
        {
            var prepends = new List<Term>();
            int work = 0;
            while (Data.Contains(UnicodeUtilNames.PrependProperty, first))
            {
                prepends.Add(Term.I(first));
                Term next = await UnicodeUtilTraversal.ReadAt(
                    context,
                    rest,
                    "gc_prepend",
                    2,
                    UnicodeUtilSourceLines.PrependRead
                );
                if (next is Nil) return Pack(prepends, Nil.Value);
                if (next is TupleTerm error) return Pack(prepends, error.Items[1]);
                var cell = (Cons)next;
                if (Control(cell.Head)) return Pack(prepends, rest);
                sources.Push(UnicodeUtilSource.Scope(
                    context,
                    "gc_prepend",
                    2,
                    UnicodeUtilSourceLines.PrependRecurse
                ));
                first = Scalar(cell.Head);
                rest = cell.Tail;
                if (++work % UnicodeUtilNames.WorkSlice == 0) await context.BumpReductionsAsync(UnicodeUtilNames.WorkSlice);
            }
            var cluster = new List<Term>(prepends) { Term.I(first) };
            if (first == '\r')
            {
                Term next = await UnicodeUtilTraversal.ReadAt(
                    context,
                    rest,
                    "gc_1",
                    2,
                    UnicodeUtilSourceLines.FirstCrRead
                );
                if (next is Cons lf && lf.Head.Equals(Term.I('\n')))
                {
                    cluster.Add(lf.Head);

                    return Pack(cluster, lf.Tail);
                }

                return Pack(cluster, rest);
            }
            if (Data.Contains(UnicodeUtilNames.ControlProperty, first)) return Pack(cluster, rest);
            State state = Data.Contains(UnicodeUtilNames.HangulLProperty, first) ? State.HangulL
                : Data.Contains(UnicodeUtilNames.HangulVProperty, first) ? State.HangulV
                : Data.Contains(UnicodeUtilNames.HangulTProperty, first) ? State.HangulT
                : IsHangul(first) ? (first - UnicodeUtilNames.HangulFirst) % UnicodeUtilNames.HangulTrailCount == 0 ? State.HangulV : State.HangulT
                : Data.Contains(UnicodeUtilNames.PictographProperty, first) ? State.Pictograph
                : Data.Contains(UnicodeUtilNames.IndicConsonantProperty, first) ? State.Indic : State.Extend;
            string cpFunction = state == State.HangulL ? "gc_h_L" : state == State.HangulV ? "gc_h_V" : state == State.HangulT ? "gc_h_T"
                : state == State.Pictograph ? "gc_ext_pict" : "gc_1";
            int cpArity = 2;
            int cpLine = state == State.HangulL ? UnicodeUtilSourceLines.HangulLRead : state == State.HangulV ? UnicodeUtilSourceLines.HangulVRead : state == State.HangulT ? UnicodeUtilSourceLines.HangulTRead
                : state == State.Pictograph ? UnicodeUtilSourceLines.PictographRead : state == State.Indic ? UnicodeUtilSourceLines.IndicInitialRead : first <= UnicodeUtilNames.Latin1Maximum ? UnicodeUtilSourceLines.Latin1Read
                : first >= UnicodeUtilNames.HangulDispatchFirst && first <= UnicodeUtilNames.HangulDispatchLast ? UnicodeUtilSourceLines.HangulSyllableRead : UnicodeUtilSourceLines.ExtendInitialRead;
            if (cpLine == UnicodeUtilSourceLines.HangulSyllableRead)
            {
                cpFunction = "gc_h_lv_lvt";
                cpArity = 3;
            }
            bool fetchedLinker = false;
            Term? pending = null;
            if (first >= UnicodeUtilNames.RegionalFirst && first <= UnicodeUtilNames.RegionalLast)
            {
                pending = await UnicodeUtilTraversal.ReadAt(
                    context,
                    rest,
                    "gc_regional",
                    2,
                    UnicodeUtilSourceLines.RegionalRead
                );
                if (pending is Cons regional && UnicodeUtilTraversal.Codepoint(regional.Head, out int cp)
                    && cp >= UnicodeUtilNames.RegionalFirst && cp <= UnicodeUtilNames.RegionalLast)
                {
                    cluster.Add(regional.Head);
                    rest = regional.Tail;
                    pending = null;
                    cpFunction = "gc_regional";
                    cpArity = 2;
                    cpLine = UnicodeUtilSourceLines.RegionalExtendRead;
                }
            }
            while (true)
            {
                if (++work % UnicodeUtilNames.WorkSlice == 0) await context.BumpReductionsAsync(UnicodeUtilNames.WorkSlice);
                Term next = pending ?? await UnicodeUtilTraversal.ReadAt(
                    context,
                    rest,
                    cpFunction,
                    cpArity,
                    cpLine
                );
                pending = null;
                if (next is Nil) return Pack(cluster, rest);
                if (next is TupleTerm error)
                {
                    if (state == State.Indic) return Pack(cluster, error.Items[1]);
                    if (state is State.Pictograph or State.PictographZwj)
                    {
                        // OTP appends the residual to the original source tail here,
                        // including the badarg from ++ when that tail is improper.
                        Term result = Pack(cluster.Skip(prepends.Count).ToList(), rest);
                        using (UnicodeUtilSource.Scope(
                            context,
                            state == State.Pictograph ? "gc_ext_pict" : "gc_ext_pict_zwj",
                            3,
                            state == State.Pictograph ? UnicodeUtilSourceLines.PictographAppend : UnicodeUtilSourceLines.PictographZwjAppend
                        ))
                            result = await context.Runtime.Modules.Call(
                                context,
                                "erlang",
                                "++",
                                result,
                                Term.List(error.Items[1])
                            );
                        if (prepends.Count == 0) return result;
                        var head = (Cons)result;
                        var combined = new List<Term>(prepends);
                        if (head.Head is Integer) combined.Add(head.Head);
                        else combined.AddRange(Cons.Items(head.Head));

                        return Pack(combined, head.Tail);
                    }

                    return cluster.Count - prepends.Count == 1 ? Pack(cluster, error.Items[1]) : Pack(cluster, Term.List(error.Items[1]));
                }
                var cell = (Cons)next;
                bool valid = UnicodeUtilTraversal.Codepoint(cell.Head, out int cp);
                bool consume = false;
                switch (state)
                {
                    case State.HangulL:
                        if (valid && Data.Contains(UnicodeUtilNames.HangulLProperty, cp)) consume = true;
                        else if (valid && Data.Contains(UnicodeUtilNames.HangulVProperty, cp))
                        {
                            state = State.HangulV;
                            consume = true;
                        }
                        else if (valid && IsHangul(cp))
                        {
                            state = (cp - UnicodeUtilNames.HangulFirst) % UnicodeUtilNames.HangulTrailCount == 0 ? State.HangulV : State.HangulT;
                            consume = true;
                        }
                        else
                        {
                            state = State.Extend;
                            pending = next;
                        }
                        break;
                    case State.HangulV:
                        if (valid && Data.Contains(UnicodeUtilNames.HangulVProperty, cp)) consume = true;
                        else if (valid && Data.Contains(UnicodeUtilNames.HangulTProperty, cp))
                        {
                            state = State.HangulT;
                            consume = true;
                        }
                        else
                        {
                            state = State.Extend;
                            pending = next;
                        }
                        break;
                    case State.HangulT:
                        if (valid && Data.Contains(UnicodeUtilNames.HangulTProperty, cp)) consume = true;
                        else
                        {
                            state = State.Extend;
                            pending = next;
                        }
                        break;
                    case State.Pictograph:
                        if (valid && cp == UnicodeUtilNames.Zwj)
                        {
                            state = State.PictographZwj;
                            consume = true;
                        }
                        else consume = valid && Data.Contains(UnicodeUtilNames.ExtendProperty, cp);
                        break;
                    case State.PictographZwj:
                        consume = valid && Data.Contains(UnicodeUtilNames.PictographProperty, cp);
                        if (consume) state = State.Pictograph;
                        break;
                    case State.Indic:
                        if (valid && Data.Contains(UnicodeUtilNames.IndicLinkerProperty, cp))
                        {
                            fetchedLinker = true;
                            consume = true;
                        }
                        else if (valid && (cp == UnicodeUtilNames.Zwj || Data.Contains(UnicodeUtilNames.ExtendProperty, cp))) consume = true;
                        else if (fetchedLinker && valid && Data.Contains(UnicodeUtilNames.IndicConsonantProperty, cp))
                        {
                            fetchedLinker = false;
                            consume = true;
                        }
                        break;
                    default:
                        consume = valid && (cp == UnicodeUtilNames.Zwj || Data.Contains(UnicodeUtilNames.ExtendProperty, cp));
                        break;
                }
                if (pending is not null) continue;
                if (!consume) return Pack(cluster, rest);
                int previousCount = cluster.Count - prepends.Count;
                cluster.Add(cell.Head);
                rest = cell.Tail;
                if (state is State.HangulL or State.HangulV or State.HangulT)
                {
                    cpFunction = state == State.HangulL ? "gc_h_L" : state == State.HangulV ? "gc_h_V" : "gc_h_T";
                    cpArity = 2;
                    cpLine = state == State.HangulL ? UnicodeUtilSourceLines.HangulLRead : state == State.HangulV ? UnicodeUtilSourceLines.HangulVRead : UnicodeUtilSourceLines.HangulTRead;
                }
                else if (state == State.Indic)
                {
                    cpFunction = "gc_indic";
                    cpArity = 4;
                    cpLine = Data.Contains(UnicodeUtilNames.IndicLinkerProperty, cp) ? UnicodeUtilSourceLines.IndicLinkerRead : cp == UnicodeUtilNames.Zwj || Data.Contains(UnicodeUtilNames.ExtendProperty, cp) ? UnicodeUtilSourceLines.IndicExtendRead : UnicodeUtilSourceLines.IndicConsonantRead;
                }
                else if (state == State.PictographZwj)
                {
                    cpFunction = "gc_ext_pict";
                    cpArity = 3;
                    cpLine = UnicodeUtilSourceLines.PictographZwjRead;
                }
                else if (state == State.Pictograph)
                {
                    cpFunction = "gc_ext_pict";
                    cpArity = 2;
                    cpLine = UnicodeUtilSourceLines.PictographRead;
                }
                else
                {
                    cpFunction = previousCount == 1 ? "gc_extend" : "gc_extend2";
                    cpArity = 3;
                    cpLine = previousCount == 1 ? UnicodeUtilSourceLines.ExtendRead : UnicodeUtilSourceLines.ExtendMoreRead;
                }
            }
        }
        finally
        {
            while (sources.TryPop(out var source)) source.Dispose();
        }
    }

    private static bool Latin1(Term value) => UnicodeUtilTraversal.Codepoint(value, out int cp) && cp <= UnicodeUtilNames.Latin1Maximum;

    private static int Scalar(Term value) => (int)((Integer)value).Value;

    private static bool Control(Term value) => UnicodeUtilTraversal.Codepoint(value, out int cp) && Data.Contains(UnicodeUtilNames.ControlProperty, cp);

    private static bool IsHangul(int cp) => cp >= UnicodeUtilNames.HangulFirst && cp <= UnicodeUtilNames.HangulLast;

    private static Term Pack(List<Term> cluster, Term tail) => new Cons(cluster.Count == 1 ? cluster[0] : Cons.From(cluster), tail);
}
