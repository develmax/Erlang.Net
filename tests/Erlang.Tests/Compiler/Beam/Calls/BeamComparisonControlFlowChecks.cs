using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamComparisonControlFlowChecks
{
    internal const string TestName = "compiler/beam/branches-directly-on-exhaustive-comparisons";
    private const string WrongImage = "An exhaustive comparison case must compile to the same direct tests as its guard form.";
    private const string WrongScope = "The comparison control-flow rewrite must leave computed or incomplete cases intact.";
    private const string Fixture = "beam_compare_control_flow.erl";
    private const string Source = """
        -module(beam_compare_control_flow).
        -export([relation/2, equality/2]).
        relation(A, B) -> case A > B of true -> big; false -> small end.
        equality(A, B) -> case A =:= B of false -> {small,A}; true -> {big,B} end.
        """;

    internal static Task Run()
    {
        string guards = Source
            .Replace("case A > B of true -> big; false -> small end", "if A > B -> big; true -> small end", StringComparison.Ordinal)
            .Replace(
                "case A =:= B of false -> {small,A}; true -> {big,B} end",
                "if A =:= B -> {big,B}; true -> {small,A} end",
                StringComparison.Ordinal
            );
        byte[] actual = CodeGeneration.CompileBeamModule(Source, Fixture);
        if (!actual.SequenceEqual(CodeGeneration.CompileBeamModule(guards, Fixture)))
        {
            throw new InvalidOperationException(WrongImage);
        }

        var body = new Expr.Literal(Term.A("result"));
        var yes = new Clause([new Pattern.Literal(Term.A("true"))], null, body);
        var no = new Clause([new Pattern.Literal(Term.A("false"))], null, body);
        var safe = new Expr.Binary(ErlangOperators.ExactEqual, new Expr.Variable("A"), new Expr.Variable("B"));
        var computed = safe with { Left = new Expr.Call("erlang", "hd", [new Expr.Variable("A")]) };
        var function = new FunctionDefinition(
            "negative",
            2,
            [new Clause([], null, new Expr.Tuple([new Expr.Case(computed, [yes, no]), new Expr.Case(safe, [yes])]))]
        );
        var pair = (Expr.Tuple)BeamComparisonControlFlow.Rewrite([function])[0].Clauses[0].Body;
        if (pair.Items.Any(item => item is not Expr.Case))
        {
            throw new InvalidOperationException(WrongScope);
        }

        var loop = new Expr.ListComprehension([new Expr.Case(safe, [yes, no])], []);
        var loopOwner = function with { Clauses = [new Clause([], null, loop)] };
        if (BeamComparisonControlFlow.Rewrite([loopOwner])[0].Clauses[0].Body is not
            Expr.ListComprehension { Items: [Expr.Case] })
        {
            throw new InvalidOperationException(WrongScope);
        }

        _ = BeamModule.Prepare(actual);

        return Task.CompletedTask;
    }
}
