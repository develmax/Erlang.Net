using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? ScheduleCallResultStores(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        var scratch = new BeamOperand(BeamOperandParameters.X, 1);
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
            int allocation = FindEntryFrameAllocation(body, start, end);
            if (allocation < 0)
            {
                continue;
            }

            List<int>? stores = null;
            for (int index = allocation + 1; index + 2 < end; index++)
            {
                if (body[index].Definition.Opcode is (LocalCall or ExternalCall or DynamicApply or CallFun or CallFunTyped)
                    && body[index + 1] is { Definition.Opcode: Move, Operands: [
                        { Kind: BeamOperandParameters.X, Value.IsZero: true }, { Kind: BeamOperandParameters.Y } saved] }
                    && body[index + 2] is { Definition.Opcode: Move, Operands: [
                        { Kind: BeamOperandParameters.Y, TypeIndex: null } read,
                        { Kind: BeamOperandParameters.X, TypeIndex: null } argument] }
                    && saved.Value != read.Value
                    && (!argument.Value.IsZero || (flow ??= new BeamProjectionFlow(body)).IsBranchTemporaryDead(index + 2, scratch)))
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
                // The planner tracks Y-slot lifetimes. Emission uses X1 if the load would overwrite X0.
                (reordered[index], reordered[index + 1]) = (reordered[index + 1], reordered[index]);
            }

            int relativeAllocation = allocation - start;
            if (PlanLinearStackSlots(reordered, relativeAllocation, reordered.Length) is not { } planned)
            {
                continue;
            }

            int original = PlanLinearStackSlots(body, allocation, end)?.Count ?? (int)body[allocation].Operands[0].Value;
            if (planned.Count >= original)
            {
                continue;
            }

            // Keep the original order when a move is not needed for the smaller frame.
            for (int candidate = stores.Count - 1; candidate >= 0; candidate--)
            {
                int index = stores[candidate] - start;
                (reordered[index], reordered[index + 1]) = (reordered[index + 1], reordered[index]);
                if (PlanLinearStackSlots(reordered, relativeAllocation, reordered.Length) is { } unchanged
                    && unchanged.Count <= planned.Count)
                {
                    stores.RemoveAt(candidate);
                }
                else
                {
                    (reordered[index], reordered[index + 1]) = (reordered[index + 1], reordered[index]);
                }
            }

            // The next allocation pass can reuse the slot released by the earlier load.
            foreach (int store in stores)
            {
                int write = body[store].Offset;
                int load = body[store + 1].Offset;
                int next = body[store + 2].Offset;
                output.Write(code.Span[copied..write]);
                var argument = body[store + 1].Operands[1];
                if (argument.Value.IsZero)
                {
                    output.WriteByte(Move);
                    WriteRegister(output, body[store + 1].Operands[0]);
                    WriteRegister(output, scratch);
                    output.Write(code.Span[write..load]);
                    output.WriteByte(Move);
                    WriteRegister(output, scratch);
                    WriteRegister(output, argument);
                }
                else
                {
                    output.Write(code.Span[load..next]);
                    output.Write(code.Span[write..load]);
                }
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
