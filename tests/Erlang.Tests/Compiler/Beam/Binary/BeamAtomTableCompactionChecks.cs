using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamAtomTableCompactionChecks
{
    internal const string TestName = "compiler/beam/compacts-atoms-after-binary-instruction-selection";
    private const string Fixture = "beam_live_atom_table.erl";
    private const string ExtendedFixture = "beam_live_atom_table_with_packed_tail.erl";
    private const string TailCommand = "get_tail";
    private const string AtomRetentionFailure = "The atom table must retain packed commands and drop only unused atoms.";
    private const string OffsetFailure = "Atom offsets must cover the exact encoded atom, including packed commands and multi-byte indices.";
    private const string Source = """
        -module(beam_live_atom_table).
        -export([pack/1, wrap/1, inspect/1]).

        pack(Input) ->
            <<Head, Rest/binary>> = make(Input),
            case Head band 128 of
                128 -> <<(byte_size(Rest)+2):32,0,Head,Rest/binary>>;
                _ -> <<(byte_size(Rest)+1):32,Head,Rest/binary>>
            end.

        wrap(Tag) ->
            fun(Input) -> {Tag, pack(Input)} end.

        inspect(Binary) ->
            {erlang:byte_size(Binary), lists:reverse([alpha,beta,gamma])}.

        make(high) -> <<128,7,3>>;
        make(low) -> <<3,5,8>>;
        make(empty) -> <<>>;
        make(unaligned) -> <<5:3>>;
        make(non_binary) -> rejected;
        make(Other) -> Other.
        """;

    private const string PackedSource = """

        unpack(Binary) ->
            <<Head, Tail/binary>> = Binary,
            {Head,Tail}.
        """;

    internal static void Run()
    {
        object compact = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        if (Atoms(compact).Contains(TailCommand)
            || !Items(compact, "Instructions").Any(instruction => Opcode(instruction) == GetBinaryTail))
        {
            throw new InvalidOperationException(AtomRetentionFailure);
        }

        string packedSource = Source.Replace(
            "beam_live_atom_table",
            "beam_live_atom_table_with_packed_tail",
            StringComparison.Ordinal
        ).Replace("inspect/1", "inspect/1, unpack/1", StringComparison.Ordinal) + PackedSource;
        object packed = Code(CodeGeneration.CompileBeamModule(packedSource, ExtendedFixture));
        if (!Atoms(packed).Contains(TailCommand))
        {
            throw new InvalidOperationException(AtomRetentionFailure);
        }

        VerifyOffsets(packed);
    }

    private static void VerifyOffsets(object code)
    {
        object structure = Property(code, "Structure");
        object header = Property(structure, "Code");
        string?[] atoms = Atoms(code);
        var bytes = (ReadOnlyMemory<byte>)Property(header, "Instructions");
        object reader = Type("BeamInstructionReader");
        var locations = new List<(int Start, int End, int Atom)>();
        Action<int, int, int> record = (start, end, atom) => locations.Add((start, end, atom));
        Call(
            reader,
            "ReadWithAtomOperands",
            header,
            atoms.Length,
            Items(structure, "Literals").Count(),
            record
        );
        int[] expected = Items(code, "Instructions")
            .SelectMany(instruction => Items(instruction, "Operands"))
            .Where(operand => (string)Property(operand, "Kind") == "atom")
            .Select(operand => (int)(BigInteger)Property(operand, "Value"))
            .ToArray();
        if (!expected.SequenceEqual(locations.Select(location => location.Atom))
            || !locations.Any(location => location.End - location.Start > 1)
            || !locations.Any(location => atoms[location.Atom] == TailCommand))
        {
            throw new InvalidOperationException(OffsetFailure);
        }

        foreach (var (start, end, atom) in locations)
        {
            object cursor = New("BeamCursor", bytes[start..end]);
            object raw = Call(Type("BeamCompactReader"), "Read", cursor);
            if ((int)Property(raw, "WireTag") != AtomTag
                || (BigInteger)Property(raw, "Value") != atom
                || (int)Property(cursor, "Remaining") != 0)
            {
                throw new InvalidOperationException(OffsetFailure);
            }
        }
    }

    private static string?[] Atoms(object code) =>
        Items(Property(code, "Structure"), "Atoms").Cast<string?>().ToArray();

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
