using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOptionalTableDecoderChecks
{
    internal const string TestName = "compiler/beam/keeps-optional-table-decode-roots-through-the-catch";
    private const string Module = "beam_optional_table_decoder_probe";
    private const string Fixture = "beam_optional_table_decoder_probe.erl";
    private const string WrongSchedule = "The table decoder lost its checked frame or split exception continuations.";
    private const string WrongValue = "The optional decode changed a result, error class, or original operand.";
    private const string Source = """
        -module(beam_optional_table_decoder_probe).
        -export([unpack/2, by_throw/2, factory/1]).
        unpack(Input, Tags) ->
            Items = match_tags(Input, Tags),
            [Head | Tail] = Items,
            Key = required(Head, [identifier]),
            {Payload, Left} = case Tail of
                [Field | Rest] -> {open(Field, []), Rest};
                _ -> {absent, Tail}
            end,
            Decode = beam_optional_table_decoder_probe:factory(Key),
            Value = case Payload of
                absent -> absent;
                _ ->
                    try Decode(field, Payload, [])
                    catch Class:Reason when Class =:= error; Class =:= exit ->
                        exit({rejected, Reason})
                    end
            end,
            case Left of
                [] -> true;
                _ -> exit({error, {options, {unexpected, Left}}})
            end,
            Result = {decoded, Key, Value},
            Result.

        by_throw(Input, Tags) ->
            Items = match_tags(Input, Tags),
            [Head | Tail] = Items,
            Key = required(Head, [identifier]),
            {Payload, Left} = case Tail of
                [Field | Rest] -> {open(Field, []), Rest};
                _ -> {absent, Tail}
            end,
            Decode = beam_optional_table_decoder_probe:factory(Key),
            Value = case Payload of
                absent -> absent;
                _ ->
                    try Decode(field, Payload, [])
                    catch Class:Reason when Class =:= error; Class =:= throw ->
                        exit({rejected, Reason})
                    end
            end,
            case Left of
                [] -> true;
                _ -> exit({error, {options, {unexpected, Left}}})
            end,
            Result = {decoded, Key, Value},
            Result.

        match_tags(Input, _) -> Input.
        required(Head, _) -> Head.
        open(Value, _) -> Value.
        factory(_Key) -> fun fields/3.
        fields(field, fail, []) -> erlang:error(rejected_value);
        fields(field, exits, []) -> exit(rejected_value);
        fields(field, throws, []) -> throw(rejected_value);
        fields(field, Value, []) -> {field, Value}.
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
                && atoms[Value(atom)] == "unpack" && Value(arity) == 2
        );
        if (start < 0)
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        object[] decode = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        object[] frames = decode.Where(instruction => Opcode(instruction) == Allocate).ToArray();
        if (frames is not [var frame] || Items(frame, "Operands").ToArray() is not [var slots, var live]
            || Value(slots) != 4 || Value(live) != 2
            || decode.Count(instruction => Opcode(instruction) == LocalCall) != 3
            || decode.Count(instruction => Opcode(instruction) == TryEnter) != 1
            || decode.Count(instruction => Opcode(instruction) == CallFun) != 1
            || decode.Count(instruction => Opcode(instruction) == TryCase) != 1
            || decode.Count(instruction => Opcode(instruction) == Raise) != 1
            || decode.Count(instruction => Opcode(instruction) == BadMatch) != 1)
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        await using var runtime = new ProcessRuntime();
        PreparedBeamImage prepared = runtime.Modules.PrepareBeam(image, new BeamDependencySelection([Module]));
        if (runtime.Modules.PublishPreparedBeam(prepared).Status != BeamPublicationStatus.Published)
        {
            throw new InvalidOperationException(WrongValue);
        }

        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term tags = Term.A("tags");
            Term key = Term.A("id");
            Term payload = Term.A("payload");
            Term absence = Term.Tuple(Term.A("decoded"), key, Term.A("absent"));
            Term presence = Term.Tuple(Term.A("decoded"), key, Term.Tuple(Term.A("field"), payload));
            if (!absence.Equals(await runtime.Modules.Call(
                context,
                Module,
                "unpack",
                [Term.List(key), tags]
            ))
                || !presence.Equals(await runtime.Modules.Call(
                    context,
                    Module,
                    "unpack",
                    [Term.List(key, payload), tags]
                )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            Term error = Term.A("rejected_value");
            Term wrapped = Term.Tuple(Term.A("rejected"), error);
            await ExpectError(
                "unpack",
                Term.List(key, Term.A("fail")),
                ErlangExceptionClasses.Exit,
                wrapped
            );
            await ExpectError(
                "unpack",
                Term.List(key, Term.A("exits")),
                ErlangExceptionClasses.Exit,
                wrapped
            );
            await ExpectError(
                "unpack",
                Term.List(key, Term.A("throws")),
                ErlangExceptionClasses.Throw,
                error
            );
            await ExpectError(
                "by_throw",
                Term.List(key, Term.A("throws")),
                ErlangExceptionClasses.Exit,
                wrapped
            );
            await ExpectError(
                "by_throw",
                Term.List(key, Term.A("exits")),
                ErlangExceptionClasses.Exit,
                error
            );
            Term extra = Term.A("extra");
            Term unexpected = Term.Tuple(Term.A("error"), Term.Tuple(Term.A("options"),
                Term.Tuple(Term.A("unexpected"), Term.List(extra))));
            await ExpectError(
                "unpack",
                Term.List(key, payload, extra),
                ErlangExceptionClasses.Exit,
                unexpected
            );
            await ExpectError(
                "unpack",
                Nil.Value,
                ErlangExceptionClasses.Error,
                Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), Nil.Value)
            );

            return Nil.Value;

            async Task ExpectError(
                string function,
                Term input,
                string kind,
                Term reason
            )
            {
                try
                {
                    _ = await runtime.Modules.Call(
                        context,
                        Module,
                        function,
                        [input, tags]
                    );
                }
                catch (ErlangException error) when (error.ExceptionClass == kind && error.Reason.Equals(reason))
                {
                    return;
                }

                throw new InvalidOperationException(WrongValue);
            }
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
