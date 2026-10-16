using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang.Otp;

internal sealed partial class ProcLibReportPrettyPrinter(ProcessRuntime runtime) : IProcLibReportTermPrinter
{
    private readonly bool printStrings = true;
    private readonly bool unicodePrintable;

    private ProcLibReportPrettyPrinter(ProcessRuntime runtime, bool printStrings, bool unicodePrintable) : this(runtime)
    {
        this.printStrings = printStrings;
        this.unicodePrintable = unicodePrintable;
    }

    public Term Write(Term term, Term encoding)
        => new ProcLibReportPrettyPrinter(runtime, false, unicodePrintable).Build(
            term,
            -1,
            -1,
            encoding,
            encoding is Atom { Name: ProcLibReportAtomNames.Latin1 }
        ).Write();

    public Term Render(
        Term term,
        Term depth,
        Term encoding,
        Term charsLimit,
        int width,
        int column
    )
    {
        int maximumDepth = Parameter(depth);
        int budget = Parameter(charsLimit);
        bool latin1 = encoding is Atom { Name: ProcLibReportAtomNames.Latin1 };
        if (maximumDepth == 0 || budget == 0)
            return Term.String(ProcLibReportPrettyNames.Ellipsis);

        ProcLibReportPrettyNode node = budget < 0
            ? Build(
                term,
                maximumDepth,
                budget,
                encoding,
                latin1
            )
            : Intermediate(
                term,
                maximumDepth,
                budget,
                encoding,
                latin1
            );
        column = Math.Max(1, column);
        if (width == 0 || node.Length < width - column)
            return node.Write();

        return PrettyRoot(node, column, width);
    }

    private static int Parameter(Term value)
    {
        if (value is not Integer integer)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return integer.Value.Sign < 0 ? -1 : (int)BigInteger.Min(integer.Value, int.MaxValue);
    }

    private ProcLibReportPrettyNode Intermediate(
        Term term,
        int depth,
        int budget,
        Term encoding,
        bool latin1
    )
    {
        int lowerDepth = 1;
        var lower = Build(
            term,
            lowerDepth,
            budget,
            encoding,
            latin1
        );
        if (lower.Dots == 0 || lower.Length > budget || depth == 1)
            return lower;

        int step = 2;
        while (true)
        {
            int upperDepth = (int)Math.Min((long)lowerDepth + 2L * step, depth < 0 ? int.MaxValue : depth);
            var upper = Expand(lower, budget, upperDepth - lowerDepth);
            if (upper.Dots == 0 || upper.Length == lower.Length)
                return upper;
            if (upper.Length <= budget && (upperDepth < depth || depth < 0))
            {
                lower = upper;
                lowerDepth = upperDepth;
                step = (int)Math.Min(2L * step, int.MaxValue);
                continue;
            }
            while (upperDepth - lowerDepth > 1)
            {
                int middleDepth = lowerDepth + (upperDepth - lowerDepth) / 2;
                var middle = Expand(lower, budget, middleDepth - lowerDepth);
                if (middle.Length > budget)
                {
                    upper = middle;
                    upperDepth = middleDepth;
                }
                else
                {
                    lower = middle;
                    lowerDepth = middleDepth;
                }
            }

            return lower.Length == budget ? lower : upper;
        }
    }

    private ProcLibReportPrettyNode Build(
        Term term,
        int depth,
        int budget,
        Term encoding,
        bool latin1
    )
    {
        switch (term)
        {
            case Nil:
                return ProcLibReportPrettyNode.Leaf(ProcLibReportPrettyNames.EmptyList);
            case Atom:
                return ProcLibReportPrettyNode.Leaf(ProcLibReportCharacters.FlattenUnicode(ProcLibReportAtomWriter.Write(term, encoding)));
            case Integer integer:
                return ProcLibReportPrettyNode.Leaf(integer.Value.ToString(CultureInfo.InvariantCulture));
            case FloatTerm:
                return ProcLibReportPrettyNode.Leaf(ErlangFloatOutput.Format([term, Term.List(Term.A(ProcLibReportPrettyNames.ShortFloat))]));
            case Pid pid:
                return ProcLibReportPrettyNode.Leaf(Term.Text(ProcessIdentifierText.PidList(runtime, pid)));
            case PortTerm port:
                return ProcLibReportPrettyNode.Leaf(Term.Text(ProcessIdentifierText.PortList(runtime, port)));
            case ReferenceTerm reference:
                return ProcLibReportPrettyNode.Leaf(Term.Text(ReferenceText.ToList(runtime, reference)));
            case FunctionTerm:
                {
                    byte[] displayed = new TermDisplayWriter(runtime).Write(term);

                    return ProcLibReportPrettyNode.Leaf(Encoding.UTF8.GetString(displayed.AsSpan(0, displayed.Length - 1)));
                }
            case Cons list:
                return BuildList(
                    list,
                    depth,
                    budget,
                    encoding,
                    latin1
                );
            case TupleTerm tuple:
                return BuildTuple(
                    tuple,
                    depth,
                    budget,
                    encoding,
                    latin1
                );
            case MapTerm map:
                return BuildMap(
                    map,
                    depth,
                    budget,
                    encoding,
                    latin1
                );
            case BitString binary:
                return BuildBinary(
                    binary,
                    depth,
                    budget,
                    latin1
                );
            case NativeRecordTerm record:
                return BuildRecord(
                    record,
                    depth,
                    budget,
                    encoding,
                    latin1
                );
            default:
                throw new NotSupportedException(ProcLibReportPrettyNames.Unsupported);
        }
    }

