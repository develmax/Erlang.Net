using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamConsumedOutgoingRootsChecks
{
    internal const string TestName = "compiler/beam/clears-a-retained-argument-at-its-final-call";
    internal const string Fixture = "beam_outgoing_last_use.erl";
    private const string MissingRootClear = "The final use of the retained argument did not release its Y root.";
    private const string ClearedLiveRoot = "A live or protected argument was cleared by outgoing call lowering.";
    private const string ClearPass = "ClearConsumedOutgoingRoots";
    private const string WrongConstruction = "The call must clear a consumed list element only when the outgoing list retains it.";
    internal const string Source = """
        -module(beam_outgoing_last_use).
        -export([terminal/1, reuse/1, caught/1, observe/0]).

        terminal(Text) ->
            {ok, Pattern} = re:compile("^[0-9A-Za-z_\\-]+$", [unicode]),
            case re:run(Text, Pattern) of
                {match, _} -> true;
                nomatch -> false
            end.

        reuse(Text) ->
            {ok, Pattern} = re:compile("^[0-9A-Za-z_\\-]+$", [unicode]),
            {Text, re:run(Text, Pattern)}.

        caught(Text) ->
            {ok, Pattern} = re:compile("^[0-9A-Za-z_\\-]+$", [unicode]),
            case catch re:run(Text, Pattern) of
                {'EXIT', _} -> {failed, Text};
                Result -> Result
            end.

        observe() ->
            [terminal("abc"), terminal("abc!"), reuse("abc"),
             caught("abc"), caught(invalid)].
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
        byte[] terminal = opcodes[entries[0]..entries[1]];
        byte[] reuse = opcodes[entries[1]..entries[2]];
        if (!ClearsBeforeSecondCall(terminal))
        {
            throw new InvalidOperationException(MissingRootClear);
        }

        if (ClearsBeforeSecondCall(reuse))
        {
            throw new InvalidOperationException(ClearedLiveRoot);
        }

        CheckListRoots();

        return Task.CompletedTask;
    }

    private static void CheckListRoots()
    {
        byte[] both = [InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1)];
        byte[] argument = [InitYRegisters, ExtendedListOperand, U(1), Y(1)];
        Check(
            FramedOutgoingCons(),
            FramedOutgoingCons(kills: both)
        );
        Check(
            FramedOutgoingCons(readAfterCall: true),
            FramedOutgoingCons(readAfterCall: true, kills: argument)
        );
        Check(
            FramedOutgoingCons(replaceList: true),
            FramedOutgoingCons(replaceList: true, kills: argument)
        );

        static void Check(ReadOnlyMemory<byte> original, byte[] expected)
        {
            var compiler = new BeamModuleCompiler("beam_outgoing_cons_roots");
            var actual = ((byte[] Code, IReadOnlySet<int> Initializations)?)Call(
                compiler,
                ClearPass,
                original,
                3
            );
            if (actual is not { } result || !result.Code.SequenceEqual(expected) || result.Initializations.Count != 1)
            {
                throw new InvalidOperationException(WrongConstruction);
            }
        }
    }

    private static byte[] FramedOutgoingCons(bool readAfterCall = false, bool replaceList = false, byte[]? kills = null)
    {
        byte[] laterRead = readAfterCall ? [Move, Y(0), X(0)] : [];
        byte[] replacement = replaceList ? [Move, X(0), X(1)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, U(2), U(2),
            Move, X(0), Y(0),
            Move, X(1), Y(1),
            TestHeap, U(2), U(1),
            PutList, Y(0), A(NilAtomIndex), X(1),
            .. replacement,
            Move, Y(1), X(0),
            .. (kills ?? []),
            Line, U(1),
            ExternalCall, U(2), U(0),
            .. laterRead,
            Deallocate, U(2), Return,
            EndCode
        ];
    }

    private static bool ClearsBeforeSecondCall(byte[] body)
    {
        int first = Array.IndexOf(body, ExternalCall);
        int last = Array.IndexOf(body, ExternalCall, first + 1);

        return first >= 0 && last > first && body[(first + 1)..last].Contains(InitYRegisters);
    }
}
