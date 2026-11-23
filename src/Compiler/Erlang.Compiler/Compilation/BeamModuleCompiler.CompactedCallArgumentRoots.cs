using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ScheduleCompactedCallArgumentRoots(
        ReadOnlyMemory<byte> code,
        int labelCount,
        IReadOnlySet<int> compactedEntries
    )
    {
        if (DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        int copied = 0;
        bool compacted = false;
        for (int first = 1; first + 4 < body.Count; first++)
        {
            if (body[first].Definition.Opcode == FunctionInfo)
            {
                compacted = body[first + 1] is { Definition.Opcode: Label, Operands: [var entry] }
                    && compactedEntries.Contains((int)entry.Value)
                    && HasUnprotectedCallKillSchedule(body, first + 1);
                continue;
            }

            if (!compacted || body[first - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[first] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var fromResult, { Kind: BeamOperandParameters.X, TypeIndex: null } result]
                } || fromResult != x0 || (result.Value < 1 || result.Value > 2)
                || !TryReadStackArgument(first + 1, out var firstSlot, out var firstArgument)
                || !TryReadStackArgument(first + 2, out var secondSlot, out var secondArgument)
                || firstSlot == secondSlot || firstArgument == secondArgument
                || firstArgument == result || secondArgument == result)
            {
                continue;
            }

            int call = first + 3;
            while (call < body.Count && body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (call == body.Count || body[call] is not
                { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value: var arity }, _] } || arity != 3)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!CanClear(first + 1, call, firstSlot) || !CanClear(first + 2, call, secondSlot))
            {
                continue;
            }

            int toX0 = firstArgument == x0 ? first + 1 : first + 2;
            int independent = toX0 == first + 1 ? first + 2 : first + 1;
            output.Write(code.Span[copied..body[first + 1].Offset]);
            CopyInstruction(independent);
            CopyInstruction(toX0);

            // The separate return copy saves X0 before either stack argument.
            // Leave the X0 argument last among the two setup moves. Both
            // copies precede the clear because it consumes their sources.
            int[] slots = [(int)firstSlot.Value, (int)secondSlot.Value];
            Array.Sort(slots);
            WriteFrameInitialization(output, slots);
            copied = body[first + 3].Offset;
            first += 2;
        }

        if (copied == 0)
        {
            return null;
        }

        highestOpcode = Math.Max(highestOpcode, InitYRegisters);
        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool TryReadStackArgument(int index, out BeamOperand slot, out BeamOperand argument)
        {
            if (body[index] is
                {
                    Definition.Opcode: Move,
                    Operands:
                    [
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } source,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } destination
                    ]
                } && destination.Value >= 0 && destination.Value < 3)
            {
                slot = source;
                argument = destination;

                return true;
            }

            slot = null!;
            argument = null!;

            return false;
        }

        bool CanClear(int read, int call, BeamOperand slot) =>
            flow!.IsInitialized(read, slot, allowOwnedCollectingWrite: true)
            && flow.IsDead(call, slot);

        void CopyInstruction(int index) => output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
    }
}
