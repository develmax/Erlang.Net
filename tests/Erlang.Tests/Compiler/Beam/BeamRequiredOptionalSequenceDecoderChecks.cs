using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRequiredOptionalSequenceDecoderChecks
{
    internal const string TestName = "compiler/beam/keeps-required-and-optional-decoder-values-in-live-frame-slots";
    private const string Fixture = "beam_required_optional_sequence_decoder_probe.erl";
    private const string IncorrectFrame = "The required inputs and optional remainders were staged in the wrong registers.";
    private const string IncorrectResult = "The sequence decoder changed fields, range limits or unmatched input.";

    private const string Source = """
        -module(beam_required_optional_sequence_decoder_probe).
        -export([decode/2]).
        decode(Value, Tags) ->
            Start = parse(Value, Tags),
            [Head | Rest1] = Start,
            Version = begin
                Integer = read_integer(Head, integer),
                if
                    1 =< Integer, Integer =< 1 ->
                        Confirmed = Integer,
                        name(Confirmed, version);
                    true ->
                        exit({error, {sequence, bad_range}})
                end
            end,
            [Content | Rest2] = Rest1,
            Required = read(Content, required),
            {Selected, Rest3} = case Rest2 of
                [{3, Data} | Tail3] -> {read(Data, selected), Tail3};
                _ -> {missing, Rest2}
            end,
            {Extra, Rest4} = case Rest3 of
                [Data4 | Tail4] -> {read(Data4, extra), Tail4};
                _ -> {absent, Rest3}
            end,
            case Rest4 of
                [] -> true;
                _ -> exit({error, {sequence, {unexpected, Rest4}}})
            end,
            Result = {decoded, Version, Required, Selected, Extra},
            Result.
        parse(Value, keep) -> Value.
        read_integer(Binary, integer) ->
            <<Integer>> = Binary,
            Integer.
        name(1, version) -> one.
        read(Value, Label) -> {Label, Value}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands").ToArray() is [_, var atom, var arity]
                && atoms[Value(atom)] == "decode" && Value(arity) == 2
        );
        if (start < 0)
        {
            throw new InvalidOperationException(IncorrectFrame);
        }

        object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        object[] frames = body.Where(instruction => Opcode(instruction) == Allocate).ToArray();
        if (frames is not [var frame] || Items(frame, "Operands").Select(Value).ToArray() is not [5, 2]
            || body.Count(instruction => Opcode(instruction) == GetList) != 4
            || body.Count(instruction => Opcode(instruction) == IsGreaterOrEqual) != 2)
        {
            throw new InvalidOperationException(IncorrectFrame);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term root = Term.A("decoded");
            Term keep = Term.A("keep");
            Term version = Term.A("one");
            Term value = Term.A("selected_value");
            Term named = Term.A("extra_value");
            Term header = new BitString([1]);
            Term content = Term.A("content");
            Term required = Term.Tuple(Term.A("required"), content);
            Term selected = Term.Tuple(Term.A("selected"), value);
            Term extra = Term.Tuple(Term.A("extra"), named);
            if (!Term.Tuple(
                root,
                version,
                required,
                selected,
                extra
            ).Equals(
                    await module.Invoke(
                        context,
                        "decode",
                        Term.List(
                            header,
                            content,
                            Term.Tuple(Term.I(3), value),
                            named
                        ),
                        keep
                    ))
                || !Term.Tuple(
                    root,
                    version,
                    required,
                    Term.A("missing"),
                    Term.A("absent")
                ).Equals(
                    await module.Invoke(
                        context,
                        "decode",
                        Term.List(header, content),
                        keep
                    )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            await CheckFailure(
                Term.List(new BitString([2]), content),
                ErlangExceptionClasses.Exit,
                Term.Tuple(Term.A("error"), Term.Tuple(Term.A("sequence"), Term.A("bad_range")))
            );
            await CheckFailure(Term.List(header), ErlangExceptionClasses.Error, Term.Tuple(Term.A("badmatch"), Nil.Value));

            return Nil.Value;

            async Task CheckFailure(Term input, string kind, Term reason)
            {
                try
                {
                    _ = await module.Invoke(
                        context,
                        "decode",
                        input,
                        keep
                    );
                }
                catch (ErlangException error) when (error.ExceptionClass == kind && error.Reason.Equals(reason))
                {
                    return;
                }

                throw new InvalidOperationException(IncorrectResult);
            }
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
