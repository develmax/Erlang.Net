using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamBoundedNameTableDecodeChecks
{
    internal const string TestName = "compiler/beam/projects-a-bounded-name-table-only-with-closed-entries";
    private const string Fixture = "beam_bounded_name_table_decode_probe.erl";
    private const string Entry = "entry";
    private const string Decode = "decode";
    private const string ClosedExports = "-export([entry/2]).";
    private const string OpenExports = "-export([entry/2, decode/4]).";
    private const string ClosedCall = "entry(Bits, _Table) -> decode(Bits, [{first,0},{fifth,5}], 0, []).";
    private const string UnknownCall = "entry(Bits, Table) -> decode(Bits, Table, 0, []).";
    private const string MalformedCall = "entry(Bits, _Table) -> decode(Bits, [not_pair], 0, []).";
    private const string IncorrectValue = "The bounded name table changed named, skipped, or unknown bit positions.";
    private const string IncorrectLayout = "Only a closed type-proven table may omit the name tuple check.";

    private const string Source = """
        -module(beam_bounded_name_table_decode_probe).
        -export([entry/2]).

        entry(Bits, _Table) -> decode(Bits, [{first,0},{fifth,5}], 0, []).

        decode([], _, _, Output) -> lists:reverse(Output);
        decode([Bit | Tail], [{Name, Position} | Table], Position, Output) ->
            if
                Bit =:= 0 -> decode(Tail, Table, Position + 1, Output);
                true -> decode(Tail, Table, Position + 1, [Name | Output])
            end;
        decode([1 | Tail], Table, Position, Output) ->
            decode(Tail, Table, Position + 1, [{bit, Position} | Output]);
        decode([0 | Tail], Table, Position, Output) ->
            decode(Tail, Table, Position + 1, Output).
        """;

    internal static async Task Run()
    {
        byte[] closed = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] unknown = CodeGeneration.CompileBeamModule(Source.Replace(ClosedCall, UnknownCall, StringComparison.Ordinal), Fixture);
        byte[] malformed = CodeGeneration.CompileBeamModule(Source.Replace(ClosedCall, MalformedCall, StringComparison.Ordinal), Fixture);
        byte[] open = CodeGeneration.CompileBeamModule(Source.Replace(ClosedExports, OpenExports, StringComparison.Ordinal), Fixture);
        var dependencies = new BeamDependencySelection(["lists"]);
        var knownModule = BeamModule.Prepare(closed, dependencies);
        var unknownModule = BeamModule.Prepare(unknown, dependencies);
        var malformedModule = BeamModule.Prepare(malformed, dependencies);
        var exportedModule = BeamModule.Prepare(open, dependencies);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term bit = Term.A("bit");
            Term first = Term.A("first");
            Term bits = Term.List(
                Term.I(1),
                Term.I(0),
                Term.I(1),
                Term.I(0),
                Term.I(0),
                Term.I(1)
            );
            Term untyped = Term.List(Term.A("not_pair"));
            Term labeled = Term.List(first, Term.Tuple(bit, Term.I(2)), Term.A("fifth"));
            Term numbered = Term.List(Term.Tuple(bit, Term.I(0)), Term.Tuple(bit, Term.I(2)), Term.Tuple(bit, Term.I(5)));
            Term shifted = Term.List(Term.Tuple(bit, Term.I(1)), Term.Tuple(bit, Term.I(3)), Term.Tuple(bit, Term.I(6)));
            if (!labeled.Equals(await knownModule.Invoke(
                context,
                Entry,
                bits,
                untyped
            ))
                || !Term.List(Term.Tuple(bit, Term.I(1))).Equals(
                    await knownModule.Invoke(
                        context,
                        Entry,
                        Term.List(Term.I(0), Term.I(1)),
                        untyped
                    ))
                || !Term.List(first).Equals(await knownModule.Invoke(
                    context,
                    Entry,
                    Term.List(Term.I(2)),
                    untyped
                ))
                || !numbered.Equals(await unknownModule.Invoke(
                    context,
                    Entry,
                    bits,
                    untyped
                ))
                || !numbered.Equals(await malformedModule.Invoke(
                    context,
                    Entry,
                    bits,
                    Nil.Value
                ))
                || !shifted.Equals(await exportedModule.Invoke(
                    context,
                    Decode,
                    bits,
                    untyped,
                    Term.I(1),
                    Nil.Value
                )))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            return Nil.Value;
        });

        if (TestsNameTuple(closed) || !TestsNameTuple(unknown) || !TestsNameTuple(malformed) || !TestsNameTuple(open))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }
    }

    private static bool TestsNameTuple(byte[] image)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var name, var arity]
                && atoms[Value(name)] == Decode && Value(arity) == 4
        );
        if (start < 0)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Any(instruction => Opcode(instruction) == TestArity);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
