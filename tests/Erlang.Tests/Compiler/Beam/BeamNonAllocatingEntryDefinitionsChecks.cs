using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNonAllocatingEntryDefinitionsChecks
{
    internal const string TestName = "compiler/beam/ignores-overwritten-entry-roots-before-handler-and-bif";
    internal const string Fixture = "beam_entry_root_defs.erl";
    private const string RedundantRoot = "A nonallocating entry definition retained its overwritten frame root.";

    internal const string Source = """
        -module(beam_entry_root_defs).
        -export([query/0, stop/0, observe/0]).

        query() -> consume({node(), registered()}).
        stop() -> catch ets:delete(erase(example_table)).
        consume({Node, Names}) -> {Node, is_list(Names)}.

        observe() ->
            put(example_table, ets:new(entry_definition_fixture, [])),
            A = stop(),
            B = case stop() of
                {'EXIT', {badarg, _}} -> missing_table;
                Other -> Other
            end,
            {query(), A, B, get(example_table)}.
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
        if (!DefinesWithoutClear(opcodes[entries[0]..entries[1]], BifZero)
            || !DefinesWithoutClear(opcodes[entries[1]..entries[2]], CatchEnter))
        {
            throw new InvalidOperationException(RedundantRoot);
        }

        return Task.CompletedTask;
    }

    private static bool DefinesWithoutClear(byte[] body, byte definition)
    {
        int assignment = Array.IndexOf(body, definition);
        int frame = Array.IndexOf(body, Allocate);

        return frame >= 0 && assignment > frame && !body[(frame + 1)..assignment].Contains(InitYRegisters);
    }
}
