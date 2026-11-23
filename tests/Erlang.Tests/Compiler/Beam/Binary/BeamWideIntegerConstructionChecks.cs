using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamWideIntegerConstructionChecks
{
    internal const string TestName = "compiler/beam/keeps-wide-bit-construction-sign-fill-compact";
    private const string Fixture = "beam_wide_integer_construction_probe.erl";
    private const string Source = """
        -module(beam_wide_integer_construction_probe).
        -export([zero/1, negative/1, split/1, split_negative/1, short/1,
                 truncated/1, dynamic/2, run/0]).

        zero(X) -> <<0:8/unit:8, X:8/unit:8>>.
        negative(X) -> <<(-1):64, X:8>>.
        split(X) -> <<16#fe:80, X:16>>.
        split_negative(X) -> <<(-2):80, X:16>>.
        short(X) -> <<0:56, X:8>>.
        truncated(X) -> <<16#10000000000000001:64, X:8>>.
        dynamic(N, X) -> <<0:N, X:8>>.

        run() ->
            <<0:64, 42:64>> = zero(42),
            <<16#ffffffffffffffff:64, 42:8>> = negative(42),
            <<0:72, 16#fe:8, 42:16>> = split(42),
            <<(-1):72/signed, 16#fe:8, 42:16>> = split_negative(42),
            <<0:56, 42:8>> = short(42),
            <<1:64, 42:8>> = truncated(42),
            <<0:3, 42:8>> = dynamic(3, 42),
            ok.
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(bytes);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] zero = SegmentOperands(instructions, atoms, "zero");
        object[] negative = SegmentOperands(instructions, atoms, "negative");
        object[] split = SegmentOperands(instructions, atoms, "split");
        object[] splitNegative = SegmentOperands(instructions, atoms, "split_negative");
        object[] shortWidth = SegmentOperands(instructions, atoms, "short");
        object[] truncated = SegmentOperands(instructions, atoms, "truncated");

        if (!HasPrefix(zero, atoms, (8, 0, 8)) || !HasPrefix(negative, atoms, (1, -1, 64))
            || !HasPrefix(split, atoms, (1, 0, 72)) || !HasPrefix(splitNegative, atoms, (1, -1, 72))
            || atoms[Value(split[6])] != "string" || Value(split[11]) != 1
            || atoms[Value(splitNegative[6])] != "string" || Value(splitNegative[11]) != 1
            || atoms[Value(shortWidth[0])] != "string" || Value(shortWidth[5]) != 7
            || atoms[Value(truncated[0])] != "string" || Value(truncated[5]) != 8)
        {
            throw new InvalidOperationException(BeamWideIntegerConstructionDiagnostics.Layout);
        }

        var image = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            if (!Term.A("ok").Equals(await image.Invoke(context, "run")))
            {
                throw new InvalidOperationException(BeamWideIntegerConstructionDiagnostics.Value);
            }

            try
            {
                await image.Invoke(context, "split", Term.A("invalid"));
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(BeamWideIntegerConstructionDiagnostics.Error);
        });
    }

    private static object[] SegmentOperands(object[] instructions, string[] atoms, string function)
    {
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands").ToArray() is [_, var atom, _]
                && atoms[Value(atom)] == function
        );
        if (start >= 0)
        {
            object construction = instructions.Skip(start + 1)
                .TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Single(instruction => Opcode(instruction) == CreateBinary);

            return Items(construction, "Operands").Skip(6).ToArray();
        }

        throw new InvalidOperationException(BeamWideIntegerConstructionDiagnostics.Layout);
    }

    private static bool HasPrefix(object[] segment, string[] atoms, (int Unit, int Number, int Size) expected)
        => atoms[Value(segment[0])] == "integer" && Value(segment[1]) == 1
            && Value(segment[2]) == expected.Unit && Value(segment[4]) == expected.Number
            && Value(segment[5]) == expected.Size;

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name)
        => ((IEnumerable)Property(owner, name)).Cast<object>();
}
