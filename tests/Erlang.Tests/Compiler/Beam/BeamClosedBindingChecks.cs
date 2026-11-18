using System.Reflection;
using System.Text;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedBindingChecks
{
    internal const string TestName = "compiler/beam/closed-literal-bindings-cross-calls-as-literals";
    private const string Module = "beam_closed_binding";
    private const string Source = """
        -module(beam_closed_binding).
        -export([straight/1, choose/1, pattern/1, guard/1, shadow/1]).

        straight(Value) ->
            Closed = {tag, 7},
            Result = keep(Value),
            {Closed, Result}.

        choose(Value) ->
            Closed = [a, b],
            case keep({Value, Closed}) of
                {take, Result} -> Result;
                _ -> missing
            end.

        pattern(Value) ->
            Closed = {tag, 7},
            case Value of
                Closed -> same;
                _ -> different
            end.

        guard(Value) ->
            Closed = [a, b],
            case Value of
                Other when Other =:= Closed -> same;
                _ -> different
            end.

        shadow(Value) ->
            Closed = {tag, 7},
            case Value of
                take -> F = fun(Closed) -> Closed end, F(Value);
                _ -> Closed
            end.

        keep(Value) -> Value.
        """;

    internal static async Task Run()
    {
        CheckTailArgumentOrder();

        ModuleDefinition module = new Parser(Source).ParseModule();
        FunctionDefinition straight = Find("straight");
        FunctionDefinition choose = Find("choose");
        if (Fold(straight).Clauses[0].Body is not Expr.Sequence
            {
                Items: [Expr.Match, Expr.Tuple { Items: [Expr.Literal { Value: TupleTerm }, Expr.Variable] }]
            }
            || Fold(choose).Clauses[0].Body is not Expr.Case
            {
                Value: Expr.Call { Arguments: [Expr.Tuple { Items: [Expr.Variable, Expr.Literal { Value: Cons }] }] }
            })
        {
            throw new InvalidOperationException(BeamClosedBindingDiagnostics.MissingFold);
        }

        foreach (string name in new[] { "pattern", "guard", "shadow" })
        {
            FunctionDefinition function = Find(name);
            if (!ReferenceEquals(function, Fold(function)))
            {
                throw new InvalidOperationException(BeamClosedBindingDiagnostics.BindingChanged);
            }
        }

        var image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(Source, Module + ".erl"));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term atom = Term.A("value");
            Term tuple = Term.Tuple(Term.A("tag"), Term.I(7));
            Term list = Term.List(Term.A("a"), Term.A("b"));
            if (!(await image.Invoke(context, "straight", atom)).Equals(Term.Tuple(tuple, atom))
                || !(await image.Invoke(context, "choose", Term.A("take"))).Equals(list)
                || !(await image.Invoke(context, "choose", atom)).Equals(Term.A("missing"))
                || !(await image.Invoke(context, "pattern", tuple)).Equals(Term.A("same"))
                || !(await image.Invoke(context, "pattern", atom)).Equals(Term.A("different"))
                || !(await image.Invoke(context, "guard", list)).Equals(Term.A("same"))
                || !(await image.Invoke(context, "guard", atom)).Equals(Term.A("different"))
                || !(await image.Invoke(context, "shadow", Term.A("take"))).Equals(Term.A("take"))
                || !(await image.Invoke(context, "shadow", atom)).Equals(tuple))
            {
                throw new InvalidOperationException(BeamClosedBindingDiagnostics.ChangedResult);
            }

            return Nil.Value;
        });

        FunctionDefinition Find(string name) => module.Functions.Single(function => function.Name == name);
    }

    private static void CheckTailArgumentOrder()
    {
        const string module = "beam_closed_tail_order";
        string file = module + ".erl";
        string source = Encoding.UTF8.GetString(BeamModuleEmissionChecks.Read(file));
        byte[] reference = BeamModuleEmissionChecks.Read(module + ".beam");
        byte[] compiled = CodeGeneration.CompileBeamModule(source, file);
        if (!compiled.AsSpan().SequenceEqual(reference))
        {
            throw new InvalidOperationException(BeamClosedBindingDiagnostics.TailArgumentOrder);
        }

        _ = BeamModule.Prepare(compiled);
    }

    private static FunctionDefinition Fold(FunctionDefinition function)
    {
        System.Type compiler = typeof(CodeGeneration).Assembly.GetType(BeamClosedBindingDiagnostics.Compiler, throwOnError: true)!;
        System.Type emitter = compiler.GetNestedType(BeamClosedBindingDiagnostics.Emitter, BindingFlags.NonPublic)!;

        return (FunctionDefinition)Call(emitter, BeamClosedBindingDiagnostics.Fold, function);
    }
}
