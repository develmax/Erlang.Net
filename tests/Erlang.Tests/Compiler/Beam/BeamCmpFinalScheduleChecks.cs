using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCmpFinalScheduleChecks
{
    internal const string TestName = "compiler/beam/keeps-named-sequence-and-checked-bookend-schedules";
    private const string Fixture = "beam_cmp_final_schedule_probe.erl";
    private const string Peer = "beam_cmp_final_schedule_peer";
    private const string WrongLayout = "Named sequence fields and checked bookends did not retain their pinned frames.";
    private const string WrongValue = "The named sequence or checked bookend changed a field.";

    private const string Source = """
        -module(beam_cmp_final_schedule_probe).
        -export([encode_named/2, decode_named/2, encode_bookends/2, decode_bookends/2]).

        encode_named(Record, Tags) ->
            {_,A,B,C} = Record,
            {AB,AL} = pack(A,[{one,1}],[1]),
            {BB,BL} = case B of
                absent -> {<<>>,0};
                _ -> pack(B,[2])
            end,
            {CB,CL} = case C of
                absent -> {<<>>,0};
                _ -> pack(C,[{flag,0}],[3])
            end,
            Parts = [AB,BB,CB],
            Length = AL + BL + CL,
            finish(Tags,Parts,Length).

        decode_named(Input, Tags) ->
            Fields = prepare(Input,Tags),
            [W|Tail0] = Fields,
            A = begin Value = read(W,[1]), rename(Value,[{one,1}]) end,
            {B,Tail1} = case Tail0 of
                [{2,V}|Rest] -> {read(V,[]),Rest};
                _ -> {absent,Tail0}
            end,
            {C,Tail2} = case Tail1 of
                [{3,V2}|Rest2] -> {read(V2,[{flag,0}],[]),Rest2};
                _ -> {absent,Tail1}
            end,
            case Tail2 of [] -> true; _ -> exit({error,{asn1,{unexpected,Tail2}}}) end,
            Result = {named,A,B,C},
            Result.

        encode_bookends(Record, Tags) ->
            {_,A,B,C,D} = Record,
            {AB,AL} = case A of
                absent -> {<<>>,0};
                _ -> pack(A,[1])
            end,
            {BB,BL} = pack(B,[2]),
            {CB,CL} = pack(C,[3]),
            {DB,DL} = case D of
                absent -> {<<>>,0};
                _ -> beam_cmp_final_schedule_peer:pack(D,[4])
            end,
            Parts = [AB,BB,CB,DB],
            Length = AL + BL + CL + DL,
            finish(Tags,Parts,Length).

        decode_bookends(Input, Tags) ->
            Fields = prepare(Input,Tags),
            {A,Tail0} = case Fields of
                [{1,V}|Rest] -> {read(V,[]),Rest};
                _ -> {absent,Fields}
            end,
            [W|Tail1] = Tail0,
            B = read(W,[2]),
            [X|Tail2] = Tail1,
            C = read(X,[3]),
            {D,Tail3} = case Tail2 of
                [{4,V2}|Rest2] -> {beam_cmp_final_schedule_peer:read(V2,[]),Rest2};
                _ -> {absent,Tail2}
            end,
            case Tail3 of [] -> true; _ -> exit({error,{asn1,{unexpected,Tail3}}}) end,
            Result = {bookends,A,B,C,D},
            Result.

        prepare(Value, _) -> Value.
        read(Value, _) -> Value.
        read(Value, _, _) -> Value.
        rename(Value, _) -> Value.
        pack(Value, _) -> {Value,1}.
        pack(Value, _, _) -> {Value,1}.
        finish(Tags, Parts, Length) -> {Tags,Parts,Length}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] dynamicOptions = CodeGeneration.CompileBeamModule(
            Source.Replace("read(V2,[{flag,0}],[])", "read(V2,Tags,[])", StringComparison.Ordinal), Fixture
        );
        var module = BeamModule.Prepare(image, new BeamDependencySelection([Peer]));
        await using var runtime = new ProcessRuntime();
        runtime.Modules.Register(
            Peer,
            "pack",
            2,
            (_, args) => ValueTask.FromResult<Term>(Term.Tuple(args[0], Term.I(1)))
        );
        runtime.Modules.Register(
            Peer,
            "read",
            2,
            (_, args) => ValueTask.FromResult(args[0])
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term outer = Term.A("record");
            Term a = Term.A("first");
            Term b = Term.A("second");
            Term c = Term.A("third");
            Term d = Term.A("last");
            await Expect("encode_named", Term.Tuple(
                outer,
                a,
                b,
                c
            ), Term.Tuple(outer, Term.List(a, b, c), Term.I(3)));
            await Expect("decode_named", Term.List(a, Tag(2, b), Tag(3, c)), Term.Tuple(
                Term.A("named"),
                a,
                b,
                c
            ));
            await Expect("encode_bookends", Term.Tuple(
                outer,
                a,
                b,
                c,
                d
            ), Term.Tuple(outer, Term.List(
                a,
                b,
                c,
                d
            ), Term.I(4)));
            await Expect("decode_bookends", Term.List(
                Tag(1, a),
                b,
                c,
                Tag(4, d)
            ), Term.Tuple(
                Term.A("bookends"),
                a,
                b,
                c,
                d
            ));

            return Nil.Value;

            async Task Expect(string function, Term input, Term expected)
            {
                if (!expected.Equals(await module.Invoke(
                    context,
                    function,
                    input,
                    outer
                )))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }
        });

        if (Frame(image, "encode_named") != 4 || Frame(image, "decode_named") != 4
            || Frame(image, "encode_bookends") != 4 || Frame(image, "decode_bookends") != 5
            || Frame(dynamicOptions, "decode_named") <= 4)
        {
            throw new InvalidOperationException(WrongLayout);
        }
    }

    private static Term Tag(int kind, Term value) => Term.Tuple(Term.I(kind), value);

    private static int Frame(byte[] image, string function)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var name, var arity]
                && atoms[Value(name)] == function && Value(arity) == 2
        );
        if (start < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object frame = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Single(instruction => Opcode(instruction) == Allocate);

        return Value(Items(frame, "Operands")[0]);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
