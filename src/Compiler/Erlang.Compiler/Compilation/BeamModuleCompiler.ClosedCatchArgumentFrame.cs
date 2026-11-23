using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? CompactClosedCatchArgumentFrame(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(CatchEnter)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int info = 0; info + 9 < body.Count; info++)
        {
            if (body[info].Definition.Opcode != FunctionInfo)
            {
                continue;
            }

            int end = info + 1;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            int allocation = FindEntryFrameAllocation(
                body,
                info + 1,
                end,
                allowUnframedProjections: true
            );
            if (allocation < 0 || allocation + 7 >= end || body[allocation] is not
                { Definition.Opcode: Allocate, Operands: [var size, var live] }
                || size.Value < 4 || size.Value > BeamOperandParameters.MaximumRegister
                || body[allocation + 1] is not { Definition.Opcode: InitYRegisters } initializers)
            {
                continue;
            }

            int frame = (int)size.Value;
            var slots = new int[frame];
            Array.Fill(slots, -1);
            int snapshot = allocation + 2;
            var saved = new SortedDictionary<int, int>();
            while (snapshot < end && body[snapshot] is { Definition.Opcode: Move, Operands: [var source, var dest] }
                && source.Kind == BeamOperandParameters.X && source.TypeIndex is null && source.Value > 0
                && source.Value < live.Value && BareStack(dest, frame) && slots[(int)dest.Value] == -1
                && saved.TryAdd((int)source.Value, (int)dest.Value))
            {
                slots[(int)dest.Value] = -2;
                snapshot++;
            }

            int enter = snapshot + 1;
            if (enter + 4 >= end || body[snapshot] is not { Definition.Opcode: Move, Operands: [var arg, var stale] }
                || arg != x0 || !BareStack(stale, frame) || slots[(int)stale.Value] != -1
                || body[enter] is not { Definition.Opcode: CatchEnter, Operands: [var handler, var target] }
                || !BareStack(handler, frame) || handler == stale || slots[(int)handler.Value] != -1)
            {
                continue;
            }

            int protectedCall = enter + 1;
            while (protectedCall < end && body[protectedCall].Definition.Opcode == Line)
            {
                protectedCall++;
            }

            int release = protectedCall + 2;
            if (release + 1 >= end
                || body[protectedCall] is not { Definition.Opcode: ExternalCall or LocalCall, Operands: [var arity, _] }
                || arity.Value != 1 || body[protectedCall + 1] is not { Definition.Opcode: Label, Operands: [var landing] }
                || landing.Value != target.Value || body[release] is not { Definition.Opcode: CatchEnd, Operands: [var tag] }
                || tag != handler)
            {
                continue;
            }

            var earlierLabels = body.Skip(info + 1).Take(release - info)
                .Where(instruction => instruction.Definition.Opcode == Label)
                .Select(instruction => instruction.Operands[0].Value).ToHashSet();
            var fresh = new Dictionary<int, int>();
            bool compatible = true;
            for (int index = release + 1; index < end && compatible; index++)
            {
                BeamInstruction instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                int width = LinearFrameSizeOperand(opcode);
                if (opcode is Allocate or AllocateHeap or InitYRegisters or TrimFrame or CatchEnter or CatchEnd
                    or TryEnter or TryEnd or TryCase or DebugLine or ExecutableLine
                    || opcode is not (LocalCall or TailLocalCall or TailLocalCallWithFrame)
                        && instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Label
                            && earlierLabels.Contains(operand.Value))
                    || width >= 0 && instruction.Operands[width].Value != frame)
                {
                    compatible = false;
                    break;
                }

                foreach (var operand in instruction.Operands)
                {
                    if (operand.Kind != BeamOperandParameters.Y)
                    {
                        continue;
                    }

                    if (!BareStack(operand, frame))
                    {
                        compatible = false;
                        break;
                    }

                    int slot = (int)operand.Value;
                    if (slots[slot] != -1 || operand == handler || fresh.ContainsKey(slot))
                    {
                        continue;
                    }

                    // Each fresh slot obtains its first value directly from a call.
                    // A subsequent path can read that value only after its definition.
                    if (instruction.Operands is [var result, var receiver] && opcode == Move && result == x0
                        && receiver == operand && body[index - 1].Definition.Opcode is LocalCall or ExternalCall)
                    {
                        fresh.Add(slot, index);
                    }
                    else
                    {
                        compatible = false;
                        break;
                    }
                }
            }

            int newFrame = 1 + saved.Count + fresh.Count;
            if (!compatible || newFrame >= frame || !fresh.ContainsKey((int)stale.Value))
            {
                continue;
            }

            var initialized = initializers.Operands.Skip(1).ToArray();
            if (initialized.Any(slot => !BareStack(slot, frame) || slot == stale || slot == handler
                    || slots[(int)slot.Value] != -1)
                || initialized.Select(slot => (int)slot.Value).Distinct().Count() != frame - saved.Count - 2)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (fresh.Keys.Any(slot => !flow.IsDead(
                release + 1,
                new BeamOperand(BeamOperandParameters.Y, slot),
                afterCall: true
            )))
            {
                continue;
            }

            // This frame needs its handler at the high end. OTP reserves
            // the entry arguments before values defined by later calls.
            int nextSlot = newFrame - 1;
            slots[(int)handler.Value] = nextSlot--;
            foreach (int slot in saved.Values)
            {
                slots[slot] = nextSlot--;
            }

            foreach (int slot in fresh.OrderBy(pair => pair.Value).Select(pair => pair.Key))
            {
                slots[slot] = nextSlot--;
            }

            output.Write(code.Span[copied..body[allocation].Offset]);
            output.WriteByte(Allocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, newFrame);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live.Value);
            WriteFrameInitialization(output, Enumerable.Range(0, fresh.Count).ToArray());
            WriteSpan(allocation + 2, snapshot);
            WriteSpan(enter, end);
            copied = body[end].Offset;
            info = end - 1;

            void WriteSpan(int start, int finish)
            {
                int from = body[start].Offset;
                int to = body[finish].Offset;
                var edits = decoded.StackOperands.Where(slot => slot.Start >= from && slot.Start < to)
                    .Select(slot => (slot.Start, slot.End, Tag: YRegisterTag, Value: slots[slot.Slot])).ToList();
                for (int index = start; index < finish; index++)
                {
                    BeamInstruction instruction = body[index];
                    int width = LinearFrameSizeOperand(instruction.Definition.Opcode);
                    if (width < 0)
                    {
                        continue;
                    }

                    int offset = instruction.Offset + 1;
                    var cursor = new BeamCursor(code[offset..]);
                    for (int position = 0; position < width; position++)
                    {
                        _ = BeamCompactReader.Read(cursor);
                    }

                    int operandStart = offset + cursor.Position;
                    _ = BeamCompactReader.Read(cursor);
                    edits.Add((operandStart, offset + cursor.Position, BeamWire.UnsignedTag, newFrame));
                }

                foreach (var edit in edits.OrderBy(edit => edit.Start))
                {
                    output.Write(code.Span[from..edit.Start]);
                    BeamCompactWriter.Write(output, edit.Tag, edit.Value);
                    from = edit.End;
                }

                output.Write(code.Span[from..to]);
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool BareStack(BeamOperand operand, int frame) =>
        operand.Kind == BeamOperandParameters.Y && operand.TypeIndex is null
        && operand.Value >= 0 && operand.Value < frame;
}
