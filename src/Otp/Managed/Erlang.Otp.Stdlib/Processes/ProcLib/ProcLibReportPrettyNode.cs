using System.Text;

namespace Erlang.Otp;

internal sealed class ProcLibReportPrettyNode
{
    public ProcLibReportPrettyKind Kind { get; init; }
    public string Text { get; init; } = string.Empty;
    public Term? Raw { get; init; }
    public Term? RawName { get; init; }
    public IReadOnlyList<ProcLibReportPrettyNode> Elements { get; init; } = [];
    public ProcLibReportPrettyNode? Tail { get; init; }
    public bool Tagged { get; init; }
    public int Length { get; init; }
    public int Dots { get; init; }

    public Func<int, int, ProcLibReportPrettyNode>? More { get; init; }

    public bool Atomic => Kind == ProcLibReportPrettyKind.Text;

    public static ProcLibReportPrettyNode Leaf(string text, int dots = 0, Term? raw = null) => new()
    {
        Kind = ProcLibReportPrettyKind.Text,
        Text = text,
        Length = text.EnumerateRunes().Count(),
        Dots = dots,
        Raw = raw
    };

    public Term Write()
    {
        if (Raw is not null)
            return Raw;

        switch (Kind)
        {
            case ProcLibReportPrettyKind.Text:
                return Term.String(Text);
            case ProcLibReportPrettyKind.Pair:
                return Term.List(Elements[0].Write(), Term.String(ProcLibReportPrettyNames.Association), Elements[1].Write());
            case ProcLibReportPrettyKind.Field:
                return new Cons(RawName ?? Term.String(Text), new Cons(Term.String(ProcLibReportPrettyNames.FieldAssignment), Elements[0].Write()));
            case ProcLibReportPrettyKind.Binary:
                return Term.List(
                    Term.I('<'),
                    Term.I('<'),
                    Body(','),
                    Term.I('>'),
                    Term.I('>')
                );
            case ProcLibReportPrettyKind.Map:
                return Term.List(
                    Term.I('#'),
                    Term.I('{'),
                    Body(','),
                    Term.I('}')
                );
            case ProcLibReportPrettyKind.Record:
                return Term.List(
                    RawName ?? Term.String(Text),
                    Term.I('{'),
                    Body(','),
                    Term.I('}')
                );
            case ProcLibReportPrettyKind.Tuple:
                return Term.List(Term.I('{'), Body(','), Term.I('}'));
            case ProcLibReportPrettyKind.List:
                return Term.List(Term.I('['), Body('|'), Term.I(']'));
            default:
                throw new NotSupportedException(ProcLibReportPrettyNames.Unsupported);
        }
    }

    private Term Body(char tailSeparator)
    {
        if (Elements.Count == 0)
            return Tail?.Write() ?? Nil.Value;

        var parts = new List<Term>();
        for (int index = 0; index < Elements.Count; index++)
        {
            if (index != 0)
                parts.Add(Term.I(','));
            if (Kind == ProcLibReportPrettyKind.Binary && Elements[index].Text.Contains(':'))
            {
                int separator = Elements[index].Text.IndexOf(':');
                parts.Add(Term.String(Elements[index].Text[..separator]));
                parts.Add(Term.I(':'));
                parts.Add(Term.String(Elements[index].Text[(separator + 1)..]));
            }
            else
                parts.Add(Elements[index].Write());
        }
        if (Tail is not null)
        {
            parts.Add(Term.I(tailSeparator));
            if (Kind != ProcLibReportPrettyKind.Binary && Tail.Dots == 3 && Tail.Text == ProcLibReportPrettyNames.Ellipsis)
                parts.AddRange(Cons.Items(Tail.Write()));
            else
                parts.Add(Tail.Write());
        }

        return Cons.From(parts);
    }
}
