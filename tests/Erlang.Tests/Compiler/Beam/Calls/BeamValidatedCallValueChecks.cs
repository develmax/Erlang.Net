using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Tests;

internal static class BeamValidatedCallValueChecks
{
    internal const string TestName = "compiler/beam/validated-call-value-registers";

    internal static Task Run()
    {
        const string source = """
            -module(beam_validated_call_value).
            -export([decode/1, align/1, ranged/1, rejected/1, protected/1]).

            decode(Input) ->
                Value = binary_to_term(Input),
                Size = byte_size(Value),
                if Size =:= 4 -> binary_to_list(Value);
                   true -> exit(bad_range)
                end.

            align(Input) ->
                Value = iolist_to_binary(Input),
                Size = byte_size(Value),
                if Size =:= 4 -> [align|Value] end.

            ranged(Input) ->
                Value = binary_to_term(Input),
                Size = byte_size(Value),
                if Size >= 2, Size < 8 -> Value;
                   true -> exit(bad_range)
                end.

            rejected(Input) ->
                Value = binary_to_term(Input),
                Size = byte_size(Value),
                if Size =:= 4 -> Value;
                   true -> {bad_range, Value}
                end.

            protected(Input) ->
                try
                    Value = binary_to_term(Input),
                    Size = byte_size(Value),
                    if Size =:= 4 -> binary_to_list(Value);
                       true -> exit(bad_range)
                    end
                catch
                    error:badarg -> caught
                end.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "beam_validated_call_value.erl");
        ReadOnlySpan<byte> code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> decode =
        [
            GcBifOne, 0x05, 0x10, 0x10, 0x03, 0x13,
            ExactEqualTest, 0x35, ExtendedTypedRegisterOperand, 0x13, 0x10, 0x41
        ];
        ReadOnlySpan<byte> range =
        [
            GcBifOne, 0x05, 0x10, 0x10, 0x03, 0x13,
            IsGreaterOrEqual, 0x95, ExtendedTypedRegisterOperand, 0x13, 0x10, 0x21,
            IsLess, 0x95, 0x13, 0x81, Deallocate, 0x00, Return
        ];
        ReadOnlySpan<byte> construction =
        [
            GcBifOne, 0x05, 0x10, 0x10, ExtendedTypedRegisterOperand, 0x03, 0x20, 0x13,
            ExactEqualTest, 0x65, ExtendedTypedRegisterOperand, 0x13, 0x10, 0x41,
            TestHeap, 0x20, 0x10, PutList, 0x92, 0x03, 0x03, Deallocate, 0x00, Return
        ];
        ReadOnlySpan<byte> failureReadsValue =
        [
            GcBifOne, 0x05, 0x00, 0x10, 0x04, 0x03, Move, 0x03, 0x13,
            ExactEqualTest, 0xc5, ExtendedTypedRegisterOperand, 0x13, 0x10, 0x41
        ];
        ReadOnlySpan<byte> activeHandler =
        [
            GcBifOne, 0x05, 0x00, 0x10, 0x14, 0x03, Move, 0x03, 0x04
        ];
        if (code.IndexOf(decode) < 0 || code.IndexOf(range) < 0 || code.IndexOf(construction) < 0)
        {
            throw new InvalidOperationException("Size validation did not retain its call value and roots in X registers.");
        }

        if (code.IndexOf(failureReadsValue) < 0 || code.IndexOf(activeHandler) < 0)
        {
            throw new InvalidOperationException("Size validation changed a frame with a dependent exit or live handler.");
        }

        _ = BeamModule.Prepare(beam);

        return Task.CompletedTask;
    }
}
