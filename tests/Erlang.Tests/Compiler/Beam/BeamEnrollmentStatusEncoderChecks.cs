using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamEnrollmentStatusEncoderChecks
{
    internal const string TestName = "compiler/beam/keeps-four-field-pairs-and-foreign-return-checks";
    private const string Fixture = "beam_status_encoder.erl";
    private const string Peer = "beam_checked_status_peer";
    private const string WrongLayout = "A four-field encoder did not retain the expected pair projections.";
    private const string WrongValue = "A four-field encoder changed a result or returned an unchecked foreign value.";

    private const string Source = """
        -module(beam_status_encoder).
        -export([status/2, foreign_fields/2]).

        status(Record, Tags) ->
            {_,A,B,C,D} = Record,
            {AB,AL} = pack(A,[{accepted,0}],[1]),
            {BB,BL} = pack(B,[2]),
            {CB,CL} = case C of absent -> {<<>>,0}; _ -> pack(C,[3]) end,
            {DB,DL} = case D of absent -> {<<>>,0}; _ -> pack(D,[]) end,
            Parts = [AB,BB,CB,DB],
            Length = AL + BL + CL + DL,
            finish(Tags,Parts,Length).

        foreign_fields(Record, Tags) ->
            {_,A,B,C,D} = Record,
            {AB,AL} = beam_checked_status_peer:pack(A,[]),
            {BB,BL} = case B of absent -> {<<>>,0}; _ -> beam_checked_status_peer:pack(B,[]) end,
            {CB,CL} = case C of absent -> {<<>>,0}; _ -> pack(C,[3]) end,
            {DB,DL} = case D of absent -> {<<>>,0}; _ -> pack(D,[{flag,0}],[4]) end,
            Parts = [AB,BB,CB,DB],
            Length = AL + BL + CL + DL,
            finish(Tags,Parts,Length).

        pack(Value, _) -> {Value,1}.
        pack(Value, _, _) -> {Value,1}.
        finish(Tags, Parts, Length) -> {Tags,Parts,Length}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] otherTags = CodeGeneration.CompileBeamModule(
            Source.Replace("pack(D,[])", "pack(D,[4])", StringComparison.Ordinal),
            Fixture
        );
        byte[] noNamedTail = CodeGeneration.CompileBeamModule(
            Source.Replace("pack(D,[{flag,0}],[4])", "pack(D,[4])", StringComparison.Ordinal),
            Fixture
        );
        var module = BeamModule.Prepare(image, new BeamDependencySelection([Peer]));
        await using var runtime = new ProcessRuntime();
        Term bad = Term.A("bad");
        runtime.Modules.Register(
            Peer,
            "pack",
            2,
            (_, args) => ValueTask.FromResult(args[0].Equals(bad) ? bad : Term.Tuple(args[0], Term.I(2)))
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term tag = Term.A("record");
            Term a = Term.A("a");
            Term b = Term.A("b");
            Term c = Term.A("c");
            Term d = Term.A("d");
            Term absent = Term.A("absent");
            Term empty = new BitString([]);
            Term input = Term.Tuple(
                tag,
                a,
                b,
                c,
                d
            );
            await Expect(
                "status",
                input,
                Term.List(
                    a,
                    b,
                    c,
                    d
                ),
                4
            );
            await Expect(
                "foreign_fields",
                input,
                Term.List(
                    a,
                    b,
                    c,
                    d
                ),
                6
            );
            await Expect(
                "status",
                Term.Tuple(
                    tag,
                    a,
                    b,
                    absent,
                    absent
                ),
                Term.List(
                    a,
                    b,
                    empty,
                    empty
                ),
                2
            );
            await Expect(
                "foreign_fields",
                Term.Tuple(
                    tag,
                    a,
                    absent,
                    absent,
                    absent
                ),
                Term.List(
                    a,
                    empty,
                    empty,
                    empty
                ),
                2
            );
            await Reject(Term.Tuple(
                tag,
                bad,
                b,
                c,
                d
            ));
            await Reject(Term.Tuple(
                tag,
                a,
                bad,
                c,
                d
            ));

            return Nil.Value;

            async Task Expect(
                string encoder,
                Term record,
                Term payload,
                int length
            )
            {
                Term expected = Term.Tuple(tag, payload, Term.I(length));
                if (!expected.Equals(await module.Invoke(
                    context,
                    encoder,
                    record,
                    tag
                )))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }

            async Task Reject(Term record)
            {
                try
                {
                    await module.Invoke(
                        context,
                        "foreign_fields",
                        record,
                        tag
                    );
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                    && error.Reason.Equals(Term.Tuple(Term.A("badmatch"), bad)))
                {
                    return;
                }

                throw new InvalidOperationException(WrongValue);
            }
        });

        var status = ReadSchedule(image, "status");
        var foreign = ReadSchedule(image, "foreign_fields");
        var taggedTail = ReadSchedule(otherTags, "status");
        var ordinaryTail = ReadSchedule(noNamedTail, "foreign_fields");
        if (status != (5, 2, 1) || foreign != (5, 2, 3) || taggedTail == status || ordinaryTail == foreign)
        {
            throw new InvalidOperationException($"{WrongLayout} {status}, {foreign}, {taggedTail}, {ordinaryTail}");
        }
    }

    private static (int Frame, int TypedSumInputs, int BadMatches) ReadSchedule(byte[] image, string function)
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

        object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int frame = Value(Items(body.Single(instruction => Opcode(instruction) == Allocate), "Operands")[0]);
        int typed = body.Where(instruction => Opcode(instruction) == GcBifTwo)
            .SelectMany(instruction => Items(instruction, "Operands").Skip(3).Take(2))
            .Count(operand => Property(operand, "TypeIndex") is not null);

        return (frame, typed, body.Count(instruction => Opcode(instruction) == BadMatch));
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
