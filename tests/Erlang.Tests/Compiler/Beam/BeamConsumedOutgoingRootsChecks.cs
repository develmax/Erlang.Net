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
    private const string CompactedRootOrder = "The joined call must order the outgoing arguments before clearing each dead root.";
    private const string RecursiveTrimError = "The recursive helper must preserve its captures and retain only the new result across recursion.";
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
        CheckCompactedArgumentRoots();
        CheckRecursiveAppendFrame();

        return Task.CompletedTask;
    }

    private static void CheckCompactedArgumentRoots()
    {
        var compiler = new BeamModuleCompiler("compacted_argument_roots_fixture");
        int branchTag = (int)Call(compiler, "Intern", "alternate");
        byte[] frame =
        [
            .. Start(
                1,
                2,
                1,
                4
            ), Allocate, U(4), U(4), Move, X(3), Y(0), Move, X(2), Y(1), Move, X(1), Y(2), Move, X(0), Y(3),
            ExternalCall, U(1), U(0), ExactEqualTest, F(3), X(0), A(branchTag)
        ];
        byte[] collecting = [GcBifOne, F(0), U(0), U(0), Y(0), Y(0)];
        byte[] next = [Move, Y(0), X(0), ExternalCall, U(1), U(0), Move, X(0), X(1)];
        byte[] moves = [Move, Y(2), X(0), Move, Y(0), X(2)];
        byte[] call = [Line, U(1), ExternalCall, U(3), U(0)];
        byte[] continuation =
        [
            Move, X(0), X(2), Jump, F(4), Label, U(3), Move, Y(2), X(2), Label, U(4),
            Move, Y(1), X(1), Move, Y(3), X(0), TailExternalCallWithFrame, U(3), U(0), U(4), EndCode
        ];
        byte[] before = [.. frame, .. collecting, .. next, .. moves, .. call, .. continuation];
        byte[] expected =
        [
            .. frame, .. collecting, .. next, Move, Y(0), X(2), Move, Y(2), X(0),
            InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(2), .. call, .. continuation
        ];
        byte[] withLaterRead = [.. frame, .. collecting, .. next, .. moves, .. call, Move, Y(0), X(2), .. continuation];
        byte[] branchOnFailure = (byte[])collecting.Clone();
        branchOnFailure[1] = F(3);
        if (Apply(before, [2]) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || Apply(before, []) is not null || Apply(withLaterRead, [2]) is not null
            || Apply([.. frame, .. branchOnFailure, .. next, .. moves, .. call, .. continuation], [2]) is not null)
        {
            throw new InvalidOperationException(CompactedRootOrder);
        }

        byte[]? Apply(byte[] code, int[] entries) => (byte[]?)Call(
            compiler,
            "ScheduleCompactedCallArgumentRoots",
            (ReadOnlyMemory<byte>)code,
            5,
            entries.ToHashSet()
        );
    }

    private static void CheckRecursiveAppendFrame()
    {
        const string helperName = "mapped_list_helper";
        var compiler = new BeamModuleCompiler("appended_frame_fixture");
        int helper = (int)Call(compiler, "Intern", helperName);
        int append = (int)Call(
            compiler,
            "Import",
            "erlang",
            "++",
            2
        );
        int producer = (int)Call(
            compiler,
            "Import",
            "sample",
            "prefix",
            1
        );
        int plus = (int)Call(
            compiler,
            "Import",
            "erlang",
            "+",
            2
        );
        int error = (int)Call(
            compiler,
            "Import",
            "erlang",
            "error",
            1
        );
        int badGenerator = (int)Call(compiler, "Intern", "bad_generator");
        var declared = new FunctionDefinition(helperName, 3, []) { IsRecursiveListMapper = true };
        byte[] entry =
        [
            .. Start(
                1,
                2,
                helper,
                3
            ), IsNonemptyList, F(3), X(0), Allocate, U(4), U(3),
            Move, X(2), Y(2), Move, X(1), Y(3), GetList, X(0), Y(1), Y(0)
        ];
        byte[] left =
        [
            Line, U(1), GcBifTwo, F(0), U(3), U(plus), X(2), I(1), X(0), ExternalCall, U(1), U(producer),
            Move, Y(1), X(1)
        ];
        byte[] add = [ExternalCall, U(2), U(append)];
        byte[] args = [Move, X(0), Y(1), Move, Y(3), X(1), Move, Y(2), X(2), Move, Y(0), X(0)];
        byte[] recur = [Line, U(2), LocalCall, U(3), F(2), TestHeap, U(2), U(1)];
        byte[] closed =
        [
            PutList, Y(1), X(0), X(0), Deallocate, U(4), Return,
            Label, U(3), Move, X(1), X(0), Return, EndCode
        ];
        byte[] original = [.. entry, .. left, .. add, .. args, .. recur, .. closed];
        byte[] expected =
        [
            .. entry, .. left, InitYRegisters, ExtendedListOperand, U(1), Y(1), .. add,
            Move, Y(0), X(3), Move, Y(3), X(1), Move, X(0), Y(3), Move, Y(2), X(2),
            TrimFrame, U(3), U(1), Move, X(3), X(0), .. recur,
            PutList, Y(0), X(0), X(0), Deallocate, U(1), Return,
            Label, U(3), Move, X(1), X(0), Return, EndCode
        ];
        byte[] withGeneratorLine = [.. entry[..2], Line, U(2), .. entry[2..]];
        byte[] hotReturn = closed[..7];
        byte[] cold =
        [
            Label, U(3), TestHeap, U(3), U(1), PutTuple, X(0), ExtendedListOperand, U(2), A(badGenerator), X(0),
            Line, U(2), TailExternalCall, U(1), U(error), EndCode
        ];
        byte[] lacksRecursiveLocation = [.. withGeneratorLine, .. left, .. add, .. args, .. recur[2..], .. hotReturn, .. cold];
        byte[] expectedLocation = [.. expected[..2], Line, U(2), .. expected[2..^7], .. cold];
        if (Apply(lacksRecursiveLocation, [declared]) is not { } location || !location.AsSpan().SequenceEqual(expectedLocation))
        {
            throw new InvalidOperationException(RecursiveTrimError);
        }

        byte[] previousCaptureInReturn = (byte[])closed.Clone();
        previousCaptureInReturn[1] = Y(3);
        if (Apply(original, [declared]) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || Apply(original, []) is not null
            || Apply([.. entry, .. left, .. add, .. args, .. recur, .. previousCaptureInReturn], [declared]) is not null)
        {
            throw new InvalidOperationException(RecursiveTrimError);
        }

        byte[]? Apply(byte[] body, FunctionDefinition[] helpers) => (byte[]?)Call(
            compiler,
            "TrimRecursiveAppendFrames",
            (ReadOnlyMemory<byte>)body,
            4,
            helpers
        );
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
