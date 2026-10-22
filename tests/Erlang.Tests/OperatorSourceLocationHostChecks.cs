using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.Tests;

internal static class OperatorSourceLocationHostChecks
{
    internal static async Task Run()
    {
        var assembly = typeof(OperatorSourceLocationHostChecks).Assembly;
        string resource = assembly.GetManifestResourceNames().Single(value => value.EndsWith(OperatorSourceLocationHostNames.Fixture, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);
        string source = await reader.ReadToEndAsync();
        ModuleDefinition parsed = new Parser(source).ParseModule();
        Require(parsed.DeclaredFunctions.Single(f => f.Name == OperatorSourceLocationHostNames.Unary).Clauses[0].Body is Expr.Unary { SourceLine: OperatorSourceLocationHostNames.UnaryLine });
        Require(parsed.DeclaredFunctions.Single(f => f.Name == OperatorSourceLocationHostNames.Binary).Clauses[0].Body is Expr.Binary { SourceLine: OperatorSourceLocationHostNames.BinaryLine });
        Require(new Expr.Unary(OperatorSourceLocationHostNames.Minus, new Expr.Literal(Term.I(1))).SourceLine == 0);
        Require(new Expr.Binary(OperatorSourceLocationHostNames.Remainder, new Expr.Literal(Term.I(1)), new Expr.Literal(Term.I(2))).SourceLine == 0);
        using var artifact = GeneratedModuleCompiler.Compile(source, OperatorSourceLocationHostNames.Fixture);
        Require(artifact.Definition.DeclaredFunctions.Single(f => f.Name == OperatorSourceLocationHostNames.Unary).Clauses[0].Body is Expr.Unary { SourceLine: OperatorSourceLocationHostNames.UnaryLine });
        Require(artifact.Definition.DeclaredFunctions.Single(f => f.Name == OperatorSourceLocationHostNames.Binary).Clauses[0].Body is Expr.Binary { SourceLine: OperatorSourceLocationHostNames.BinaryLine });
        await using var runtime = new ProcessRuntime();
        artifact.Definition.Register(runtime.Modules);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        foreach (int depth in new[] { -1, 0, 1, 2, 8 })
        {
            await actor.InvokeAsync(async context =>
            {
                if (depth >= 0)
                    await runtime.Modules.Call(
                        context,
                        OperatorSourceLocationHostNames.Erlang,
                        OperatorSourceLocationHostNames.SystemFlag,
                        Term.A(OperatorSourceLocationHostNames.BacktraceDepth),
                        Term.I(depth)
                    );
                Term value = await runtime.Modules.Call(context, artifact.Definition.Name, OperatorSourceLocationHostNames.Entry);
                Require(value is TupleTerm { Items.Count: 7 });
                var rows = ((TupleTerm)value).Items;
                CheckArithmetic(
                    rows[0],
                    OperatorSourceLocationHostNames.Minus,
                    OperatorSourceLocationHostNames.Unary,
                    1,
                    OperatorSourceLocationHostNames.UnaryLine,
                    depth
                );
                CheckArithmetic(
                    rows[1],
                    OperatorSourceLocationHostNames.Remainder,
                    OperatorSourceLocationHostNames.Binary,
                    2,
                    OperatorSourceLocationHostNames.BinaryLine,
                    depth
                );
                Require(rows[2] is TupleTerm { Items.Count: 3 });
                var operand = (TupleTerm)rows[2];
                Require(operand.Items[1].Equals(Term.A(OperatorSourceLocationHostNames.OperandFailure)));
                Term[] operandFrames = Cons.Items(operand.Items[2]).ToArray();
                Require(operandFrames[0].Equals(Frame(OperatorSourceLocationHostNames.Operand, 0, OperatorSourceLocationHostNames.OperandLine)));
                if (depth is not 0 and not 1)
                    Require(operandFrames[1].Equals(Frame(OperatorSourceLocationHostNames.Outer, 0, OperatorSourceLocationHostNames.OuterOperandLine)));
                Require(rows[3] is TupleTerm { Items.Count: 4 });
                var order = (TupleTerm)rows[3];
                Require(order.Items[0].Equals(Term.List(Term.A(OperatorSourceLocationHostNames.Right), Term.A(OperatorSourceLocationHostNames.Left))));
                CheckArithmetic(
                    Term.Tuple(order.Items[1], order.Items[2], order.Items[3]),
                    OperatorSourceLocationHostNames.Remainder,
                    OperatorSourceLocationHostNames.Order,
                    0,
                    OperatorSourceLocationHostNames.OrderLine,
                    depth
                );
                Require(rows[4].Equals(Term.A(OperatorSourceLocationHostNames.Other)));
                Require(rows[5].Equals(CoreModules.Boolean(false)) && rows[6].Equals(CoreModules.Boolean(true)));

                return Nil.Value;
            });
        }
    }

    private static void CheckArithmetic(
        Term value,
        string operation,
        string caller,
        int arity,
        int line,
        int depth
    )
    {
        Require(value is TupleTerm { Items.Count: 3 });
        var outcome = (TupleTerm)value;
        Require(outcome.Items[0].Equals(Term.A(OperatorSourceLocationHostNames.Error)) && outcome.Items[1].Equals(Term.A(OperatorSourceLocationHostNames.BadArithmetic)));
        Term[] frames = Cons.Items(outcome.Items[2]).ToArray();
        Require(frames.Length > 0 && frames[0] is TupleTerm { Items.Count: 4 });
        var first = (TupleTerm)frames[0];
        Require(first.Items[0].Equals(Term.A(OperatorSourceLocationHostNames.Erlang)) && first.Items[1].Equals(Term.A(operation)));
        Require(first.Items[3].Equals(Term.List(Term.Tuple(
            Term.A(OperatorSourceLocationHostNames.ErrorInfo),
            new MapTerm(new Dictionary<Term, Term>
            {
                [Term.A(OperatorSourceLocationHostNames.ModuleKey)] = Term.A(OperatorSourceLocationHostNames.Formatter)
            })
        ))));
        if (depth is 0 or 1)
            Require(frames.Length == 1);
        else
            Require(frames.Length >= 2 && frames[1].Equals(Frame(caller, arity, line)));
    }

    private static Term Frame(string function, int arity, int line) => Term.Tuple(
        Term.A(OperatorSourceLocationHostNames.Module),
        Term.A(function),
        Term.I(arity),
        Term.List(
            Term.Tuple(Term.A(OperatorSourceLocationHostNames.File), Term.String(OperatorSourceLocationHostNames.Fixture)),
            Term.Tuple(Term.A(OperatorSourceLocationHostNames.Line), Term.I(line))
        )
    );

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(OperatorSourceLocationHostNames.Failure);
    }
}
