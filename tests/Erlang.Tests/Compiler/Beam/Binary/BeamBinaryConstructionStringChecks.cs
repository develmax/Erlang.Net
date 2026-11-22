using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamBinaryConstructionStringChecks
{
    internal const string TestName = "compiler/beam/coalesces-adjacent-binary-construction-strings";
    private const string Fixture = "beam_binary_construction_string_probe.erl";
    private const string IncorrectLayout = "The binary construction string runs, alignment or segment origins changed.";
    private const string IncorrectValue = "The emitted binary construction returned an unexpected bitstring.";

    internal const string Source = """
        -module(beam_binary_construction_string_probe).
        -export([split/2, partial/1, native/1, control/1, packed/1, barrier/1]).
        split(X, Y) -> <<X/binary, 13, 10, Y/binary, "0", 13, 10>>.
        partial(X) -> <<17, 34, X:5>>.
        native(X) -> <<17, 34, 4660:16/native, 68, 85, X>>.
        control(X) -> <<X/binary, 42>>.
        packed(X) -> <<5:3, 19:5, X:8, 7:3, 1:7>>.
        barrier(X) -> <<5:3, X:8, 1:5>>.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        object structure = Property(code, "Structure");
        string[] atoms = Items(structure, "Atoms").Cast<string>().ToArray();
        object chunk = Items(structure, "Chunks").Single(entry =>
            (string)Property(entry, "Id") == BeamBinaryHostNames.Strings);
        var strings = (ReadOnlyMemory<byte>)Property(chunk, "Payload");
        object[][] constructions = Items(code, "Instructions")
            .Where(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode") == CreateBinary)
            .Select(instruction => Items(instruction, "Operands").ToArray()).ToArray();
        if (constructions.Length != 6)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        VerifyConstruction(
            constructions[0],
            atoms,
            strings,
            8,
            [("append", 1, []), ("string", 0, [13, 10]), ("binary", 4, []), ("string", 0, [48, 13, 10])]
        );
        VerifyConstruction(
            constructions[1],
            atoms,
            strings,
            1,
            [("string", 0, [17, 34]), ("integer", 3, [])]
        );
        VerifyConstruction(
            constructions[2],
            atoms,
            strings,
            8,
            [("string", 0, [17, 34]), ("integer", 3, []), ("string", 0, [68, 85]), ("integer", 6, [])]
        );
        VerifyConstruction(
            constructions[3],
            atoms,
            strings,
            8,
            [("append", 1, []), ("string", 0, [42])]
        );

        VerifyConstruction(
            constructions[4],
            atoms,
            strings,
            2,
            [("string", 0, [179]), ("integer", 3, []), ("string", 0, [224]), ("integer", 0, [])]
        );
        VerifyConstruction(
            constructions[5],
            atoms,
            strings,
            16,
            [("integer", 0, []), ("integer", 2, []), ("integer", 0, [])]
        );

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            BitString left = new([1]);
            BitString right = new([2]);
            byte[] native = BitConverter.GetBytes((ushort)4660);
            if (!new BitString([1, 13, 10, 2, 48, 13, 10]).Equals(await module.Invoke(
                context,
                "split",
                left,
                right
            ))
                || !new BitString([17, 34, 248], 21).Equals(await module.Invoke(context, "partial", Term.I(31)))
                || !new BitString([17, 34, .. native, 68, 85, 7]).Equals(await module.Invoke(context, "native", Term.I(7)))
                || !new BitString([1, 42]).Equals(await module.Invoke(context, "control", left))
                || !new BitString([179, 42, 224, 64], 26).Equals(await module.Invoke(context, "packed", Term.I(42)))
                || !new BitString([165, 65]).Equals(await module.Invoke(context, "barrier", Term.I(42))))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            return Nil.Value;
        });
    }

    private static void VerifyConstruction(
        object[] operands,
        string[] atoms,
        ReadOnlyMemory<byte> strings,
        int alignment,
        (string Kind, int Number, byte[] Bytes)[] expected
    )
    {
        const int headerOperands = 6;
        const int segmentOperands = 6;
        if (Value(operands[3]) != alignment || operands.Length != headerOperands + expected.Length * segmentOperands)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        int index = headerOperands;
        foreach (var segment in expected)
        {
            if (atoms[Value(operands[index])] != segment.Kind
                || Value(operands[index + 1]) != segment.Number
                || segment.Kind == "string"
                    && !strings.Span.Slice(Value(operands[index + 4]), Value(operands[index + 5])).SequenceEqual(segment.Bytes))
            {
                throw new InvalidOperationException(IncorrectLayout);
            }

            index += segmentOperands;
        }
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
