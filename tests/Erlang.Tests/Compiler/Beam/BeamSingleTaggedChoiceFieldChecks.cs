using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSingleTaggedChoiceFieldChecks
{
    internal const string TestName = "compiler/beam/allocates-frames-only-on-the-required-part-of-a-tagged-choice";
    private const string Fixture = "beam_single_tagged_choice_field_probe.erl";
    private const string IncorrectLayout = "The choice reserved a slot before proving the selected field.";
    private const string IncorrectResult = "The tagged choice changed its output or invalid choice reason.";

    private const string Source = """
        -module(beam_single_tagged_choice_field_probe).
        -export([encode/2, decode/2]).
        encode(Value, Options) ->
            {Bytes, Length} = case element(1, Value) of
                chosen ->
                    read_pair(element(2, Value), encode);
                Other ->
                    exit({error, {choice, {invalid_type, Other}}})
            end,
            finish(Options, Bytes, Length).
        decode(Tagged, Options) ->
            Prepared = prepare(Tagged, Options),
            case (case Prepared of [One] -> One; _ -> Prepared end) of
                {42, Value} ->
                    {chosen, read_value(Value, decode)};
                Other ->
                    exit({error, {choice, {invalid_tag, Other}}})
            end.
        read_pair(Value, _Mode) -> {Value, 1}.
        read_value(Value, _Mode) -> {decoded, Value}.
        finish(Options, Bytes, Length) -> {Options, Bytes, Length}.
        prepare(Value, _Mode) -> Value.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] enc = Read("encode");
        object[] dec = Read("decode");
        byte[] entry = enc.Select(Opcode).Where(opcode => opcode is ExactEqualTest or BifTwo or Allocate).ToArray();
        object[] frames = dec.Where(instruction => Opcode(instruction) == Allocate).ToArray();
        if (entry is not [BifTwo, ExactEqualTest, BifTwo, Allocate]
            || frames is not [var frame] || Items(frame, "Operands").Select(Value).ToArray() is not [0, 2]
            || enc.Concat(dec).Any(instruction => Opcode(instruction) == InitYRegisters))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term chosen = Term.A("chosen");
            Term data = Term.I(13);
            Term options = Term.A("options");
            Term tagged = Term.Tuple(Term.I(42), data);
            Term encoded = Term.Tuple(options, data, Term.I(1));
            Term decoded = Term.Tuple(chosen, Term.Tuple(Term.A("decoded"), data));
            if (!encoded.Equals(await module.Invoke(
                context,
                "encode",
                Term.Tuple(chosen, data),
                options
            ))
                || !decoded.Equals(await module.Invoke(
                    context,
                    "decode",
                    tagged,
                    options
                ))
                || !decoded.Equals(await module.Invoke(
                    context,
                    "decode",
                    Term.List(tagged),
                    options
                )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            Term rejected = Term.A("other");
            Term extra = Term.List(tagged, tagged);
            await CheckExit(
                "encode",
                Term.Tuple(rejected, data),
                Term.A("invalid_type"),
                rejected
            );
            await CheckExit(
                "decode",
                extra,
                Term.A("invalid_tag"),
                extra
            );

            return Nil.Value;

            async Task CheckExit(
                string function,
                Term input,
                Term tag,
                Term actual
            )
            {
                Term reason = Term.Tuple(Term.A("error"), Term.Tuple(Term.A("choice"), Term.Tuple(tag, actual)));
                try
                {
                    _ = await module.Invoke(
                        context,
                        function,
                        input,
                        options
                    );
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Exit && error.Reason.Equals(reason))
                {
                    return;
                }

                throw new InvalidOperationException(IncorrectResult);
            }
        });

        object[] Read(string name)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, var arity]
                    && atoms[Value(atom)] == name && Value(arity) == 2
            );
            if (start < 0)
            {
                throw new InvalidOperationException(IncorrectLayout);
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
