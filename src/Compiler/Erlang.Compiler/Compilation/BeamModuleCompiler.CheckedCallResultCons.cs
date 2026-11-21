using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? KeepCheckedCallResultThroughCons(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TestHeap)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var emptyFrames = new HashSet<int>();
        if (RewriteCheckedCallResultThroughCons(code, decoded.Body, emptyFrames) is not { } rewritten)
        {
            return null;
        }

        if (emptyFrames.Count == 0 || DecodeLateAtomicProjectionSlots(rewritten, labelCount) is not { } updated)
        {
            return rewritten;
        }

        return RewriteDisjointFrames(
            rewritten,
            updated.Body,
            updated.StackOperands,
            atomicOnly: false,
            entriesToCompact: emptyFrames
        ) ?? rewritten;
    }

    private static byte[]? RewriteCheckedCallResultThroughCons(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        HashSet<int> emptyFrames
    )
    {
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        var slot = new BeamOperand(BeamOperandParameters.Y, 0);
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int info = 0; info + 2 < body.Count; info++)
        {
            if (body[info].Definition.Opcode != FunctionInfo
                || body[info + 1] is not { Definition.Opcode: Label, Operands: [var entry] }
                || body[info + 2] is not { Definition.Opcode: Allocate, Operands: [{ Value.IsOne: true }, _] }
                || info + 3 >= body.Count || body[info + 3] is not
                {
                    Definition.Opcode: InitYRegisters, Operands: [{ Value.IsOne: true }, var initialized]
                } || initialized != slot)
            {
                continue;
            }

            int producer = info + 4;
            while (producer < body.Count && IsArgumentPreparation(body[producer]))
            {
                producer++;
            }

            if (producer + 6 >= body.Count || body[producer].Definition.Opcode is not (LocalCall or ExternalCall))
            {
                continue;
            }

            int check = producer + 1;
            int projection = check + 1;
            int spill = projection + 1;
            int heap = spill + 1;
            int list = heap + 1;
            if (body[projection] is not
                {
                    Definition.Opcode: GetTupleElement, Operands: [var source, var field, var target]
                } || source != result || target != result
                || !IsCheckedTupleResult(body[check], field, body[producer].Definition.Opcode)
                || body[spill] is not { Definition.Opcode: Move, Operands: [var value, var saved] }
                || value != result || saved != slot
                || body[heap] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsZero: true }] }
                || ReadHeapWords(code, body[heap]) is not { Words: ConsHeapWords, End: var heapEnd }
                || body[list] is not
                {
                    Definition.Opcode: PutList,
                    Operands: [var head, { Kind: BeamOperandParameters.Nil }, { Kind: BeamOperandParameters.X, TypeIndex: null } argument]
                } || head != slot)
            {
                continue;
            }

            int finish = list + 1;
            while (finish < body.Count && IsArgumentPreparation(body[finish])
                && (body[finish].Definition.Opcode == Line || body[finish].Operands[^1] != argument))
            {
                finish++;
            }

            if (finish + 1 >= body.Count || body[finish].Operands.Count == 0
                || body[finish].Operands[0].Value <= argument.Value || !((flow ??= new BeamProjectionFlow(body))
                    .HasStraightCheckedCallPrefix(info + 1, finish)))
            {
                continue;
            }

            bool tail = body[finish] is
            {
                Definition.Opcode: TailLocalCallWithFrame or TailExternalCallWithFrame,
                Operands: [_, _, { Value.IsOne: true }]
            };
            if (!tail && (body[finish].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[finish + 1] is not { Definition.Opcode: CatchEnter, Operands: [var handlerSlot, _] }
                || handlerSlot != slot || !flow.HasStraightCheckedCallPrefix(finish, finish + 1)))
            {
                continue;
            }

            output.Write(code.Span[copied..body[projection].Offset]);
            output.Write(code.Span[body[heap].Offset..heapEnd]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            output.Write(code.Span[body[projection].Offset..body[spill].Offset]);
            output.WriteByte(PutList);
            WriteRegister(output, result);
            BeamCompactWriter.Write(output, AtomTag, 0);
            WriteRegister(output, argument);
            copied = body[list + 1].Offset;
            if (tail)
            {
                emptyFrames.Add((int)entry.Value);
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool IsArgumentPreparation(BeamInstruction instruction) =>
        instruction.Definition.Opcode == Line || instruction is
        {
            Definition.Opcode: Move,
            Operands: [var value, { Kind: BeamOperandParameters.X, TypeIndex: null }]
        } && IsNonRegisterValue(value);

    private static bool IsCheckedTupleResult(BeamInstruction instruction, BeamOperand field, byte producer) =>
        instruction is
        {
            Definition.Opcode: IsTaggedTuple,
            Operands: [_, { Kind: BeamOperandParameters.X, Value.IsZero: true }, var arity, _]
        } && field.Value < arity.Value
        // A bare tuple test is emitted only for a narrowed local result. The
        // existing projection already relies on that call's exact tuple shape.
        || producer == LocalCall && instruction is
        {
            Definition.Opcode: IsTuple,
            Operands: [_, { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: not null }]
        };

    private sealed partial class BeamProjectionFlow
    {
        internal bool HasStraightCheckedCallPrefix(int start, int end)
        {
            for (int index = start + 1; index <= end; index++)
            {
                if (predecessors[index] is not [int previous] || previous != index - 1)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
