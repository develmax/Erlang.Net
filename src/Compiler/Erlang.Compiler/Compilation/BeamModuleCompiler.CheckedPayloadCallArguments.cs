using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ForwardCheckedPayloadCallArgument(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        byte[]? rewritten = RewriteCheckedPayloadCallArgument(code, body);
        if (rewritten is not null)
        {
            highestOpcode = Math.Max(highestOpcode, InitYRegisters);
        }

        return rewritten;
    }

    private static byte[]? RewriteCheckedPayloadCallArgument(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        int copied = 0;
        int entry = 0;
        for (int spill = 0; spill + 3 < body.Count; spill++)
        {
            if (body[spill].Definition.Opcode == FunctionInfo)
            {
                entry = spill + 1;
                continue;
            }

            if (body[spill] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var producer, { Kind: BeamOperandParameters.Y, TypeIndex: null } storage]
                } || producer != result)
            {
                continue;
            }

            int reload = spill + 1;
            while (IsIndependentArgumentSetup(reload, result, storage))
            {
                reload++;
            }

            if (reload >= body.Count || body[reload] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var from, { Kind: BeamOperandParameters.X, TypeIndex: null, Value.Sign: > 0 } argument]
                } || from != storage)
            {
                continue;
            }

            int call = reload + 1;
            while (IsIndependentArgumentSetup(call, argument, storage))
            {
                call++;
            }

            if (call + 1 >= body.Count || body[call] is not
                { Definition.Opcode: LocalCall or ExternalCall, Operands: [var arity, _] }
                || arity.Value <= argument.Value
                || !(flow ??= new BeamProjectionFlow(body)).HasCheckedPayloadDefinition(spill, entry, result)
                || !flow.HasUnprotectedIncomingPath(spill, entry)
                || !flow.IsDead(
                    call + 1,
                    storage,
                    afterCall: true,
                    endsAtCatchSlotWrite: true
                ))
            {
                continue;
            }

            // X0 still holds the checked payload. Clear the old physical
            // root after copying the payload into its outgoing register.
            output.Write(code.Span[copied..body[spill].Offset]);
            output.Write(code.Span[body[spill + 1].Offset..body[reload].Offset]);
            output.WriteByte(Move);
            WriteRegister(output, result);
            WriteRegister(output, argument);
            WriteFrameInitialization(output, [(int)storage.Value]);
            copied = body[reload + 1].Offset;
            spill = reload;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool IsIndependentArgumentSetup(int index, BeamOperand preserved, BeamOperand slot) =>
            index < body.Count && (body[index].Definition.Opcode == Line
                || body[index] is { Definition.Opcode: Move, Operands: [var from, var destination] }
                && !SameProjectionRegister(from, slot) && !SameProjectionRegister(destination, slot)
                && !SameProjectionRegister(destination, preserved));
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool HasCheckedPayloadDefinition(int start, int entry, BeamOperand result)
        {
            int index = start;
            while (predecessors[index] is [var previous] && previous >= entry && previous < index)
            {
                BeamInstruction instruction = body[previous];
                if (instruction is { Definition.Opcode: GetTupleElement, Operands: [var carrier, var field, var value] })
                {
                    return carrier == result && value == result && predecessors[previous] is [var check]
                        && check + 1 == previous && body[check] is
                        { Definition.Opcode: IsTaggedTuple, Operands: [_, var tested, var arity, _] }
                        && SameProjectionRegister(tested, carrier) && field.Value < arity.Value;
                }

                if (instruction.Definition.Opcode is not (Label or Line or Jump or ExactEqualTest or ExactNotEqualTest))
                {
                    return false;
                }

                index = previous;
            }

            return false;
        }

        internal bool HasUnprotectedIncomingPath(int start, int entry)
        {
            var pending = new Stack<int>();
            var seen = new HashSet<int>();
            bool foundEntry = false;
            pending.Push(start);
            while (pending.TryPop(out int index))
            {
                if (index == entry)
                {
                    foundEntry = true;
                    continue;
                }

                if (!seen.Add(index))
                {
                    continue;
                }

                if (index < entry || predecessors[index].Count == 0 || body[index].Definition.Opcode is
                    CatchEnter or CatchEnd or TryEnter or TryEnd or TryCase or TryCaseEnd or DebugLine or ExecutableLine
                    or LoopReceive or LoopReceiveEnd or Wait or WaitTimeout)
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

            return foundEntry;
        }
    }
}
