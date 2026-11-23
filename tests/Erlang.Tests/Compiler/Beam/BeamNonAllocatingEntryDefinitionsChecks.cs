using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamNonAllocatingEntryDefinitionsChecks
{
    internal const string TestName = "compiler/beam/ignores-overwritten-entry-roots-before-handler-and-bif";
    internal const string Fixture = "beam_entry_root_defs.erl";
    private const string GuardedRoot = "Only a slot replaced on every uncollected path may lose its initial clear.";
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

        CheckGuardedInitializations();

        return Task.CompletedTask;
    }

    private static void CheckGuardedInitializations()
    {
        var compiler = new BeamModuleCompiler("guarded_entry_initialization_fixture");
        _ = Call(compiler, "Intern", BeamOpcodeEdgeNames.Run);
        byte[] entry = Start(
            1,
            2,
            2,
            1
        );
        byte[] frame = [Allocate, U(2), U(1)];
        byte[] initial = [InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1)];
        byte[] retained = [InitYRegisters, ExtendedListOperand, U(1), Y(1)];
        byte[] guards = [IsTuple, F(3), X(0), TestArity, F(3), X(0), U(1)];
        byte[] match = [GetTupleElement, X(0), U(0), Y(0), Jump, F(4)];
        byte[] failure = [Label, U(3), Move, A(0), Y(0)];
        byte[] join =
        [
            Label, U(4), TestHeap, U(3), U(0), PutTuple, X(0), ExtendedListOperand, U(2), Y(0), Y(1),
            Deallocate, U(2), Return, EndCode
        ];
        byte[] original = [.. entry, .. frame, .. initial, .. guards, .. match, .. failure, .. join];
        byte[] expected = [.. entry, .. frame, .. retained, .. guards, .. match, .. failure, .. join];
        if (Rewrite(original) is not { } actual || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(GuardedRoot);
        }

        byte[] collector =
        [
            .. entry, .. frame, .. initial, .. guards, .. match,
            Label, U(3), TestHeap, U(3), U(1), Move, A(0), Y(0), .. join
        ];
        byte[] observed =
        [
            .. entry, .. frame, .. initial, .. guards, .. match,
            Label, U(3), Move, Y(0), X(0), Move, A(0), Y(0), .. join
        ];
        if (Rewrite(collector) is not null || Rewrite(observed) is not null)
        {
            throw new InvalidOperationException(GuardedRoot);
        }

        byte[]? Rewrite(byte[] bytes) => (byte[]?)Call(
            compiler,
            "EliminateGuardedEntryInitializations",
            (ReadOnlyMemory<byte>)bytes,
            5
        );
    }

    private static bool DefinesWithoutClear(byte[] body, byte definition)
    {
        int assignment = Array.IndexOf(body, definition);
        int frame = Array.IndexOf(body, Allocate);

        return frame >= 0 && assignment > frame && !body[(frame + 1)..assignment].Contains(InitYRegisters);
    }
}
