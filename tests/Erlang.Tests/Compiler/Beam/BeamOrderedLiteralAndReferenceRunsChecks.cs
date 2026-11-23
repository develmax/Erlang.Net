using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOrderedLiteralAndReferenceRunsChecks
{
    internal const string TestName = "compiler/beam/preserves-the-selector-between-literal-closure-and-reference-runs";
    private const string Fixture = "beam_ordered_literal_and_reference_runs_probe.erl";
    private const string IncorrectInstructions = "The literal closure and declared reference runs have the wrong branch layout.";
    private const string IncorrectResults = "The selector fell through to the wrong function.";

    private const string Source = """
        -module(beam_ordered_literal_and_reference_runs_probe).
        -export([value/1, mapped/1]).

        mapped(Input) ->
            Values = prepare(Input),
            [map_value(Value) || Value <- Values].

        value(Key) -> (factory(Key))().
        factory({1,2,7,3}) -> fun() -> a end;
        factory({1,3,7,4}) -> fun() -> b end;
        factory(Id) when Id =:= {1,4,7,5} -> fun special/0;
        factory({2,4}) -> fun() -> c end;
        factory({2,5}) -> fun() -> d end;
        factory(_) -> fun() -> reset end.

        special() -> declared.
        prepare(Input) -> Input.
        map_value(Value) -> {mapped, Value}.
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
                && Items(instruction, "Operands").ToArray() is [_, var atom, _]
                && atoms[Value(atom)] == "factory"
        );
        int firstClosure = FindFunctionInfo("-factory/1-fun-0-");
        int comprehension = FindFunctionInfo("-mapped/1-lc$^0/1-0-");
        if (start < 0 || firstClosure < 0 || comprehension <= firstClosure)
        {
            throw new InvalidOperationException(IncorrectInstructions);
        }

        object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        if (body.Any(value => Opcode(value) == Allocate)
            || body.Count(value => Opcode(value) == SelectValue) < 2)
        {
            throw new InvalidOperationException(IncorrectInstructions);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            foreach (var (key, expected) in new (Term, string)[]
            {
                (Key(
                    1,
                    2,
                    7,
                    3
                ), "a"),
                (Key(
                    1,
                    3,
                    7,
                    4
                ), "b"),
                (Key(
                    1,
                    4,
                    7,
                    5
                ), "declared"),
                (Term.Tuple(Term.I(2), Term.I(4)), "c"),
                (Term.Tuple(Term.I(2), Term.I(5)), "d"),
                (Key(
                    1,
                    4,
                    7,
                    6
                ), "reset"),
                (Term.Tuple(Term.I(2), Term.I(6)), "reset"),
                (Term.A("malformed"), "reset")
            })
            {
                if (!Term.A(expected).Equals(await module.Invoke(context, "value", key)))
                {
                    throw new InvalidOperationException(IncorrectResults);
                }
            }

            Term mapped = Term.Tuple(Term.A("mapped"), Term.A("element"));
            Term actual = await module.Invoke(context, "mapped", Term.List(Term.A("element")));
            if (!Term.List(mapped).Equals(actual))
            {
                throw new InvalidOperationException(IncorrectResults);
            }

            return Nil.Value;
        });

        int FindFunctionInfo(string name) => Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands").ToArray() is [_, var atom, _]
                && atoms[Value(atom)] == name
        );
    }

    private static Term Key(
        int first,
        int second,
        int third,
        int fourth
    ) => Term.Tuple(
        Term.I(first),
        Term.I(second),
        Term.I(third),
        Term.I(fourth)
    );

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string member) =>
        ((IEnumerable)Property(owner, member)).Cast<object>();
}
