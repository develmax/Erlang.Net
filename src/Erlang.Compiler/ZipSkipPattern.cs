// Modified: current-pattern adaptation of OTP-29.1.1 v3_core:replace_vars/skip_segments.
namespace Erlang.Compiler;

internal static class ZipSkipPattern
{
    public static Pattern Create(Pattern pattern, IReadOnlySet<string> strictVariables)
    {
        if (pattern is BitPattern)
            return Rewrite(pattern, strictVariables);

        return strictVariables.Count == 0 ? new Pattern.Any() : Collapse(Rewrite(pattern, strictVariables));
    }

    private static Pattern Rewrite(Pattern pattern, IReadOnlySet<string> strictVariables)
    {
        if (pattern is BitPattern bits)
            return new BitPattern(bits.Segments.Select(segment => new BitPatternSegment(
                segment.Value is Pattern.Variable ? segment.Value : new Pattern.Any(),
                segment.Specification.Type == BitSegmentTypes.Float ? segment.Specification with { Type = BitSegmentTypes.Integer } : segment.Specification
            )).ToArray());
        if (pattern is Pattern.Variable variable)
            return strictVariables.Contains(variable.Name) ? variable : new Pattern.Any();
        if (pattern is Pattern.Tuple tuple)
            return Collapse(new Pattern.Tuple(tuple.Items.Select(item => Rewrite(item, strictVariables)).ToArray()));
        if (pattern is Pattern.List list)
        {
            Pattern tail = list.Tail is null ? new Pattern.Literal(Nil.Value) : Rewrite(list.Tail, strictVariables);
            for (int index = list.Items.Count - 1; index >= 0; index--)
                tail = Collapse(new Pattern.List([Rewrite(list.Items[index], strictVariables)], tail));

            return tail;
        }
        if (pattern is MapPattern map)
            return Collapse(new MapPattern(map.Fields.Select(field => field with { Value = Rewrite(field.Value, strictVariables) }).Where(field => Semantics.Variables(field.Value).Any()).ToArray()));

        return pattern;
    }

    private static Pattern Collapse(Pattern pattern) => Semantics.Variables(pattern).Any() ? pattern : new Pattern.Any();
}
