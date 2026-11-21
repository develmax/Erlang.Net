using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalReconstructionChecks
{
    internal const string TestName = "compiler/beam/folds-exhaustive-terminal-reconstruction";
    internal const string Fixture = "beam_terminal_reconstruction.erl";
    private const string RetainedRoundTrip = "Exhaustive terminal reconstruction retained a tagged branch.";
    private const string RemovedRequiredMatch = "Terminal reconstruction removed a binding or a changed result.";

    internal const string Source = """
        -module(beam_terminal_reconstruction).
        -export([same/1, bound/2, changed/1, observe/0]).

        same(Value) ->
            case selector([Value]) of
                {accepted, Result} -> {accepted, Result};
                Other -> Other
            end.

        bound(Existing, Value) ->
            case selector([Value]) of
                {accepted, Result} -> {accepted, Result};
                Existing -> Existing
            end.

        changed(Value) ->
            case selector([Value]) of
                {accepted, Result} -> {changed, Result};
                Other -> Other
            end.

        selector([bad]) -> erlang:error(selector_failure);
        selector([direct]) -> put(visited, direct), direct_result;
        selector([Value]) -> put(visited, accepted), {accepted, Value}.

        observe() ->
            First = same(example),
            Seen = get(visited),
            Second = same(direct),
            Next = get(visited),
            Rejection = try bound(elsewhere, direct)
                        catch error:{case_clause, direct_result} -> rejected end,
            Error = try same(bad)
                    catch error:selector_failure:Trace ->
                        [{?MODULE, selector, _, Location} | _] = Trace,
                        proplists:get_value(line, Location)
                    end,
            {First, Seen, Second, Next, Rejection, Error, changed(example)}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        byte[] opcodes = ((IEnumerable)Property(code, "Instructions")).Cast<object>()
            .Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode"))
            .ToArray();
        int[] entries = Enumerable.Range(0, opcodes.Length)
            .Where(index => opcodes[index] == FunctionInfo)
            .ToArray();
        byte[] same = opcodes[entries[0]..entries[1]];
        if (!same.Contains(TailLocalCall) || same.Contains(IsTaggedTuple) || same.Contains(IsTuple))
        {
            throw new InvalidOperationException(RetainedRoundTrip);
        }

        byte[] bound = opcodes[entries[1]..entries[2]];
        byte[] changed = opcodes[entries[2]..entries[3]];
        if (!bound.Contains(IsTuple) || !bound.Contains(ExactEqualTest) || !bound.Contains(CaseEnd)
            || !changed.Contains(IsTuple) || !changed.Contains(GetTupleElement) || !changed.Contains(PutTuple))
        {
            throw new InvalidOperationException(RemovedRequiredMatch);
        }

        return Task.CompletedTask;
    }
}
