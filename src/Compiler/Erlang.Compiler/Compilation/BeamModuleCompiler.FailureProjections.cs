using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? RemoveRepeatedFailureProjections(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        var removals = new SortedDictionary<int, int>();
        BeamProjectionFlow? flow = null;
        for (int index = 1; index < body.Count; index++)
        {
            BeamInstruction test = body[index];
            if (body[index - 1].Definition.Opcode != GetTupleElement
                || !IsEntryFramePredicate(test.Definition.Opcode)
                || test.Operands[0].Kind != BeamOperandParameters.Label
                || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(
                    index,
                    (int)test.Operands[0].Value,
                    out int target
                ))
            {
                continue;
            }

            int first = index - 1;
            while (first > 0 && body[first - 1].Definition.Opcode == GetTupleElement)
            {
                first--;
            }

            int repeat = target + 1;
            int live = BeamOperandParameters.MaximumRegister;
            while (repeat < body.Count && body[repeat].Definition.Opcode is Line or TestHeap)
            {
                if (body[repeat].Definition.Opcode == TestHeap)
                {
                    live = Math.Min(live, (int)body[repeat].Operands[^1].Value);
                }

                repeat++;
            }

            int count = index - first;
            if (repeat + count >= body.Count || !SameStableProjections(
                body,
                first,
                repeat,
                count,
                live
            ))
            {
                continue;
            }

            removals.Add(body[repeat].Offset, body[repeat + count].Offset);
        }

        if (removals.Count == 0)
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        foreach (var (from, to) in removals)
        {
            if (from >= copied)
            {
                output.Write(code.Span[copied..from]);
            }

            copied = Math.Max(copied, to);
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool SameStableProjections(
        IReadOnlyList<BeamInstruction> body,
        int first,
        int repeat,
        int count,
        int live
    )
    {
        var sources = new HashSet<BeamOperand>();
        var destinations = new HashSet<BeamOperand>();
        for (int offset = 0; offset < count; offset++)
        {
            BeamInstruction original = body[first + offset];
            BeamInstruction duplicate = body[repeat + offset];
            if (duplicate.Definition.Opcode != GetTupleElement
                || !original.Operands.SequenceEqual(duplicate.Operands))
            {
                return false;
            }

            BeamOperand source = original.Operands[0];
            BeamOperand destination = original.Operands[^1];
            if (destination.Kind == BeamOperandParameters.X && destination.Value >= live)
            {
                return false;
            }

            sources.Add(source with { TypeIndex = null });
            destinations.Add(destination with { TypeIndex = null });
        }

        return !sources.Overlaps(destinations);
    }
}
