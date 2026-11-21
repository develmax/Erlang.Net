using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGuardedBytePrefixScannerChecks
{
    internal const string TestName = "compiler/beam/guarded-byte-groups-reuse-context-and-restore-on-error";
    private const string ShapeError = "The byte scanner did not share the guarded prefix and restore the input on failure.";
    private const string Source = """
        -module(beam_guarded_byte_prefix_scanner).
        -export([decode/1]).

        decode(Bin) -> decode(Bin,0).

        decode(<<>>, N) -> [{done,N}];
        decode(<<92, C, Tail/binary>>, N) when C == 65; C == 66 ->
            [{group_a,N,C}|decode(Tail,N+2)];
        decode(<<92, C, Tail/binary>>, N) when C == 67; C == 68 ->
            [{group_b,N,C}|decode(Tail,N+2)];
        decode(<<C/utf8, Tail/binary>>, N) when C /= 92 ->
            [classify(C,N)|decode(Tail,N+1)];
        decode(Bin, N) -> error({bad_character,N,Bin}).

        classify(C,N) -> {scalar,N,C}.
        """;

    internal static void Run()
    {
        string exported = Source.Replace("decode/1]", "decode/1,decode/2]", StringComparison.Ordinal);
        string nonInteger = Source.Replace("decode(Bin,0).", "decode(Bin,0.0).", StringComparison.Ordinal);
        if (!HasSharedContext(Source) || HasSharedContext(exported) || HasSharedContext(nonInteger))
        {
            throw new InvalidOperationException(ShapeError);
        }
    }

    private static bool HasSharedContext(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, "beam_guarded_byte_prefix_scanner.erl"));
        var atoms = ((IEnumerable)Property(Property(code, "Structure"), "Atoms")).Cast<string?>().ToArray();
        var operations = ((IEnumerable)Property(code, "Instructions")).Cast<object>()
            .SkipWhile(operation => Opcode(operation) != FunctionInfo
                || atoms[Value(Operands(operation)[1])] != "decode" || Value(Operands(operation)[2]) != 2)
            .Skip(1).TakeWhile(operation => Opcode(operation) != FunctionInfo).Select(Opcode).ToArray();

        return operations.Count(opcode => opcode == StartBinaryMatch) == 1
            && operations.Count(opcode => opcode == GetBinaryPosition) == 1
            && operations.Count(opcode => opcode == SetBinaryPosition) == 2
            && operations.Contains(SelectValue) && operations.Contains(TrimFrame);
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object operation) => ((IEnumerable)Property(operation, "Operands")).Cast<object>().ToArray();

    private static byte Opcode(object operation) => (byte)Property(Property(operation, "Definition"), "Opcode");
}
