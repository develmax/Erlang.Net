using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Tests;

internal static class BeamLocalAtomTupleResultChecks
{
    internal const string TestName = "compiler/beam/local-success-atom-or-tuple-result";
    private const int AtomOrTupleType = 0x0801;
    private const string WrongType = "The immediate tuple test should carry only its proven local return type.";
    private const string Source = """
        -module(beam_adjacent_numeric).
        %% options
        -export([value/2, choose/2]).
        value(Tag, Value) ->
            case choose(Tag, Value) of
                {_, Result} -> Result;
                _ -> absent
            end.
        choose(Tag, Value) ->
            case Tag of
                found -> {ok, Value};
                extra -> {other, Value, 0};
                refused -> erlang:error({refused, Value});
                _ -> error
            end.
        """;

    internal static Task Run()
    {
        byte[] direct = BeamAdjacentNumericResultChecks.Compile(Source);
        byte[] disabled = BeamAdjacentNumericResultChecks.Compile(
            Source.Replace("%% options", "-compile(no_ssa_opt).", StringComparison.Ordinal),
            disabled: true
        );
        byte[] mixed = BeamAdjacentNumericResultChecks.Compile(Source.Replace(
            "_ -> error",
            "other -> 4;\n        _ -> error",
            StringComparison.Ordinal
        ));
        byte[] remote = BeamAdjacentNumericResultChecks.Compile(Source.Replace(
            "case choose(Tag, Value)",
            "case beam_adjacent_numeric:choose(Tag, Value)",
            StringComparison.Ordinal
        ));
        byte[] dynamicCall = BeamAdjacentNumericResultChecks.Compile(Source.Replace(
            "value(Tag, Value) ->\n    case choose(Tag, Value)",
            "value(Producer, Value) ->\n    case Producer(found, Value)",
            StringComparison.Ordinal
        ));
        if (ReadType(direct) != AtomOrTupleType || ReadType(disabled) is not null
            || ReadType(mixed) == AtomOrTupleType || ReadType(remote) is not null || ReadType(dynamicCall) is not null)
        {
            throw new InvalidOperationException(WrongType);
        }

        return Task.CompletedTask;
    }

    private static int? ReadType(byte[] beam) => BeamAdjacentNumericResultChecks.InstructionInputType(
        beam,
        IsTuple,
        1,
        first: true
    );
}
