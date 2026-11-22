using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? CoalesceSplitConstructorHeaps(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TestHeap)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return RewriteSplitConstructorHeaps(code, body);
    }

    private static byte[]? RewriteSplitConstructorHeaps(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        BeamProjectionFlow? flow = null;
        int copied = 0;
        bool unprotected = false;
        for (int heap = 0; heap + 4 < body.Count; heap++)
        {
            if (body[heap].Definition.Opcode == FunctionInfo)
            {
                unprotected = HasUnprotectedCallKillSchedule(body, heap + 1);
                continue;
            }

            if (!unprotected
                || body[heap] is not { Definition.Opcode: TestHeap, Operands: [var first, var live] }
                || body[heap + 1] is not { Definition.Opcode: PutTuple or PutList } constructor
                || constructor.Operands[StackWriteOperand(constructor)] != x0
                || first.Value != (constructor.Definition.Opcode == PutTuple
                    ? constructor.Operands.Count - 1 : ConsHeapWords)
                || body[heap + 2] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var produced, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                } || produced != x0
                || body[heap + 3] is not { Definition.Opcode: TestHeap, Operands: [var second, { Value.IsZero: true }] }
                || second.Value <= 0 || first.Value + second.Value > int.MaxValue
                || !HasScalarHeapWords(heap) || !HasScalarHeapWords(heap + 3))
            {
                continue;
            }

            output.Write(code.Span[copied..body[heap].Offset]);
            output.WriteByte(TestHeap);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, first.Value + second.Value);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live.Value);
            if (heap + 8 < body.Count && constructor.Definition.Opcode == PutTuple
                && !constructor.Operands.Any(item => SameProjectionRegister(item, x1))
                && body[heap + 4] is
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [{ Kind: BeamOperandParameters.Y } carrier, _, var tail]
                } && !SameProjectionRegister(carrier, saved) && tail == x0
                && body[heap + 5] is { Definition.Opcode: PutList, Operands: [var head, var listTail, var list] }
                && head == saved && listTail == x0 && list == x0
                && body[heap + 6] is { Definition.Opcode: Move, Operands: [var result, var retained] }
                && result == x0 && retained == saved
                && (flow ??= new BeamProjectionFlow(body)).IsEntryTemporaryDead(heap + 7, x0)
                && flow.IsEntryTemporaryDead(heap + 7, x1))
            {
                // The independently rooted tail can occupy X1. The constructed
                // pair and cons need no intermediate spill before committing Y.
                output.Write(code.Span[body[heap + 4].Offset..(body[heap + 5].Offset - 1)]);
                WriteRegister(output, x1);
                output.Write(code.Span[body[heap + 1].Offset..body[heap + 2].Offset]);
                output.WriteByte(PutList);
                WriteRegister(output, x0);
                WriteRegister(output, x1);
                WriteRegister(output, saved);
                copied = body[heap + 7].Offset;
                heap += 6;
                continue;
            }

            output.Write(code.Span[body[heap + 1].Offset..body[heap + 3].Offset]);
            copied = body[heap + 4].Offset;
            heap += 3;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool HasScalarHeapWords(int index) =>
            BeamCompactReader.Read(new BeamCursor(code[(body[index].Offset + 1)..])).WireTag == BeamWire.UnsignedTag;
    }
}
