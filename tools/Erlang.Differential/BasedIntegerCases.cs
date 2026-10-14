namespace Erlang.Differential;

public static class BasedIntegerCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();
        foreach (var contract in new (string Name, string Expression, string Expected)[]
        {
            ("binary", "2#101010", "42"),
            ("maximum-base", "36#z", "35"),
            ("lowercase", "16#af", "175"),
            ("uppercase", "16#AF", "175"),
            ("base-and-digit-separators", "1_6#F_F", "255"),
            ("binary-separators", "2#1_0_1", "5"),
            ("octal", "8#177", "127"),
            ("large-positive", "16#FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", "340282366920938463463374607431768211455"),
            ("unary-negative-surrogate", "-16#D800", "-55296"),
            ("non-power-base", "3#102", "11"),
            ("decimal-base", "10#1_2_3", "123"),
            ("binary-segment", "<<16#FF:8,2#1:8>>", "<<255,1>>")
        })
            cases.Add(new(
                "based-integer-" + contract.Name,
                "-module(based_integer_contract). -export([run/0]). run()->" + contract.Expected + " = " + contract.Expression + ",done.",
                Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A("done"))
            ));

        return cases.AsReadOnly();
    }
}
