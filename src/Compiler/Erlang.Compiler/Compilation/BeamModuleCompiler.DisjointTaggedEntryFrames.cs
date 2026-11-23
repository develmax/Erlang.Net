using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static EntryFramePlacement? PlanDisjointTaggedEntryFrames(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int allocation,
        int end,
        ref BeamProjectionFlow? flow
    )
    {
        if (start == 0 || allocation != start + 1 || allocation + 3 >= end
            || body[start - 1] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var formals] }
            || body[allocation] is not
            {
                Definition.Opcode: Allocate,
                Operands: [{ Value.Sign: > 0 } width, { Value.Sign: > 0 } live]
            }
            || live.Value > formals.Value)
        {
            return null;
        }

        // Model the setup exactly. In particular, initialization kills an
        // earlier spill to the same slot.
        var saved = new Dictionary<int, int>();
        int check = allocation + 1;
        for (; check < end; check++)
        {
            BeamInstruction instruction = body[check];
            if (instruction is
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X } source, { Kind: BeamOperandParameters.Y } target]
                } && source.Value < live.Value && target.Value < width.Value)
            {
                saved[(int)target.Value] = (int)source.Value;
            }
            else if (instruction.Definition.Opcode == InitYRegisters)
            {
                foreach (BeamOperand slot in instruction.Operands.Skip(1))
                {
                    saved.Remove((int)slot.Value);
                }
            }
            else
            {
                break;
            }
        }

        if (check + 1 >= end || body[check] is not
            {
                Definition.Opcode: IsTaggedTuple,
                Operands: [
                { Kind: BeamOperandParameters.Label, Value.Sign: > 0 } rejected,
                    var selector,
                { Kind: BeamOperandParameters.Unsigned, Value.Sign: > 0 },
                { Kind: BeamOperandParameters.Atom }
                ]
            }
            || selector.Kind == BeamOperandParameters.X && selector.Value >= live.Value
            || selector.Kind == BeamOperandParameters.Y && !saved.ContainsKey((int)selector.Value)
            || selector.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y)
            || ReadDisjointFrameOwnership(
                body,
                start,
                end,
                trackSiblingTrimEpochs: true
            ) is not { } ownership
            || ownership.Owners[check - start] != allocation)
        {
            return null;
        }

        flow ??= new BeamProjectionFlow(body);
        if (!flow.TryGetSingleForwardTarget(check, (int)rejected.Value, out int fallback)
            || fallback <= check + 1 || fallback + 1 >= end
            || ownership.Owners[check + 1 - start] != allocation
            || ownership.Owners[fallback - start] != allocation
            || !flow.HasDisjointCallingArms(
                check,
                check + 1,
                fallback,
                end
            ))
        {
            return null;
        }

        // Both arms see the original frame and all original input values.
        // Only the non-allocating constructor test sees the incoming X bank.
        return new(
            check,
            saved,
            [check],
            new Dictionary<int, int>
            {
                [check + 1] = (int)live.Value,
                [fallback + 1] = (int)live.Value
            },
            new Dictionary<int, int>(),
            new Dictionary<int, (int Slot, int Register)[]>()
        );
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool HasDisjointCallingArms(
            int split,
            int matched,
            int rejected,
            int end
        )
        {
            if (predecessors[matched] is not [var incoming] || incoming != split)
            {
                return false;
            }

            var matchedBlocks = new HashSet<int>();
            var rejectedBlocks = new HashSet<int>();

            return Walk(matched, matchedBlocks) && Walk(rejected, rejectedBlocks)
                && !matchedBlocks.Overlaps(rejectedBlocks);

            bool Walk(int first, HashSet<int> visited)
            {
                var pending = new Stack<int>();
                pending.Push(first);
                bool calls = false;
                while (pending.TryPop(out int index))
                {
                    if (index <= split || index >= end)
                    {
                        return false;
                    }

                    if (!visited.Add(index))
                    {
                        continue;
                    }

                    byte opcode = body[index].Definition.Opcode;
                    calls |= opcode is LocalCall or ExternalCall or CallFun or DynamicApply;
                    // The generic flow conservatively falls through these
                    // exception exits. They leave this unprotected function.
                    if (opcode is BadRecord or Raise)
                    {
                        continue;
                    }

                    foreach (int next in successors[index])
                    {
                        pending.Push(next);
                    }
                }

                return calls;
            }
        }
    }
}
