using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Tests;

internal static class BeamCallArithmeticCorridorChecks
{
    internal const string TestName = "compiler/beam/rooted-call-arithmetic-corridors";

    internal static Task Run()
    {
        const string source = """
            -module(beam_call_arithmetic_corridors).
            -export([single/1, chain/1, guarded/1, protected/1, opaque/1]).
            single(Encoded) ->
                N = binary_to_term(Encoded),
                R = N bsl 3,
                {term_to_binary(R), N}.
            chain(Encoded) ->
                N = binary_to_term(Encoded),
                Masked = (N bsl 5) band 255,
                Result = ?MODULE:opaque(ready),
                {N, Masked, Result}.
            guarded(Encoded) ->
                N = binary_to_term(Encoded),
                case N of X when X bsl 3 > 0 -> positive; X -> X end.
            protected(Encoded) ->
                try
                    N = binary_to_term(Encoded),
                    R = N bsl 3,
                    {term_to_binary(R), N}
                catch error:badarith -> handled
                end.
            opaque(X) ->
                put(arithmetic_probe, X).
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "beam_call_arithmetic_corridors.erl");
        ReadOnlySpan<byte> code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> single =
        [
            GcBifTwo, 0x05, 0x10, 0x10, 0x03, 0x31, 0x13,
            Move, 0x03, 0x04,
            Move, 0x13, 0x03,
            ExternalCall, 0x10, 0x20
        ];
        ReadOnlySpan<byte> chain =
        [
            GcBifTwo, 0x05, 0x10, 0x10, 0x03, 0x51, 0x13,
            GcBifTwo, 0x05, 0x20, 0x30, ExtendedTypedRegisterOperand, 0x13, 0x10, 0x09, 0xff, 0x14,
            Move, 0x03, 0x04
        ];
        ReadOnlySpan<byte> guard = [GcBifTwo, 0x75, 0x00, 0x10, 0x04, 0x31, 0x03];
        ReadOnlySpan<byte> protectedOperation = [GcBifTwo, 0x05, 0x00, 0x10, 0x14, 0x31, 0x03];
        if (code.IndexOf(single) < 0 || code.IndexOf(chain) < 0)
        {
            throw new InvalidOperationException("Arithmetic before a call lost the original result, GC roots or typed register.");
        }

        if (code.IndexOf(guard) < 0 || code.IndexOf(protectedOperation) < 0)
        {
            throw new InvalidOperationException("Arithmetic moved across an active guard or exception handler.");
        }

        _ = BeamModule.Prepare(beam);

        return Task.CompletedTask;
    }
}
