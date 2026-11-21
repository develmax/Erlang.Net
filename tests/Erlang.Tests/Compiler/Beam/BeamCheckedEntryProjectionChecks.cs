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
