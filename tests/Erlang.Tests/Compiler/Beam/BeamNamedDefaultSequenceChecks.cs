using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNamedDefaultSequenceChecks
{
    internal const string TestName = "compiler/beam/preserves-the-leading-named-default-and-later-record-fields";
    private const string Fixture = "beam_named_default_sequence_probe.erl";
    private const string Peer = "beam_named_default_sequence_peer";
    private const string IncorrectInstructions = "The sequence with a leading default emitted the wrong register layout.";
    private const string IncorrectResults = "The sequence with a leading default returned the wrong fields.";
    private const string InvalidForeignPair = "The encoder accepted an invalid foreign pair.";

    private const string Source = """
        -module(beam_named_default_sequence_probe).
        -export([encode/2, decode/2]).

        encode(Record, Tags) ->
            {_,Version,A,B,C,D,E} = Record,
            {VB,VL} = case default(Version) of
                true -> {[],0};
                false -> version(Version,[{named,0}],[<<2>>])
            end,
            {AB,AL} = pair(A,[]),
            {BB,BL} = beam_named_default_sequence_peer:pair(B,[<<4>>]),
            {CB,CL} = pair(C,[<<6>>]),
            {DB,DL} = case D of absent -> {<<>>,0}; _ -> pair(D,[<<8>>]) end,
            {EB,EL} = case E of absent -> {<<>>,0}; _ -> pair(E,[<<10>>]) end,
            Bytes = [VB,AB,BB,CB,DB,EB],
            Length = VL + AL + BL + CL + DL + EL,
            finish(Tags,Bytes,Length).

        decode(Input, Tags) ->
            L0 = prepare(Input,Tags),
            {Version,L1} = case L0 of
                [{2,Head}|Tail] -> {begin Number = read_field(Head,[]), rename(Number,[{named,0}]) end,Tail};
                _ -> {0,L0}
            end,
            [A0|L2] = L1,
            A = read_field(A0,[]),
            [B0|L3] = L2,
            B = beam_named_default_sequence_peer:decode(B0,[4]),
            [C0|L4] = L3,
            C = read_field(C0,[6]),
            {D,L5} = case L4 of [{8,D0}|D1] -> {read_field(D0,[]),D1}; _ -> {absent,L4} end,
            {E,L6} = case L5 of [{10,E0}|E1] -> {read_field(E0,[]),E1}; _ -> {absent,L5} end,
            case L6 of [] -> true; _ -> exit({error,{asn1,{unexpected,L6}}}) end,
            Record = {decoded,Version,A,B,C,D,E},
            Record.

        default(0) -> true; default(_) -> false.
        version(Value, _, _) -> {Value,1}.
        pair(Value, _) -> {Value,1}.
        finish(Tags, Bytes, Size) -> {Tags,Bytes,Size}.
        prepare(Value, _) -> Value.
        read_field(Value, _) -> Value.
        rename(0, _) -> named; rename(Value, _) -> Value.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        CheckFrame("encode");
        CheckFrame("decode");

        await using var runtime = new ProcessRuntime();
        Term malformed = Term.A("malformed");
        runtime.Modules.Register(
            Peer,
            "pair",
            2,
            (_, arguments) =>
                ValueTask.FromResult(arguments[0].Equals(malformed) ? malformed : Term.Tuple(arguments[0], Term.I(1)))
        );
        runtime.Modules.Register(
            Peer,
            "decode",
            2,
            (_, arguments) => ValueTask.FromResult(arguments[0])
        );
        var module = BeamModule.Prepare(image, new BeamDependencySelection([Peer]));
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term left = Term.A("left");
            Term middle = Term.A("middle");
            Term right = Term.A("right");
            Term absent = Term.A("absent");
            Term tags = Term.A("outer");
            Term empty = new BitString([]);
            Term expectedBytes = Term.List(
                Nil.Value,
                left,
                middle,
                right,
                empty,
                empty
            );
            Term expected = Term.Tuple(tags, expectedBytes, Term.I(3));
            Term actual = await module.Invoke(
                context,
                "encode",
                Record(Term.I(0), middle),
                tags
            );
            if (!expected.Equals(actual))
            {
                throw new InvalidOperationException(IncorrectResults);
            }

            Term decoded = await module.Invoke(
                context,
                "decode",
                Term.List(left, middle, right),
                tags
            );
            Term tagged = await module.Invoke(
                context,
                "decode",
                Term.List(
                    Term.Tuple(Term.I(2), Term.I(0)),
                    left,
                    middle,
                    right,
                    Term.Tuple(Term.I(10), right)
                ),
                tags
            );
            if (!Record(Term.I(0), middle).Equals(decoded)
                || !Term.Tuple(
                    Term.A("decoded"),
                    Term.A("named"),
                    left,
                    middle,
                    right,
                    absent,
                    right
                ).Equals(tagged))
            {
                throw new InvalidOperationException(IncorrectResults);
            }

            try
            {
                await module.Invoke(
                    context,
                    "encode",
                    Record(Term.I(0), malformed),
                    tags
                );
                throw new InvalidOperationException(InvalidForeignPair);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badmatch"), malformed)))
            {
                return Nil.Value;
            }

            Term Record(Term version, Term imported) => Term.Tuple(
                Term.A("decoded"),
                version,
                left,
                imported,
                right,
                absent,
                absent
            );
        });

        void CheckFrame(string name)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, _]
                    && atoms[Value(atom)] == name
            );

            if (start < 0)
            {
                throw new InvalidOperationException(IncorrectInstructions);
            }

            int[] frames = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Where(instruction => Opcode(instruction) == Allocate)
                .Select(instruction => Value(Items(instruction, "Operands").First())).ToArray();

            if (frames is not [7])
            {
                throw new InvalidOperationException(IncorrectInstructions);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
