using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamIntegerTaggedAliasResultChecks
{
    internal const string TestName = "compiler/beam/selects-disjoint-integer-tags-and-preserves-whole-tuples";
    private const string Fixture = "beam_integer_tagged_alias_result.erl";
    private const string WrongLayout = "Integer-tagged tuple aliases did not have the expected selector and fallback.";
    private const string WrongValue = "Integer-tagged tuple aliases changed their result.";

    private const string Source = """
        -module(beam_integer_tagged_alias_result).
        -export([two/1, four/1, overlapping/1, distinct_usage/1]).
        two(Value) ->
            case Value of
                Pair = {20,_} -> {2,Pair};
                Pair = {19,_} -> {1,Pair};
                Other -> {3,Other}
            end.
        four(Value) ->
            case Value of
                Pair = {3,_} -> {30,Pair};
                Pair = {1,_} -> {10,Pair};
                Pair = {4,_} -> {40,Pair};
                Pair = {2,_} -> {20,Pair};
                Other -> {0,Other}
            end.
        overlapping(Value) ->
            case Value of
                Pair = {19,_} -> {1,Pair};
                Pair = {19,_} -> {2,Pair};
                Other -> {3,Other}
            end.
        distinct_usage(Value) ->
            case Value of
                {19,Payload} -> {1,Payload};
                Pair = {20,_} -> {2,Pair};
                Other -> {3,Other}
            end.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        var module = BeamModule.Prepare(image, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term payload = Term.A("payload");
            Term nineteen = Term.Tuple(Term.I(19), payload);
            Term twenty = Term.Tuple(Term.I(20), payload);
            Term unknown = Term.Tuple(Term.I(21), payload);
            Term wrongArity = Term.Tuple(Term.I(19), payload, payload);
            await Expect(
                "two",
                nineteen,
                1,
                nineteen
            );
            await Expect(
                "two",
                twenty,
                2,
                twenty
            );
            await Expect(
                "two",
                unknown,
                3,
                unknown
            );
            await Expect(
                "two",
                wrongArity,
                3,
                wrongArity
            );
            await Expect(
                "two",
                payload,
                3,
                payload
            );
            await Expect(
                "overlapping",
                nineteen,
                1,
                nineteen
            );
            await Expect(
                "distinct_usage",
                nineteen,
                1,
                payload
            );
            for (int tag = 1; tag <= 4; tag++)
            {
                Term input = Term.Tuple(Term.I(tag), payload);
                await Expect(
                    "four",
                    input,
                    tag * 10,
                    input
                );
            }

            return Nil.Value;

            async Task Expect(
                string function,
                Term input,
                int prefix,
                Term output
            )
            {
                if (!Term.Tuple(Term.I(prefix), output).Equals(await module.Invoke(context, function, input)))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }
        });

        if (!HasSingleTupleSelector(image, "two") || !HasSingleTupleSelector(image, "four")
            || HasSingleTupleSelector(image, "overlapping") || HasSingleTupleSelector(image, "distinct_usage"))
        {
            throw new InvalidOperationException(WrongLayout);
        }
    }

    private static bool HasSingleTupleSelector(byte[] image, string function)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var name, var arity]
                && atoms[Value(name)] == function && Value(arity) == 1
        );
        if (start < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        byte[] ops = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).Select(Opcode).ToArray();

        return ops.Count(op => op == IsTuple) == 1 && ops.Count(op => op == TestArity) == 1
            && ops.Count(op => op == GetTupleElement) == 1 && ops.Count(op => op == SelectValue) == 1
            && !ops.Contains(ExactEqualTest);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
