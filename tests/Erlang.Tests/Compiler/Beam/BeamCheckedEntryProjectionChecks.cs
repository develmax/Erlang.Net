using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCheckedEntryProjectionChecks
{
    internal const string TestName = "compiler/beam/projects-checked-entry-fields-before-their-frame";
    private const string Fixture = "beam_checked_entry_projection_probe.erl";
    private const string WrongFrame = "The checked projection used the wrong frame or live roots.";
    private const string WrongResult = "Changing a checked projection's frame changed an observable result.";
    private const string WrongRecordSpill = "Moving the checked record spill changed its payload, source line, or frame owner.";
    private const string Source = """
        -module(beam_checked_entry_projection_probe).
        -export([nested/2, simple/2, fallback/2, retained/2, retained_fallback/2, caught/2]).
        -record(outer, {value, extra}).
        -record(inner, {value, mark}).
        
        nested(#outer{value = #inner{value = Value}}, Key) ->
            Previous = erlang:put(Key, Value),
            {Value, Previous}.
        simple(#outer{value = Value}, Key) when is_tuple(Value) ->
            Previous = erlang:put(Key, Value),
            {Value, Previous}.
        fallback(#outer{value = #inner{value = Value}}, Key) ->
            Previous = erlang:put(Key, Value),
            {Value, Previous};
        fallback(_, _) -> fallback.
        retained(#outer{value = #inner{} = Value} = Original, Key) ->
            Previous = erlang:put(Key, Value),
            {Original, Value, Previous}.
        
        retained_fallback(#outer{value = #inner{} = Value} = Original, Key) ->
            Previous = erlang:put(Key, Value),
            {Original, Value, Previous};
        retained_fallback(_, Key) -> erlang:put(Key, fallback).
        
        caught(#outer{value = #inner{} = Value} = Original, Key) ->
            Previous = catch erlang:put(Key, Value),
            {Original, Value, Previous}.
        """;

    internal static async Task Run()
    {
        CheckRecordSpillLayout();
        CheckConsChildCapture();
        CheckPlainOuterTupleProjection();
        CheckTerminalPayloadTrim();
        CheckProjectedTerminalBranchLayout();
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        CheckLayouts(beam);
        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term key = Term.A("key");
            Term inner = Term.Tuple(Term.A("inner"), Term.A("value"), Term.A("mark"));
            Term outer = Term.Tuple(Term.A("outer"), inner, Term.A("spare"));
            Term rejected = Term.Tuple(Term.A("outer"), Term.A("wrong"), Term.A("spare"));
            Term prior = Term.A("undefined");
            Term expected = Term.Tuple(outer, inner, prior);
            if (!expected.Equals(await module.Invoke(
                context,
                "retained",
                outer,
                key
            ))
                || !inner.Equals(await module.Invoke(
                    context,
                    "retained_fallback",
                    rejected,
                    key
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                _ = await module.Invoke(
                    context,
                    "retained",
                    rejected,
                    key
                );
            }
            catch (ErlangException failure) when (failure.Reason.Equals(Term.A("function_clause")))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongResult);
        });
    }

    private static void CheckConsChildCapture()
    {
        if (Schedule(ConsChild()) is not { } moved || !moved.AsSpan().SequenceEqual(ConsChild(scheduled: true))
            || Schedule(moved) is not null
            || Schedule(ConsChild(rejectionReadsOldHead: true)) is not null
            || Schedule(ConsChild(readsOriginalHead: true)) is not null
            || Schedule(ConsChild(overwritesChild: true)) is not null)
        {
            throw new InvalidOperationException(WrongFrame);
        }

        static byte[]? Schedule(byte[] code) => (byte[]?)Call(
            new BeamModuleCompiler("beam_checked_cons_child_probe"),
            "ScheduleCheckedConsChildCapture",
            new ReadOnlyMemory<byte>(code),
            4
        );
    }

    private static byte[] ConsChild(
        bool scheduled = false,
        bool rejectionReadsOldHead = false,
        bool readsOriginalHead = false,
        bool overwritesChild = false
    )
    {
        byte parent = X(scheduled ? 4 : 3);
        byte[] early = scheduled ? [GetTupleElement, X(2), U(1), X(3)] : [];
        byte[] field = scheduled ? [Move, X(3), Y(0)] : [GetTupleElement, X(2), U(1), Y(0)];
        byte[] overwrite = overwritesChild ? [Move, X(1), X(2)] : [];
        byte[] observe = readsOriginalHead ? [Move, X(3), X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Move, X(1), X(3), IsNonemptyList, F(3), X(0), GetList, X(0), X(2), X(0),
            IsTuple, F(3), X(2), TestArity, F(3), X(2), U(3), .. early,
            IsNonemptyList, F(3), X(0), GetList, X(0), parent, X(0),
            IsTuple, F(3), parent, TestArity, F(3), parent, U(3),
            Allocate, U(2), U(scheduled ? 5 : 4), .. overwrite, .. field, Move, parent, Y(1), .. observe,
            Line, U(1), LocalCall, U(1), F(2), TestHeap, U(3), U(0),
            PutTuple, X(0), ExtendedListOperand, U(2), Y(0), Y(1), Deallocate, U(2), Return,
            Label, U(3), Move, X(rejectionReadsOldHead ? 3 : 1), X(0), Return, EndCode
        ];
    }

    private static void CheckTerminalPayloadTrim()
    {
        byte[] entry =
        [
            Label, U(1), Allocate, U(3), U(2),
            InitYRegisters, ExtendedListOperand, U(2), Y(1), Y(2), Move, X(0), Y(0)
        ];
        byte[] compare = [ExactEqualTest, F(2), X(0), X(1)];
        byte[] success = [Deallocate, U(3), Return, Label, U(2)];
        byte[] literal = [Move, A(1), X(0)];
        byte[] call = [Line, U(1), ExternalCall, U(2), U(0), Move, X(0), X(1), Move, Y(0), X(0)];
        byte[] finish = [ExternalCall, U(2), U(0), Deallocate, U(0), Return, EndCode];
        byte[] cleared = [InitYRegisters, ExtendedListOperand, U(1), Y(1)];
        byte[] original =
        [
            .. entry, .. compare, .. success, .. literal, .. cleared, .. call, TrimFrame, U(3), U(0), .. finish
        ];
        byte[] expected =
        [
            .. entry, .. compare, .. success, Move, Y(0), Y(2), TrimFrame, U(2), U(1),
            .. literal, .. call, TrimFrame, U(1), U(0), .. finish
        ];
        byte[] shared =
        [
            .. entry, .. compare, .. compare, .. success, .. literal, .. cleared, .. call, TrimFrame, U(3), U(0), .. finish
        ];
        byte[] clearedPayload =
        [
            .. entry, .. compare, .. success, .. literal, InitYRegisters, ExtendedListOperand, U(1), Y(0),
            .. call, TrimFrame, U(3), U(0), .. finish
        ];
        if (Rewrite(original) is not { } shortened || !shortened.AsSpan().SequenceEqual(expected)
            || Rewrite(shared) is not null || Rewrite(clearedPayload) is not null)
        {
            throw new InvalidOperationException(WrongFrame);
        }

        static byte[]? Rewrite(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            new BeamModuleCompiler("terminal_payload_trim.erl"),
            "TrimTerminalCallPayloadFrames",
            code,
            3
        );
    }

    private static void CheckRecordSpillLayout()
    {
        byte[] check = [.. Start(
            1,
            2,
            2,
            2
        ), IsTaggedTuple, F(3), X(0), U(2), A(3)];
        byte[] frame = [Allocate, U(2), U(2), InitYRegisters, ExtendedListOperand, U(1), Y(0), Move, X(1), Y(1)];
        byte[] projection = [GetTupleElement, X(0), U(1), X(0)];
        byte[] spill = [Move, X(0), Y(0)];
        byte[] nested = [IsTaggedTuple, F(4), Y(0), U(2), A(4)];
        byte[] lifted = [IsTaggedTuple, F(4), X(0), U(2), A(4)];
        byte[] continuation = [GetTupleElement, Y(0), U(1), X(0), LocalCall, U(2), F(2)];
        byte[] release = [Move, Y(1), X(0), Deallocate, U(2), Return];
        byte[] outerError = [Label, U(3), Line, U(1), BadRecord, X(0)];
        byte[] innerError = [Label, U(4), Line, U(2), BadRecord, Y(0), EndCode];
        byte[] liftedError = [Label, U(4), Line, U(2), BadRecord, X(0), EndCode];
        byte[] original =
        [
            .. check, .. frame, .. projection, .. spill, .. nested,
            .. continuation, .. release, .. outerError, .. innerError
        ];
        byte[] expected =
        [
            .. check, .. projection, .. lifted, .. frame, .. spill,
            .. continuation, .. release, .. outerError, .. liftedError
        ];
        if (RewriteRecordSpill(original) is not { } result || !result.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(WrongRecordSpill);
        }

        byte[] saveOriginal = [.. frame[..^2], X(0), Y(1)];
        byte[] overwrittenOriginal =
        [
            .. check, .. saveOriginal, .. projection, .. spill, .. nested,
            .. continuation, .. release, .. outerError, .. innerError
        ];
        byte[] sharedFailure =
        [
            .. check, .. frame, .. projection, .. spill, .. nested,
            .. continuation, IsAtom, F(4), X(0), .. release, .. outerError, .. innerError
        ];
        byte[] differentFailure =
        [
            .. check, .. frame, .. projection, .. spill, .. nested,
            .. continuation, .. release, .. outerError, Label, U(4), Line, U(2), BadMatch, Y(0), EndCode
        ];
        if (RewriteRecordSpill(overwrittenOriginal) is not null || RewriteRecordSpill(sharedFailure) is not null
            || RewriteRecordSpill(differentFailure) is not null)
        {
            throw new InvalidOperationException(WrongRecordSpill);
        }

        // The setup preserves original X0. Both old X values die after the inner check.
        byte[] differentInput = [.. Start(
            1,
            2,
            2,
            2
        ), IsTaggedTuple, F(3), X(1), U(2), A(3)];
        byte[] secondProjection = [GetTupleElement, X(1), U(1), X(0)];
        byte[] reusedInput = [GetTupleElement, X(1), U(1), X(1)];
        byte[] retargeted = [IsTaggedTuple, F(4), X(1), U(2), A(4)];
        byte[] storeInFrame = [Move, X(1), Y(0)];
        byte[] lateOverwrite = [GetTupleElement, Y(0), U(1), X(0), ExternalCall, U(1), U(0)];
        byte[] sourceError = [Label, U(3), Line, U(1), BadRecord, X(1)];
        byte[] resultError = [Label, U(4), Line, U(2), BadRecord, X(1), EndCode];
        byte[] before =
        [
            .. differentInput, .. saveOriginal, .. secondProjection, .. spill, .. nested,
            .. lateOverwrite, .. release, .. sourceError, .. innerError
        ];
        byte[] after =
        [
            .. differentInput, .. reusedInput, .. retargeted, .. saveOriginal, .. storeInFrame,
            .. lateOverwrite, .. release, .. sourceError, .. resultError
        ];
        byte[] liveInput =
        [
            .. differentInput, .. saveOriginal, .. secondProjection, .. spill, .. nested,
            .. lateOverwrite[..^2], U(2), U(0), .. release, .. sourceError, .. innerError
        ];
        byte[] liveDestination =
        [
            .. differentInput, .. saveOriginal, .. secondProjection, .. spill, .. nested,
            ExternalCall, U(1), U(0), .. release, .. sourceError, .. innerError
        ];
        if (RewriteRecordSpill(before) is not { } alternate || !alternate.AsSpan().SequenceEqual(after)
            || RewriteRecordSpill(liveInput) is not null || RewriteRecordSpill(liveDestination) is not null)
        {
            throw new InvalidOperationException(WrongRecordSpill);
        }
    }

    private static void CheckProjectedTerminalBranchLayout()
    {
        byte[] check =
        [
            .. Start(
                1,
                2,
                2,
                2
            ),
            IsTaggedTuple, F(1), X(1), U(2), A(3)
        ];
        byte[] setup = [InitYRegisters, ExtendedListOperand, U(1), Y(0), Move, X(1), Y(1)];
        byte[] frame = [Allocate, U(2), U(2), .. setup];
        byte[] relocatedFrame = [Allocate, U(2), U(3), .. setup, Move, X(2), Y(0)];
        byte[] projection = [GetTupleElement, Y(1), U(1), Y(0), ExactEqualTest, F(3), Y(0), A(4)];
        byte[] projected = [GetTupleElement, X(1), U(1), X(2), ExactEqualTest, F(3), X(2), A(4)];
        byte[] construction =
        [
            TestHeap, U(6), U(0), PutTuple, X(0), ExtendedListOperand, U(2), Y(1), A(4),
            PutTuple, X(0), ExtendedListOperand, U(2), A(3), X(0)
        ];
        byte[] framedReturn = [Deallocate, U(2), Return];
        byte[] framelessConstruction =
        [
            TestHeap, U(6), U(2), PutTuple, X(0), ExtendedListOperand, U(2), X(1), A(4),
            PutTuple, X(0), ExtendedListOperand, U(2), A(3), X(0)
        ];
        byte[] fallback = [Label, U(3)];
        byte[] continuation = [Move, Y(0), X(0), .. framedReturn, EndCode];
        byte[] original =
        [
            .. check, .. frame, .. projection, .. construction, .. framedReturn, .. fallback, .. continuation
        ];
        byte[] expected =
        [
            .. check, .. projected, .. framelessConstruction, Return,
            .. fallback, .. relocatedFrame, .. continuation
        ];
        byte[] protectedContinuation =
        [
            TryEnter, Y(1), F(4), ExternalCall, U(1), U(0), TryEnd, Y(1), .. framedReturn,
            Label, U(4), TryCase, Y(1), Move, Y(0), X(0), .. framedReturn, EndCode
        ];
        byte[] protectedOriginal =
        [
            .. check, .. frame, .. projection, .. construction, .. framedReturn, .. fallback, .. protectedContinuation
        ];
        byte[] protectedExpected =
        [
            .. check, .. projected, .. framelessConstruction, Return,
            .. fallback, .. relocatedFrame, .. protectedContinuation
        ];
        byte[] shared =
        [
            .. check, .. frame, .. projection, .. construction, .. framedReturn,
            .. fallback, Jump, F(3), EndCode
        ];
        byte[] effect =
        [
            .. check, .. frame, .. projection, ExternalCall, U(1), U(0),
            .. construction, .. framedReturn, .. fallback, .. continuation
        ];
        if (RewriteRecordSpill(original) is not { } result || !result.AsSpan().SequenceEqual(expected)
            || RewriteRecordSpill(protectedOriginal) is not { } protectedResult
            || !protectedResult.AsSpan().SequenceEqual(protectedExpected)
            || RewriteRecordSpill(shared) is not null || RewriteRecordSpill(effect) is not null)
        {
            throw new InvalidOperationException(WrongFrame);
        }
    }

    private static void CheckPlainOuterTupleProjection()
    {
        byte[] entry = [Label, U(1), FunctionInfo, A(1), A(2), U(1), Label, U(2)];
        byte[] outer = [IsTuple, F(1), X(0), TestArity, F(1), X(0), U(2)];
        byte[] differentFailure = [IsTuple, F(4), X(0), TestArity, F(1), X(0), U(2)];
        byte[] frame = [Allocate, U(1), U(1)];
        byte[] projection = [GetTupleElement, X(0), U(0), Y(0), IsTaggedTuple, F(3), Y(0), U(2), A(3)];
        byte[] body = [Move, Y(0), X(0), ExternalCall, U(1), U(0), Deallocate, U(1), Return];
        byte[] relay = [Label, U(3), Deallocate, U(1), Jump, F(1), EndCode];
        byte[] separateFailure = [Label, U(3), Deallocate, U(1), Jump, F(1), Label, U(4), Move, A(1), X(0), Return, EndCode];
        byte[] liveFailure = [Label, U(3), Move, Y(0), X(0), Deallocate, U(1), Return, EndCode];
        byte[] expected =
        [
            .. entry, .. outer,
            GetTupleElement, X(0), U(0), X(1), IsTaggedTuple, F(3), X(1), U(2), A(3),
            Allocate, U(1), U(2), Move, X(1), Y(0),
            .. body, Label, U(3), Jump, F(1), EndCode
        ];
        if (RewriteRecordSpill([.. entry, .. outer, .. frame, .. projection, .. body, .. relay]) is not { } rewritten
            || !rewritten.AsSpan().SequenceEqual(expected)
            || RewriteRecordSpill([.. entry, .. differentFailure, .. frame, .. projection, .. body, .. separateFailure]) is not null
            || RewriteRecordSpill([.. entry, .. outer, .. frame, .. projection, .. body, .. liveFailure]) is not null)
        {
            throw new InvalidOperationException(WrongFrame);
        }
    }

    private static byte[]? RewriteRecordSpill(ReadOnlyMemory<byte> code)
    {
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            5,
            1,
            code
        );
        var stackOperands = new List<(int Start, int End, int Slot)>();
        Action<int, int, int> trackSlot = (start, end, slot) => stackOperands.Add((start, end, slot));
        object instructions = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            5,
            0,
            null,
            trackSlot
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "DelayEntryStackFrame",
            code,
            instructions,
            stackOperands,
            null,
            new Func<object, bool>(static _ => false),
            false
        );
    }

    private static void CheckLayouts(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Children(code, "Instructions").ToArray();
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] positive = Body("retained");
        int allocate = Array.FindIndex(positive, instruction => Opcode(instruction) == Allocate);
        if (allocate < 0
            || !positive.Take(allocate).Any(instruction => Opcode(instruction) == GetTupleElement)
            || positive.Take(allocate).Count(instruction => Opcode(instruction) == IsTaggedTuple) != 2
            || Value(Operands(positive[allocate])[^1]) != 3
            || !KeepsFrameBeforeProjection("retained_fallback")
            || !KeepsFrameBeforeProjection("caught"))
        {
            throw new InvalidOperationException(WrongFrame);
        }

        bool KeepsFrameBeforeProjection(string name)
        {
            object[] body = Body(name);
            int frame = Array.FindIndex(body, instruction => Opcode(instruction) == Allocate);
            int projected = Array.FindIndex(body, instruction => Opcode(instruction) == GetTupleElement);

            return frame >= 0 && projected > frame;
        }

        object[] Body(string name)
        {
            int entry = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Operands(instruction)[1])] == name);

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Children(instruction, "Operands").ToArray();

    private static IEnumerable<object> Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
