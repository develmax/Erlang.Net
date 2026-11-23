using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static HashSet<int>? ReadClosedHandlerTailCallLines(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        ref BeamProjectionFlow? flow
    )
    {
        int entry = -1;
        int arity = -1;
        int entered = -1;
        int closed = -1;
        int handled = -1;
        bool catches = false;
        BeamOperand? tag = null;
        for (int index = start; index < end; index++)
        {
            switch (body[index].Definition.Opcode)
            {
                case FunctionInfo:
                    if (entry >= 0 || index + 1 >= end || body[index + 1].Definition.Opcode != Label)
                    {
                        return null;
                    }

                    arity = (int)body[index].Operands[2].Value;
                    entry = (int)body[index + 1].Operands[0].Value;
                    break;
                case TryEnter:
                case CatchEnter:
                    if (entered >= 0 || body[index] is not
                        {
                            Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } slot,
                            { Kind: BeamOperandParameters.Label }]
                        })
                    {
                        return null;
                    }

                    entered = index;
                    catches = body[index].Definition.Opcode == CatchEnter;
                    tag = slot;
                    break;
                case TryEnd:
                case CatchEnd:
                    if (tag is null || closed >= 0 || body[index].Operands[0] != tag
                        || catches != (body[index].Definition.Opcode == CatchEnd))
                    {
                        return null;
                    }

                    closed = index;
                    break;
                case TryCase:
                    if (catches || tag is null || handled >= 0 || body[index].Operands[0] != tag)
                    {
                        return null;
                    }

                    handled = index;
                    break;
                case LoopReceive:
                case LoopReceiveEnd:
                case Wait:
                case WaitTimeout:
                case DebugLine:
                case ExecutableLine:
                    return null;
            }
        }

        if (entry < 0 || entered < 0 || closed <= entered)
        {
            return null;
        }

        var ownership = flow ??= new BeamProjectionFlow(body);
        var exceptionTarget = body[entered].Operands[1];
        if (catches)
        {
            // Normal completion and the catch transfer close the same slot.
            // A different incoming route must not acquire the unprotected fact.
            if (closed < entered + 3 || body[closed - 2].Definition.Opcode is not (LocalCall or ExternalCall)
                || !ownership.TryGetExclusiveForwardTarget(
                    entered,
                    closed - 2,
                    (int)exceptionTarget.Value,
                    out int landing
                ) || landing != closed - 1)
            {
                return null;
            }
        }
        else if (handled <= entered
            || body[handled - 1] is not { Definition.Opcode: Label, Operands: [var handler] }
            || handler.Value != exceptionTarget.Value)
        {
            return null;
        }

        var lines = new HashSet<int>();
        for (int index = closed + 1; index + 1 < end; index++)
        {
            if (body[index].Definition.Opcode == Line
                && body[index + 1] is
                {
                    Definition.Opcode: TailLocalCallWithFrame or TailLocalCall,
                    Operands: [var count, { Kind: BeamOperandParameters.Label } target, ..]
                }
                && count.Value == arity && target.Value == entry
                && ownership.IsNormalContinuationOfHandlerClose(closed, index))
            {
                lines.Add(index);
            }
        }

        return lines.Count > 0 ? lines : null;
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool IsNormalContinuationOfHandlerClose(int closed, int line, bool requireStableFrame = false)
        {
            var seen = new HashSet<int>();
            var pending = new Stack<int>();
            pending.Push(line);
            while (pending.TryPop(out int index))
            {
                if (index == closed || !seen.Add(index))
                {
                    continue;
                }

                // All incoming paths must pass through this same normal close.
                // A handler predecessor, an independent entry, or a cycle
                // cannot establish this property for the candidate tail line.
                byte opcode = body[index].Definition.Opcode;
                if (index < closed || opcode is TryCase or TryCaseEnd || predecessors[index].Count == 0
                    || requireStableFrame && opcode is
                        (Allocate or AllocateHeap or Deallocate or TrimFrame or LoopReceive or LoopReceiveEnd
                        or Wait or WaitTimeout or DebugLine or ExecutableLine))
                {
                    return false;
                }

                foreach (int previous in predecessors[index])
                {
                    if (previous >= index)
                    {
                        return false;
                    }

                    pending.Push(previous);
                }
            }

            return true;
        }
    }
}
