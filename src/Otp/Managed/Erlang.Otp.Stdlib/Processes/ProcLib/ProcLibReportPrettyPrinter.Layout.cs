using System.Text;

namespace Erlang.Otp;

internal sealed partial class ProcLibReportPrettyPrinter
{
    private static Term PrettyRoot(ProcLibReportPrettyNode node, int column, int width)
    {
        int tagIndent = 1;
        foreach (int candidate in new[] { -1, 4 })
        {
            try
            {
                Pretty(
                    node,
                    column,
                    width,
                    node.Length,
                    candidate,
                    Spaces(column - 1),
                    0,
                    0,
                    true
                );
                tagIndent = candidate;
                break;
            }
            catch (ProcLibReportPrettyIndentException)
            {
            }
        }

        return Pretty(
            node,
            column,
            width,
            node.Length,
            tagIndent,
            Spaces(column - 1),
            0,
            0,
            false
        );
    }

    private static bool Fits(
        ProcLibReportPrettyNode node,
        int column,
        int width,
        int maximum,
        int lastDepth,
        int used
    )
        => node.Length < width - column - lastDepth && node.Length + used + lastDepth <= maximum;

    private static Term Pretty(
        ProcLibReportPrettyNode node,
        int column,
        int width,
        int maximum,
        int tagIndent,
        Term indent,
        int lastDepth,
        int used,
        bool checking
    )
    {
        if (Fits(
            node,
            column,
            width,
            maximum,
            lastDepth,
            used
        ) || node.Atomic)
            return node.Write();

        switch (node.Kind)
        {
            case ProcLibReportPrettyKind.List:
                return Term.List(
                    Term.I('['),
                    PrettySequence(
                        node,
                        0,
                        column + 1,
                        width,
                        maximum,
                        tagIndent,
                        Indent(1, indent),
                        lastDepth,
                        '|',
                        used + 1,
                        checking
                    ),
                    Term.I(']')
                );
            case ProcLibReportPrettyKind.Tuple when !node.Tagged:
                return Term.List(
                    Term.I('{'),
                    PrettySequence(
                        node,
                        0,
                        column + 1,
                        width,
                        maximum,
                        tagIndent,
                        Indent(1, indent),
                        lastDepth,
                        ',',
                        used + 1,
                        checking
                    ),
                    Term.I('}')
                );
            case ProcLibReportPrettyKind.Tuple:
                {
                    if (node.Elements.Count == 0)
                        return Term.List(Term.I('{'), Term.String(ProcLibReportPrettyNames.Ellipsis), Term.I('}'));

                    var tag = node.Elements[0];
                    int tagWidth = tag.Length + 2;
                    int nextColumn = column + tagWidth;
                    Term body;
                    if (tagIndent > 0 && tagWidth > tagIndent)
                    {
                        int baseColumn = column + tagIndent;
                        CheckColumn(checking, maximum + baseColumn <= width || baseColumn <= width / 2);
                        body = new Cons(
                            tag.Write(),
                            PrettyTail(
                                node,
                                1,
                                baseColumn,
                                nextColumn,
                                width,
                                maximum,
                                tagIndent,
                                Indent(tagIndent, indent),
                                lastDepth,
                                ',',
                                used + 1 + tag.Length,
                                checking
                            )
                        );
                    }
                    else
                    {
                        CheckColumn(checking, maximum + nextColumn < width || nextColumn < width / 2);
                        body = Cons.From(
                            [tag.Write(), Term.I(',')],
                            PrettySequence(
                                node,
                                1,
                                nextColumn,
                                width,
                                maximum,
                                tagIndent,
                                Indent(tagWidth, indent),
                                lastDepth,
                                ',',
                                used + tag.Length + 2,
                                checking
                            )
                        );
                    }

                    return Term.List(Term.I('{'), body, Term.I('}'));
                }
            case ProcLibReportPrettyKind.Map:
                return Term.List(
                    Term.I('#'),
                    Term.I('{'),
                    PrettyPairs(
                        node,
                        column + 2,
                        width,
                        maximum,
                        tagIndent,
                        Indent(2, indent),
                        lastDepth,
                        used + (checking ? 2 : 1),
                        checking
                    ),
                    Term.I('}')
                );
            case ProcLibReportPrettyKind.Binary:
                return PrettyBinary(
                    node,
                    column + 2,
                    width,
                    maximum,
                    Indent(2, indent),
                    lastDepth,
                    used
                );
            case ProcLibReportPrettyKind.Record:
                {
                    int prefix = node.Text.EnumerateRunes().Count() + 1;
                    var placement = RecordIndent(
                        prefix,
                        tagIndent,
                        column,
                        indent,
                        used + prefix,
                        width,
                        maximum,
                        checking
                    );
                    Term body = PrettyPairs(
                        node,
                        placement.Column,
                        width,
                        maximum,
                        tagIndent,
                        placement.Indent,
                        lastDepth,
                        placement.Used,
                        checking
                    );

                    return Term.List(
                        node.RawName ?? Term.String(node.Text),
                        Term.I('{'),
                        new Cons(placement.Separator, body),
                        Term.I('}')
                    );
                }
            default:
                throw new NotSupportedException(ProcLibReportPrettyNames.Unsupported);
        }
    }

