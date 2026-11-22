using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ClearConsumedTaggedCallArguments(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        byte[]? rewritten = RewriteConsumedTaggedCallArguments(code, body);
        if (rewritten is not null)
        {
            highestOpcode = Math.Max(highestOpcode, InitYRegisters);
        }

        return rewritten;
    }

    private static byte[]? RewriteConsumedTaggedCallArguments(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int info = 0; info + 9 < body.Count; info++)
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

            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            bool? unprotected = null;
            for (int call = entry + 6; call < end; call++)
            {
                if (body[call] is not
                    {
                        Definition.Opcode: LocalCall or ExternalCall,
                        Operands: [var arity, _]
                    } || arity.Value < 3 || arity.Value > FirstReservedXRegister)
                {
                    continue;
                }

                int x0Move = call - 1;
                while (body[x0Move].Definition.Opcode == Line)
                {
                    x0Move--;
                }

                if (body[x0Move] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } input, var target]
                    } || target != x0)
                {
                    continue;
                }

                int start = x0Move;
                var destinations = new HashSet<int> { 0 };
                var consumed = new List<BeamOperand>();
                while (start > entry && body[start - 1] is
                    {
                        Definition.Opcode: Move,
                        Operands: [var source, { Kind: BeamOperandParameters.X, TypeIndex: null } argument]
                    } && argument.Value < arity.Value && destinations.Add((int)argument.Value))
                {
                    if (source is { Kind: BeamOperandParameters.Y, TypeIndex: null } slot
                        && slot != input && !consumed.Contains(slot))
                    {
                        consumed.Add(slot);
                    }
                    else if (!IsNonRegisterValue(source))
                    {
                        break;
                    }

                    start--;
                }

                if (consumed.Count == 0 || destinations.Count != arity.Value || start < entry + 3)
                {
                    continue;
                }

                int check;
                if (consumed is [var carrier])
                {
                    if (body[start - 1].Definition.Opcode != Label)
                    {
                        continue;
                    }

                    check = (flow ??= new BeamProjectionFlow(body)).FindCheckedCarrierFallback(start - 1, carrier);
                }
                else
                {
                    if (body[start - 1] is not
                        {
                            Definition.Opcode: GetTupleElement,
                            Operands: [var tuple, _, var projected]
                        } || tuple != x0 || projected != input)
                    {
                        continue;
                    }

                    check = start - 2;
                    if (body[check] is { Definition.Opcode: Move, Operands: [var saved, { Kind: BeamOperandParameters.Y }] }
                        && saved == x0)
                    {
                        check--;
                    }

                    if (body[check] is not { Definition.Opcode: IsTaggedTuple, Operands: [_, var tested, _, _] } || tested != x0)
                    {
                        continue;
                    }
                }

                if (check < entry || !(unprotected ??= HasUnprotectedCallKillSchedule(body, entry))
                    || (ownership ??= ReadDisjointFrameOwnership(
                        body,
                        entry,
                        end,
                        trackSiblingTrimEpochs: true
                    )) is not { } frame)
                {
                    continue;
                }

                int owner = frame.Owners[call - entry];
                if (owner < 0 || frame.Owners[check - entry] != owner)
                {
                    continue;
                }

                // Each candidate has just been read by an unconditional move
                // in the same frame, and the next call owns every X argument.
                // The deferred X0 copy reads another slot that remains live.
                flow ??= new BeamProjectionFlow(body);
                if (!consumed.All(slot => slot.Value < frame.Uses[owner].Length
                        && flow.IsDead(
                            call,
                            slot,
                            allowBadRecordExit: true,
                            allowEntryFramePredicates: true
                        )))
                {
                    continue;
                }

                int insert = body[x0Move].Offset;
                output.Write(code.Span[copied..insert]);
                WriteFrameInitialization(output, consumed.Select(slot => (int)slot.Value).Order().ToArray());
                copied = insert;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
