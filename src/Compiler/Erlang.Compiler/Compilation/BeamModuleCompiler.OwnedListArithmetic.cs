using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record OwnedListArithmetic(
        int Match,
        int Arithmetic,
        int Heap,
        int Last,
        int Branch,
        int Allocation,
        BeamOperand Head,
        BeamOperand Tail,
        BeamOperand Temporary,
        BeamOperand KeptHead,
        BeamOperand KeptList
    );

    private byte[]? KeepOwnedListArithmeticInRegisters(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GcBifTwo))
        {
            return null;
        }

        var names = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var imported = imports.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();

        return KeepOwnedListArithmeticInRegistersCore(
            code,
            labelCount,
            instruction => IsLeafArithmeticGcBif(instruction, names, imported)
        );
    }

    private byte[]? KeepOwnedListArithmeticInRegistersCore(
        ReadOnlyMemory<byte> code,
        int labelCount,
        Func<BeamInstruction, bool> isArithmetic
    )
    {
        if (DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        var selected = new Dictionary<int, int>();
        BeamProjectionFlow? flow = null;
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
            if (ReadOwnedListArithmetic(
                body,
                start,
                end,
                isArithmetic,
                ref flow
            ) is not { } plan)
            {
                continue;
            }

            // The old zero-live arithmetic killed every incoming X definition.
            // The operands here need only the head and the tail across that GC.
            var head = new BeamOperand(BeamOperandParameters.X, 0);
            var tail = new BeamOperand(BeamOperandParameters.X, 1);
            var computed = new BeamOperand(BeamOperandParameters.X, 2);
            output.Write(code.Span[copied..body[plan.Match].Offset]);
            output.WriteByte(GetList);
            WriteRegister(output, body[plan.Match].Operands[0]);
            WriteRegister(output, head);
            WriteRegister(output, tail);
            output.Write(code.Span[body[plan.Match + 2].Offset..body[plan.Arithmetic].Offset]);

            var operands = body[plan.Arithmetic].Operands;
            output.WriteByte(GcBifTwo);
            BeamCompactWriter.Write(output, LabelTag, 0);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, operands[2].Value);
            WriteRegister(output, head);
            BeamCompactWriter.Write(output, BeamWire.IntegerTag, operands[4].Value);
            WriteRegister(output, computed);

            output.WriteByte(TestHeap);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, body[plan.Heap].Operands[0].Value);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 3);
            for (int index = plan.Heap + 1; index < plan.Last; index++)
            {
                var value = body[index].Operands[0];
                output.WriteByte(PutList);
                WriteTupleJoinOperand(output, value == plan.Temporary ? computed : value == plan.Head ? head : value);
                WriteRegister(output, tail);
                WriteRegister(output, tail);
            }

            output.WriteByte(Move);
            WriteRegister(output, tail);
            WriteRegister(output, plan.KeptList);
            output.WriteByte(Move);
            WriteRegister(output, head);
            WriteRegister(output, plan.KeptHead);
            copied = body[plan.Last + 1].Offset;
            selected.Add((int)body[plan.Branch].Operands[0].Value, plan.Allocation - plan.Branch);
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return CompactSelectedBranchFrames(
            output.ToArray(),
            labelCount,
            selected,
            reuseValueLifetimes: true
        );
    }

    private static OwnedListArithmetic? ReadOwnedListArithmetic(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        Func<BeamInstruction, bool> isArithmetic,
        ref BeamProjectionFlow? flow
    )
    {
        int branch = -1;
        int allocation = -1;
        int label = start;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        for (int index = start; index + 7 < end; index++)
        {
            var item = body[index];
            if (item.Definition.Opcode == Label)
            {
                label = index;
            }
            else if (item.Definition.Opcode is Allocate or AllocateHeap)
            {
                branch = label;
                allocation = index;
            }

            if (branch <= start || index <= allocation + 1 || item is not
                {
                    Definition.Opcode: GetList,
                    Operands: [
                        var list,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } head,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } tail
                    ]
                } || list != x0 || head == tail || body[index - 1] is not
                {
                    Definition.Opcode: IsNonemptyList,
                    Operands: [_, var checkedList]
                } || checkedList != list || body[index + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var savedFrom, { Kind: BeamOperandParameters.Y, TypeIndex: null } keptHead]
                } || savedFrom != head || keptHead == head || keptHead == tail)
            {
                continue;
            }

            int arithmetic = index + 2;
            while (arithmetic < end && body[arithmetic].Definition.Opcode == Line)
            {
                arithmetic++;
            }

            int heap = arithmetic + 2;
            if (heap + 2 >= end || body[arithmetic].Definition.Opcode != GcBifTwo
                || !isArithmetic(body[arithmetic])
                || body[arithmetic].Operands is not
                [
                { Value.IsZero: true }, { Value.IsZero: true }, _, var input,
                { Kind: BeamOperandParameters.Integer }, var result
                ] || input != head || result != x0 || body[arithmetic + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var computed, { Kind: BeamOperandParameters.Y, TypeIndex: null } temporary]
                } || computed != x0 || temporary == head || temporary == tail || temporary == keptHead
                || body[heap] is not { Definition.Opcode: TestHeap, Operands: [var words, { Value.IsZero: true }] })
            {
                continue;
            }

            int arithmeticUse = -1;
            int last = heap + 1;
            for (; last < end; last++)
            {
                if (body[last] is not
                    { Definition.Opcode: PutList, Operands: [var value, var listTail, var destination] }
                    || destination != x0 || listTail != (last == heap + 1 ? tail : x0)
                    || value != head && value != temporary && value.Kind is not
                    (BeamOperandParameters.Atom or BeamOperandParameters.Integer
                        or BeamOperandParameters.Nil or BeamOperandParameters.LiteralValue))
                {
                    break;
                }

                if (value == temporary)
                {
                    if (arithmeticUse >= 0)
                    {
                        break;
                    }

                    arithmeticUse = last;
                }
            }

            if (last + 1 >= end || arithmeticUse < 0 || words.Value != (last - heap - 1) * ConsHeapWords
                || body[last] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var built, { Kind: BeamOperandParameters.Y, TypeIndex: null } keptList]
                } || built != x0 || keptList == head || keptList == tail || keptList == temporary || keptList == keptHead
                || ReadExclusiveBranchFrame(
                    body,
                    start,
                    branch,
                    allocation,
                    end
                ) is not { } owned || !Enumerable.Range(index - 1, last - index + 2).All(owned.Contains)
                || owned.Where(at => at < index || at > last)
                    .Where(at => body[at].Definition.Opcode != InitYRegisters)
                    .SelectMany(at => body[at].Operands).Any(operand =>
                        SameProjectionRegister(operand, head) || SameProjectionRegister(operand, tail)))
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.IsInitialized(index + 1, keptHead, allowFramePredicates: true)
                || !flow.HasSingleStackCopyUse(
                    branch,
                    arithmetic + 1,
                    end,
                    temporary,
                    arithmeticUse
                )
                || !flow.IsEntryTemporaryDead(last + 1, x0)
                || !flow.IsEntryTemporaryDead(last + 1, new BeamOperand(BeamOperandParameters.X, 1))
                || !flow.IsEntryTemporaryDead(last + 1, new BeamOperand(BeamOperandParameters.X, 2)))
            {
                continue;
            }

            return new OwnedListArithmetic(
                index,
                arithmetic,
                heap,
                last,
                branch,
                allocation,
                head,
                tail,
                temporary,
                keptHead,
                keptList
            );
        }

        return null;
    }
}
