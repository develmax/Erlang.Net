using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamBinaryConstructionUnitChecks
{
    internal const string TestName = "compiler/beam/infers-binary-construction-result-unit";
    private const string Fixture = "beam_binary_construction_unit_probe.erl";
    private const string IncorrectUnit = "The construction result unit did not match the fixed and variable bit widths.";
    private const string IncorrectValue = "The emitted binary construction returned an unexpected bitstring.";

    internal const string Source = """
        -module(beam_binary_construction_unit_probe).
        -export([fixed/1, small/2, variable/2, prefix/1, maximum/1, utf/2]).
        fixed(X) -> <<17, 34, X:16>>.
        small(X, Y) -> <<X:3, Y:5>>.
        variable(X, N) -> <<17, 34, X:N/unit:16>>.
        prefix(X) -> <<X/binary, 17, 34>>.
        maximum(X) -> <<X:256, X:256>>.
        utf(X, Y) -> <<X/utf32, Y:32>>.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        const int unitOperand = 3;
        int[] units = Items(code, "Instructions")
            .Where(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode") == CreateBinary)
            .Select(instruction => (int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(unitOperand), "Value"))
            .ToArray();
        if (!units.SequenceEqual([32, 8, 16, 8, 256, 64]))
        {
            throw new InvalidOperationException(IncorrectUnit);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            (string Function, Term[] Args, BitString Result)[] probes =
            [
                ("fixed", [Term.I(0x1234)], new BitString([17, 34, 18, 52])),
                ("small", [Term.I(5), Term.I(18)], new BitString([178])),
                ("variable", [Term.I(0x1234), Term.I(1)], new BitString([17, 34, 18, 52])),
                ("prefix", [new BitString([1, 2])], new BitString([1, 2, 17, 34])),
                ("maximum", [Term.I(0)], new BitString(new byte[64])),
                ("utf", [Term.I(65), Term.I(42)], new BitString([0, 0, 0, 65, 0, 0, 0, 42]))
            ];
            foreach (var (function, args, expected) in probes)
            {
                if (!expected.Equals(await module.Invoke(context, function, args)))
                {
                    throw new InvalidOperationException(IncorrectValue);
                }
            }

            return Nil.Value;
        });
    }

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
