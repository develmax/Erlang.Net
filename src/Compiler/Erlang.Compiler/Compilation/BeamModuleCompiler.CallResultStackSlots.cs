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
                    && body[index + 1] is
                    {
                        Definition.Opcode: Move, Operands: [
                        { Kind: BeamOperandParameters.X, Value.IsZero: true }, { Kind: BeamOperandParameters.Y } saved]
                    }
                    && body[index + 2] is
                    {
                        Definition.Opcode: Move, Operands: [
                        { Kind: BeamOperandParameters.Y, TypeIndex: null } read,
                        { Kind: BeamOperandParameters.X, TypeIndex: null } argument]
                    }
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

    private byte[]? ReuseCallResultSlotForSafeBifArgument(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (!SsaTypeInferenceEnabled || ModuleOptimizationDisabled || !code.Span.Contains(BifZero)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var safeImports = ReadLocationNeutralExternalImports();
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var x2 = new BeamOperand(BeamOperandParameters.X, 2);
        var y0 = new BeamOperand(BeamOperandParameters.Y, 0);
        var y1 = new BeamOperand(BeamOperandParameters.Y, 1);
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int entry = 0;
        int copied = 0;
        for (int index = 0; index + 11 < body.Count; index++)
        {
            if (body[index].Definition.Opcode == FunctionInfo)
            {
                entry = index + 1;
                continue;
            }

            if (entry == 0 || body[index] is not
                { Definition.Opcode: Allocate, Operands: [{ Value: var width }, { Value.IsOne: true }] }
                || width != 2 || body[index + 1] is not
                { Definition.Opcode: InitYRegisters, Operands: [_, var initialized] }
                || initialized != y1 || !IsMove(index + 2, x0, y0))
            {
                continue;
            }

            int first = SkipLines(index + 3);
            if (first + 7 >= body.Count || body[first] is not
                { Definition.Opcode: ExternalCall, Operands: [{ Value.IsZero: true }, _] }
                || !IsMove(first + 1, x0, y1) || body[first + 2] is not
                { Definition.Opcode: BifZero, Operands: [var bif, var destination] }
                || destination != x0 || !safeImports.Contains((int)bif.Value) || !IsMove(first + 3, y0, x1))
            {
                continue;
            }

            int next = SkipLines(first + 4);
            if (next + 4 >= body.Count || body[next] is not
                { Definition.Opcode: ExternalCall, Operands: [{ Value: var arguments }, _] }
                || arguments != 2 || !IsMove(next + 1, y1, x0) || body[next + 2] is not
                { Definition.Opcode: Deallocate, Operands: [var release] } || release.Value != width
                || body[next + 3].Definition.Opcode != Return
                || !(flow ??= new BeamProjectionFlow(body)).ReachesLinearFrameUnframed(entry, index))
            {
                continue;
            }

            // The first result need not occupy a second Y slot before the
            // nonthrowing argument is produced. X2 carries the consumed input.
            output.Write(code.Span[copied..body[index].Offset]);
            output.WriteByte(Allocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            output.Write(code.Span[body[index + 2].Offset..body[first + 1].Offset]);
            output.Write(code.Span[body[first + 2].Offset..(body[first + 3].Offset - CompactRegisterCountWidth(0))]);
            WriteRegister(output, x1);
            WriteMove(y0, x2);
            WriteMove(x0, y0);
            WriteMove(x1, x0);
            WriteMove(x2, x1);
            output.Write(code.Span[body[first + 4].Offset..body[next + 1].Offset]);
            WriteMove(y0, x0);
            output.WriteByte(Deallocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            copied = body[next + 3].Offset;
            index = next + 2;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool IsMove(int index, BeamOperand input, BeamOperand destination) => body[index] is
        { Definition.Opcode: Move, Operands: [var source, var target] }
            && source == input && target == destination;

        int SkipLines(int index)
        {
            while (index < body.Count && body[index].Definition.Opcode == Line)
            {
                index++;
            }

            return index;
        }

        void WriteMove(BeamOperand source, BeamOperand destination)
        {
            output.WriteByte(Move);
            WriteRegister(output, source);
            WriteRegister(output, destination);
        }
    }
}
