using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record BoundPairConsTailPlan(
        int Frame,
        int Width,
        int FirstCall,
        int Pair,
        int Tuple,
        int TailCall,
        int Release,
        BeamOperand Retained,
        BeamOperand Outer
    );

    private byte[]? ScheduleBoundPairConsTail(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetTupleElement)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        using var output = new MemoryStream();
        int copied = 0;
        for (int info = 0; info + 27 < body.Count; info++)
        {
            if (body[info].Definition.Opcode != FunctionInfo || ReadBoundPairConsTail(body, info + 1) is not { } plan)
            {
                continue;
            }

            output.Write(code.Span[copied..body[plan.Frame].Offset]);
            output.WriteByte(Allocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, plan.Width - 1);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, body[plan.Frame].Operands[1].Value);

            for (int index = plan.Frame + 1; index <= plan.FirstCall; index++)
            {
                BeamInstruction before = body[index];
                if (before.Definition.Opcode == InitYRegisters)
                {
                    WriteFrameInitialization(output, before.Operands.Skip(1)
                        .Where(slot => slot.Value != plan.Width - 1).Select(slot => (int)slot.Value).ToArray());
                }
                else
                {
                    output.Write(code.Span[before.Offset..body[index + 1].Offset]);
                }
            }

            output.WriteByte(GetTupleElement);
            WriteRegister(output, x0);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, body[plan.Pair].Operands[1].Value);
            WriteRegister(output, plan.Retained);
            output.Write(code.Span[body[plan.Pair + 1].Offset..body[plan.Pair + 2].Offset]);
            output.Write(code.Span[body[plan.Pair + 4].Offset..body[plan.Tuple - 1].Offset]);

            // Both effectful calls retain their order. Build only the pure tuple
            // and the two cons cells after the last call.
            output.WriteByte(Swap);
            WriteRegister(output, plan.Retained);
            WriteRegister(output, x0);
            output.Write(code.Span[body[plan.Tuple + 4].Offset..body[plan.TailCall + 1].Offset]);
            output.WriteByte(TestHeap);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag,
                body[plan.Tuple - 1].Operands[0].Value + body[plan.TailCall + 1].Operands[0].Value);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            output.WriteByte(PutTuple);
            WriteRegister(output, x1);
            output.WriteByte(ExtendedListOperand);
            var tupleOperands = body[plan.Tuple].Operands;
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, tupleOperands.Count - 2);
            foreach (var value in tupleOperands.Skip(2))
            {
                if (value.Kind is BeamOperandParameters.X or BeamOperandParameters.Y)
                {
                    WriteRegister(output, value == x0 ? plan.Retained : value);
                }
                else
                {
                    BeamCompactWriter.Write(output,
                        value.Kind == BeamOperandParameters.Integer ? BeamWire.IntegerTag : AtomTag, value.Value);
                }
            }

            WriteCons(x1);
            WriteCons(plan.Outer);
            output.WriteByte(Deallocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, plan.Width - 1);
            copied = body[plan.Release + 1].Offset;
            info = plan.Release;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void WriteCons(BeamOperand head)
        {
            output.WriteByte(PutList);
            WriteRegister(output, head);
            WriteRegister(output, x0);
            WriteRegister(output, x0);
        }
    }

    private static BoundPairConsTailPlan? ReadBoundPairConsTail(IReadOnlyList<BeamInstruction> body, int entry)
    {
        int frame = entry + 1;
        if (body[entry].Definition.Opcode != Label || body[frame] is not
            { Definition.Opcode: Allocate, Operands: [{ Value: var size }, _] }
            || size < 3 || size > BeamOperandParameters.MaximumRegister)
        {
            return null;
        }

        int width = (int)size;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var removed = new BeamOperand(BeamOperandParameters.Y, width - 1);
        int call = frame + 1;
        while (body[call].Definition.Opcode is Move or Swap or InitYRegisters or Line)
        {
            if (body[call].Definition.Opcode != InitYRegisters && body[call].Operands.Contains(removed))
            {
                return null;
            }

            call++;
        }

        int pair = call + 1;
        if (pair + 10 >= body.Count || body[call].Definition.Opcode is not (LocalCall or ExternalCall)
            || body[pair] is not { Definition.Opcode: GetTupleElement, Operands: [var input, var field, var result] }
            || input != x0 || result != x1 || body[pair + 1] is not
            {
                Definition.Opcode: GetTupleElement, Operands: [var flagInput, var flagField, var flagResult]
            } || flagInput != x0 || flagResult != x0 || flagField == field
            || body[pair + 2] is not
            {
                Definition.Opcode: Move,
                Operands: [var flag, { Kind: BeamOperandParameters.Y, TypeIndex: null } retained]
            } || flag != x0 || retained.Value >= width - 2
            || !IsExpectedMove(pair + 3, x1, removed)
            || body[pair + 4] is not
            {
                Definition.Opcode: ExactEqualTest, Operands: [var fallback, var tested, { Kind: BeamOperandParameters.Atom }]
            } || tested != x0 || body[pair + 5] is not
            { Definition.Opcode: TestHeap, Operands: [{ Value: var consWords }, { Value.IsZero: true }] }
            || consWords != ConsHeapWords || body[pair + 6] is not
            {
                Definition.Opcode: PutList,
                Operands: [var literal, { Kind: BeamOperandParameters.Y, TypeIndex: null } branchSlot, var branchResult]
            } || !IsNonRegisterValue(literal) || branchResult != x0 || branchSlot == retained || branchSlot == removed
            || !IsExpectedMove(pair + 7, x0, branchSlot)
            || body[pair + 8] is not { Definition.Opcode: Label, Operands: [var join] } || join.Value != fallback.Value
            || body[pair + 9] is not { Definition.Opcode: Move, Operands: [var constant, var argument] }
            || !IsNonRegisterValue(constant) || argument != x0)
        {
            return null;
        }

        int effect = SkipLines(pair + 10);
        int tuple = effect + 2;
        if (tuple + 10 >= body.Count || !Returns(effect)
            || body[effect + 1] is not
            {
                Definition.Opcode: TestHeap,
                Operands: [{ Kind: BeamOperandParameters.Unsigned, Value: var tupleWords }, { Value.IsOne: true }]
            }
            || body[tuple] is not { Definition.Opcode: PutTuple, Operands: [var target, _, ..] }
            || target != retained || tupleWords != body[tuple].Operands.Count - 1
            || body[tuple].Operands.Skip(2).Count(operand => operand == x0) != 1
            || !body[tuple].Operands.Skip(2).All(AvailableTupleOperand)
            || !IsExpectedMove(tuple + 1, removed, x0) || !IsExpectedMove(tuple + 2, retained, removed)
            || body[tuple + 3] is not { Definition.Opcode: TrimFrame, Operands: [var discarded, var kept] }
            || discarded.Value != width - 2 || kept.Value != 2)
        {
            return null;
        }

        int tailCall = SkipLines(tuple + 4);
        int release = tailCall + 4;
        if (release + 1 >= body.Count || !Returns(tailCall)
            || body[tailCall + 1] is not
            { Definition.Opcode: TestHeap, Operands: [{ Value: var listWords }, { Value.IsOne: true }] }
            || listWords != ConsHeapWords * 2
            || body[tailCall + 2] is not { Definition.Opcode: PutList, Operands: [var head, var tail, var built] }
            || head != new BeamOperand(BeamOperandParameters.Y, 1) || tail != x0 || built != x0
            || body[tailCall + 3] is not { Definition.Opcode: PutList, Operands: [var outer, var list, var answer] }
            || outer != new BeamOperand(BeamOperandParameters.Y, 0) || list != x0 || answer != x0
            || body[release] is not { Definition.Opcode: Deallocate, Operands: [var count] } || count.Value != 2
            || body[release + 1].Definition.Opcode != Return
            || !HasUnprotectedCheckedFailureRegion(body, frame, release + 1))
        {
            return null;
        }

        int end = release + 2;
        while (body[end].Definition.Opcode is Label or Line)
        {
            end++;
        }

        if (body[end].Definition.Opcode is not (FunctionInfo or EndCode)
            || ReadDisjointFrameOwnership(
                body,
                entry,
                end,
                trackSiblingTrimEpochs: true
            ) is not { } ownership
            || ownership.Uses.Count != 2 || !ownership.Uses.ContainsKey(frame) || !ownership.Uses.ContainsKey(tuple + 3))
        {
            return null;
        }

        return new(
            frame,
            width,
            call,
            pair,
            tuple,
            tailCall,
            release,
            retained,
            new BeamOperand(BeamOperandParameters.Y, width - 2)
        );

        bool IsExpectedMove(int index, BeamOperand source, BeamOperand destination) => body[index] is
        { Definition.Opcode: Move, Operands: [var moved, var to] } && moved == source && to == destination;

        int SkipLines(int start)
        {
            while (body[start].Definition.Opcode == Line)
            {
                start++;
            }

            return start;
        }

        bool Returns(int index) => body[index] is
        { Definition.Opcode: ExternalCall or LocalCall, Operands: [{ Value.IsOne: true }, _] };

        bool AvailableTupleOperand(BeamOperand operand) =>
            operand == x0 || operand is { Kind: BeamOperandParameters.Atom or BeamOperandParameters.Nil or BeamOperandParameters.Integer }
            || operand is { Kind: BeamOperandParameters.Y, TypeIndex: null }
                && operand.Value < width - 1 && operand != retained;
    }
}
