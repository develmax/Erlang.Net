using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamBinaryArgumentFrameChecks
{
    internal const string TestName = "compiler/beam/binary-argument-branch-frame";
    private const string Module = "beam_binary_argument_frame_probe";
    private const string Fixture = Module + ".erl";
    private const string WrongFrame = "The binary argument branch or its fallback has incorrect frame ownership.";
    private const string WrongResult = "The binary argument branch changed its result.";

    private const string Source = """
        -module(beam_binary_argument_frame_probe).
        -export([wrap/3, retain/3, pair/3, zero/1]).
        wrap(_, Input, _) ->
            case Input of
                Binary when is_binary(Binary) -> {tag, Binary};
                _ -> {tag, encode(Input)}
            end.
        retain(_, Input, Data) ->
            case Input of
                Binary when is_binary(Binary) -> {tag, Binary, Data};
                _ -> {tag, encode(Input), Data}
            end.
        pair(_, Input, _) ->
            case Input of
                Binary when is_binary(Binary) -> {fast, Binary};
                _ -> {slow, encode(Input)}
            end.
        zero(Input) ->
            case Input of
                Binary when is_binary(Binary) -> {tag, Binary};
                _ -> {tag, encode(Input)}
            end.
        encode(Value) -> erlang:iolist_to_binary(Value).
        """;

    internal static async Task Run()
    {
        byte[] beam = CompileFixture();
        CheckFrames(
            beam,
            "wrap",
            2,
            needsFrameSlots: false
        );
        CheckFrames(
            beam,
            "zero",
            1,
            needsFrameSlots: false
        );
        CheckFrames(
            beam,
            "retain",
            3,
            needsFrameSlots: true
        );

        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term data = Term.A("data");
            Term binary = new BitString(new byte[] { 65 });
            Term input = Term.List(Term.I(65));
            Term wrapped = Term.Tuple(Term.A("tag"), binary);
            if (!wrapped.Equals(await module.Invoke(
                context,
                "wrap",
                data,
                binary,
                data
            ))
                || !wrapped.Equals(await module.Invoke(
                    context,
                    "wrap",
                    data,
                    input,
                    data
                ))
                || !wrapped.Equals(await module.Invoke(context, "zero", input))
                || !Term.Tuple(Term.A("tag"), binary, data).Equals(await module.Invoke(
                    context,
                    "retain",
                    data,
                    input,
                    data
                ))
                || !Term.Tuple(Term.A("fast"), binary).Equals(await module.Invoke(
                    context,
                    "pair",
                    data,
                    binary,
                    data
                ))
                || !Term.Tuple(Term.A("slow"), binary).Equals(await module.Invoke(
                    context,
                    "pair",
                    data,
                    input,
                    data
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            return Nil.Value;
        });
    }

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    private static void CheckFrames(
        byte[] beam,
        string function,
        int acceptedLive,
        bool needsFrameSlots
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
        object test = body.First(instruction => Opcode(instruction) == IsBinary);
        if (needsFrameSlots)
        {
            if (!body.Any(instruction => Opcode(instruction) == Allocate
                && Value(Children(instruction, "Operands").First()) > 0))
            {
                throw new InvalidOperationException(WrongFrame);
            }

            return;
        }

        var failure = Value(Children(test, "Operands").First());
        int rejected = Array.FindIndex(body, instruction => Opcode(instruction) == Label
            && Value(Children(instruction, "Operands").First()) == failure);
        object[] accepted = body.Take(rejected).Where(instruction => Opcode(instruction) is not (Label or Line)).ToArray();
        if (accepted.Length != 4
            || !ReferenceEquals(accepted[0], test)
            || Opcode(accepted[1]) != TestHeap
            || Value(Children(accepted[1], "Operands").Last()) != acceptedLive
            || Opcode(accepted[2]) != PutTuple
            || Opcode(accepted[3]) != Return
            || Opcode(body[rejected + 1]) != Allocate
            || Value(Children(body[rejected + 1], "Operands").First()) != 0)
        {
            throw new InvalidOperationException(WrongFrame);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
