using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRecursiveComprehensionFrameChecks
{
    internal const string TestName = "compiler/beam/retains-only-the-needed-recursive-list-head";
    internal const string Fixture = "beam_recursive_comprehension_frames.erl";
    private const string WrongProjection = "Expected the record check before the frame and its safe field read after recursion.";
    private const string WrongConstructor = "Expected pure nested singleton constructors after the recursive call.";

    internal const string Source = """
        -module(beam_recursive_comprehension_frames).
        -export([fields/1, nested/1, effects/1, observe/0]).
        -record(entry, {value, padding}).

        fields(Input) ->
            Proper = [Item || Item <- Input],
            [Record#entry.value || Record <- Proper].

        nested(Input) ->
            Proper = [Item || Item <- Input],
            [[[ {tag, Value} ]] || Value <- Proper].

        effects(Input) ->
            Proper = [Item || Item <- Input],
            [[{tag, track(Value)}] || Value <- Proper].

        track(Value) ->
            case get(effects) of
                undefined -> put(effects, [Value]);
                Seen -> put(effects, [Value | Seen])
            end,
            Value.

        observe() ->
            FieldValues = fields([#entry{value = first}, #entry{value = last}]),
            Wrapped = nested([first, last]),
            erase(effects),
            EffectValues = effects([first, last]),
            Rejection = try fields([#entry{value = first}, broken, other])
                        catch error:{badrecord, broken}:Trace ->
                            [{?MODULE, _, _, Frame} | _] = Trace,
                            proplists:get_value(line, Frame)
                        end,
            {FieldValues, Wrapped, EffectValues, get(effects), Rejection}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        byte[] all = ((IEnumerable)Property(code, "Instructions")).Cast<object>()
            .Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode"))
            .ToArray();
        int[] boundaries = [.. Enumerable.Range(0, all.Length).Where(index => all[index] == FunctionInfo), all.Length];
        var bodies = boundaries.Zip(boundaries.Skip(1), (start, end) => all[start..end]).ToArray();

        if (!bodies.Any(ReadsCheckedFieldAfterCall))
        {
            throw new InvalidOperationException(WrongProjection);
        }

        if (!bodies.Any(ConstructsNestedTupleAfterCall))
        {
            throw new InvalidOperationException(WrongConstructor);
        }

        return Task.CompletedTask;
    }

    private static bool ReadsCheckedFieldAfterCall(byte[] body)
    {
        int check = Array.IndexOf(body, IsTaggedTuple);
        int frame = Array.IndexOf(body, Allocate);
        int call = Array.IndexOf(body, LocalCall);
        int projection = Array.IndexOf(body, GetTupleElement);

        return check >= 0 && frame > check && call > frame && projection > call && body.Contains(BadRecord);
    }

    private static bool ConstructsNestedTupleAfterCall(byte[] body)
    {
        int call = Array.IndexOf(body, LocalCall);
        int construction = Array.IndexOf(body, PutTuple);

        return call >= 0 && construction > call && body[construction..].Count(opcode => opcode == PutList) == 3;
    }
}
