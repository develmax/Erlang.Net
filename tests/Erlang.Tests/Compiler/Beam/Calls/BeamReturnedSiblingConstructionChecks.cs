using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Tests;

internal static class BeamReturnedSiblingConstructionChecks
{
    internal const string TestName = "compiler/beam/constructs-returned-siblings-from-live-call-roots";
    private const string ExpectedRootedConstruction = "Expected the call result to stay in X0 while the retained arguments construct the returned siblings.";

    internal static Task Compare()
    {
        const string fixture = """
            -module(beam_returned_sibling_construction).
            -export([build/3, bound/4, effected/3, normalize/1]).

            build(Name, Value, Modules) ->
                Result = ?MODULE:normalize(Value),
                {Name, {Name, start, []}, permanent, Result, worker, [Name] ++ Modules}.

            bound(Name, Value, Modules, Result) ->
                Result = ?MODULE:normalize(Value),
                {Name, {Name, start, []}, permanent, Result, worker, [Name] ++ Modules}.

            effected(Name, Value, Modules) ->
                Result = ?MODULE:normalize(Value),
                {Name, {Name, start, erase(effect_cell)}, permanent, Result, worker, [Name] ++ Modules}.

            normalize(Value) ->
                put(effect_cell, Value),
                Value.
            """;

        byte[] image = CodeGeneration.CompileBeamModule(fixture, "beam_returned_sibling_construction.erl");
        _ = BeamModule.Prepare(image);
        ReadOnlySpan<byte> code = BeamDocumentationEmissionChecks.Chunks(image).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> beforeCall = [Allocate, 0x20, 0x30, Move, 0x23, 0x04, Move, 0x03, 0x14];
        ReadOnlySpan<byte> afterCall = [TestHeap, 0xd0, 0x10, PutList, 0x14, 0x04, 0x13, PutTuple, 0x23];
        int start = code.IndexOf(beforeCall);
        if (start < 0 || code[start..].IndexOf(afterCall) < 0 || code.IndexOf(ExactNotEqualTest) < 0)
        {
            throw new InvalidOperationException(ExpectedRootedConstruction);
        }

        return Task.CompletedTask;
    }
}
