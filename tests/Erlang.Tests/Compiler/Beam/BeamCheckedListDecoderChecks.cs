using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedListDecoderChecks
{
    internal const string TestName = "compiler/beam/checked-list-decoder-tail";
    private const string Fixture = "beam_checked_list_decoder_probe.erl";
    private const string WrongSchedule = "The checked decoder did not retain its required fields and final tail in the expected registers.";
    private const string WrongOutcome = "The checked decoder changed its result or list failure.";

    private const string Source = """
        -module(beam_checked_list_decoder_probe).
        -compile({no_auto_import,[exit/1,byte_size/1]}).
        -export([decode2/2,decode3/2,decode6/2,optional/2,custom/2,sized/2,shadowed_size/2]).
        
        decode2(Input, Tag) ->
            Tail0 = read(Input, Tag),
            [Value0|Tail1] = Tail0,
            Field0 = field(Value0, [2]),
            [Value1|Tail2] = Tail1,
            Field1 = field(Value1, [2]),
            case Tail2 of
                [] -> true;
                _ -> erlang:exit({invalid,{extra,Tail2}})
            end,
            Result = {decode2, Field0, Field1},
            Result.
        
        decode3(Input, Tag) ->
            Tail0 = read(Input, Tag),
            [Value0|Tail1] = Tail0,
            Field0 = field(Value0, [2]),
            [Value1|Tail2] = Tail1,
            Field1 = field(Value1, [2]),
            [Value2|Tail3] = Tail2,
            Field2 = field(Value2, [2]),
            case Tail3 of
                [] -> true;
                _ -> erlang:exit({invalid,{extra,Tail3}})
            end,
            Result = {decode3, Field0, Field1, Field2},
            Result.
        
        decode6(Input, Tag) ->
            Tail0 = read(Input, Tag),
            [Value0|Tail1] = Tail0,
            Field0 = field(Value0, [2]),
            [Value1|Tail2] = Tail1,
            Field1 = field(Value1, [2]),
            [Value2|Tail3] = Tail2,
            Field2 = field(Value2, [2]),
            [Value3|Tail4] = Tail3,
            Field3 = field(Value3, [2]),
            [Value4|Tail5] = Tail4,
            Field4 = field(Value4, [2]),
            [Value5|Tail6] = Tail5,
            Field5 = field(Value5, [2]),
            case Tail6 of
                [] -> true;
                _ -> erlang:exit({invalid,{extra,Tail6}})
            end,
            Result = {decode6, Field0, Field1, Field2, Field3, Field4, Field5},
            Result.
        
        custom(Input, Tag) ->
            Tail0 = read(Input, Tag),
            [Value0|Tail1] = Tail0,
            Field0 = field(Value0, [2]),
            [Value1|Tail2] = Tail1,
            Field1 = field(Value1, [2]),
            case Tail2 of
                [] -> true;
                _ -> exit({invalid,{extra,Tail2}})
            end,
            Result = {custom, Field0, Field1},
            Result.
        
        optional(Input, Tag) ->
            Tail0 = read(Input, Tag),
            [Value0|Tail1] = Tail0,
            Field0 = field(Value0, [2]),
            [Value1|Tail2] = Tail1,
            Field1 = field(Value1, [2]),
            {Field2,Tail3} = case Tail2 of
                [{2,Value2}|Rest] -> {field(Value2, []), Rest};
                _ -> {missing,Tail2}
            end,
            case Tail3 of
                [] -> true;
                _ -> erlang:exit({invalid,{extra,Tail3}})
            end,
            Result = {optional, Field0, Field1, Field2},
            Result.
        sized(Input, Tag) ->
            Tail0 = read(Input, Tag),
            [Value0|Tail1] = Tail0,
            Field0 = begin
                Decoded0 = field(Value0, [2]),
                Count0 = erlang:byte_size(Decoded0),
                if Count0 =:= 2 -> Decoded0; true -> erlang:exit(wrong_first) end
            end,
            [Value1|Tail2] = Tail1,
            Field1 = begin
                Decoded1 = field(Value1, [2]),
                Count1 = erlang:byte_size(Decoded1),
                if Count1 =:= 1 -> Decoded1; true -> erlang:exit(wrong_second) end
            end,
            case Tail2 of
                [] -> true;
                _ -> erlang:exit({invalid,{extra,Tail2}})
            end,
            Result = {sized, Field0, Field1},
            Result.

        shadowed_size(Input, Tag) ->
            Tail0 = read(Input, Tag),
            [Value0|Tail1] = Tail0,
            Field0 = begin
                Decoded0 = field(Value0, [2]),
                Count0 = byte_size(Decoded0),
                if Count0 =:= 1 -> Decoded0; true -> erlang:exit(wrong_first) end
            end,
            [Value1|Tail2] = Tail1,
            Field1 = begin
                Decoded1 = field(Value1, [2]),
                Count1 = byte_size(Decoded1),
                if Count1 =:= 1 -> Decoded1; true -> erlang:exit(wrong_second) end
            end,
            case Tail2 of
                [] -> true;
                _ -> erlang:exit({invalid,{extra,Tail2}})
            end,
            Result = {shadowed_size, Field0, Field1},
            Result.

        byte_size(_) -> 1.
        read(Input, _) -> Input.
        field(Value, _) -> Value.
        exit(_) -> ignored.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] beam = CompileFixture();
        CheckFields(beam, "decode2", [2, 0]);
        CheckFields(beam, "decode3", [3, 1, 0]);
        CheckFields(beam, "decode6", [6, 4, 3, 2, 1, 0]);
        CheckFields(
            beam,
            "optional",
            [3, 1, 0],
            tailRegister: 1
        );
        CheckFields(
            beam,
            "sized",
            [2, 0],
            expectedSizedFields: 2
        );
        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term exact = Term.List(Term.I(5), Term.I(6));
            Term overlong = Term.List(Term.I(5), Term.I(6), Term.I(7));
            Term partial = Term.List(Term.I(5));
            Term tag = Term.A("ignored");
            Term tagged = Term.Tuple(Term.I(2), Term.I(7));
            if (!Term.Tuple(Term.A("decode2"), Term.I(5), Term.I(6)).Equals(await module.Invoke(
                context,
                "decode2",
                exact,
                tag
            ))
                || !Term.Tuple(
                    Term.A("optional"),
                    Term.I(5),
                    Term.I(6),
                    Term.A("missing")
                )
                    .Equals(await module.Invoke(
                        context,
                        "optional",
                        exact,
                        tag
                    ))
                || !Term.Tuple(
                    Term.A("optional"),
                    Term.I(5),
                    Term.I(6),
                    Term.I(7)
                )
                    .Equals(await module.Invoke(
                        context,
                        "optional",
                        Term.List(Term.I(5), Term.I(6), tagged),
                        tag
                    ))
                || !Term.Tuple(Term.A("custom"), Term.I(5), Term.I(6)).Equals(await module.Invoke(
                    context,
                    "custom",
                    overlong,
                    tag
                )))
            {
                throw new InvalidOperationException(WrongOutcome);
            }

            await CheckFailure(
                "decode2",
                partial,
                ErlangExceptionClasses.Error,
                Term.Tuple(Term.A("badmatch"), Nil.Value)
            );
            await CheckFailure(
                "decode2",
                overlong,
                ErlangExceptionClasses.Exit,
                Term.Tuple(Term.A("invalid"), Term.Tuple(Term.A("extra"), Term.List(Term.I(7))))
            );

            await CheckFailure(
                "optional",
                overlong,
                ErlangExceptionClasses.Exit,
                Term.Tuple(Term.A("invalid"), Term.Tuple(Term.A("extra"), Term.List(Term.I(7))))
            );
            await CheckFailure(
                "optional",
                Term.List(
                    Term.I(5),
                    Term.I(6),
                    tagged,
                    Term.I(9)
                ),
                ErlangExceptionClasses.Exit,
                Term.Tuple(Term.A("invalid"), Term.Tuple(Term.A("extra"), Term.List(Term.I(9))))
            );

            Term first = new BitString(new byte[] { 1, 2 });
            Term second = new BitString(new byte[] { 3 });
            if (!Term.Tuple(Term.A("sized"), first, second)
                    .Equals(await module.Invoke(
                        context,
                        "sized",
                        Term.List(first, second),
                        tag
                    ))
                || !Term.Tuple(Term.A("shadowed_size"), Term.I(5), Term.I(6))
                    .Equals(await module.Invoke(
                        context,
                        "shadowed_size",
                        exact,
                        tag
                    )))
            {
                throw new InvalidOperationException(WrongOutcome);
            }

            await CheckFailure(
                "sized",
                Term.List(second),
                ErlangExceptionClasses.Exit,
                Term.A("wrong_first")
            );
            await CheckFailure(
                "sized",
                Term.List(first, first),
                ErlangExceptionClasses.Exit,
                Term.A("wrong_second")
            );
            await CheckFailure(
                "sized",
                Term.List(first, Term.A("invalid")),
                ErlangExceptionClasses.Error,
                Term.A("badarg")
            );

            return Nil.Value;

            async Task CheckFailure(
                string name,
                Term input,
                string exceptionClass,
                Term reason
            )
            {
                try
                {
                    _ = await module.Invoke(
                        context,
                        name,
                        input,
                        tag
                    );
                }
                catch (ErlangException failure) when (failure.ExceptionClass == exceptionClass && failure.Reason.Equals(reason))
                {
                    return;
                }

                throw new InvalidOperationException(WrongOutcome);
            }
        });
    }

    private static void CheckFields(
        byte[] beam,
        string function,
        int[] tailRegisters,
        int tailRegister = 0,
        int expectedSizedFields = 0
    )
    {
        object code = Code(beam);
        object[] instructions = Children(code, "Instructions").ToArray();
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int entry = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
            && atoms[(int)Value(Children(instruction, "Operands").ElementAt(1))] == function
        );
        object[] body = instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int frame = tailRegisters.Length + 1;
        object allocate = body.First(instruction => Opcode(instruction) == Allocate);
        int[] actualTailRegisters = body.Where(instruction => Opcode(instruction) == GetList)
            .Select(instruction => (int)Value(Children(instruction, "Operands").Last())).ToArray();
        object? empty = body.FirstOrDefault(instruction => Opcode(instruction) == IsNil);
        object? exit = body.FirstOrDefault(instruction => Opcode(instruction) == TailExternalCallWithFrame);
        var measurements = body.Where(instruction => Opcode(instruction) == GcBifOne).ToArray();
        if (measurements.Length != expectedSizedFields || measurements.Any(measurement =>
            Value(Children(measurement, "Operands").ElementAt(1)) != 1
            || Value(Children(measurement, "Operands").Last()) != 1))
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        if (Value(Children(allocate, "Operands").First()) != frame
            || !actualTailRegisters.SequenceEqual(tailRegisters)
            || empty is null || Value(Children(empty, "Operands").Last()) != tailRegister
            || exit is null || Value(Children(exit, "Operands").Last()) != frame)
        {
            throw new InvalidOperationException(WrongSchedule);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
