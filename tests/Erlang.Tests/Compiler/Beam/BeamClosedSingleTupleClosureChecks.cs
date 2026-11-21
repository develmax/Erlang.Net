using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedSingleTupleClosureChecks
{
    internal const string TestName = "compiler/beam/compares-the-whole-literal-before-returning-a-closure";
    private const string Fixture = "beam_closed_single_tuple_closure_probe.erl";
    private const string IncorrectComparison = "The closed selector did not retain the exact literal guard.";
    private const string IncorrectResult = "The literal or captured selection changed its behavior.";
    private const string Source = """
        -module(beam_closed_single_tuple_closure_probe).
        -export([select/1, select_captured/1, invoke/3]).
        select({1,2,840,exact}) ->
            fun(X) -> {exact,X} end;
        select(_) ->
            fun(X) -> {fallback,X} end.
        select_captured({1,2,840,exact}) ->
            fun(X) -> {exact,X} end;
        select_captured(Selector) ->
            fun(X) -> {captured,Selector,X} end.
        invoke(closed, Key, Value) ->
            Callback = select(Key),
            Callback(Value);
        invoke(captured, Key, Value) ->
            Callback = select_captured(Key),
            Callback(Value).
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        int entry = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands").ToArray() is [_, var atom, var arity]
                && atoms[Value(atom)] == "select" && Value(arity) == 1
        );
        if (entry < 0)
        {
            throw new InvalidOperationException(IncorrectComparison);
        }

        byte[] selection = instructions.Skip(entry + 1).Select(Opcode).TakeWhile(opcode => opcode != FunctionInfo).ToArray();
        if (selection.Count(opcode => opcode == ExactEqualTest) != 1 || selection.Contains(Allocate)
            || selection.Contains(GetTupleElement) || selection.Count(opcode => opcode == MakeFun) != 2)
        {
            throw new InvalidOperationException(IncorrectComparison);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term exact = Term.Tuple(
                Term.I(1),
                Term.I(2),
                Term.I(840),
                Term.A("exact")
            );
            Term unequal = Term.Tuple(
                Term.I(1),
                Term.I(2),
                new FloatTerm(840.0),
                Term.A("exact")
            );
            Term payload = Term.A("payload");
            if (!Term.Tuple(Term.A("exact"), payload).Equals(await module.Invoke(
                context,
                "invoke",
                Term.A("closed"),
                exact,
                payload
            ))
                || !Term.Tuple(Term.A("fallback"), payload).Equals(await module.Invoke(
                    context,
                    "invoke",
                    Term.A("closed"),
                    unequal,
                    payload
                ))
                || !Term.Tuple(Term.A("captured"), unequal, payload).Equals(await module.Invoke(
                    context,
                    "invoke",
                    Term.A("captured"),
                    unequal,
                    payload
                )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            return Nil.Value;
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
