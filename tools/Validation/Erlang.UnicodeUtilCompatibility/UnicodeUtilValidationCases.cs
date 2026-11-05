using Erlang;

internal static class UnicodeUtilValidationCases
{
    internal static IEnumerable<Term> Probes(int cp)
    {
        int[][] points = [[cp,769,65],[65,cp,66],[cp,65,66],[cp,8205,128512,65],
            [cp,2381,2325],[2325,cp,2325],[cp,4352,4449,4520,65],[4352,cp,4449,4520,65]];

        return points.Select(probe => Cons.From(probe.Select(value => (Term)Term.I(value))));
    }
}
