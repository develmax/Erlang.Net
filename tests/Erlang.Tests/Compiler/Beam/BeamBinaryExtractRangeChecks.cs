using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Tests;

internal static class BeamBinaryExtractRangeChecks
{
    internal const string TestName = "compiler/beam/binary-extract-keeps-proven-operand-range";
    private const string ExpectedRanges = "Expected bounds from fixed binary matches and UTF without typing unrelated or dynamic operands.";

    internal static Task Compare()
    {
        const string source = """
            -module(binary_extract_range_probe).
            -export([fixed/1, plus/1, signed_le/1, utf/1, dynamic/2, overwrite/2]).
            fixed(<<N:8, _/binary>>) -> if N >= 17 -> high; true -> low end;
            fixed(_) -> invalid.
            plus(<<N:6, _/bits>>) -> N + 1;
            plus(_) -> invalid.
            signed_le(<<N:16/little-signed, _/binary>>) -> if N < 0 -> negative; true -> nonnegative end;
            signed_le(_) -> invalid.
            utf(<<N/utf16, _/binary>>) -> if N >= 80 -> high; true -> low end;
            utf(_) -> invalid.
            dynamic(B, Width) -> case B of <<N:Width, _/bits>> -> N + 1; _ -> invalid end.
            overwrite(<<N:8, _/binary>>, Other) -> {N, if Other >= 17 -> high; true -> low end};
            overwrite(_, _) -> invalid.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "binary_extract_range_probe.erl");
        var chunks = BeamDocumentationEmissionChecks.Chunks(beam);
        byte[] types = chunks.Single(chunk => chunk.Id == "Type").Data;
        ReadOnlySpan<byte> code = chunks.Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> byteRange =
        [
            0x60, 0x20, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255
        ];
        ReadOnlySpan<byte> sixBitRange =
        [
            0x60, 0x20, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 63
        ];
        ReadOnlySpan<byte> unicodeRange =
        [
            0x60, 0x20, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x10, 0xff, 0xff
        ];
        if (types.Length != 64 || !types.AsSpan(10, 18).SequenceEqual(byteRange)
            || !types.AsSpan(28, 18).SequenceEqual(sixBitRange) || !types.AsSpan(46, 18).SequenceEqual(unicodeRange)
            || Count(code, [ExtendedTypedRegisterOperand, 0x04, 0x10]) != 1
            || Count(code, [ExtendedTypedRegisterOperand, 0x04, 0x20]) != 1
            || Count(code, [ExtendedTypedRegisterOperand, 0x04, 0x30]) != 1
            || CompileWithoutSsa(source).Length != 10)
        {
            throw new InvalidOperationException(ExpectedRanges);
        }

        return Task.CompletedTask;
    }

    private static int Count(ReadOnlySpan<byte> code, ReadOnlySpan<byte> marker)
    {
        int uses = 0;
        for (int index = 0; index <= code.Length - marker.Length; index++)
        {
            if (code[index..].StartsWith(marker))
            {
                uses++;
            }
        }

        return uses;
    }

    private static byte[] CompileWithoutSsa(string source)
    {
        var module = new Parser(source.Insert(source.IndexOf('\n') + 1, "-compile(no_ssa_opt).\n")).ParseModule();
        var lines = Enumerable.Range(3, module.DeclaredFunctions.Count).Select(line => new BeamSourceLocation(null, line)).ToArray();
        byte[] beam = new BeamModuleCompiler(module.Name, sourceModule: module).Emit(
            module.DeclaredFunctions,
            lines,
            module.DeclaredExports.ToHashSet(),
            new BeamModuleMetadata(module.Attributes, null)
        );

        return BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Type").Data;
    }
}
