using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? OrderSharedArityDispatchRegisters(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(SelectTupleArity)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamProjectionFlow? flow = null;
        var positions = new SortedDictionary<int, (int Lower, int Higher)>();
        for (int entry = 0; entry + 4 < body.Count; entry++)
        {
            if (body[entry].Definition.Opcode != Label || body[entry + 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } carrier, { Value.IsZero: true },
                    { Kind: BeamOperandParameters.X, TypeIndex: null } tag]
                } || body[entry + 2] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var source, { Value.IsOne: true }, { Kind: BeamOperandParameters.X, TypeIndex: null } payload]
                } || source != carrier || payload.Value <= carrier.Value || tag.Value != payload.Value + 1
                || body[entry + 3] is not
                {
                    Definition.Opcode: SelectValue,
                    Operands: [var tested, { Kind: BeamOperandParameters.Label }, { Value: var entries }, ..]
                } || tested != tag || entries < 6
                || !HasUnprotectedCheckedFailureRegion(body, entry, entry + 3)
                || !(flow ??= new BeamProjectionFlow(body)).OwnsCheckedPairArity(entry, carrier)
                || flow.ReadClosedArityDispatchRegisters(entry + 3, payload, tag) is not { } region
                || region.Any(positions.ContainsKey) || positions.ContainsKey(entry + 1)
                || positions.ContainsKey(entry + 2) || positions.ContainsKey(entry + 3))
            {
                continue;
            }

            var pair = ((int)payload.Value, (int)tag.Value);
            positions.Add(entry + 1, pair);
            positions.Add(entry + 2, pair);
            positions.Add(entry + 3, pair);
            foreach (int index in region)
            {
                positions.Add(index, pair);
            }
        }

        var edits = new List<CodeEdit>();
        foreach (var (index, (lower, higher)) in positions)
        {
            int start = body[index].Offset + 1;
            var cursor = new BeamCursor(code[start..body[index + 1].Offset]);
            while (cursor.Remaining != 0)
            {
                int from = start + cursor.Position;
                BeamCompactValue operand = BeamCompactReader.Read(cursor);
                if (operand.WireTag == XRegisterTag && (operand.Value == lower || operand.Value == higher))
                {
                    edits.Add(new(
                        from,
                        start + cursor.Position,
                        XRegisterTag,
                        operand.Value == lower ? higher : lower
                    ));
                }
            }
        }

        return ApplyDuplicatedFrameReadEdits(code, edits);
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool OwnsCheckedPairArity(int entry, BeamOperand carrier)
        {
            if (predecessors[entry] is not [var select] || select >= entry || body[select] is not
                {
                    Definition.Opcode: SelectTupleArity, Operands: [var input, _, _, ..]
                } owner || !SameProjectionRegister(input, carrier))
            {
                return false;
            }

            var label = body[entry].Operands[0];
            for (int index = 3; index + 1 < owner.Operands.Count; index += 2)
            {
                if (owner.Operands[index].Value == 2 && owner.Operands[index + 1].Value == label.Value)
                {
                    return true;
                }
            }

            return false;
        }

        internal HashSet<int>? ReadClosedArityDispatchRegisters(int dispatch, BeamOperand lower, BeamOperand higher)
        {
            var operands = body[dispatch].Operands;
            if (!TryGetForwardLabel(dispatch, (int)operands[1].Value, out int fallback)
                || !IsEntryTemporaryDead(fallback, lower) || !IsEntryTemporaryDead(fallback, higher))
            {
                return null;
            }

            var pending = new Stack<int>();
            for (int index = 3; index + 1 < operands.Count; index += 2)
            {
                if (operands[index].Kind != BeamOperandParameters.Atom
                    || !TryGetSingleForwardTarget(dispatch, (int)operands[index + 1].Value, out int arm)
                    || arm == fallback)
                {
                    return null;
                }

                pending.Push(arm);
            }

            var region = new HashSet<int>();
            while (pending.TryPop(out int index))
            {
                if (index == fallback || region.Contains(index))
                {
                    continue;
                }

                BeamInstruction instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                if (opcode is LocalCall or ExternalCall or TailLocalCall or TailLocalCallWithFrame
                    or TailExternalCall or TailExternalCallWithFrame)
                {
                    if (instruction.Operands[0].Value > lower.Value)
                    {
                        return null;
                    }

                    continue;
                }

                if (opcode == Return)
                {
                    continue;
                }

                if (opcode is Allocate or AllocateHeap or TestHeap && instruction.Operands[^1].Value == higher.Value
                    || !IsEntryFramePredicate(opcode) && opcode is not
                        (Label or Line or Jump or SelectValue or SelectTupleArity or Move or Swap or GetTupleElement
                        or GetList or GetHead or GetTail or PutList or PutTuple or InitYRegisters
                        or Allocate or AllocateHeap or TestHeap or TrimFrame or Deallocate)
                    || successors[index].Length == 0 || successors[index].Any(target => target <= index))
                {
                    return null;
                }

                region.Add(index);
                foreach (int target in successors[index])
                {
                    pending.Push(target);
                }
            }

            // Match the lowest free register for the earlier tag definition.
            // Keep the permutation within the selector's exclusive live range.
            return region.All(index => predecessors[index].All(source => source == dispatch || region.Contains(source)))
                ? region
                : null;
        }
    }
}
