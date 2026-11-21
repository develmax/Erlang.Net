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
            || !code.Span.Contains(InitYRegisters) && (finalPass || !code.Span.Contains(TrimFrame))
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
            if (finalPass && (source is not { Kind: BeamOperandParameters.Y, TypeIndex: null }
                || destination.TypeIndex is not null || body[end].Definition.Opcode != InitYRegisters
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
                if (source is not { Kind: BeamOperandParameters.Y, TypeIndex: null }
                    || destination.TypeIndex is not null
                    || source.Value < removed.Value || source.Value >= removed.Value + kept.Value)
                {
                    continue;
                }

                survivingSlot = (int)(source.Value - removed.Value);
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
                } || arity.Value <= 0)
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
}