    private static Term PrettySequence(
        ProcLibReportPrettyNode node,
        int index,
        int baseColumn,
        int width,
        int maximum,
        int tagIndent,
        Term indent,
        int lastDepth,
        char tailSeparator,
        int used,
        bool checking
    )
    {
        if (index == node.Elements.Count)
            return node.Tail?.Write() ?? Nil.Value;

        int elementDepth = index + 1 < node.Elements.Count ? 0 : lastDepth + 1;
        var element = PrettyElement(
            node.Elements[index],
            baseColumn,
            width,
            maximum,
            tagIndent,
            indent,
            elementDepth,
            used,
            checking
        );

        return new Cons(
            element.Text,
            PrettyTail(
                node,
                index + 1,
                baseColumn,
                baseColumn + element.Used,
                width,
                maximum,
                tagIndent,
                indent,
                lastDepth,
                tailSeparator,
                used + element.Used,
                checking
            )
        );
    }

    private static Term PrettyTail(
        ProcLibReportPrettyNode node,
        int index,
        int baseColumn,
        int column,
        int width,
        int maximum,
        int tagIndent,
        Term indent,
        int lastDepth,
        char tailSeparator,
        int used,
        bool checking
    )
    {
        var parts = new List<Term>();
        for (; index < node.Elements.Count; index++)
        {
            var element = node.Elements[index];
            int elementDepth = index + 1 < node.Elements.Count ? 0 : lastDepth + 1;
            int length = element.Length + 1;
            parts.Add(Term.I(','));
            if (element.Atomic && FitsTail(
                length,
                column,
                width,
                maximum,
                elementDepth,
                used
            ))
            {
                parts.Add(element.Write());
                column += length;
                used += length;
            }
            else
            {
                var rendered = PrettyElement(
                    element,
                    baseColumn,
                    width,
                    maximum,
                    tagIndent,
                    indent,
                    elementDepth,
                    0,
                    checking
                );
                parts.Add(Term.I('\n'));
                parts.Add(indent);
                parts.Add(rendered.Text);
                column = baseColumn + rendered.Used;
                used = rendered.Used;
            }
        }
        if (node.Tail is not null)
        {
            parts.Add(Term.I(tailSeparator));
            if (node.Tail.Dots == 3 && node.Tail.Text == ProcLibReportPrettyNames.Ellipsis)
                parts.AddRange(Cons.Items(node.Tail.Write()));
            else if (node.Tail.Atomic && node.Tail.Length + 1 < width - column - lastDepth - 1 &&
                node.Tail.Length + 1 + used + lastDepth + 1 <= maximum)
                return Cons.From(parts, node.Tail.Write());
            else
            {
                parts.Add(Term.I('\n'));
                parts.Add(indent);

                return Cons.From(parts, Pretty(
                    node.Tail,
                    checking ? column : baseColumn,
                    width,
                    maximum,
                    tagIndent,
                    indent,
                    lastDepth + 1,
                    0,
                    checking
                ));
            }
        }

        return Cons.From(parts);
    }

