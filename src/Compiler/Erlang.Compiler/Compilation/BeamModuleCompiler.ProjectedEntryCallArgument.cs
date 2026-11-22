using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ReuseProjectedEntryCallArgument(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetTupleElement)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return RewriteProjectedEntryCallArgument(code, decoded.Body);
    }

    private static byte[]? RewriteProjectedEntryCallArgument(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        int copied = 0;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        for (int call = 5; call < body.Count; call++)
        {
            if (body[call] is not { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value.IsOne: true }, _] })
            {
                continue;
            }

            int load = call - 1;
            while (load > 0 && body[load].Definition.Opcode == Line)
            {
                load--;
            }

            if (body[load] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } slot, var destination]
                } || destination != x0 || FindProjectedCopy(load, slot) is not { } projected)
            {
                continue;
            }

            // The projected value remains in X throughout the save corridor.
            // Later calls may clobber X, so keep the independent frame copy.
            output.Write(code.Span[copied..body[load].Offset]);
            output.WriteByte(Move);
            BeamCompactWriter.Write(output, XRegisterTag, projected.Value);
            BeamCompactWriter.Write(output, XRegisterTag, 0);
            copied = body[load + 1].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        BeamOperand? FindProjectedCopy(int load, BeamOperand slot)
        {
            BeamOperand? source = null;
            int before = load - 1;
            for (; before >= 0; before--)
            {
                BeamInstruction instruction = body[before];
                if (instruction is { Definition.Opcode: InitYRegisters })
                {
                    if (source is null && instruction.Operands.Skip(1).Contains(slot))
                    {
                        return null;
                    }

                    continue;
                }

                if (instruction is not
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } from,
                        { Kind: BeamOperandParameters.Y, TypeIndex: null } to]
                    })
                {
                    break;
                }

                if (to == slot && source is null)
                {
                    source = from;
                }
            }

            if (before < 1 || source is null || source.Value.IsZero
                || body[before].Definition.Opcode is not (Allocate or AllocateHeap)
                || source.Value >= body[before].Operands[^1].Value)
            {
                return null;
            }

            for (int index = before - 1; index >= 0 && body[index].Definition.Opcode == GetTupleElement; index--)
            {
                if (body[index].Operands[^1] == source)
                {
                    return source;
                }
            }

            return null;
        }
    }
}
