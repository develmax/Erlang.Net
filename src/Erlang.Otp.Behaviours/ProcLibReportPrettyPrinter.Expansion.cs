using System.Text;

namespace Erlang.Otp;

internal sealed partial class ProcLibReportPrettyPrinter
{
    private ProcLibReportPrettyNode BuildTupleRemainder(
        TupleTerm tuple,
        int start,
        int depth,
        int budget,
        Term encoding,
        bool latin1
    )
    {
        var elements = new List<ProcLibReportPrettyNode>();
        ProcLibReportPrettyNode? tail = null;
        for (int index = start; index < tuple.Items.Count; index++)
        {
            if (depth == 1 || budget == 0)
            {
                int retainedIndex = index;
                int retainedDepth = depth;
                tail = WithMore(
                    Dots(),
                    (nextBudget, addedDepth) => BuildTupleRemainder(
                        tuple,
                        retainedIndex,
                        AddDepth(retainedDepth, addedDepth),
                        nextBudget,
                        encoding,
                        latin1
                    )
                );
                break;
            }
            int elementBudget = index + 1 == tuple.Items.Count ? budget : Subtract(budget, 1);
            var element = Build(
                tuple.Items[index],
                Decrement(depth),
                elementBudget,
                encoding,
                latin1
            );
            elements.Add(element);
            budget = Subtract(elementBudget, element.Length);
            depth = Decrement(depth);
        }

        return Container(
            ProcLibReportPrettyKind.Sequence,
            elements,
            tail,
            0
        );
    }

    private ProcLibReportPrettyNode BuildMapRemainder(
        IReadOnlyList<KeyValuePair<Term, Term>> entries,
        int start,
        int depth,
        int valueDepth,
        int budget,
        Term encoding,
        bool latin1
    )
    {
        var elements = new List<ProcLibReportPrettyNode>();
        ProcLibReportPrettyNode? tail = null;
        for (int index = start; index < entries.Count; index++)
        {
            if (depth == 1 || budget == 0)
            {
                int retainedIndex = index;
                int retainedDepth = depth;
                tail = WithMore(
                    Dots(),
                    (nextBudget, addedDepth) => BuildMapRemainder(
                        entries,
                        retainedIndex,
                        AddDepth(retainedDepth, addedDepth),
                        valueDepth,
                        nextBudget,
                        encoding,
                        latin1
                    )
                );
                break;
            }
            int pairBudget = index + 1 == entries.Count ? budget : Subtract(budget, 1);
            var key = Build(
                entries[index].Key,
                valueDepth,
                pairBudget,
                encoding,
                latin1
            );
            var value = Build(
                entries[index].Value,
                valueDepth,
                Subtract(pairBudget, key.Length + ProcLibReportPrettyParameters.AssociationWidth),
                encoding,
                latin1
            );
            var pair = Pair(key, value);
            elements.Add(pair);
            budget = Subtract(pairBudget, pair.Length);
            depth = Decrement(depth);
        }

        return Container(
            ProcLibReportPrettyKind.Sequence,
            elements,
            tail,
            0
        );
    }

    private ProcLibReportPrettyNode BuildRecordRemainder(
        NativeRecordTerm record,
        int start,
        int depth,
        int budget,
        Term encoding,
        bool latin1
    )
    {
        var elements = new List<ProcLibReportPrettyNode>();
        ProcLibReportPrettyNode? tail = null;
        for (int index = start; index < record.FieldNames.Count; index++)
        {
            if (depth == 1 || budget == 0)
            {
                int retainedIndex = index;
                int retainedDepth = depth;
                tail = WithMore(
                    Dots(),
                    (nextBudget, addedDepth) => BuildRecordRemainder(
                        record,
                        retainedIndex,
                        AddDepth(retainedDepth, addedDepth),
                        nextBudget,
                        encoding,
                        latin1
                    )
                );
                break;
            }
            // Keep the pinned last-field reservation, which differs from tuple/list rules.
            int fieldBudget = index + 1 == record.FieldNames.Count ? Subtract(budget, 1) : budget;
            Term rawName = ProcLibReportAtomWriter.Write(record.FieldNames[index], encoding);
            string name = ProcLibReportCharacters.FlattenUnicode(rawName);
            int prefix = name.EnumerateRunes().Count() + ProcLibReportPrettyParameters.FieldAssignmentWidth;
            var value = Build(
                record.Values[index],
                Decrement(depth),
                Subtract(fieldBudget, prefix),
                encoding,
                latin1
            );
            var field = Field(name, rawName, value);
            elements.Add(field);
            budget = Subtract(fieldBudget, field.Length);
            depth = Decrement(depth);
        }

        return Container(
            ProcLibReportPrettyKind.Sequence,
            elements,
            tail,
            0
        );
    }

    private static ProcLibReportPrettyNode Field(string name, Term? rawName, ProcLibReportPrettyNode value) => new()
    {
        Kind = ProcLibReportPrettyKind.Field,
        Text = name,
        RawName = rawName,
        Elements = [value],
        Length = name.EnumerateRunes().Count() + ProcLibReportPrettyParameters.FieldAssignmentWidth + value.Length,
        Dots = value.Dots
    };

