using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? RemoveOverwrittenTupleProjections(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        var pending = new Dictionary<int, int>();
        var unread = new List<int>();

        for (int index = 0; index < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            BeamOperand destination;
            switch (instruction)
            {
                case { Definition.Opcode: GetTupleElement, Operands: [var source, _, var target] }:
                    Read(source);
                    destination = target;
                    break;
                case { Definition.Opcode: Move, Operands: [var source, var target] }:
                    Read(source);
                    destination = target;
                    break;
                case { Definition.Opcode: PutList, Operands: [var head, var tail, var target] }:
                    Read(head);
                    Read(tail);
                    destination = target;
                    break;
                case { Definition.Opcode: PutTuple, Operands: [var target, { Kind: BeamOperandParameters.Count }, ..] }:
                    for (int operand = 2; operand < instruction.Operands.Count; operand++)
                    {
                        Read(instruction.Operands[operand]);
                    }

                    destination = target;
                    break;
                case { Definition.Opcode: TestHeap, Operands: [_, var live] }:
                    // The GC can scan every X register below Live.
                    foreach (int register in pending.Keys.Where(register => register < live.Value).ToArray())
                    {
                        pending.Remove(register);
                    }

                    continue;
                case { Definition.Opcode: Line or Deallocate }:
                    continue;
                case { Definition.Opcode: Return }:
                    // Return reads X0 even though that operand is implicit.
                    pending.Remove(0);
                    unread.AddRange(pending.Values);
                    pending.Clear();
                    continue;
                default:
                    // A branch, call, or unmodeled instruction ends the linear proof.
                    pending.Clear();
                    continue;
            }

            if (destination.Kind != BeamOperandParameters.X)
            {
                continue;
            }

            int written = (int)destination.Value;
            if (pending.Remove(written, out int unusedIndex))
            {
                unread.Add(unusedIndex);
            }

            if (instruction.Definition.Opcode == GetTupleElement)
            {
                // Tuple projections in generated code are already known to be safe.
                pending.Add(written, index);
            }
        }

        if (unread.Count == 0)
        {
            return null;
        }

        unread.Sort();
        using var output = new MemoryStream();
        int copied = 0;
        foreach (int index in unread)
        {
            output.Write(code.Span[copied..body[index].Offset]);
            copied = body[index + 1].Offset;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void Read(BeamOperand source)
        {
            if (source.Kind == BeamOperandParameters.X)
            {
                pending.Remove((int)source.Value);
            }
        }
    }
}
