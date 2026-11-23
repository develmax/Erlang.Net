using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? KeepProtectedListResultInX0(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TryEnter)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        BeamProjectionFlow? flow = null;
        var changes = new List<CodeEdit>();
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            if (ReadProtectedListResultPlan(
                body,
                decoded.StackOperands,
                start,
                end,
                ref flow
            ) is { } plan)
            {
                changes.AddRange(plan);
            }

            start = end + 1;
        }

        return ApplyDuplicatedFrameReadEdits(code, changes);
    }

    private static List<CodeEdit>? ReadProtectedListResultPlan(
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stack,
        int start,
        int end,
        ref BeamProjectionFlow? flow
    )
    {
        int allocation = start + 1;
        int init = allocation + 1;
        if (init + 9 >= end || body[start].Definition.Opcode != Label
            || body[allocation] is not { Definition.Opcode: Allocate, Operands: [var width, _] }
            || width.Value <= 1 || width.Value > BeamOperandParameters.MaximumRegister
            || body[init] is not
            {
                Definition.Opcode: InitYRegisters,
                Operands: [_, { Kind: BeamOperandParameters.Y, TypeIndex: null } retired]
            } || retired.Value >= width.Value)
        {
            return null;
        }

        int enter = init + 1;
        while (enter < end && body[enter].Definition.Opcode is Move or Line)
        {
            enter++;
        }

        if (enter + 7 >= end || body[enter] is not
            { Definition.Opcode: TryEnter, Operands: [var token, { Kind: BeamOperandParameters.Label } rejected] }
            || token.Kind != BeamOperandParameters.Y || token == retired)
        {
            return null;
        }

        int producer = enter + 1;
        while (producer < end && body[producer].Definition.Opcode == Line)
        {
            producer++;
        }

        int save = producer + 1;
        int heap = save + 1;
        int first = heap + 1;
        if (first + 4 >= end || body[producer].Definition.Opcode is not (LocalCall or ExternalCall)
            || body[save] is not { Definition.Opcode: Move, Operands: [var returned, var saved] }
            || !IsBareX0(returned) || saved != retired
            || body[heap] is not
            {
                Definition.Opcode: TestHeap,
                Operands: [{ Kind: BeamOperandParameters.Unsigned } words, { Value.IsZero: true }]
            }
            || body[first] is not { Definition.Opcode: PutList, Operands: [var firstHead, var nil, var accumulator] }
            || firstHead == retired || firstHead.Kind == BeamOperandParameters.X
            || nil.Kind != BeamOperandParameters.Nil || !IsBareX0(accumulator))
        {
            return null;
        }

        int last = first + 1;
        while (last < end && body[last] is { Definition.Opcode: PutList, Operands: [var head, var from, var to] }
            && head != retired && head.Kind != BeamOperandParameters.X && from == accumulator && to == accumulator)
        {
            last++;
        }

        int consumer = last + 1;
        int exit = consumer + 1;
        const int wordsPerCons = 2;
        if (exit >= end || words.Value != wordsPerCons * (last - first + 1)
            || body[last] is not { Definition.Opcode: PutList, Operands: [var finalHead, var rest, var result] }
            || finalHead != retired || rest != accumulator || result != accumulator
            || body[consumer] is not { Definition.Opcode: ExternalCall, Operands: [{ Value.IsOne: true }, _] }
            || body[exit] is not { Definition.Opcode: TryEnd, Operands: [var released] } || released != token
            || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(enter, (int)rejected.Value, out int landing)
            || landing <= exit || landing + 1 >= end
            || body[landing + 1] is not { Definition.Opcode: TryCase, Operands: [var caught] } || caught != token)
        {
            return null;
        }

        var labels = body.Skip(start).Take(end - start)
            .Where(instruction => instruction.Definition.Opcode == Label)
            .Select(instruction => instruction.Operands[0].Value).ToHashSet();
        int originalWidth = (int)width.Value;
        var edits = new List<CodeEdit>();
        for (int index = start; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            int oldWidth = LinearFrameSizeOperand(opcode);
            if (opcode is AllocateHeap or TrimFrame or CatchEnter or CatchEnd or TryCaseEnd or DebugLine or ExecutableLine
                || opcode == Allocate && index != allocation || opcode == InitYRegisters && index != init
                || opcode == TryEnter && index != enter || opcode == TryEnd && index != exit
                || opcode == TryCase && index != landing + 1
                || oldWidth >= 0 && instruction.Operands[oldWidth].Value != originalWidth
                || opcode is not (LocalCall or TailLocalCall or TailLocalCallWithFrame)
                    && instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Label
                        && !labels.Contains(operand.Value)))
            {
                return null;
            }

            if (oldWidth >= 0 && index != allocation)
            {
                int next = body[index + 1].Offset;
                edits.Add(new(
                    next - CompactRegisterCountWidth(originalWidth),
                    next,
                    BeamWire.UnsignedTag,
                    originalWidth - 1
                ));
            }
        }

        int slot = (int)retired.Value;
        int references = 0;
        foreach (var (from, to, current) in stack)
        {
            if (from < body[start].Offset)
            {
                continue;
            }

            if (from >= body[end].Offset)
            {
                break;
            }

            if (current == slot)
            {
                references++;
                if (from == body[last].Offset + 1)
                {
                    edits.Add(new(
                        from,
                        to,
                        XRegisterTag,
                        0
                    ));
                }
                else if (from < body[init].Offset || from >= body[init + 1].Offset)
                {
                    if (from < body[save].Offset || from >= body[save + 1].Offset)
                    {
                        return null;
                    }
                }
            }
            else if (current > slot)
            {
                edits.Add(new(
                    from,
                    to,
                    YRegisterTag,
                    current - 1
                ));
            }
        }

        // All original references are the sole initializer, result store and
        // final head. The failed protected call can never observe the result.
        if (references != 3)
        {
            return null;
        }

        Remove(init);
        Remove(save);
        int widthStart = body[allocation].Offset + 1;
        edits.Add(new(
            widthStart,
            widthStart + CompactRegisterCountWidth(originalWidth),
            BeamWire.UnsignedTag,
            originalWidth - 1
        ));
        int live = body[first].Offset - 1;
        edits.Add(new(
            live,
            live + 1,
            BeamWire.UnsignedTag,
            1
        ));

        // The ordinary producer defines X0. The one-root heap check preserves it
        // while X1 becomes the independent accumulator for the other cons cells.
        for (int index = first; index <= last; index++)
        {
            int next = body[index + 1].Offset;
            if (index > first)
            {
                edits.Add(new(
                    next - 2,
                    next - 1,
                    XRegisterTag,
                    1
                ));
            }

            if (index != last)
            {
                edits.Add(new(
                    next - 1,
                    next,
                    XRegisterTag,
                    1
                ));
            }
        }

        return edits.OrderBy(edit => edit.Start).ToList();

        void Remove(int index) => edits.Add(new(
            body[index].Offset,
            body[index + 1].Offset,
            0,
            0,
            IsRemoval: true
        ));
    }
}
