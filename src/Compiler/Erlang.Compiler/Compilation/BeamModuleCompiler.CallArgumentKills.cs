using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? OrderCallArgumentAfterKills(
        ReadOnlyMemory<byte> code,
        int labelCount,
        bool finalPass = false
    )
    {
        if (ModuleOptimizationDisabled || finalPass && !SsaTypeInferenceEnabled
            || !code.Span.Contains(InitYRegisters) && !code.Span.Contains(TrimFrame)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        using var output = new MemoryStream();
        int copied = 0;
        int functionStart = -1;
        bool? unprotected = null;
        for (int move = 0; move + 2 < body.Count; move++)
        {
            if (finalPass && body[move].Definition.Opcode == FunctionInfo)
            {
                functionStart = move + 1;
                unprotected = null;
            }

            if (body[move] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var source, { Kind: BeamOperandParameters.X } destination]
                } || destination.Value != 0)
            {
                continue;
            }

            int end = move + 1;
            // beam_ssa_codegen moves independent X0 constants after the kill.
            // beam_trim can turn that kill into a trim before the ordinary call.
            bool literalBeforeTrim = finalPass && IsNonRegisterValue(source)
                && body[end].Definition.Opcode == TrimFrame && end + 2 < body.Count
                && body[end + 1].Definition.Opcode == Line && body[end + 2].Definition.Opcode == LocalCall;
            bool literalBeforeListKill = finalPass && move > 0 && move > functionStart && IsNonRegisterValue(source)
                && body[move - 1] is
                {
                    Definition.Opcode: PutList,
                    Operands: [
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } head,
                    { Kind: BeamOperandParameters.Nil },
                    { Kind: BeamOperandParameters.X, Value.IsOne: true }
                    ]
                }
                && body[end].Definition.Opcode == InitYRegisters
                && body[end].Operands.Skip(1).Any(slot => SameProjectionRegister(head, slot));
            if (finalPass && (destination.TypeIndex is not null
                || !literalBeforeTrim && !literalBeforeListKill && (source is not { Kind: BeamOperandParameters.Y, TypeIndex: null }
                    || body[end].Definition.Opcode != InitYRegisters)
                || functionStart < 0 || !(unprotected ??= HasUnprotectedCallKillSchedule(body, functionStart))))
            {
                continue;
            }

            while (source.Kind == BeamOperandParameters.Y && body[end] is
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Y }, { Kind: BeamOperandParameters.Y } relocated]
                } && !SameProjectionRegister(source, relocated))
            {
                end++;
            }

            // beam_trim may insert Y relocations in front of a trim. They
            // can precede the X0 load when they leave its source intact.
            if (body[end].Definition.Opcode is not (InitYRegisters or TrimFrame)
                || end != move + 1 && body[end].Definition.Opcode != TrimFrame)
            {
                continue;
            }

            bool readsClearedSlot = false;
            int? survivingSlot = null;
            if (body[end] is { Definition.Opcode: TrimFrame, Operands: [var removed, var kept] })
            {
                if (!literalBeforeTrim)
                {
                    if (source is not { Kind: BeamOperandParameters.Y, TypeIndex: null }
                        || destination.TypeIndex is not null || source.Value < removed.Value
                        || source.Value >= removed.Value + kept.Value)
                    {
                        continue;
                    }

                    survivingSlot = (int)(source.Value - removed.Value);
                }

                end++;
            }

            while (body[end].Definition.Opcode == InitYRegisters)
            {
                var read = survivingSlot is int slot ? source with { Value = slot } : source;
                readsClearedSlot |= body[end].Operands.Any(operand => SameProjectionRegister(operand, read));
                end++;
            }

            int call = end;
            while (body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (readsClearedSlot || body[call] is not
                {
                    Definition.Opcode: LocalCall or ExternalCall,
                    Operands: [var arity, _]
                } || arity.Value <= 0 || literalBeforeListKill
                    && (body[call].Definition.Opcode != ExternalCall || arity.Value != 2))
            {
                continue;
            }

            // Keep the independent X0 copy next to its call, after the dead Y slots are cleared.
            int start = body[move].Offset;
            int kills = body[move + 1].Offset;
            output.Write(code.Span[copied..start]);
            output.Write(code.Span[kills..body[end].Offset]);
            if (survivingSlot is int saved)
            {
                output.WriteByte(Move);
                BeamCompactWriter.Write(output, YRegisterTag, saved);
                BeamCompactWriter.Write(output, XRegisterTag, 0);
            }
            else
            {
                output.Write(code.Span[start..kills]);
            }
            copied = body[end].Offset;
            move = end - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private byte[]? ScheduleRetiredCallArgumentRoots(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(InitYRegisters)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return OrderRetiredArgumentRoots(code, body);
    }

    private static byte[]? OrderRetiredArgumentRoots(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int info = 0; info + 4 < body.Count; info++)
        {
            if (body[info].Definition.Opcode != FunctionInfo)
            {
                continue;
            }

            int entry = info + 1;
            int end = entry;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            bool? unprotected = null;
            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            for (int call = entry + 2; call + 2 < end; call++)
            {
                if (ReadRetiredArgumentMove(body, entry, call) is not { } setup
                    || !(unprotected ??= HasUnprotectedCheckedFailureRegion(body, entry, call + 1)))
                {
                    continue;
                }

                ownership ??= ReadDisjointFrameOwnership(
                    body,
                    entry,
                    end,
                    trackSiblingTrimEpochs: true,
                    allowFrameless: true
                );
                if (ownership is not { } frame)
                {
                    break;
                }

                int epoch = frame.Owners[call - entry];
                if (epoch < 0 || frame.Owners[setup.FirstMove - entry] != epoch
                    || frame.Owners[call + 1 - entry] != epoch)
                {
                    continue;
                }

                flow ??= new BeamProjectionFlow(body);
                if (setup.Dead.Any(slot => !flow.IsDead(call, slot)))
                {
                    continue;
                }

                int insert = body[setup.Insert].Offset;
                int clear = body[call + 1].Offset;
                int afterClear = body[call + 2].Offset;
                output.Write(code.Span[copied..insert]);
                output.Write(code.Span[clear..afterClear]);
                output.Write(code.Span[insert..clear]);
                copied = afterClear;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static (int FirstMove, int Insert, BeamOperand[] Dead)? ReadRetiredArgumentMove(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int call
    )
    {
        if (body[call] is not { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value: var arity }, _] }
            || arity <= 0 || body[call + 1].Definition.Opcode != InitYRegisters)
        {
            return null;
        }

        int move = call - 1;
        while (move > entry && body[move].Definition.Opcode == Line)
        {
            move--;
        }

        if (body[move] is not { Definition.Opcode: Move, Operands: [var first, var destination] }
            || !IsBareX0(destination))
        {
            return null;
        }

        BeamOperand[] dead = body[call + 1].Operands.Skip(1).ToArray();
        if (dead.Length == 0 || dead.Any(slot => slot is not { Kind: BeamOperandParameters.Y, TypeIndex: null }))
        {
            return null;
        }

        var destinations = new HashSet<int>();
        var outgoingRoots = new HashSet<int>();
        int start = move;
        while (start > entry && body[start] is { Definition.Opcode: Move, Operands: [var source, var result] }
            && IsBareX(result))
        {
            if (result.Value >= arity || !destinations.Add((int)result.Value))
            {
                return null;
            }

            if (source.Kind == BeamOperandParameters.Y)
            {
                outgoingRoots.Add((int)source.Value);
            }

            start--;
        }

        if (dead.Any(slot => !outgoingRoots.Contains((int)slot.Value)))
        {
            return null;
        }

        // Keep a last X0 load of a retiring Y value ahead of its clear.
        int insert = dead.Any(slot => SameProjectionRegister(slot, first)) ? move + 1 : move;

        return (start + 1, insert, dead);
    }

    private static bool HasUnprotectedCallKillSchedule(IReadOnlyList<BeamInstruction> body, int start)
    {
        for (int index = start; body[index].Definition.Opcode is not (FunctionInfo or EndCode); index++)
        {
            if (body[index].Definition.Opcode is CatchEnter or TryEnter or TryEnd or TryCase or CatchEnd
                or LoopReceive or LoopReceiveEnd or Wait or WaitTimeout or DebugLine or ExecutableLine)
            {
                return false;
            }
        }

        return true;
    }

    private byte[]? OrderArgumentsWithRetainedCallResult(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(TrimFrame)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        for (int start = 1; start + 7 < body.Count; start++)
        {
            if (body[start - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[start].Definition.Opcode != Move)
            {
                continue;
            }

            int save = start;
            var reads = new List<int>();
            while (IsRetainedCallArgumentRead(body[save]))
            {
                reads.Add(save++);
            }

            if (reads.Count < 3 || body[save] is not
                {
                    Definition.Opcode: Move,
                    Operands:
                    [
                    { Kind: BeamOperandParameters.X, Value.IsZero: true },
                    { Kind: BeamOperandParameters.Y } retained
                    ]
                })
            {
                continue;
            }

            int trim = save + 1;
            while (IsRetainedCallArgumentRead(body[trim]))
            {
                reads.Add(trim++);
            }

            if (trim + 3 >= body.Count || body[trim] is not
                { Definition.Opcode: TrimFrame, Operands: [var removed, { Value.IsOne: true }] }
                || removed.Value != retained.Value
                || body[trim + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands:
                    [
                    { Kind: BeamOperandParameters.X } deferred,
                    { Kind: BeamOperandParameters.X, Value.IsZero: true }
                    ]
                } || deferred.Value != removed.Value + 1)
            {
                continue;
            }

            int call = trim + 2;
            while (call < body.Count && body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (call >= body.Count || body[call] is not
                { Definition.Opcode: LocalCall or ExternalCall, Operands: [var arity, _] }
                || arity.Value != deferred.Value || arity.Value != reads.Count)
            {
                continue;
            }

            BeamOperand Input(int read) => body[read].Operands[0];

            BeamOperand Output(int read) => body[read].Operands[1];

            if (reads.Select(read => Input(read).Value).Distinct().Count() != reads.Count
                || reads.Select(read => Output(read).Value).Distinct().Count() != reads.Count
                || reads.Any(read => Input(read).Value > retained.Value || Output(read).Value > deferred.Value))
            {
                continue;
            }

            int deferredRead = reads.Single(read => Output(read).Value == deferred.Value);
            int finalRead = reads.Single(read => Input(read).Value.IsZero);
            if (deferredRead >= save || Input(deferredRead).Value != retained.Value
                || Output(finalRead).Value != deferred.Value - 1)
            {
                continue;
            }

            int[] independent = reads.Where(read => read != deferredRead && read != finalRead)
                .OrderByDescending(read => Output(read).Value).ToArray();
            if (finalRead == trim - 1 && deferredRead == save - 1
                && independent.SequenceEqual(reads.Take(independent.Length)))
            {
                continue;
            }

            // Set up the independent arguments first. Save the displaced X0
            // result after consuming the retained Y slot's previous value.
            output.Write(code.Span[copied..body[start].Offset]);
            foreach (int read in independent)
            {
                CopyInstruction(read);
            }

            CopyInstruction(deferredRead);
            CopyInstruction(save);
            CopyInstruction(finalRead);
            copied = body[trim].Offset;
            start = trim;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void CopyInstruction(int index) => output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
    }

    private static bool IsRetainedCallArgumentRead(BeamInstruction instruction) =>
        instruction is
        {
            Definition.Opcode: Move,
            Operands:
            [
            { Kind: BeamOperandParameters.Y, TypeIndex: null },
            { Kind: BeamOperandParameters.X, TypeIndex: null, Value.Sign: > 0 }
            ]
        };
}