    private ProcLibReportPrettyNode BuildTuple(
        TupleTerm tuple,
        int depth,
        int budget,
        Term encoding,
        bool latin1
    )
    {
        if (tuple.Items.Count == 0)
            return ProcLibReportPrettyNode.Leaf(ProcLibReportPrettyNames.EmptyTuple);
        if (depth == 1)
            return WithMore(
                ProcLibReportPrettyNode.Leaf(ProcLibReportPrettyNames.TupleOpen + ProcLibReportPrettyNames.Ellipsis + ProcLibReportPrettyNames.TupleClose, 3),
                (nextBudget, addedDepth) => BuildTuple(
                    tuple,
                    1 + addedDepth,
                    nextBudget,
                    encoding,
                    latin1
                )
            );

        var sequence = BuildTupleRemainder(
            tuple,
            0,
            depth,
            Subtract(budget, ProcLibReportPrettyParameters.TupleDelimiters),
            encoding,
            latin1
        );

        return Container(
            ProcLibReportPrettyKind.Tuple,
            sequence.Elements,
            sequence.Tail,
            ProcLibReportPrettyParameters.TupleDelimiters,
            tuple.Items.Count > 1 && tuple.Items[0] is Atom
        );
    }

    private ProcLibReportPrettyNode BuildList(
        Cons list,
        int depth,
        int budget,
        Term encoding,
        bool latin1
    )
    {
        if (printStrings && depth != 1 && TryString(
            list,
            budget,
            latin1,
            out string? text,
            out bool truncated
        ))
        {
            string quoted = Quote(text!);
            Term? raw = truncated ? new Cons(Term.String(quoted), Term.String(ProcLibReportPrettyNames.Ellipsis)) : null;

            return ProcLibReportPrettyNode.Leaf(quoted + (truncated ? ProcLibReportPrettyNames.Ellipsis : string.Empty), raw: raw);
        }

        int originalDepth = depth;
        var elements = new List<ProcLibReportPrettyNode>();
        Term remaining = list;
        ProcLibReportPrettyNode? tail = null;
        budget = Subtract(budget, ProcLibReportPrettyParameters.ListDelimiters);
        while (remaining is not Nil)
        {
            if (depth == 1 || budget == 0)
            {
                tail = Dots();
                break;
            }
            if (remaining is not Cons cell)
            {
                tail = Build(
                    remaining,
                    Decrement(depth),
                    budget,
                    encoding,
                    latin1
                );
                break;
            }
            int elementBudget = cell.Tail is Nil ? budget : Subtract(budget, 1);
            var element = Build(
                cell.Head,
                Decrement(depth),
                elementBudget,
                encoding,
                latin1
            );
            elements.Add(element);
            remaining = cell.Tail;
            depth = Decrement(depth);
            budget = Subtract(elementBudget, element.Length);
        }

        var node = Container(
            ProcLibReportPrettyKind.List,
            elements,
            tail,
            ProcLibReportPrettyParameters.ListDelimiters
        );

        return node.Dots == 0 ? node : WithMore(node, (nextBudget, addedDepth) => BuildList(
            list,
            AddDepth(originalDepth, addedDepth),
            nextBudget,
            encoding,
            latin1
        ));
    }

