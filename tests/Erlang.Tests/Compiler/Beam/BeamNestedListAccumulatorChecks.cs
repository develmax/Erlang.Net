using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNestedListAccumulatorChecks
{
    internal const string TestName = "compiler/beam/keeps-list-suffixes-across-nested-pattern-failure";
    private const string Fixture = "beam_nested_list_accumulator_probe.erl";
    private const string Entry = "entry";
    private const string Unpack = "unpack";
    private const string Exports = "-export([entry/1]).";
    private const string Conversion = "binary_to_list(Input)";
    private const string IncorrectValue = "Nested patterns changed scalar, fallback, or failure results.";
    private const string IncorrectLayout = "Only proved proper-list callers may omit the final nil test.";

    private const string Source = """
        -module(beam_nested_list_accumulator_probe).
        -export([entry/1]).

        entry(Input) ->
            try wrapper(binary_to_list(Input))
            catch error:function_clause -> rejected
            end.
        wrapper(Data) -> unpack(Data, [suffix]).

        unpack([], Acc) -> lists:reverse(Acc);
        unpack([0, 0, 0, Scalar | Tail], Acc) -> unpack(Tail, [Scalar | Acc]);
        unpack([A, B, C, D | Tail], Acc) -> unpack(Tail, [{A, B, C, D} | Acc]).
        """;

    internal static async Task Run()
    {
        byte[] closed = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] unknown = CodeGeneration.CompileBeamModule(Source.Replace(Conversion, "Input", StringComparison.Ordinal), Fixture);
        byte[] exposed = CodeGeneration.CompileBeamModule(Source.Replace(Exports, "-export([entry/1, unpack/2]).", StringComparison.Ordinal), Fixture);
        var dependencies = new BeamDependencySelection(["lists"]);
        var closedModule = BeamModule.Prepare(closed, dependencies);
        var unknownModule = BeamModule.Prepare(unknown, dependencies);
        Term suffix = Term.A("suffix");
        Term rejected = Term.A("rejected");
        Term tuple = Term.Tuple(
            Term.I(0),
            Term.I(0),
            Term.I(3),
            Term.I(4)
        );
        Term expected = Term.List(suffix, Term.I(65), tuple);

        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            if (!expected.Equals(await closedModule.Invoke(context, Entry, new BitString([0, 0, 0, 65, 0, 0, 3, 4])))
                || !Term.List(suffix).Equals(await closedModule.Invoke(context, Entry, new BitString([])))
                || !rejected.Equals(await closedModule.Invoke(context, Entry, new BitString([0, 0, 0])))
                || !rejected.Equals(await closedModule.Invoke(context, Entry, new BitString([0, 0, 3])))
                || !rejected.Equals(await closedModule.Invoke(context, Entry, new BitString([1])))
                || !rejected.Equals(await unknownModule.Invoke(context, Entry, Term.A("not_a_list"))))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            return Nil.Value;
        });

        if (HasNilTest(closed) || !HasNilTest(unknown) || !HasNilTest(exposed))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }
    }

    private static bool HasNilTest(byte[] image)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var name, var arity]
                && atoms[Value(name)] == Unpack && Value(arity) == 2
        );
        if (start < 0)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Any(instruction => Opcode(instruction) == IsNil);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
