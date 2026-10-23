namespace Erlang;

internal static class ProplistSourceLocation
{
    // OTP-29.1.1 proplists.erl: the get_value/3 function-clause entry.
    internal static Term GetValue { get; } = Term.List(
        Term.Tuple(Term.A(ProplistSourceLocationNames.File), Term.String(ProplistSourceLocationNames.FileName)),
        Term.Tuple(Term.A(ProplistSourceLocationNames.Line), Term.I(ProplistSourceLocationNames.GetValueLine))
    );
}
