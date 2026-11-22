using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? TrimSingleSlotAfterFinalCallSetup(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TrimFrame)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamOperand x0 = new(BeamOperandParameters.X, 0);
        BeamOperand y0 = new(BeamOperandParameters.Y, 0);
        var importsByIndex = imports.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var atomNames = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            int returnAt = end - 1;
            while (returnAt > start && body[returnAt].Definition.Opcode is Label or Line)
            {
                returnAt--;
            }

            if (start + 5 >= returnAt || body[returnAt].Definition.Opcode != Return
                || body[returnAt - 1] is not { Definition.Opcode: Deallocate, Operands: [{ Value.IsOne: true }] }
                || body[returnAt - 2] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Atom, TypeIndex: null }, var destination]
                }
                || destination != x0 || !CallsEffect(body[returnAt - 3]))
            {
                continue;
            }

            int setupEnd = returnAt - 3;
            while (setupEnd > start && body[setupEnd - 1].Definition.Opcode == Line)
            {
                setupEnd--;
            }

            if (setupEnd <= start || !IsMove(body[setupEnd - 1], y0, x0))
            {
                continue;
            }

            // Apply the next zero-suffix trim only in an existing straight-line
            // epoch that already retained one slot. The last argument move owns
            // that value in X0, and the call continuation returns a literal.
            bool oneSlot = false;
            for (int index = start + 1; index < setupEnd; index++)
            {
                BeamInstruction instruction = body[index];
                if (instruction.Definition.Opcode is Label or TryEnter or CatchEnter or ExecutableLine)
                {
                    oneSlot = false;
                    break;
                }

                if (instruction.Definition.Opcode == TrimFrame)
                {
                    oneSlot = instruction.Operands is [{ Value: var released }, { Value.IsOne: true }]
                        && released > 0;
                }
                else if (instruction.Definition.Opcode is Allocate or AllocateHeap or Deallocate)
                {
                    oneSlot = false;
                }
            }

            if (!oneSlot)
            {
                continue;
            }

            output.Write(code.Span[copied..body[setupEnd].Offset]);
            output.WriteByte(TrimFrame);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
            output.Write(code.Span[body[setupEnd].Offset..body[returnAt - 1].Offset]);
            output.WriteByte(Deallocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
            copied = body[returnAt].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool CallsEffect(BeamInstruction call)
        {
            if (call.Definition.Opcode is not (LocalCall or ExternalCall) || call.Operands[0].Value <= 0)
            {
                return false;
            }

            if (call.Definition.Opcode == LocalCall)
            {
                return true;
            }

            int index = (int)call.Operands[1].Value;
            if (index >= importsByIndex.Length)
            {
                return false;
            }

            var (module, function, arity) = importsByIndex[index];

            return !BeamTrimBifs.IsPure(atomNames[module - 1], atomNames[function - 1], arity);
        }
    }
}
