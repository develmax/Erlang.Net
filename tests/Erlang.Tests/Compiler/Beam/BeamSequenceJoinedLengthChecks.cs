using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSequenceJoinedLengthChecks
{
    internal const string TestName = "compiler/beam/uses-joined-length-evidence-for-sequence-annotations";
    private const string Fixture = "beam_sequence_joined_length_probe.erl";
    private const string Entry = "entry";
    private const string Collect = "collect";
    private const string OpenExports = "-export([entry/1, wrap/3]).";
    private const string ClosedExports = "-export([entry/1]).";
    private const int NumberType = 0x0028;
    private const int IntegerType = 0x0020;
    private const string IncorrectValue = "The joined length changed the sequence bytes or total.";
    private const string IncorrectLayout = "A projected length may be annotated only with a joined return fact.";

    private const string Source = """
        -module(beam_sequence_joined_length_probe).
        -export([entry/1, wrap/3]).
        entry(Values) -> collect(Values, [], 0).

        collect([], Bytes, Total) -> {lists:reverse(Bytes), Total};
        collect([Value | Rest], Bytes, Total) ->
            {Part, Size} = piece(Value, [tag]),
            collect(Rest, [Part | Bytes], Total + Size).

        piece(Value, Tags) -> wrap(Tags, Value, 1).
        wrap([], Bytes, Size) -> {Bytes, Size};
        wrap([_ | Tags], Bytes, Size) -> wrap(Tags, Bytes, Size + 1).
        """;

    internal static async Task Run()
    {
        byte[] open = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] closed = CodeGeneration.CompileBeamModule(
            Source.Replace(OpenExports, ClosedExports, StringComparison.Ordinal), Fixture
        );
        var dependencies = new BeamDependencySelection(["lists"]);
        var openModule = BeamModule.Prepare(open, dependencies);
        var closedModule = BeamModule.Prepare(closed, dependencies);
        Term input = Term.List(Term.I(127), Term.I(128));
        Term expected = Term.Tuple(input, Term.I(4));
        Term empty = Term.Tuple(Nil.Value, Term.I(0));

        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            if (!expected.Equals(await openModule.Invoke(context, Entry, input))
                || !expected.Equals(await closedModule.Invoke(context, Entry, input))
                || !empty.Equals(await openModule.Invoke(context, Entry, Nil.Value)))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            return Nil.Value;
        });

        if (ReadAddTypes(open) != (NumberType, null) || ReadAddTypes(closed) != (IntegerType, IntegerType))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }
    }

    private static (int? Left, int? Right) ReadAddTypes(byte[] image)
    {
        object code = Code(image);
        object structure = Property(code, "Structure");
        string[] atoms = Items(structure, "Atoms").Cast<string>().ToArray();
        object[] types = Items(Property(code, "Types"), "Entries");
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var name, var arity]
                && atoms[Value(name)] == Collect && Value(arity) == 3
        );
        if (start < 0)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] operation = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Where(instruction => Opcode(instruction) == GcBifTwo)
            .Select(instruction => Items(instruction, "Operands")).Single();

        return (Type(operation[3]), Type(operation[4]));

        int? Type(object operand) => Property(operand, "TypeIndex") is int index
            ? (int)Property(types[index], "TypeUnion") : null;
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