    private static bool FitsTail(
        int length,
        int column,
        int width,
        int maximum,
        int lastDepth,
        int used
    )
        => lastDepth == 0
            ? length + 1 < width - column && used + length + 1 <= maximum
            : length < width - column - lastDepth && used + length + lastDepth <= maximum;

    private static (Term Text, int Used) PrettyElement(
        ProcLibReportPrettyNode node,
        int column,
        int width,
        int maximum,
        int tagIndent,
        Term indent,
        int lastDepth,
        int used,
        bool checking
    )
    {
        if (node.Atomic && Fits(
            node,
            column,
            width,
            maximum,
            lastDepth,
            used
        ))
            return (node.Write(), node.Length);

        return (Pretty(
            node,
            column,
            width,
            maximum,
            tagIndent,
            indent,
            lastDepth,
            used,
            checking
        ), width);
    }

    private static Term PrettyPairs(
        ProcLibReportPrettyNode node,
        int baseColumn,
        int width,
        int maximum,
        int tagIndent,
        Term indent,
        int lastDepth,
        int used,
        bool checking
    )
    {
        if (node.Elements.Count == 0)
            return node.Tail?.Write() ?? Nil.Value;

        var parts = new List<Term>();
        int column = baseColumn;
        for (int index = 0; index < node.Elements.Count; index++)
        {
            var pair = node.Elements[index];
            int pairDepth = index + 1 < node.Elements.Count ? 0 : lastDepth + 1;
            bool atomic = pair.Kind == ProcLibReportPrettyKind.Pair
                ? pair.Elements.All(element => element.Atomic)
                : pair.Elements[0].Atomic;
            if (index > 0)
            {
                parts.Add(Term.I(','));
                if (atomic && FitsTail(
                    pair.Length + 1,
                    column,
                    width,
                    maximum,
                    pairDepth,
                    used
                ))
                {
                    parts.Add(pair.Write());
                    column += pair.Length + 1;
                    used += pair.Length + 1;
                    continue;
                }
                parts.Add(Term.I('\n'));
                parts.Add(indent);
                column = baseColumn;
                used = 0;
            }
            if (Fits(
                pair,
                column,
                width,
                maximum,
                pairDepth,
                used
            ))
            {
                parts.Add(pair.Write());
                int consumed = atomic ? pair.Length : width;
                column += consumed;
                used = (checking || node.Kind == ProcLibReportPrettyKind.Record) && index == 0 ? used + consumed : consumed;
                continue;
            }
            if (pair.Kind == ProcLibReportPrettyKind.Pair)
            {
                int valueIndent = tagIndent > 0 ? tagIndent : 4;
                var valueIndentation = Indent(valueIndent, indent);
                parts.Add(Cons.From(
                    [
                        Pretty(
                            pair.Elements[0],
                            baseColumn,
                            width,
                            maximum,
                            tagIndent,
                            indent,
                            pairDepth,
                            used,
                            checking
                        ),
                        Term.String(ProcLibReportPrettyNames.LineAssociation),
                        valueIndentation
                    ],
                    Pretty(
                        pair.Elements[1],
                        baseColumn + valueIndent,
                        width,
                        maximum,
                        tagIndent,
                        valueIndentation,
                        pairDepth,
                        0,
                        checking
                    )
                ));
            }
            else
            {
                int prefix = pair.Text.EnumerateRunes().Count() + 3;
                var placement = RecordIndent(
                    prefix,
                    tagIndent,
                    baseColumn,
                    indent,
                    used + prefix,
                    width,
                    maximum,
                    checking
                );
                string separator = placement.Separator is Nil ? ProcLibReportPrettyNames.FieldAssignment : ProcLibReportPrettyNames.LineFieldAssignment;
                parts.Add(Cons.From(
                    [pair.RawName ?? Term.String(pair.Text), Term.String(separator), placement.Separator],
                    Pretty(
                        pair.Elements[0],
                        placement.Column,
                        width,
                        maximum,
                        tagIndent,
                        placement.Indent,
                        pairDepth,
                        placement.Used,
                        checking
                    )
                ));
            }
            column = baseColumn + width;
            used = width;
        }
        if (node.Tail is not null)
        {
            parts.Add(Term.I(','));
            parts.AddRange(Cons.Items(node.Tail.Write()));
        }

        return Cons.From(parts);
    }

