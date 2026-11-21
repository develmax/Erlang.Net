using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? RemoveProtectedEntryArgumentReloads(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int info = 0; info + 3 < body.Count; info++)
        {
            if (body[info].Definition.Opcode != FunctionInfo
                || body[info + 1].Definition.Opcode != Label
                || body[info + 2].Definition.Opcode is not (Allocate or AllocateHeap))
            {
                continue;
            }

            var copies = new Dictionary<int, BeamOperand>();
            int index = info + 3;
            while (index < body.Count)
            {
                BeamInstruction instruction = body[index];
                if (instruction is
                    {
                        Definition.Opcode: Move,
                        Operands: [
                        { Kind: BeamOperandParameters.X, TypeIndex: null } source,
                        { Kind: BeamOperandParameters.Y, TypeIndex: null } saved
                        ]
                    })
                {
                    copies[(int)saved.Value] = source;
                }
                else if (instruction.Definition.Opcode == InitYRegisters)
                {
                    foreach (BeamOperand slot in instruction.Operands.Where(slot => slot.Kind == BeamOperandParameters.Y))
                    {
                        copies.Remove((int)slot.Value);
                    }
                }
                else if (instruction.Definition.Opcode != Line)
                {
                    break;
                }

                index++;
            }

            if (copies.Count == 0 || index >= body.Count || body[index] is not
                {
                    Definition.Opcode: TryEnter or CatchEnter,
                    Operands: [{ Kind: BeamOperandParameters.Y } tag, _]
                })
            {
                continue;
            }

            // Installing the handler replaces its tag slot. The other Y copies
            // and the original argument registers survive the instruction.
            copies.Remove((int)tag.Value);
            for (int reload = index + 1; reload + 1 < body.Count; reload++)
            {
                BeamInstruction instruction = body[reload];
                if (instruction.Definition.Opcode == Line)
                {
                    continue;
                }

                if (instruction is not
                    {
                        Definition.Opcode: Move,
                        Operands: [
                        { Kind: BeamOperandParameters.Y, TypeIndex: null } saved,
                        { Kind: BeamOperandParameters.X, TypeIndex: null } argument
                        ]
                    }
                    || !copies.TryGetValue((int)saved.Value, out var original)
                    || original != argument)
                {
                    break;
                }

                output.Write(code.Span[copied..instruction.Offset]);
                copied = body[reload + 1].Offset;
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
