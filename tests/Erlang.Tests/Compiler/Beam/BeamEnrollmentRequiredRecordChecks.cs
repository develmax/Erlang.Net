using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamEnrollmentRequiredRecordChecks
{
    internal const string TestName = "compiler/beam/retains-required-fields-around-a-checked-foreign-pair";
    private const string Fixture = "beam_required_pairs.erl";
    private const string Peer = "beam_required_pair_peer";
    private const string WrongLayout = "A checked required record did not retain the pair schedule.";
    private const string WrongValue = "A checked required record changed the pair result or foreign failure.";

    private const string Source = """
        -module(beam_required_pairs).
        -export([final_three/2, middle_four/2, middle_five/2]).

        final_three(Record, Tags) ->
            {_,A,B,C} = Record,
            {AB,AL} = pack(A,[1]),
            {BB,BL} = pack(B,[2]),
            {CB,CL} = beam_required_pair_peer:pack(C,[3]),
            Parts = [AB,BB,CB],
            Length = AL + BL + CL,
            finish(Tags, Parts, Length).

        middle_four(Record, Tags) ->
            {_,A,B,C,D} = Record,
            {AB,AL} = pack(A,[1]),
            {BB,BL} = beam_required_pair_peer:pack(B,[]),
            {CB,CL} = pack(C,[3]),
            {DB,DL} = pack(D,[4]),
            Parts = [AB,BB,CB,DB],
            Length = AL + BL + CL + DL,
            finish(Tags, Parts, Length).

        middle_five(Record, Tags) ->
            {_,A,B,C,D,E} = Record,
            {AB,AL} = lead(A,[]),
            {BB,BL} = beam_required_pair_peer:pack(B,[2]),
            {CB,CL} = pack(C,[3]),
            {DB,DL} = pack(D,[4]),
            {EB,EL} = pack(E,[5]),
            Parts = [AB,BB,CB,DB,EB],
            Length = AL + BL + CL + DL + EL,
            finish(Tags, Parts, Length).

        pack(Value, _) -> {Value,1}.
        lead(Value, []) ->
            {Bytes,Length} = case Value of
                {relay, Forward} -> beam_required_pair_peer:pack(Forward,[]);
                _ -> {Value,1}
            end,
            pair(Bytes,Length).
        pair(Bytes,Length) -> {Bytes,Length}.
        finish(Tags, Parts, Length) -> {Tags,Parts,Length}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] otherOptions = CodeGeneration.CompileBeamModule(
            Source.Replace("beam_required_pair_peer:pack(C,[3])", "beam_required_pair_peer:pack(C,[])", StringComparison.Ordinal),
            Fixture
        );
        byte[] anotherForeign = CodeGeneration.CompileBeamModule(
            Source.Replace("{CB,CL} = pack(C,[3]),", "{CB,CL} = beam_required_pair_peer:pack(C,[3]),", StringComparison.Ordinal),
            Fixture
        );
        var module = BeamModule.Prepare(image, new BeamDependencySelection([Peer]));
        await using var runtime = new ProcessRuntime();
        Term bad = Term.A("bad");
        Term nonnumeric = Term.A("nonnumeric");
        runtime.Modules.Register(
            Peer,
            "pack",
            2,
            (_, args) => ValueTask.FromResult(args[0].Equals(bad) ? bad : Term.Tuple(
                args[0],
                args[0].Equals(nonnumeric) ? nonnumeric : Term.I(2)
            ))
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term tag = Term.A("record");
            Term a = Term.A("a");
            Term b = Term.A("b");
            Term c = Term.A("c");
            Term d = Term.A("d");
            Term e = Term.A("e");
            Term relay = Term.A("relay");
            await Expect(
                "final_three",
                [a, b, c],
                Term.List(a, b, c),
                4
            );
            await Expect(
                "middle_four",
                [a, b, c, d],
                Term.List(
                    a,
                    b,
                    c,
                    d
                ),
                5
            );
            await Expect(
                "middle_five",
                [a, b, c, d, e],
                Term.List(
                    a,
                    b,
                    c,
                    d,
                    e
                ),
                6
            );
            await Expect(
                "middle_five",
                [Term.Tuple(relay, a), b, c, d, e],
                Term.List(
                    a,
                    b,
                    c,
                    d,
                    e
                ),
                7
            );
            Term badPair = Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), bad);
            await Reject("final_three", [a, b, bad], badPair);
            await Reject("middle_four", [a, bad, c, d], badPair);
            await Reject("middle_five", [a, bad, c, d, e], badPair);
            await Reject(
                "middle_five",
                [Term.Tuple(relay, nonnumeric), b, c, d, e],
                Term.A(ErlangErrorReasons.BadArithmetic)
            );

            return Nil.Value;

            async Task Expect(
                string encoder,
                Term[] values,
                Term payload,
                int length
            )
            {
                Term expected = Term.Tuple(tag, payload, Term.I(length));
                if (!expected.Equals(await module.Invoke(
                    context,
                    encoder,
                    Record(values),
                    tag
                )))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }

            async Task Reject(string encoder, Term[] values, Term reason)
            {
                try
                {
                    await module.Invoke(
                        context,
                        encoder,
                        Record(values),
                        tag
                    );
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                    && error.Reason.Equals(reason))
                {
                    return;
                }

                throw new InvalidOperationException(WrongValue);
            }

            Term Record(Term[] values) => Term.Tuple([tag, .. values]);
        });

        var last = Layout(image, "final_three");
        var four = Layout(image, "middle_four");
        var five = Layout(image, "middle_five");
        if (last != (3, 2) || four != (4, 2) || five != (5, 2)
            || Layout(otherOptions, "final_three") == last || Layout(anotherForeign, "middle_five") == five)
        {
            throw new InvalidOperationException($"{WrongLayout} {last}, {four}, {five}");
        }
    }

    private static (int Frame, int BadMatches) Layout(byte[] image, string function)
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

        return (frame, body.Count(instruction => Opcode(instruction) == BadMatch));
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
