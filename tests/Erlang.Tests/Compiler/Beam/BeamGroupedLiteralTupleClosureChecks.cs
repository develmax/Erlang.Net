using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGroupedLiteralTupleClosureChecks
{
    internal const string TestName = "compiler/beam/keeps-captured-fallback-input-through-tuple-arity-switch";
    private const string Fixture = "beam_grouped_literal_tuple_closure_probe.erl";
    private const string IncorrectDispatch = "The literal arity dispatch did not retain its frame-free fallback path.";
    private const string IncorrectClosure = "The arity dispatch changed a selected or captured closure.";
    private const string Source = """
        -module(beam_grouped_literal_tuple_closure_probe).
        -export([select/1, select_mixed/1, invoke/3]).
        select({4,2}) ->
            fun(X) -> {small,X} end;
        select({4,2,9}) ->
            fun(X) -> {large_b,X} end;
        select({4,2,1}) ->
            fun(X) -> {large_a,X} end;
        select(Key) ->
            fun(X) -> {fallback,Key,X} end.

        select_mixed({4,2}) ->
            fun(X) -> {small,X} end;
        select_mixed({4,K,9}) ->
            fun(X) -> {mixed,K,X} end;
        select_mixed(Key) ->
            fun(X) -> {fallback,Key,X} end.

        invoke(grouped, Key, Value) ->
            Callback = select(Key),
            Callback(Value);
        invoke(mixed, Key, Value) ->
            Callback = select_mixed(Key),
            Callback(Value).
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands").ToArray() is [_, var atom, var arity]
                && atoms[Value(atom)] == "select" && Value(arity) == 1
        );
        if (start < 0)
        {
            throw new InvalidOperationException(IncorrectDispatch);
        }

        byte[] selector = instructions.Skip(start + 1).Select(Opcode).TakeWhile(opcode => opcode != FunctionInfo).ToArray();
        if (selector.Count(opcode => opcode == SelectTupleArity) != 1
            || selector.Count(opcode => opcode == SelectValue) != 1
            || selector.Contains(Allocate) || selector.Count(opcode => opcode == MakeFun) != 4)
        {
            throw new InvalidOperationException(IncorrectDispatch);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term payload = Term.A("payload");
            Term fallback = Term.Tuple(new FloatTerm(4.0), Term.I(2), Term.I(1));
            var cases = new (string Type, Term Key, Term Expected)[]
            {
                ("grouped", Term.Tuple(Term.I(4), Term.I(2)), Term.Tuple(Term.A("small"), payload)),
                ("grouped", Term.Tuple(Term.I(4), Term.I(2), Term.I(1)), Term.Tuple(Term.A("large_a"), payload)),
                ("grouped", Term.Tuple(Term.I(4), Term.I(2), Term.I(9)), Term.Tuple(Term.A("large_b"), payload)),
                ("grouped", fallback, Term.Tuple(Term.A("fallback"), fallback, payload)),
                ("mixed", Term.Tuple(Term.I(4), Term.I(7), Term.I(9)), Term.Tuple(Term.A("mixed"), Term.I(7), payload))
            };
            foreach (var (type, key, expected) in cases)
            {
                if (!expected.Equals(await module.Invoke(
                    context,
                    "invoke",
                    Term.A(type),
                    key,
                    payload
                )))
                {
                    throw new InvalidOperationException(IncorrectClosure);
                }
            }

            return Nil.Value;
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
