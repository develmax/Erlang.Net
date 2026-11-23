using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamUnpaddedBitStringContextChecks
{
    internal const string TestName = "compiler/beam/keeps-the-position-of-unpadded-bitstrings-on-a-failed-match";
    private const string Fixture = "beam_unpadded_bit_string_context_probe.erl";
    private const string IncorrectContext = "The decoder recreated the match context or lost its failure position.";
    private const string IncorrectResult = "The binary padding count or recoverable failed-match value changed.";

    private const string Source = """
        -module(beam_unpadded_bit_string_context_probe).
        -export([decode/2]).
        decode(Buffer, Tags) ->
            case collect(Buffer, Tags) of
                <<0>> ->
                    <<>>;
                <<Unused, Bits/binary>> ->
                    Size = bit_size(Bits) - Unused,
                    <<Value:Size/bitstring, _:Unused/bitstring>> = Bits,
                    Value
            end.
        collect(Buffer, direct) when is_binary(Buffer) -> Buffer;
        collect(Buffer, from_list) -> list_to_binary(Buffer).
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
            throw new InvalidOperationException(IncorrectContext);
        }

        object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        object[] allocations = body.Where(instruction => Opcode(instruction) == Allocate).ToArray();
        if (allocations is not [var allocation] || Items(allocation, "Operands").Select(Value).ToArray() is not [0, 2]
            || body.Count(instruction => Opcode(instruction) == StartBinaryMatchKnown) != 1
            || body.Count(instruction => Opcode(instruction) == GetBinaryPosition) != 1
            || body.Count(instruction => Opcode(instruction) == SetBinaryPosition) != 1)
        {
            throw new InvalidOperationException(IncorrectContext);
        }

        object[] types = Items(Call(Type("BeamTypeTableReader"), "Read", Property(code, "Structure")), "Entries").ToArray();
        object[] contexts = body.Where(instruction => Opcode(instruction) is GetBinarySegment or SkipBinaryBits)
            .Select(instruction => Items(instruction, "Operands").ElementAt(1)).ToArray();
        if (contexts.Length != 2 || contexts.Any(operand => Property(operand, "TypeIndex") is not int index
            || (int)Property(types[index], "MetadataFlags") != 0))
        {
            throw new InvalidOperationException(IncorrectContext);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term empty = new BitString([]);
            Term direct = Term.A("direct");
            Term fromList = Term.A("from_list");
            Term fiveBits = new BitString([168], 5);
            if (!empty.Equals(await module.Invoke(
                context,
                "decode",
                new BitString([0]),
                direct
            ))
                || !fiveBits.Equals(await module.Invoke(
                    context,
                    "decode",
                    new BitString([3, 171]),
                    direct
                ))
                || !fiveBits.Equals(await module.Invoke(
                    context,
                    "decode",
                    Term.List(Term.I(3), Term.I(171)),
                    fromList
                )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            await CheckFailure(empty, Term.Tuple(Term.A("case_clause"), empty));
            await CheckFailure(new BitString([255, 171]), Term.Tuple(Term.A("badmatch"), new BitString([171])));

            return Nil.Value;

            async Task CheckFailure(Term input, Term reason)
            {
                try
                {
                    _ = await module.Invoke(
                        context,
                        "decode",
                        input,
                        direct
                    );
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error && error.Reason.Equals(reason))
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
