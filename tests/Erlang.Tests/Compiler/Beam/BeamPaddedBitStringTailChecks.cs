using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamPaddedBitStringTailChecks
{
    internal const string TestName = "compiler/beam/builds-and-measures-padded-bitstrings-in-entry-registers";
    private const string Fixture = "beam_padded_bit_string_tail_probe.erl";
    private const string IncorrectLayout = "Padding, binary construction or byte measurement spilled the entry arguments.";
    private const string IncorrectValue = "The padded bitstring bytes, length or argument failure changed.";

    private const string Source = """
        -module(beam_padded_bit_string_tail_probe).
        -export([pad/2]).
        pad(Bits, Options) ->
            Unused = (8 - bit_size(Bits) band 7) band 7,
            Binary = <<Unused, Bits/bitstring, 0:Unused>>,
            finish(Options, Binary, byte_size(Binary)).
        finish(Options, Binary, Size) -> {Options, Binary, Size}.
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
                && atoms[Value(atom)] == "pad" && Value(arity) == 2
        );
        if (start < 0)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        if (body.Any(instruction => Opcode(instruction) == Allocate)
            || body.Count(instruction => Opcode(instruction) == CreateBinary) != 1
            || body.Count(instruction => Opcode(instruction) == GcBifTwo) != 3
            || body.Count(instruction => Opcode(instruction) == Swap) != 1)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] types = Items(Call(Type("BeamTypeTableReader"), "Read", Property(code, "Structure")), "Entries").ToArray();
        object construction = body.Single(instruction => Opcode(instruction) == CreateBinary);
        object[] input = Items(construction, "Operands")
            .Where(operand => Property(operand, "TypeIndex") is int && Value(operand) == 0).ToArray();
        if (input is not [var source] || Property(source, "TypeIndex") is not int index
            || (int)Property(types[index], "MetadataFlags") != 0)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term options = Term.A("selected");
            (BitString Input, BitString Output, int Count)[] paths =
            [
                (new BitString([]), new BitString([0]), 1),
                (new BitString([189]), new BitString([0, 189]), 2),
                (new BitString([160], 3), new BitString([5, 160]), 2),
                (new BitString([255, 128], 9), new BitString([7, 255, 128]), 3)
            ];
            foreach (var (input, expected, count) in paths)
            {
                if (!Term.Tuple(options, expected, Term.I(count)).Equals(await module.Invoke(
                    context,
                    "pad",
                    input,
                    options
                )))
                {
                    throw new InvalidOperationException(IncorrectValue);
                }
            }

            try
            {
                _ = await module.Invoke(
                    context,
                    "pad",
                    Term.I(1),
                    options
                );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(IncorrectValue);
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
