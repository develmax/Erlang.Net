using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? ShareFallthroughTupleReturns(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        var fallthrough = new Dictionary<string, int>(StringComparer.Ordinal);
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            if (!body.Skip(start).Take(end - start).Any(instruction =>
                instruction.Definition.Opcode is TryEnter or CatchEnter))
            {
                fallthrough.Clear();
                for (int first = end - 5; first > start; first--)
                {
                    if (body[first] is not
                        {
                            Definition.Opcode: TestHeap,
                            Operands: [_, { Value.IsZero: true }]
                        }
                        || body[first + 1] is not
                        {
                            Definition.Opcode: PutTuple,
                            Operands: [{ Kind: BeamOperandParameters.X, Value.IsZero: true }, _, ..]
                        }
                        || body[first + 2].Definition.Opcode != Deallocate || body[first + 3].Definition.Opcode != Return)
                    {
                        continue;
                    }

                    int after = body[first + 4].Offset;
                    int sequence = body[first].Offset;
                    var predecessor = body[first - 1];
                    if (predecessor is
                        {
                            Definition.Opcode: ExactEqualTest or ExactNotEqualTest,
                            Operands: [_, { Kind: BeamOperandParameters.X, Value.IsZero: true }, { Kind: BeamOperandParameters.Atom }]
                        })
                    {
                        fallthrough.TryAdd(Convert.ToHexString(code.Span[sequence..after]), first);
                    }
                    else if (fallthrough.Count != 0 && predecessor.Definition.Opcode == Label && first > start + 1
                        && EndsBeamBlock(body[first - 2].Definition.Opcode)
                        && fallthrough.TryGetValue(Convert.ToHexString(code.Span[sequence..after]), out int retained))
                    {
                        // The return sequence carries no location change. Its old label can
                        // follow the same return at the later physical fallthrough.
                        using var output = new MemoryStream(code.Length);
                        int retainedOffset = body[retained].Offset;
                        output.Write(code.Span[..predecessor.Offset]);
                        output.Write(code.Span[after..retainedOffset]);
                        output.Write(code.Span[predecessor.Offset..sequence]);
                        output.Write(code.Span[retainedOffset..]);

                        return output.ToArray();
                    }
                }
            }

            start = end + 1;
        }

        return null;
    }

    private static byte[]? ShareProjectedConstructionTails(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        var edits = new List<(int Start, int End, int Target)>();
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            var instructions = body.Skip(start).Take(end - start);
            if (!instructions.Any(instruction => instruction.Definition.Opcode == SelectTupleArity)
                || instructions.Any(instruction => instruction.Definition.Opcode is TryEnter or CatchEnter
                    or LoopReceive or Wait or WaitTimeout or DebugLine or ExecutableLine))
            {
                continue;
            }

            var labelledReturns = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = end - 4; index >= start + 2; index--)
            {
                if (body[index].Definition.Opcode != TestHeap)
                {
                    continue;
                }

                int next = index + 1;
                bool projects = false;
                bool constructs = false;
                while (next < end && body[next].Definition.Opcode is GetTupleElement or PutTuple)
                {
                    projects |= body[next].Definition.Opcode == GetTupleElement;
                    constructs |= body[next].Definition.Opcode == PutTuple;
                    next++;
                }

                if (!projects || !constructs || next + 2 >= end || body[next].Definition.Opcode != Deallocate
                    || body[next + 1].Definition.Opcode != Return)
                {
                    continue;
                }

                int begin = body[index].Offset;
                int finish = body[next + 2].Offset;
                string key = Convert.ToHexString(code.Span[begin..finish]);
                if (body[index - 1] is { Definition.Opcode: Label, Operands: [var label] })
                {
                    labelledReturns.TryAdd(key, (int)label.Value);
                }
                else if (body[index - 1] is
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [_, var tagResult, { Kind: BeamOperandParameters.Atom }]
                } && body[index - 2] is
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsZero: true },
                    { Value.IsZero: true }, var destination]
                } && destination == tagResult && labelledReturns.TryGetValue(key, out int target))
                {
                    // Keep the tag check and its failure. The following constructors
                    // have the same registers, heap roots and return frame at either entry.
                    edits.Add((begin, finish, target));
                }
            }
        }

        if (edits.Count == 0)
        {
            return null;
        }

        using var output = new MemoryStream(code.Length);
        int copied = 0;
        foreach (var edit in edits.OrderBy(edit => edit.Start))
        {
            output.Write(code.Span[copied..edit.Start]);
            output.WriteByte(Jump);
            BeamCompactWriter.Write(output, LabelTag, edit.Target);
            copied = edit.End;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
