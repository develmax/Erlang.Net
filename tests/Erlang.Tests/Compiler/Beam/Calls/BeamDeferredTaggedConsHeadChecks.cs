using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamDeferredTaggedConsHeadChecks
{
    internal const string TestName = "compiler/beam/defers-safe-recursive-cons-head";
    private const string WrongCode = "The tagged head must use the saved operands after the recursive tail returns.";
    private const string Source = """
        -module(beam_deferred_tagged_cons).
        -export([entry/1, effect/1]).
        entry([{tag, Name, Payload}|Rest]) ->
            Group = {packed, Payload},
            [{frame, Name, Group}|entry(Rest)];
        entry([]) ->
            [].
        effect([{tag, Name, Payload}|Rest]) ->
            Group = erlang:put(mark, Payload),
            [{frame, Name, Group}|effect(Rest)];
        effect([]) ->
            [].
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_deferred_tagged_cons.erl"));
        object function = Items(code, "Functions").Single(item => (string)Property(item, "Name") == "entry");
        int first = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");
        byte[] opcodes = Items(code, "Instructions")[first..end].Select(instruction =>
            (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        ReadOnlySpan<byte> afterCall = [LocalCall, TestHeap, PutTuple, PutList, Deallocate, Return];
        if (opcodes.AsSpan().IndexOf(afterCall) < 0)
        {
            throw new InvalidOperationException(WrongCode);
        }

        var compiler = new BeamModuleCompiler("beam_deferred_saved_tagged_cons");
        _ = Call(compiler, "Intern", "run");
        _ = Call(compiler, "Intern", "tag");
        foreach (bool nested in new[] { false, true })
        {
            if (Rewrite(SavedFixture(nested)) is not { } rewritten
                || !rewritten.SequenceEqual(SavedFixture(nested, deferred: true))
                || Rewrite(SavedFixture(nested, between: true)) is not null)
            {
                throw new InvalidOperationException(WrongCode);
            }
        }

        return Task.CompletedTask;

        byte[]? Rewrite(ReadOnlyMemory<byte> instructions) => (byte[]?)Call(
            compiler,
            "DeferSavedTaggedRecursiveConsHeads",
            instructions,
            4
        );
    }

    private static byte[] SavedFixture(bool nested, bool deferred = false, bool between = false)
    {
        int heap = nested ? 11 : 7;
        byte[] first = nested ? [PutTuple, X(0), ExtendedListOperand, U(3), A(3), X(2), X(3)] : [];
        byte[] middle = between ? [TestHeap, U(0), U(4)] : [];
        byte[] init = between ? [U(2), Y(0), Y(1)] : [U(1), Y(0)];
        byte[] frameHead = deferred
            ? [Move, X(1), Y(1)]
            : [Move, X(0), Y(1), PutTuple, X(0), ExtendedListOperand, U(3), A(3), X(1), Y(1), Move, X(0), Y(0)];
        byte[] postHead = deferred ? [PutTuple, X(1), ExtendedListOperand, U(3), A(3), Y(1), Y(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                2,
                1
            ),
            Move, X(0), X(1), Move, X(0), X(2), Move, X(0), X(3),
            AllocateHeap, U(2), U(deferred ? heap - 4 : heap), U(4),
            InitYRegisters, ExtendedListOperand, .. init, .. first, .. middle,
            PutTuple, deferred ? Y(0) : X(0), ExtendedListOperand, U(2), A(3), nested ? X(0) : X(2),
            .. frameHead, Move, X(3), X(0), Line, U(1), LocalCall, U(1), F(2),
            TestHeap, U(deferred ? 6 : 2), U(1), .. postHead,
            PutList, deferred ? X(1) : Y(0), X(0), X(0), Deallocate, U(2), Return, EndCode
        ];
    }

    private static object[] Items(object owner, string property) =>
        ((IEnumerable)Property(owner, property)).Cast<object>().ToArray();
}