    private static ProcLibReportPrettyNode NamedContainer(
        ProcLibReportPrettyKind kind,
        string name,
        Term? rawName,
        IReadOnlyList<ProcLibReportPrettyNode> elements,
        ProcLibReportPrettyNode? tail,
        int delimiters
    )
    {
        var node = Container(
            kind,
            elements,
            tail,
            delimiters
        );

        return new ProcLibReportPrettyNode
        {
            Kind = node.Kind,
            Text = name,
            RawName = rawName,
            Elements = node.Elements,
            Tail = node.Tail,
            Tagged = node.Tagged,
            Length = node.Length,
            Dots = node.Dots
        };
    }

    private static ProcLibReportPrettyNode Pair(ProcLibReportPrettyNode key, ProcLibReportPrettyNode value) => new()
    {
        Kind = ProcLibReportPrettyKind.Pair,
        Elements = [key, value],
        Length = key.Length + ProcLibReportPrettyParameters.AssociationWidth + value.Length,
        Dots = key.Dots + value.Dots
    };

    private static ProcLibReportPrettyNode Expand(ProcLibReportPrettyNode node, int budget, int addedDepth)
    {
        if (node.Dots == 0 && node.More is null)
            return node;
        if (node.More is not null)
            return node.More(budget, addedDepth);
        if (node.Kind == ProcLibReportPrettyKind.Pair)
        {
            var key = Expand(node.Elements[0], Subtract(budget, 1), addedDepth);
            var value = Expand(node.Elements[1], Subtract(budget, key.Length + ProcLibReportPrettyParameters.AssociationWidth), addedDepth);

            return Pair(key, value);
        }
        if (node.Kind == ProcLibReportPrettyKind.Field)
        {
            int prefix = node.Text.EnumerateRunes().Count() + ProcLibReportPrettyParameters.FieldAssignmentWidth;
            var value = Expand(node.Elements[0], Subtract(budget, prefix), addedDepth);

            return Field(node.Text, node.RawName, value);
        }
        if (node.Kind == ProcLibReportPrettyKind.Record)
        {
            int recordDelimiters = node.Text.EnumerateRunes().Count() + ProcLibReportPrettyParameters.TupleDelimiters;
            var fields = ExpandSequence(node, Subtract(budget, recordDelimiters), addedDepth);

            return NamedContainer(
                node.Kind,
                node.Text,
                node.RawName,
                fields.Elements,
                fields.Tail,
                recordDelimiters
            );
        }
        int delimiters = node.Kind switch
        {
            ProcLibReportPrettyKind.Tuple => ProcLibReportPrettyParameters.TupleDelimiters,
            ProcLibReportPrettyKind.Map => ProcLibReportPrettyParameters.MapDelimiters,
            _ => throw new NotSupportedException(ProcLibReportPrettyNames.Unsupported)
        };
        var sequence = ExpandSequence(node, Subtract(budget, delimiters), addedDepth);

        return Container(
            node.Kind,
            sequence.Elements,
            sequence.Tail,
            delimiters,
            node.Tagged
        );
    }

    private static ProcLibReportPrettyNode ExpandSequence(ProcLibReportPrettyNode node, int budget, int addedDepth)
    {
        var elements = new List<ProcLibReportPrettyNode>();
        foreach (var element in node.Elements)
        {
            bool last = elements.Count + 1 == node.Elements.Count && node.Tail is null;
            int elementBudget = last ? budget : Subtract(budget, 1);
            var expanded = Expand(element, elementBudget, addedDepth);
            elements.Add(expanded);
            budget = Subtract(elementBudget, expanded.Length);
        }
        ProcLibReportPrettyNode? tail = null;
        if (node.Tail is not null)
        {
            var expanded = Expand(node.Tail, budget, addedDepth);
            if (expanded.Kind == ProcLibReportPrettyKind.Sequence)
            {
                elements.AddRange(expanded.Elements);
                tail = expanded.Tail;
            }
            else
                tail = expanded;
        }

        return Container(
            ProcLibReportPrettyKind.Sequence,
            elements,
            tail,
            0
        );
    }

    private static ProcLibReportPrettyNode WithMore(ProcLibReportPrettyNode node, Func<int, int, ProcLibReportPrettyNode> more) => new()
    {
        Kind = node.Kind,
        Text = node.Text,
        Raw = node.Raw,
        RawName = node.RawName,
        Elements = node.Elements,
        Tail = node.Tail,
        Tagged = node.Tagged,
        Length = node.Length,
        Dots = node.Dots,
        More = more
    };

    private static ProcLibReportPrettyNode WithRaw(ProcLibReportPrettyNode node, Term raw) => new()
    {
        Kind = node.Kind,
        Text = node.Text,
        Raw = raw,
        RawName = node.RawName,
        Elements = node.Elements,
        Tail = node.Tail,
        Tagged = node.Tagged,
        Length = node.Length,
        Dots = node.Dots,
        More = node.More
    };

    private static int AddDepth(int depth, int addedDepth) => depth < 0 ? -1 : (int)Math.Min((long)depth + addedDepth, int.MaxValue);
}
