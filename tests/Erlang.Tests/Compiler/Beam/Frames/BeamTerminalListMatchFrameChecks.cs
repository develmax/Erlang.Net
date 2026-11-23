using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalListMatchFrameChecks
{
    internal const string TestName = "compiler/beam/delays-the-frame-past-a-terminal-list-match-failure";
    private const string MissingPlacement = "Expected the list check and its terminal badmatch to run without a frame.";
    private const string TuplePlacement = "The public tuple checks must preserve the original badmatch payload and line without a frame.";
    private const string InvalidPlacement = "A changed input, intervening heap check, or shared failure entry must retain the frame.";
    private const string TailArgumentPlacement = "A frameless tail must initialize its arguments and preserve each original input read.";
    private const string NilDefaultReentry = "A literal default must reenter the same function without delaying its continuation's frame.";
    private const string NestedArmPlacement = "Only an owned literal failure may leave the checked nested tuple arm before its frame.";
    private const string FramelessSiblingPlacement = "A tail that fits in the entry registers must preserve an existing frameless sibling.";
    private const byte Zero = 0;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Four = 0x40;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte Y2 = 0x24;
    private const byte Module = 0x12;
    private const byte Function = 0x22;
    private const byte RecursiveEntry = 0x25;
    private const byte Failed = 0x35;
    private const byte OtherFailure = 0x45;

    internal static Task Run()
    {
        byte[] entry = [Label, One, FunctionInfo, Module, Function, Two, Label, Two];
        byte[] frame = [Allocate, Zero, Two];
        byte[] test = [IsNonemptyList, Failed, X1];
        byte[] success = [LocalCall, Two, RecursiveEntry, Deallocate, Zero, Return];
        byte[] error = [Label, Three, Move, X1, X0, Line, One, BadMatch, X0, EndCode];
        byte[] original = [.. entry, .. frame, .. test, .. success, .. error];
        byte[] expected = [.. entry, .. test, .. frame, .. success, .. error];
        if (Rewrite(original) is not { } actual || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(MissingPlacement);
        }

        byte[] savedValueFrame = [Allocate, One, Two, Move, X0, Y0];
        byte[] savedValueSuccess = [LocalCall, Two, RecursiveEntry, Move, Y0, X0, Deallocate, One, Return];
        byte[] savedValue = [.. entry, .. savedValueFrame, .. test, .. savedValueSuccess, .. error];
        byte[] savedValueExpected = [.. entry, .. test, .. savedValueFrame, .. savedValueSuccess, .. error];
        if (Rewrite(savedValue) is not { } savedValueActual || !savedValueActual.AsSpan().SequenceEqual(savedValueExpected))
        {
            throw new InvalidOperationException(MissingPlacement);
        }

        byte[] otherInput =
        [
            ..entry, ..frame, ..test, ..success,
            Label, Three, Move, X0, X0, BadMatch, X0, EndCode
        ];
        byte[] interveningHeapCheck =
        [
            .. entry, .. frame, .. test, .. success,
            Label, Three, Move, X1, X0, TestHeap, One, One, BadMatch, X0, EndCode
        ];
        byte[] sharedFailure =
        [
            .. entry, .. frame, .. test,
            LocalCall, Two, RecursiveEntry, Jump, Failed, .. error
        ];
        if (Rewrite(otherInput) is not null || Rewrite(interveningHeapCheck) is not null || Rewrite(sharedFailure) is not null)
        {
            throw new InvalidOperationException(InvalidPlacement);
        }

        byte[] discardHeadFrame = [Allocate, Three, Two, InitYRegisters, ExtendedListOperand, One, Y2, Move, X0, Y0, Move, X1, Y1];
        byte[] discardHead = [IsNonemptyList, Failed, Y1, GetHead, Y1, Y2, IsNil, Failed, Y2, Move, 0x02, X0];
        byte[] outOfFrameHead = [IsNonemptyList, Failed, X1, GetHead, X1, 0x23, IsNil, Failed, 0x23, Move, 0x02, X0];
        byte[] headContinuation =
        [
            Label, Three, Line, One, LocalCall, Two, RecursiveEntry, Move, X0, Y2,
            Move, Y2, X0, Deallocate, Three, Return, EndCode
        ];
        byte[] headSource = [.. entry, .. discardHeadFrame, .. discardHead, Deallocate, Three, Return, .. headContinuation];
        byte[] headExpected =
        [
            .. entry, .. outOfFrameHead, Return, Label, Three, .. discardHeadFrame, .. headContinuation[2..]
        ];
        byte[] readsIncomingHead =
        [
            .. entry, .. discardHeadFrame, .. discardHead, Deallocate, Three, Return, Label, Three,
            Line, One, LocalCall, Two, RecursiveEntry, Move, Y2, X0, Deallocate, Three, Return, EndCode
        ];
        if (Rewrite(headSource) is not { } headActual || !headActual.AsSpan().SequenceEqual(headExpected)
            || Rewrite(readsIncomingHead) is not null)
        {
            throw new InvalidOperationException(FramelessSiblingPlacement);
        }

        byte[] tupleFrame = [Allocate, One, Two, Move, X1, Y0];
        byte[] tupleFailure = [Label, Three, Move, Y0, X0, Line, One, BadMatch, X0, EndCode];
        byte[][] tupleChecks =
        [
            [IsTuple, Failed, Y0, TestArity, Failed, Y0, Two],
            [IsTaggedTuple, Failed, Y0, Two, Module]
        ];
        byte[][] unframedChecks =
        [
            [IsTuple, Failed, X1, TestArity, Failed, X1, Two],
            [IsTaggedTuple, Failed, X1, Two, Module]
        ];
        for (int index = 0; index < tupleChecks.Length; index++)
        {
            byte[] tuple = [.. entry, .. tupleFrame, .. tupleChecks[index], .. savedValueSuccess, .. tupleFailure];
            byte[] tupleExpected = [.. entry, .. unframedChecks[index], .. tupleFrame, .. savedValueSuccess, .. error];
            if (Rewrite(tuple, publicEntry: true) is not { } tupleActual
                || !tupleActual.AsSpan().SequenceEqual(tupleExpected)
                || Rewrite(tuple) is not null)
            {
                throw new InvalidOperationException(TuplePlacement);
            }
        }

        byte[] differentTargets =
        [
            .. entry, .. tupleFrame,
            IsTuple, Failed, Y0, TestArity, OtherFailure, Y0, Two, .. savedValueSuccess,
            Label, Three, Move, Y0, X0, Line, One, BadMatch, X0,
            Label, Four, Move, Y0, X0, Line, Two, BadMatch, X0, EndCode
        ];
        if (Rewrite(differentTargets, publicEntry: true) is not null)
        {
            throw new InvalidOperationException(InvalidPlacement);
        }

        VerifyFramelessTailArguments();
        VerifyExistingFramelessSiblings();
        VerifyNilDefaultReentry();
        VerifyNestedArmFrame();

        return Task.CompletedTask;
    }

    private static void VerifyFramelessTailArguments()
    {
        byte[] entry = [Label, One, FunctionInfo, Module, Function, One, Label, Two];
        byte[] frame = [Allocate, One, One, Move, X0, Y0];
        byte[] pidCheck = [IsPid, Failed, Y0];
        byte[] tail = [TailLocalCallWithFrame, Two, RecursiveEntry, One];
        byte[] unframedTail = [TailLocalCall, Two, RecursiveEntry];
        byte[] sibling =
        [
            IsTuple, OtherFailure, Y0, Move, Y0, X0, LocalCall, One, RecursiveEntry, Deallocate, One, Return,
            Label, Four, Move, Module, X0, Deallocate, One, Return, EndCode
        ];
        byte[] appendLiteral = [Move, Module, X1];
        byte[] preserveThenReplace = [Move, Y0, X1, Move, Module, X0];
        byte[] replaceThenRead = [Move, Module, X0, Move, Y0, X1];
        byte[] rewrittenMoves = [Move, X0, X1, Move, Module, X0];
        byte[] expected =
        [
            .. entry, IsPid, Failed, X0, .. appendLiteral, .. unframedTail, Label, Three, .. frame, .. sibling
        ];
        byte[] pairedExpected =
        [
            .. entry, IsPid, Failed, X0, .. rewrittenMoves, .. unframedTail, Label, Three, .. frame, .. sibling
        ];
        if (Rewrite([.. entry, .. frame, .. pidCheck, .. appendLiteral, .. tail, Label, Three, .. sibling])
                is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || Rewrite([.. entry, .. frame, .. pidCheck, .. preserveThenReplace, .. tail, Label, Three, .. sibling])
                is not { } paired || !paired.AsSpan().SequenceEqual(pairedExpected)
            || Rewrite([.. entry, .. frame, .. pidCheck, .. replaceThenRead, .. tail, Label, Three, .. sibling])
                is not null)
        {
            throw new InvalidOperationException(TailArgumentPlacement);
        }
    }

    private static void VerifyExistingFramelessSiblings()
    {
        const string source = """
            -module(entry_arity_sibling_probe).
            -export([list_or_return/2, projected/7]).
            list_or_return([], Context) -> pack(1, Context);
            list_or_return([_|_]=List, Context) -> {List, Context}.
            projected(_, '$end', _, Stack, _, _, _) -> {ok, hd(Stack)};
            projected(_, _, _, _, Error, _, _) -> rejected(Error).
            pack(Kind, Value) -> {Kind, Value}.
            rejected(Value) -> {rejected, Value}.
            """;
        object code = Code(CodeGeneration.CompileBeamModule(source, "entry_arity_sibling_probe.erl"));
        object[] instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        string[] atoms = ((IEnumerable)Property(Property(code, "Structure"), "Atoms")).Cast<string>().ToArray();
        Verify("list_or_return", IsNonemptyList);
        Verify("projected", ExactEqualTest);

        void Verify(string name, byte first)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => ReadOpcode(instruction) == FunctionInfo
                    && atoms[(int)(BigInteger)Property(
                        ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(1),
                        "Value"
                    )] == name
            );
            byte[] body = instructions.Skip(info + 1).Select(ReadOpcode)
                .TakeWhile(opcode => opcode != FunctionInfo).Where(opcode => opcode is not (Label or Line)).ToArray();
            if (info < 0 || body.Length == 0 || body[0] != first
                || body.Any(opcode => opcode is Allocate or AllocateHeap or Deallocate))
            {
                throw new InvalidOperationException(FramelessSiblingPlacement);
            }
        }

        static byte ReadOpcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
    }

    private static void VerifyNestedArmFrame()
    {
        byte[] entry =
        [
            Label, One, FunctionInfo, Module, Function, One, Label, Two,
            IsPid, Failed, X0, TailLocalCall, One, RecursiveEntry, Label, Three
        ];
        byte[] setup = [InitYRegisters, ExtendedListOperand, Two, Y1, Y2, Move, X0, Y0];
        byte[] outer = [IsTuple, OtherFailure, Y0, TestArity, OtherFailure, Y0, Three];
        byte[] inner = [IsTaggedTuple, OtherFailure, Y2, Two, Module];
        byte[] sibling = [GetTupleElement, Y0, Zero, Y1];
        byte[] nested = [GetTupleElement, Y0, Two, Y2];
        byte[] success = [Move, Y2, X1, Move, Y1, X0, LocalCall, Two, RecursiveEntry, Deallocate, Three, Return];
        byte[] failure = [Label, Four, Move, Module, X0, Deallocate, Three, Return, EndCode];
        byte[] before = [.. entry, Allocate, Three, One, .. setup, .. outer, .. sibling, .. nested, .. inner];
        byte[] expected =
        [
            .. entry, IsTuple, OtherFailure, X0, TestArity, OtherFailure, X0, Three,
            GetTupleElement, X0, Two, X1, IsTaggedTuple, OtherFailure, X1, Two, Module,
            Allocate, Three, Two, .. setup, Move, X1, Y2, .. sibling, .. success,
            Label, Four, Move, Module, X0, Return, EndCode
        ];
        if (Rewrite([.. before, .. success, .. failure]) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || Rewrite([.. before, IsAtom, OtherFailure, X0, .. success, .. failure]) is not null
            || Rewrite([.. before, .. success, Label, Four, Move, Y1, X0, Deallocate, Three, Return, EndCode]) is not null)
        {
            throw new InvalidOperationException(NestedArmPlacement);
        }
    }

    private static void VerifyNilDefaultReentry()
    {
        const string source = """
            -module(entry_nil_lc_probe).
            -export([run/2]).
            run(Local, Remote) -> protocol_vsn(Local, Remote).
            protocol_vsn([], Vsns) -> protocol_vsn(["1.0"], Vsns);
            protocol_vsn(Local, Remote) ->
                lists:max([false | [V || V <- Remote, lists:member(V, Local)]]).
            """;
        object code = Code(CodeGeneration.CompileBeamModule(source, "entry_nil_lc_probe.erl"));
        object[] instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        string[] atoms = ((IEnumerable)Property(Property(code, "Structure"), "Atoms")).Cast<string>().ToArray();
        int info = Array.FindIndex(
            instructions,
            instruction => ReadOpcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(
                    ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(1),
                    "Value"
                )] == "protocol_vsn"
        );
        byte[] body = instructions.Skip(info + 1).Select(ReadOpcode).TakeWhile(opcode => opcode != FunctionInfo)
            .Where(opcode => opcode is not (Label or Line)).ToArray();
        if (info < 0 || body is not [IsNil, Move, TailLocalCall, Allocate, Swap, LocalCall, ..])
        {
            throw new InvalidOperationException(NilDefaultReentry);
        }

        static byte ReadOpcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
    }

    private static byte[]? Rewrite(byte[] source, bool publicEntry = false)
    {
        ReadOnlyMemory<byte> instructions = source;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            5,
            1,
            instructions
        );
        var stackOperands = new List<(int Start, int End, int Slot)>();
        Action<int, int, int> stack = (start, end, slot) => stackOperands.Add((start, end, slot));
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            0,
            null,
            stack
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "DelayEntryStackFrame",
            instructions,
            body,
            stackOperands,
            null,
            new Func<object, bool>(_ => publicEntry),
            false
        );
    }
}
