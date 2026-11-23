using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Tests;

internal static class BeamEntryCheckedProjectionPairChecks
{
    internal const string TestName = "compiler/beam/checked-entry-keeps-two-projections-before-frame";
    private const string ExpectedProjectionPlacement = "Expected checked projections before selector dispatch or frame entry with the original fallback ownership.";

    internal static Task Compare()
    {
        const string direct = """
            -module(beam_entry_checked_projection_pair).
            -export([pick/2]).
            pick({Tag, Value}, State) ->
                Result = case Tag of
                    first -> probe:first(Value, State);
                    second -> probe:second(Value, State);
                    third -> probe:third(Value, State);
                    fourth -> probe:fourth(Value, State);
                    fifth -> probe:fifth(Value, State)
                end,
                {Tag, Result}.
            """;
        const string differentFailure = """
            -module(beam_entry_checked_projection_pair).
            -export([pick/2]).
            pick({Tag, Value}, State) ->
                Result = case Tag of
                    first -> probe:first(Value, State);
                    second -> probe:second(Value, State);
                    third -> probe:third(Value, State);
                    fourth -> probe:fourth(Value, State);
                    fifth -> probe:fifth(Value, State)
                end,
                {Tag, Result};
            pick(Other, State) ->
                {fallback, Other, State}.
            """;
        const string tagged = """
            -module(beam_entry_checked_projection_pair).
            -export([pick/3]).
            pick(Version, Info, {started, Node, Version, Info}) ->
                {ok, Node, []};
            pick(Version, Info, {started, Node, OtherVersion, OtherInfo}) ->
                Warning = probe:warning(Version, Info, OtherVersion, OtherInfo),
                {ok, Node, Warning}.
            """;

        const string sharedCall = """
            -module(beam_entry_checked_projection_pair).
            -export([pick/2]).
            pick({started, Value, Mode}, State) ->
                Prepared = case Mode of
                    cached -> cached;
                    _ -> probe:first(Mode, State)
                end,
                {Value, probe:second(Prepared, State)}.
            """;


        byte[] directCode = CompileCode(direct);
        byte[] fallbackCode = CompileCode(differentFailure);
        byte[] taggedCode = CompileCode(tagged);
        byte[] sharedCallCode = CompileCode(sharedCall);
        if (!HasPlainEntryPair(directCode, commonFailure: true, nextOpcode: SelectValue)
            || !HasPlainEntryPair(fallbackCode, commonFailure: false, nextOpcode: Allocate)
            || !HasTaggedEntryPair(taggedCode, input: 0x23, arity: 0x40)
            || HasTaggedEntryPair(sharedCallCode, input: 0x03, arity: 0x30))
        {
            throw new InvalidOperationException(ExpectedProjectionPlacement);
        }

        return Task.CompletedTask;
    }

    private static bool HasPlainEntryPair(ReadOnlySpan<byte> code, bool commonFailure, byte nextOpcode)
    {
        int prefixLength = nextOpcode == SelectValue ? 17 : 16;
        for (int index = 0; index <= code.Length - prefixLength; index++)
        {
            ReadOnlySpan<byte> it = code[index..];
            if (it[0] == IsTuple && (it[1] & 0x0f) == 5 && (it[1] == 0x15) == commonFailure
                && it[2] == 0x03 && it[3] == TestArity && it[4] == it[1] && it[5] == 0x03 && it[6] == 0x20
                && IsRegisterPair(
                    it[7..],
                    0x03,
                    0x00,
                    0x10,
                    nextOpcode
                )
                && (nextOpcode != SelectValue || it[16] == it[10])
                && (commonFailure || it[14] == 0x33))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasTaggedEntryPair(ReadOnlySpan<byte> code, byte input, byte arity)
    {
        ReadOnlySpan<byte> prefix = [IsTaggedTuple, 0x15, input, arity];
        int index = code.IndexOf(prefix);
        if (index < 0 || index > code.Length - 14)
        {
            return false;
        }

        return IsRegisterPair(
            code[(index + prefix.Length + 1)..],
            input,
            0x10,
            0x20,
            Allocate
        );
    }

    private static bool IsRegisterPair(
        ReadOnlySpan<byte> code,
        byte input,
        byte first,
        byte second,
        byte nextOpcode
    ) =>
        code[0] == GetTupleElement && code[1] == input && code[2] == first && (code[3] & 0x0f) == 3
        && code[4] == GetTupleElement && code[5] == input && code[6] == second && (code[7] & 0x0f) == 3
        && code[8] == nextOpcode;

    private static byte[] CompileCode(string source)
    {
        byte[] beam = CodeGeneration.CompileBeamModule(source, "beam_entry_checked_projection_pair.erl");
        _ = BeamModule.Prepare(beam);

        return BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
    }
}
