using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RemoveUnchangedCallSpills(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TrimFrame)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        for (int allocation = 0; allocation + 5 < body.Count; allocation++)
        {
            if (body[allocation] is not
                { Definition.Opcode: Allocate, Operands: [{ Value.IsOne: true }, var oldLive] }
                || body[allocation + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X } saved, { Kind: BeamOperandParameters.Y, Value.IsZero: true }]
                } || saved.Value >= oldLive.Value)
            {
                continue;
            }

            int firstMove = allocation + 2;
            int restore = firstMove;
            while (restore < body.Count && body[restore] is
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X } source, { Kind: BeamOperandParameters.X } destination]
                } && source.Value < oldLive.Value && destination.Value < oldLive.Value && destination.Value != saved.Value)
            {
                restore++;
            }

            if (restore == firstMove || restore + 2 >= body.Count
                || body[restore] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Y, Value.IsZero: true }, { Kind: BeamOperandParameters.X } restored]
                } || restored.Value != saved.Value || body[restore + 1] is not
                { Definition.Opcode: TrimFrame, Operands: [{ Value.IsOne: true }, { Value.IsZero: true }] })
            {
                continue;
            }

            int call = restore + 2;
            while (call < body.Count && body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (call == body.Count || body[call] is not
                { Definition.Opcode: LocalCall or ExternalCall, Operands: [var arity, _] }
                || arity.Value > oldLive.Value || arity.Value > BeamOperandParameters.MaximumRegister)
            {
                continue;
            }

            // No instruction observes the frame until the call, and the saved X value is still in place.
            output.Write(code.Span[copied..body[allocation].Offset]);
            output.Write(code.Span[body[firstMove].Offset..body[restore].Offset]);
            output.WriteByte(Allocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, (int)arity.Value);
            copied = body[restore + 2].Offset;
            allocation = restore + 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
