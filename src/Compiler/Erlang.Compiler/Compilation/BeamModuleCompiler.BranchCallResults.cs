using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? ForwardSavedCallResults(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        List<(int Start, int End, int Slot)> stackOperands
    )
    {
        using var output = new MemoryStream();
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int operand = 0;
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = start;
            start = end + 1;
            bool? hasHandler = null;
            for (int save = entry + 1; save + 1 < end; save++)
            {
                if (body[save - 1].Definition.Opcode is not (LocalCall or ExternalCall or DynamicApply or CallFun or CallFunTyped)
                    || body[save] is not { Definition.Opcode: Move, Operands: [var source, var slot] }
                    || source != result || slot is not { Kind: BeamOperandParameters.Y, TypeIndex: null })
                {
                    continue;
                }

                int next = save + 1;
                while (next < end && body[next].Definition.Opcode == Line)
                {
                    next++;
                }

                if (next == end)
                {
                    continue;
                }

                BeamInstruction first = body[next];
                byte opcode = first.Definition.Opcode;
                bool heap = opcode == TestHeap && first.Operands[^1].Value.IsZero
                    && next + 1 < end && body[next + 1].Definition.Opcode is PutTuple or PutList or UpdateRecord
                    && body[next + 1].Operands.Any(value => SameProjectionRegister(value, slot));
                bool independentBranch = (IsEntryFramePredicate(opcode) || opcode is SelectValue or SelectTupleArity)
                    && !first.Operands.Any(value => SameProjectionRegister(value, result));
                if (!(heap || independentBranch) || (hasHandler ??= body.Skip(entry).Take(end - entry)
                        .Any(instruction => instruction.Definition.Opcode is TryEnter or CatchEnter)))
                {
                    continue;
                }

                flow ??= new BeamProjectionFlow(body);
                if (flow.PlanSavedCallResult(
                    save,
                    end,
                    slot,
                    result,
                    heap ? next : null
                ) is not { Count: > 0 } uses)
                {
                    continue;
                }

                output.Write(code.Span[copied..body[save].Offset]);
                if (!flow.IsInitialized(save, slot))
                {
                    output.WriteByte(InitYRegisters);
                    output.WriteByte(ExtendedListOperand);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                    WriteRegister(output, slot);
                }

                copied = body[save + 1].Offset;
                if (heap)
                {
                    int live = body[next + 1].Offset - 1;
                    output.Write(code.Span[copied..live]);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                    copied = live + 1;
                }

                foreach (int index in uses.Order())
                {
                    BeamInstruction instruction = body[index];
                    int instructionEnd = body[index + 1].Offset;
                    if (instruction.Definition.Opcode == Move && instruction.Operands[1] == result)
                    {
                        output.Write(code.Span[copied..instruction.Offset]);
                        copied = instructionEnd;
                        continue;
                    }

                    while (operand < stackOperands.Count && stackOperands[operand].Start < instruction.Offset)
                    {
                        operand++;
                    }

                    while (operand < stackOperands.Count && stackOperands[operand].Start < instructionEnd)
                    {
                        var (from, to, register) = stackOperands[operand++];
                        if (register == slot.Value)
                        {
                            output.Write(code.Span[copied..from]);
                            WriteRegister(output, result);
                            copied = to;
                        }
                    }
                }

                // The next pass can analyze other saves against this function's revised control flow.
                break;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private sealed partial class BeamProjectionFlow
    {
        internal HashSet<int>? PlanSavedCallResult(
            int save,
            int end,
            BeamOperand slot,
            BeamOperand result,
            int? retainedHeap
        )
        {
            var uses = new HashSet<int>();
            var preserves = new HashSet<int>();
            var visited = new HashSet<(int Index, bool InResult)>();
            var work = new Stack<(int Index, bool InResult)>();
            work.Push((save + 1, true));
            while (work.TryPop(out var state))
            {
                int index = state.Index;
                if (!visited.Add(state))
                {
                    continue;
                }

                if (index <= save || index >= end)
                {
                    return null;
                }

                BeamInstruction instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                bool inResult = state.InResult;
                if (opcode is Deallocate or Return or IfEnd or TailLocalCall or TailLocalCallWithFrame
                    or TailExternalCall or TailExternalCallWithFrame or TailDynamicApply)
                {
                    continue;
                }

                if (opcode == InitYRegisters && instruction.Operands.Any(value => SameProjectionRegister(value, slot)))
                {
                    continue;
                }

                bool call = opcode is LocalCall or ExternalCall or DynamicApply or CallFun or CallFunTyped;
                bool terminal = opcode is BadMatch or CaseEnd or BadRecord;
                int destination = opcode switch
                {
                    Move or GetTupleElement or GetHead or GetTail or GetList or PutList or PutTuple or UpdateRecord =>
                        StackWriteOperand(instruction),
                    _ => -1
                };
                if (destination < 0 && !(call || terminal || IsEntryFramePredicate(opcode)
                    || opcode is Label or Line or Jump or SelectValue or SelectTupleArity or InitYRegisters or TestHeap))
                {
                    return null;
                }

                bool readsSlot = false;
                bool writesSlot = false;
                bool writesResult = false;
                for (int position = 0; position < instruction.Operands.Count; position++)
                {
                    BeamOperand operand = instruction.Operands[position];
                    bool writes = position == destination || opcode == GetList && position == 2;
                    if (SameProjectionRegister(operand, slot))
                    {
                        readsSlot |= !writes;
                        writesSlot |= writes;
                    }

                    writesResult |= writes && SameProjectionRegister(operand, result);
                }

                if (readsSlot)
                {
                    if (!inResult || writesSlot)
                    {
                        return null;
                    }

                    uses.Add(index);
                }

                if (writesSlot || terminal)
                {
                    continue;
                }

                bool reload = opcode == Move && readsSlot;
                inResult &= !(call || writesResult && !reload
                    || opcode == TestHeap && instruction.Operands[^1].Value.IsZero && index != retainedHeap);
                if (inResult)
                {
                    preserves.Add(index);
                }

                foreach (int target in successors[index])
                {
                    work.Push((target, inResult));
                }
            }

            // At a join, every incoming path must have obtained X0 from the same call.
            var incoming = new Stack<int>(uses);
            var checkedUses = new HashSet<int>();
            while (incoming.TryPop(out int index))
            {
                if (!checkedUses.Add(index))
                {
                    continue;
                }

                foreach (int previous in predecessors[index])
                {
                    if (previous == save)
                    {
                        continue;
                    }

                    if (!preserves.Contains(previous))
                    {
                        return null;
                    }

                    incoming.Push(previous);
                }
            }

            return uses;
        }
    }
}