    private ProcLibReportPrettyNode BuildMap(
        MapTerm map,
        int depth,
        int budget,
        Term encoding,
        bool latin1
    )
    {
        if (map.Entries.Count == 0)
            return ProcLibReportPrettyNode.Leaf(ProcLibReportPrettyNames.EmptyMap);
        if (depth == 1)
            return WithMore(
                ProcLibReportPrettyNode.Leaf(ProcLibReportPrettyNames.MapOpen + ProcLibReportPrettyNames.Ellipsis + ProcLibReportPrettyNames.TupleClose, 3),
                (nextBudget, addedDepth) => BuildMap(
                    map,
                    1 + addedDepth,
                    nextBudget,
                    encoding,
                    latin1
                )
            );

        var sequence = BuildMapRemainder(
            TermDisplayMapOrder.Entries(map),
            0,
            depth,
            Decrement(depth),
            Subtract(budget, ProcLibReportPrettyParameters.MapDelimiters),
            encoding,
            latin1
        );

        return Container(
            ProcLibReportPrettyKind.Map,
            sequence.Elements,
            sequence.Tail,
            ProcLibReportPrettyParameters.MapDelimiters
        );
    }

    private ProcLibReportPrettyNode BuildRecord(
        NativeRecordTerm record,
        int depth,
        int budget,
        Term encoding,
        bool latin1
    )
    {
        Term moduleName = ProcLibReportAtomWriter.Write(record.Module, encoding);
        Term recordName = ProcLibReportAtomWriter.Write(record.Name, encoding);
        Term qualifiedName = Term.List(
            Term.I('#'),
            moduleName,
            Term.I(':'),
            recordName
        );
        string name = ProcLibReportCharacters.FlattenUnicode(qualifiedName);
        if (depth == 1)
        {
            Term fields = record.FieldNames.Count == 0 ? Nil.Value : Term.String(ProcLibReportPrettyNames.Ellipsis);
            Term raw = Term.List(
                Term.I('#'),
                moduleName,
                Term.I(':'),
                recordName,
                Term.I('{'),
                fields,
                Term.I('}')
            );
            string text = name + ProcLibReportPrettyNames.TupleOpen +
                (record.FieldNames.Count == 0 ? string.Empty : ProcLibReportPrettyNames.Ellipsis) + ProcLibReportPrettyNames.TupleClose;

            return WithMore(
                ProcLibReportPrettyNode.Leaf(text, ProcLibReportPrettyParameters.EllipsisWidth, raw),
                (nextBudget, addedDepth) => BuildRecord(
                    record,
                    1 + addedDepth,
                    nextBudget,
                    encoding,
                    latin1
                )
            );
        }
        var sequence = BuildRecordRemainder(
            record,
            0,
            Decrement(depth),
            Subtract(budget, name.EnumerateRunes().Count() + ProcLibReportPrettyParameters.TupleDelimiters),
            encoding,
            latin1
        );
        var node = NamedContainer(
            ProcLibReportPrettyKind.Record,
            name,
            qualifiedName,
            sequence.Elements,
            sequence.Tail,
            name.EnumerateRunes().Count() + ProcLibReportPrettyParameters.TupleDelimiters
        );
        if (printStrings)
            return node;

        var plainFields = new List<Term>();
        for (int index = 0; index < record.FieldNames.Count; index++)
        {
            plainFields.Add(ProcLibReportAtomWriter.Write(record.FieldNames[index], encoding));
            plainFields.Add(Term.String(ProcLibReportPrettyNames.FieldAssignment));
            plainFields.Add(node.Elements[index].Elements[0].Write());
            if (index + 1 < record.FieldNames.Count)
                plainFields.Add(Term.String(ProcLibReportPrettyNames.FieldSeparator));
        }
        Term plain = Term.List(
            Term.I('#'),
            moduleName,
            Term.I(':'),
            recordName,
            Term.I('{'),
            Cons.From(plainFields),
            Term.I('}')
        );

        return WithRaw(node, plain);
    }

    private static ProcLibReportPrettyNode Container(
        ProcLibReportPrettyKind kind,
        IReadOnlyList<ProcLibReportPrettyNode> elements,
        ProcLibReportPrettyNode? tail,
        int delimiters,
        bool tagged = false
    ) => new()
    {
        Kind = kind,
        Elements = elements,
        Tail = tail,
        Tagged = tagged,
        Length = delimiters + elements.Sum(element => element.Length) + Math.Max(0, elements.Count - 1) +
            (tail is null ? 0 : tail.Length + (elements.Count == 0 ? 0 : 1)),
        Dots = elements.Sum(element => element.Dots) + (tail?.Dots ?? 0)
    };

    private static ProcLibReportPrettyNode Dots() => ProcLibReportPrettyNode.Leaf(ProcLibReportPrettyNames.Ellipsis, 3);

    private static int Decrement(int value) => value < 0 ? -1 : value - 1;

    private static int Subtract(int budget, int used) => budget < 0 ? -1 : Math.Max(0, budget - used);
}
