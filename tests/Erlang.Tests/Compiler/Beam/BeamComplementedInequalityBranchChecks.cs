using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamCallConstructionProbe;

namespace Erlang.Tests;

internal static class BeamComplementedInequalityBranchChecks
{
    internal const string TestName = "compiler/beam/complements-total-inequality-at-a-two-arm-if";
    private const string Failure = "The conditional lost its comparison kind, selected effect or guard failure path.";

    internal static Task Run()
    {
        const string source = """
            -module(complemented_inequality_branch).
            -export([numeric/2, exact/2, checked/2]).
            numeric(Left, Right) ->
                _ = if Left /= Right -> probe:different();
                       true -> probe:equal()
                    end,
                probe:done().
            exact(Left, Right) ->
                _ = if Left =/= Right -> probe:different();
                       true -> probe:equal()
                    end,
                probe:done().
            checked(Tuple, Right) ->
                _ = if element(1, Tuple) /= Right -> probe:different();
                       true -> probe:equal()
                    end,
                probe:done().
            """;
        var code = new BeamCallConstructionProbe(CodeGeneration.CompileBeamModule(source, "complemented_inequality_branch.erl"));
        if (!HasComparison(
            "numeric",
            NumericEqualTest,
            "equal",
            "different"
        )
            || !HasComparison(
                "exact",
                ExactEqualTest,
                "equal",
                "different"
            )
            || !HasComparison(
                "checked",
                NumericNotEqualTest,
                "different",
                "equal"
            ))
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;

        bool HasComparison(
            string name,
            byte comparison,
            string first,
            string second
        )
        {
            object[] body = code.Body(name);
            int test = Array.FindIndex(body, instruction => Opcode(instruction) == comparison);
            int firstCall = code.CallIndex(
                body,
                "probe",
                first,
                0
            );
            int secondCall = code.CallIndex(
                body,
                "probe",
                second,
                0
            );

            return test >= 0 && firstCall > test && secondCall > firstCall;
        }
    }
}
