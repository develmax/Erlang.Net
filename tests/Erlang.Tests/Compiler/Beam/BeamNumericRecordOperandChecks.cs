using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Tests;

internal static class BeamNumericRecordOperandChecks
{
    internal const string TestName = "compiler/beam/record-field-carries-successful-number";
    private const int NumberType = 0x0028;
    private const int RecordValueOperand = 6;
    private const string Failure = "Expected a record value annotation from a valid arithmetic definition in the same block.";
    private const string Source = """
        -module(beam_adjacent_numeric).
        %% options
        -export([value/2]).
        -record(row, {name, value, a, b, c, d}).
        value(R = #row{}, Input) ->
            Replacement = Input + 1,
            R#row{value = Replacement}.
        """;

    internal static Task Run()
    {
        byte[] positive = BeamAdjacentNumericResultChecks.Compile(Source);
        byte[] disabled = BeamAdjacentNumericResultChecks.Compile(
            Source.Replace("%% options", "-compile(no_ssa_opt).", StringComparison.Ordinal),
            disabled: true
        );
        byte[] clobber = BeamAdjacentNumericResultChecks.Compile(Source.Replace(
            "value = Replacement",
            "value = probe:observe(Replacement)",
            StringComparison.Ordinal
        ));
        byte[] call = BeamAdjacentNumericResultChecks.Compile(Source.Replace(
            "R#row{",
            "probe:observe(Replacement), R#row{",
            StringComparison.Ordinal
        ));
        byte[] join = BeamAdjacentNumericResultChecks.Compile(Source
            .Replace("value/2", "value/3", StringComparison.Ordinal)
            .Replace("Input) ->", "Input, Mode) ->", StringComparison.Ordinal)
            .Replace("Input + 1", "case Mode of add -> Input + 1; _ -> Input end", StringComparison.Ordinal));
        byte[] bounded = BeamAdjacentNumericResultChecks.Compile(Source.Replace(
            "Input + 1",
            "erlang:byte_size(Input) + 1",
            StringComparison.Ordinal
        ));
        _ = BeamAdjacentNumericResultChecks.InstructionInputType(
            positive,
            AllocateHeap,
            2,
            first: true
        );
        if (ReadValueType(positive) != NumberType || ReadValueType(disabled, required: false) is not null
            || ReadValueType(clobber) is not null || ReadValueType(call) is not null
            || ReadValueType(join) is not null || ReadValueType(bounded) == NumberType)
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;
    }

    private static int? ReadValueType(byte[] beam, bool required = true) =>
        BeamAdjacentNumericResultChecks.InstructionInputType(
            beam,
            UpdateRecord,
            RecordValueOperand,
            required: required
        );
}
