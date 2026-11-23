using System.Collections;
using System.Numerics;
using System.Text;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedEffectArgumentsChecks
{
    internal const string TestName = "compiler/beam/rematerializes-private-singletons-after-call-effects";
    private const string Fixture = "beam_closed_effect_arguments_probe.erl";
    private const string UnexpectedReference = "Expected a static function reference to preserve its directly forwarded accumulator.";
    private const string UnexpectedLiveRoots = "Expected closed effect arguments to be rematerialized without changing open entries.";

    internal const string Source = """
        -module(beam_closed_effect_arguments_probe).
        -export([list_run/1, count_run/1, escaped_run/1, escaped/0, open_run/2, forwarded/1, matched/2, read/1, tail/1, reference/1, dynamic_reference/2]).

        list_run(Input) -> list_body(Input, []).
        count_run(Input) -> count_body(Input, 47).
        escaped_run(Input) -> escaped_body(Input, 71).
        escaped() -> fun escaped_body/2.
        open_run(Input, Count) -> open_body(Input, Count).

        list_body(Input, Options) -> [?MODULE:read(Input)|?MODULE:tail(Options)].
        count_body(Input, Limit) -> ?MODULE:read(Input), ?MODULE:tail(Limit).
        escaped_body(Input, Limit) -> ?MODULE:read(Input), ?MODULE:tail(Limit).
        open_body(Input, Limit) -> ?MODULE:read(Input), ?MODULE:tail(Limit).

        forwarded(Input) -> forward_body(Input, []).
        forward_body(Input, Options) -> ?MODULE:tail(matched(Input, Options)).
        matched(Input, Options) -> ?MODULE:read({Input, Options}).

        reference(Rows) -> reference_body(Rows, []).
        reference_body(Rows, Acc) -> lists:foldl(fun lists:reverse/2, Acc, Rows).

        dynamic_reference(Rows, Module) -> dynamic_reference_body(Rows, [], Module).
        dynamic_reference_body(Rows, Acc, Module) -> lists:foldl(fun Module:reverse/2, Acc, Rows).

        read(fail) -> self() ! {read,fail}, erlang:error(probe_failure);
        read(Value) -> self() ! {read,Value}, Value.
        tail(Value) -> self() ! {tail,Value}, Value.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static void Run()
    {
        (object[] instructions, string?[] atoms) = ReadInstructions(CompileFixture());
        if (!HasMove("list_body", "nil", 0) || !HasMove("count_body", "integer", 47)
            || HasMove("escaped_body", "integer", 71) || HasMove("open_body", "integer", 71)
            || HasMove("forward_body", "nil", 0) || !HasEntryAllocate("forward_body", 2))
        {
            throw new InvalidOperationException(UnexpectedLiveRoots);
        }

        if (HasMove("reference_body", "nil", 0))
        {
            throw new InvalidOperationException(UnexpectedReference);
        }

        bool HasMove(string name, string kind, int value) => Function(name)
            .Where(instruction => Opcode(instruction) == Move)
            .Select(instruction => Items(instruction, "Operands").First())
            .Any(operand => Equals(Property(operand, "Kind"), kind) && Value(operand) == value);

        bool HasEntryAllocate(string name, int live) => Function(name).Any(instruction =>
            Opcode(instruction) == Allocate && Value(Items(instruction, "Operands").ElementAt(1)) == live);

        IEnumerable<object> Function(string name)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Items(instruction, "Operands").ElementAt(1))] == name
            );
            if (start < 0)
            {
                throw new InvalidOperationException($"Missing fixture function {name}.");
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo);
        }
    }

    private static (object[] Instructions, string?[] Atoms) ReadInstructions(ReadOnlyMemory<byte> image)
    {
        // Read the code without decoding the static external function in the literal table.
        object cursor = New("BeamCursor", image[12..]);
        var chunks = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal);
        while ((int)Property(cursor, "Remaining") != 0)
        {
            var id = (ReadOnlyMemory<byte>)Call(cursor, "Read", 4);
            int size = (int)Call(cursor, "ReadInt32");
            chunks.Add(Encoding.ASCII.GetString(id.Span), (ReadOnlyMemory<byte>)Call(cursor, "Read", size));
            Call(cursor, "Read", (4 - size % 4) % 4);
        }

        object header = Call(Type("BeamStructureReader"), "ReadCode", chunks["Code"]);
        var atoms = (IReadOnlyList<string?>)Call(Type("BeamStructureReader"), "ReadAtoms", chunks["AtU8"]);
        object literalHeader = New("BeamCursor", chunks["LitT"]);
        int capacity = (int)Call(literalHeader, "ReadInt32");
        var table = (ReadOnlyMemory<byte>)Call(literalHeader, "Read", Property(literalHeader, "Remaining"));
        if (capacity != 0)
        {
            table = (ReadOnlyMemory<byte>)Call(
                Type("BeamLiteralReader"),
                "Decompress",
                table,
                capacity
            );
        }

        int literalCount = (int)Call(New("BeamCursor", table), "ReadInt32");
        object instructions = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            atoms.Count,
            literalCount,
            null,
            null
        );

        return (((IEnumerable)instructions).Cast<object>().ToArray(), atoms.ToArray());
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