    private static (int Column, Term Indent, Term Separator, int Used) RecordIndent(
        int prefix,
        int tagIndent,
        int column,
        Term indent,
        int used,
        int width,
        int maximum,
        bool checking
    )
    {
        bool newline = tagIndent > 0 && prefix > tagIndent;
        int increment = newline ? tagIndent : prefix;
        int nextColumn = column + increment;
        CheckColumn(checking, maximum + nextColumn <= width || nextColumn <= width / 2);
        Term nextIndent = Indent(increment, indent);

        return (nextColumn, nextIndent, newline ? new Cons(Term.I('\n'), nextIndent) : Nil.Value, newline ? 0 : used);
    }

    private static Term PrettyBinary(
        ProcLibReportPrettyNode node,
        int column,
        int width,
        int maximum,
        Term indent,
        int lastDepth,
        int used
    )
    {
        int capacity = Math.Max(
            ProcLibReportPrettyParameters.MinimumBinaryLineCapacity,
            Math.Min(width - column, maximum - ProcLibReportPrettyParameters.BinaryDelimiters - used) - lastDepth
        );
        int remaining = capacity;
        var parts = new List<Term>();
        for (int index = 0; index < node.Elements.Count; index++)
        {
            var segment = node.Elements[index];
            bool hasComma = index + 1 < node.Elements.Count || node.Tail is not null;
            int length = segment.Length + (hasComma ? 1 : 0);
            if (remaining - length < 0)
            {
                parts.Add(Term.I('\n'));
                parts.Add(indent);
                remaining = capacity;
            }
            parts.Add(segment.Write());
            if (hasComma)
                parts.Add(Term.I(','));
            remaining -= length;
        }
        if (node.Tail is not null)
        {
            if (node.Tail.Length > remaining)
            {
                parts.Add(Term.I('\n'));
                parts.Add(indent);
            }
            parts.Add(node.Tail.Write());
        }

        return Term.List(
            Term.I('<'),
            Term.I('<'),
            Cons.From(parts),
            Term.I('>'),
            Term.I('>')
        );
    }

    private static void CheckColumn(bool checking, bool fits)
    {
        if (checking && !fits)
            throw new ProcLibReportPrettyIndentException();
    }

    private static Term Spaces(int count)
    {
        if (count == 0)
            return Nil.Value;
        if (count == 2)
            return Term.List(Term.I(' '), Term.I(' '));
        if (count == 3)
            return Term.List(Term.I(' '), Term.I(' '), Term.I(' '));

        Term half = Spaces(count / 2);

        return count % 2 == 0 ? new Cons(half, half) : new Cons(Term.I(' '), new Cons(half, half));
    }

    private static Term Indent(int count, Term indent)
    {
        if (count == 1)
            return new Cons(Term.I(' '), indent);
        if (count == 4)
        {
            Term two = Term.List(Term.I(' '), Term.I(' '));

            return new Cons(two, new Cons(two, indent));
        }

        return new Cons(Spaces(count), indent);
    }
}
