using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? ReuseProducerBinaryMatchFailure(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<string> atomNames
    )
    {
        using var output = new MemoryStream();
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int index = 1; index + 7 < body.Count; index++)
        {
            if (body[index - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[index] is not { Definition.Opcode: Move, Operands: [var produced, { Kind: BeamOperandParameters.Y } saved] }
                || !SameProjectionRegister(produced, result)
                || body[index + 1] is not
                {
                    Definition.Opcode: StartBinaryMatch,
                    Operands: [var failure, var input, _, { Kind: BeamOperandParameters.X } context]
                }
                || !SameProjectionRegister(input, result) || SameProjectionRegister(context, result)
                || body[index + 2] is not { Definition.Opcode: BinaryMatch, Operands: var commands }
                || !PreservesProducerOnMismatch(
                    commands,
                    failure,
                    context,
                    result,
                    saved,
                    atomNames
                )
                )
            {
                continue;
            }

            int jump = index + 3;
            bool replaced = false;
            while (jump < body.Count && body[jump] is
                { Definition.Opcode: Move, Operands: [var value, var target] })
            {
                if (!replaced && SameProjectionRegister(value, saved))
                {
                    break;
                }

                replaced |= SameProjectionRegister(target, saved);
                jump++;
            }

            int failureEntry = jump + 1;
            if (failureEntry >= body.Count
                || body[jump] is not { Definition.Opcode: Jump, Operands: [var success] }
                || body[failureEntry] is not { Definition.Opcode: Label, Operands: [var label] }
                || label.Value != failure.Value)
            {
                continue;
            }

            int restore = failureEntry + 1;
            while (restore < body.Count && body[restore].Definition.Opcode == Line)
            {
                restore++;
            }

            if (restore >= body.Count - 2 || body[restore] is not { Definition.Opcode: Move, Operands: [var from, var to] }
                || !SameProjectionRegister(from, saved) || !SameProjectionRegister(to, result))
            {
                continue;
            }

            int exit = restore + 1;
            while (exit < body.Count && body[exit].Definition.Opcode == Line)
            {
                exit++;
            }

            if (exit >= body.Count - 1
                || body[exit] is not { Definition.Opcode: BadMatch, Operands: [var failed] }
                || !SameProjectionRegister(failed, result)
                || !(flow ??= new BeamProjectionFlow(body)).HasBinaryFailureInputs(failureEntry, index + 1, index + 2)
                || !flow.TryGetSingleForwardTarget(jump, (int)success.Value, out int next)
                || next != exit + 1
                || !(flow.IsInitialized(index, saved) || ReadsInitializedProducerFrameSlot(body, index - 1, saved))
                || !replaced && !flow.IsSavedBinaryProducerDead(next, saved))
            {
                continue;
            }

            // The fused match can only fail before writing X0. Keep its original term as badmatch's argument.
            output.Write(code.Span[copied..body[index].Offset]);
            output.Write(code.Span[body[index + 1].Offset..body[restore].Offset]);
            copied = body[restore + 1].Offset;
            index = exit;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool ReadsInitializedProducerFrameSlot(
        IReadOnlyList<BeamInstruction> body,
        int call,
        BeamOperand slot
    )
    {
        int prior = call - 1;
        while (prior >= 0 && body[prior].Definition.Opcode == Line)
        {
            prior--;
        }

        // An adjacent BIF argument already needed a term in the slot before the ordinary call.
        return prior >= 0 && body[prior] is
        { Definition.Opcode: GcBifOne or GcBifTwo or GcBifThree, Operands: var operands }
            && operands.Skip(3).SkipLast(1).Any(operand => SameProjectionRegister(operand, slot));
    }

    private static bool PreservesProducerOnMismatch(
        IReadOnlyList<BeamOperand> commands,
        BeamOperand failure,
        BeamOperand context,
        BeamOperand result,
        BeamOperand saved,
        IReadOnlyList<string> atomNames
    )
    {
        if (commands.Count <= BeamBinaryNames.MatchPrefix || commands[0].Value != failure.Value
            || !SameProjectionRegister(commands[1], context) || commands.Any(operand => SameProjectionRegister(operand, saved)))
        {
            return false;
        }

        if (commands is
            [
                _, _, _,
            { Kind: BeamOperandParameters.Atom } ensure,
            { Kind: BeamOperandParameters.Unsigned } minimum,
            { Kind: BeamOperandParameters.Unsigned } alignment,
            { Kind: BeamOperandParameters.Atom } extract,
                _,
            { Kind: BeamOperandParameters.Nil },
            { Kind: BeamOperandParameters.Unsigned } size,
            { Kind: BeamOperandParameters.Unsigned } unit,
                var destination
            ]
            && SameProjectionRegister(destination, result)
            && atomNames[(int)ensure.Value - 1] == BeamBinaryNames.EnsureAtLeast
            && atomNames[(int)extract.Value - 1] == BeamBinaryNames.IntegerSegment
            && size.Value.Sign > 0 && unit.Value.Sign > 0 && alignment.Value.Sign > 0
            && minimum.Value >= size.Value * unit.Value)
        {
            // This single integer extraction is ensured by the preceding bounds check.
            return true;
        }

        // All other commands must preserve the original register, including their GC live prefixes.
        int cursor = BeamBinaryNames.MatchPrefix;
        while (cursor < commands.Count)
        {
            if (commands[cursor++] is not { Kind: BeamOperandParameters.Atom } opcode)
            {
                return false;
            }

            string name = atomNames[(int)opcode.Value - 1];
            int argumentCount = name switch
            {
                BeamBinaryNames.EnsureAtLeast => 2,
                BeamBinaryNames.EnsureExactly or BeamBinaryNames.SkipCommand => 1,
                BeamBinaryNames.EqualCommand or BeamBinaryNames.GetTail => 3,
                BeamBinaryNames.BinarySegment or BeamBinaryNames.IntegerSegment => 5,
                _ => -1
            };
            if (argumentCount < 0 || cursor + argumentCount > commands.Count)
            {
                return false;
            }

            if (name is BeamBinaryNames.GetTail or BeamBinaryNames.BinarySegment or BeamBinaryNames.IntegerSegment
                && (commands[cursor].Value <= result.Value || SameProjectionRegister(commands[cursor + argumentCount - 1], result)))
            {
                return false;
            }

            cursor += argumentCount;
        }

        return true;
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool HasBinaryFailureInputs(int label, int start, int match) =>
            predecessors[label] is [var first, var second] && first == start && second == match;

        internal bool IsSavedBinaryProducerDead(int continuation, BeamOperand slot)
        {
            var pending = new Stack<int>([continuation]);
            var visited = new HashSet<int>();
            while (pending.TryPop(out int index))
            {
                if (!visited.Add(index))
                {
                    continue;
                }

                BeamInstruction instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                if (instruction.Operands.Any(operand => SameProjectionRegister(operand, slot))
                    || opcode is Allocate or AllocateHeap or TrimFrame or TryEnter or CatchEnter
                    or LoopReceive or Wait or WaitTimeout or FunctionInfo or EndCode)
                {
                    return false;
                }

                if (opcode is Deallocate or Return or TailLocalCall or TailExternalCall
                    or TailLocalCallWithFrame or TailExternalCallWithFrame or CaseEnd or BadMatch or IfEnd or TryCaseEnd)
                {
                    continue;
                }

                if (successors[index].Length == 0)
                {
                    return false;
                }

                foreach (int next in successors[index])
                {
                    pending.Push(next);
                }
            }

            return true;
        }
    }
}
