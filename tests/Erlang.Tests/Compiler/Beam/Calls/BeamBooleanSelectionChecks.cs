using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamBooleanSelectionChecks
{
    internal const string TestName = "compiler/beam/prepared-boolean-case-orders-clause-and-failure-labels";
    private const string BooleanSelectionMismatch = "Boolean selection labels or failure line do not match the emitted block order.";

    internal static Task Run()
    {
        const string source = """
            -module(disjoint_select).
            -export([tail_case/3, pure/1]).
            tail_case(M, F, A) ->
                case erlang:function_exported(M, F, 1) of
                    false -> probe:keep({missing, M, F, A});
                    true -> M:F(A)
                end.
            pure(A) ->
                case probe:select(A) of
                    false -> absent;
                    true -> here
                end.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "disjoint_select.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> tailSelection =
        [
            BeamEmissionFormat.SelectValue, 0x03, 0x55, BeamEmissionFormat.ExtendedListOperand, 0x40,
            0x52, 0x45, 0x62, 0x35, BeamEmissionFormat.Label, 0x30
        ];
        ReadOnlySpan<byte> tailFailure =
        [
            BeamEmissionFormat.Label, 0x50, BeamEmissionFormat.Line, 0x20, BeamEmissionFormat.CaseEnd, 0x03
        ];
        ReadOnlySpan<byte> pureFailure =
        [
            BeamEmissionFormat.Label, 0xA0, BeamEmissionFormat.CaseEnd, 0x03
        ];
        if (code.AsSpan().IndexOf(tailSelection) < 0
            || code.AsSpan().IndexOf(tailFailure) < 0 || code.AsSpan().IndexOf(pureFailure) < 0)
        {
            throw new InvalidOperationException(BooleanSelectionMismatch);
        }

        _ = BeamModule.Prepare(beam);

        return Task.CompletedTask;
    }
}
