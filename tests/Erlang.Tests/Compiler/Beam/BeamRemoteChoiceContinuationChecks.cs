using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRemoteChoiceContinuationChecks
{
    internal const string TestName = "compiler/beam/checks-a-foreign-choice-pair-before-projecting-and-tail-calling";
    private const string Fixture = "beam_remote_choice_continuation_probe.erl";
    private const string Dependency = "beam_remote_choice_probe_peer";
    private const string IncorrectInstructions = "The choice continuation has the wrong frame or result checks.";
    private const string IncorrectResults = "The choice continuation returned an unexpected value.";
    private const string AcceptedInvalidPair = "The choice continuation accepted a malformed pair.";

    private const string Source = """
        -module(beam_remote_choice_continuation_probe).
        -export([encode/2, decode/2]).

        encode(Input, Tags) ->
            {Data, Size} = case element(1, Input) of
                first -> beam_remote_choice_probe_peer:pair(element(2, Input), [2]);
                second -> pair(element(2, Input), [4]);
                Other -> exit({error, {asn1, {invalid_choice, Other}}})
            end,
            finish(Tags, Data, Size).

        decode(Input, Tags) ->
            Value = prepare(Input, Tags),
            case case Value of [One] -> One; _ -> Value end of
                {2, Number} -> {first, beam_remote_choice_probe_peer:pair(Number, [])};
                {4, Bytes} -> {second, passthrough(Bytes, [])};
                Other -> exit({error, {asn1, {invalid_choice, Other}}})
            end.

        pair(Value, Opts) -> {Value, Opts}.
        finish(Tags, Data, Size) -> {Tags, Data, Size}.
        prepare(Input, _) -> Input.
        passthrough(Input, _) -> Input.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] encode = Read("encode", 1);
        object[] decode = Read("decode", 0);
        if (encode.Count(instruction => Opcode(instruction) == BadMatch) != 1
            || encode.Count(instruction => Opcode(instruction) == SelectValue) != 1
            || decode.Count(instruction => Opcode(instruction) == SelectValue) != 1)
        {
            throw new InvalidOperationException(IncorrectInstructions);
        }

        await using var runtime = new ProcessRuntime();
        Term malformed = Term.A("malformed");
        runtime.Modules.Register(
            Dependency,
            "pair",
            2,
            (_, args) => ValueTask.FromResult(args[0].Equals(malformed) ? malformed : Term.Tuple(args[0], args[1]))
        );
        var module = BeamModule.Prepare(image, new BeamDependencySelection([Dependency]));
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term value = Term.A("data");
            Term tags = Term.A("saved");
            Term encoded = Term.Tuple(tags, value, Term.List(Term.I(2)));
            Term decoded = Term.Tuple(Term.A("first"), Term.Tuple(value, Nil.Value));
            if (!encoded.Equals(await module.Invoke(
                context,
                "encode",
                Term.Tuple(Term.A("first"), value),
                tags
            ))
                || !decoded.Equals(await module.Invoke(
                    context,
                    "decode",
                    Term.List(Term.Tuple(Term.I(2), value)),
                    tags
                )))
            {
                throw new InvalidOperationException(IncorrectResults);
            }

            try
            {
                await module.Invoke(
                    context,
                    "encode",
                    Term.Tuple(Term.A("first"), malformed),
                    tags
                );
                throw new InvalidOperationException(AcceptedInvalidPair);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badmatch"), malformed)))
            {
                return Nil.Value;
            }
        });

        object[] Read(string name, int frame)
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

            object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
            int[] frames = body.Where(instruction => Opcode(instruction) == Allocate)
                .Select(instruction => Value(Items(instruction, "Operands").First())).ToArray();
            if (frames is not [var size] || size != frame)
            {
                throw new InvalidOperationException(IncorrectInstructions);
            }

            return body;
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string member) =>
        ((IEnumerable)Property(owner, member)).Cast<object>();
}
