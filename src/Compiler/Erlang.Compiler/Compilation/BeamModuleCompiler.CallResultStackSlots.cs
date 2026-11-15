using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? ScheduleCallResultStores(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
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
            if (start + 1 >= end || body[start].Definition.Opcode != Label
                || body[start + 1].Definition.Opcode is not (Allocate or AllocateHeap))
            {
                continue;
            }

            List<int>? stores = null;
            for (int index = start + 2; index + 2 < end; index++)
            {
                if (body[index].Definition.Opcode is (LocalCall or ExternalCall or DynamicApply or CallFun or CallFunTyped)
                    && body[index + 1] is { Definition.Opcode: Move, Operands: [
                        { Kind: BeamOperandParameters.X, Value.IsZero: true }, { Kind: BeamOperandParameters.Y } saved] }
                    && body[index + 2] is { Definition.Opcode: Move, Operands: [
                        { Kind: BeamOperandParameters.Y } read, { Kind: BeamOperandParameters.X, Value.IsZero: false }] }
                    && saved.Value != read.Value)
                {
                    (stores ??= []).Add(index + 1);
                }
            }

            if (stores is null)
            {
                continue;
            }

            var reordered = new BeamInstruction[end - start];
            for (int index = start; index < end; index++)
            {
                reordered[index - start] = body[index];
            }

            foreach (int store in stores)
            {
                int index = store - start;
                // The load neither reads the destination slot nor overwrites the call result.
                (reordered[index], reordered[index + 1]) = (reordered[index + 1], reordered[index]);
            }

            if (PlanLinearStackSlots(reordered, 0, reordered.Length) is not { } planned)
            {
                continue;
            }

            int original = PlanLinearStackSlots(body, start, end)?.Count ?? (int)body[start + 1].Operands[0].Value;
            if (planned.Count >= original)
            {
                continue;
            }

            // The next allocation pass can reuse the slot released by the earlier load.
            foreach (int store in stores)
            {
                int write = body[store].Offset;
                int load = body[store + 1].Offset;
                int next = body[store + 2].Offset;
                output.Write(code.Span[copied..write]);
                output.Write(code.Span[load..next]);
                output.Write(code.Span[write..load]);
                copied = next;
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
