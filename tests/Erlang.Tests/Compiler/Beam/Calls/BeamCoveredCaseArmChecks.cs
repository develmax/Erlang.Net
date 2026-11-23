using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamCoveredCaseArmChecks
{
    internal const string TestName = "compiler/beam/removes-covered-adjacent-case-arms";
    private const string Filename = "beam_covered_case_arm.erl";
    private const string Cover = "covered_detail";
    private const string Binding = "binding_detail";
    private const string Guard = "guard_detail";
    private const string Effect = "effect_detail";
    private const string Order = "order_detail";
    private const string LocalTail = "ordinary-header";
    private const string GuardedTail = "guarded-header";
    private const string OverlappingTail = "overlap-header";
    private const string EffectfulTail = "effectful-header";

    private const string Source = """
        -module(beam_covered_case_arm).
        -record(saved, {file, handle, cont}).
        -export([open/1, binding/1, guarded/1, effect/1, ordered/1, collect/3, guarded_tail/3, overlap_tail/3, effect_tail/3]).

        open(Filename) ->
            File = Filename,
            case provider:check(File) of
                {error, Reason} -> {error, Reason};
                _Info ->
                    case provider:open(File) of
                        {ok, Handle} -> {ok, #saved{file = File, handle = Handle, cont = start}};
                        {repaired, Handle, _, {covered_detail, 0}} -> {ok, #saved{file = File, handle = Handle, cont = start}};
                        {repaired, Handle, _, _} -> {ok, #saved{file = File, handle = Handle, cont = start}};
                        {error, Reason} -> {error, Reason}
                    end
            end.

        binding(Value) ->
            case Value of
                {repaired, Field, _, {binding_detail, 0}} -> {ok, Field};
                {repaired, _, Field, _} -> {ok, Field}
            end.

        guarded(Value) ->
            case Value of
                {repaired, Field, _, {guard_detail, 0}} -> {ok, Field};
                {repaired, Field, _, _} when is_integer(Field) -> {ok, Field}
            end.

        effect(Value) ->
            case Value of
                {repaired, Field, _, {effect_detail, 0}} -> {ok, provider:finish(Field)};
                {repaired, Field, _, _} -> {ok, provider:finish(Field)}
            end.

        ordered(Value) ->
            case Value of
                {repaired, Field, _, {order_detail, 0}} -> {ok, Field};
                {repaired, Field, _, {_, 0}} -> {special, Field};
                {repaired, Field, _, _} -> {ok, Field}
            end.

        collect([], Acc, _Accepted) -> Acc;
        collect([{Key, Value} | Rest], Acc, Accepted) ->
            case Key of
                "ordinary-header" -> collect(Rest, [{Key, Value} | Acc], Accepted);
                "ignored-header" -> collect(Rest, Acc, Accepted);
                _ -> collect(Rest, [{Key, Value} | Acc], Accepted)
            end.

        guarded_tail([], Acc, _Accepted) -> Acc;
        guarded_tail([{Key, Value} | Rest], Acc, Accepted) ->
            case Key of
                "guarded-header" when Accepted -> guarded_tail(Rest, [{Key, Value} | Acc], Accepted);
                _ when is_atom(Key) -> guarded_tail(Rest, [{Key, Value} | Acc], Accepted)
            end.

        overlap_tail([], Acc, Accepted) -> {Acc, Accepted};
        overlap_tail([{Key, Value} | Rest], Acc, Accepted) ->
            case Key of
                "overlap-header" -> overlap_tail(Rest, [{Key, Value} | Acc], Accepted);
                [$o | _] -> overlap_tail(Rest, Acc, Accepted);
                _ -> overlap_tail(Rest, [{Key, Value} | Acc], Accepted)
            end.

        effect_tail([], Acc, Accepted) -> {Acc, Accepted};
        effect_tail([{Key, Value} | Rest], Acc, Accepted) ->
            case Key of
                "effectful-header" -> effect_tail(Rest, [provider:finish(Value) | Acc], Accepted);
                _ -> effect_tail(Rest, [provider:finish(Value) | Acc], Accepted)
            end.
        """;

    internal static Task Run()
    {
        byte[] original = CodeGeneration.CompileBeamModule(Source, Filename);
        CheckRemoval(original, Cover, equivalent: true);
        CheckRemoval(original, Binding, equivalent: false);
        CheckRemoval(original, Guard, equivalent: false);
        CheckRemoval(original, Effect, equivalent: false);
        CheckRemoval(original, Order, equivalent: false);
        CheckRemoval(original, LocalTail, equivalent: true);
        CheckRemoval(original, GuardedTail, equivalent: false);
        CheckRemoval(original, OverlappingTail, equivalent: false);
        CheckRemoval(original, EffectfulTail, equivalent: false);

        return Task.CompletedTask;
    }

    private static void CheckRemoval(byte[] original, string marker, bool equivalent)
    {
        int markerIndex = Source.IndexOf(marker, StringComparison.Ordinal);
        int lineStart = Source.LastIndexOf('\n', markerIndex) + 1;
        int lineEnd = Source.IndexOf('\n', markerIndex);
        // Leave the newline so offsets for every retained expression are unchanged.
        byte[] alternative = CodeGeneration.CompileBeamModule(Source.Remove(lineStart, lineEnd - lineStart), Filename);
        if (original.SequenceEqual(alternative) != equivalent)
        {
            throw new InvalidOperationException($"Covered-arm byte comparison for {marker} expected equality {equivalent}.");
        }
    }
}
