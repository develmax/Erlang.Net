using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? MoveCaughtListHeadChecksBeforeFrame(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(CatchEnter)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var references = body.Where(instruction => instruction.Definition.Opcode != Label)
            .SelectMany(instruction => instruction.Operands)
            .Where(operand => operand.Kind == BeamOperandParameters.Label)
            .GroupBy(operand => operand.Value).ToDictionary(group => group.Key, group => group.Count());
        using var output = new MemoryStream();
        int copied = 0;
        for (int info = 1; info + 12 < body.Count; info++)
        {
            if (body[info] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var arity] }
                || arity.Value <= 1 || arity.Value >= FirstReservedXRegister
                || body[info + 1].Definition.Opcode != Label
                || body[info + 2] is not { Definition.Opcode: IsNonemptyList, Operands: [var entryFailure, var input] }
                || input != x0 || !IsEntryFailure(info, entryFailure)
                || body[info + 3] is not
                {
                    Definition.Opcode: Allocate,
                    Operands: [var width, var live]
                } || live.Value != arity.Value || width.Value > BeamOperandParameters.MaximumRegister)
            {
                continue;
            }

            int end = info + 4;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            int first = info + 4;
            if (body[first].Definition.Opcode == InitYRegisters)
            {
                first++;
            }

            int arityCount = (int)arity.Value;
            int head = first + arityCount;
            if (head + 6 >= end || ReadSavedArguments(first, arityCount) is not { } saved
                || body[head] is not { Definition.Opcode: GetHead, Operands: [var list, var cached] }
                || list != saved[0]
                || body[head + 1] is not { Definition.Opcode: IsTuple, Operands: [var rejected, var checkedHead] }
                || checkedHead != cached || rejected == entryFailure || references.GetValueOrDefault(rejected.Value) != 2
                || body[head + 2] is not { Definition.Opcode: TestArity, Operands: [var wrongArity, var value, var tupleWidth] }
                || wrongArity != rejected || value != cached
                || body[head + 3] is not { Definition.Opcode: CatchEnter, Operands: [var handler, var continuation] }
                || handler != cached || saved.Contains(handler)
                || body[head + 4] is not { Definition.Opcode: Move, Operands: [var argument, var callInput] }
                || argument != list || callInput != x0)
            {
                continue;
            }

            int call = head + 5;
            while (call < end && body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (call + 2 >= end || body[call] is not
                {
                    Definition.Opcode: ExternalCall or LocalCall,
                    Operands: [{ Value.IsOne: true }, _]
                }
                || body[call + 1] is not { Definition.Opcode: Label, Operands: [var landing] }
                || landing.Value != continuation.Value
                || body[call + 2] is not { Definition.Opcode: CatchEnd, Operands: [var ended] }
                || ended != handler
                || FindRestore(
                    call + 3,
                    end,
                    rejected,
                    saved,
                    width,
                    entryFailure
                ) is not { } restore)
            {
                continue;
            }

            // The predicates only inspect the entry argument and both failure
            // paths raise with the original arguments. No handler exists yet.
            var scratch = new BeamOperand(BeamOperandParameters.X, arityCount);
            output.Write(code.Span[copied..body[info + 3].Offset]);
            output.WriteByte(GetHead);
            WriteRegister(output, x0);
            WriteRegister(output, scratch);
            output.WriteByte(IsTuple);
            BeamCompactWriter.Write(output, LabelTag, entryFailure.Value);
            WriteRegister(output, scratch);
            output.WriteByte(TestArity);
            BeamCompactWriter.Write(output, LabelTag, entryFailure.Value);
            WriteRegister(output, scratch);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, tupleWidth.Value);
            Copy(info + 3, head);
            Copy(head + 3, head + 4);
            Copy(head + 5, restore.Start);
            copied = body[restore.End].Offset;
            info = end - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool IsEntryFailure(int info, BeamOperand failure)
        {
            int prefix = info - 1;
            while (prefix > 0 && body[prefix].Definition.Opcode == Line)
            {
                prefix--;
            }

            return body[prefix] is { Definition.Opcode: Label, Operands: [var entry] } && entry.Value == failure.Value;
        }

        BeamOperand[]? ReadSavedArguments(int start, int arity)
        {
            var entries = new Dictionary<int, BeamOperand>();
            for (int index = start; index < start + arity; index++)
            {
                if (body[index] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } from,
                        { Kind: BeamOperandParameters.Y, TypeIndex: null } to]
                    }
                    || from.Value >= arity || entries.ContainsValue(to) || !entries.TryAdd((int)from.Value, to))
                {
                    return null;
                }
            }

            return Enumerable.Range(0, arity).Select(register => entries[register]).ToArray();
        }

        (int Start, int End)? FindRestore(
            int start,
            int end,
            BeamOperand rejected,
            BeamOperand[] saved,
            BeamOperand width,
            BeamOperand entryFailure
        )
        {
            for (int index = start; index + saved.Length + 2 < end; index++)
            {
                if (body[index] is not { Definition.Opcode: Label, Operands: [var label] } || label.Value != rejected.Value
                    || !EndsBeamBlock(body[index - 1].Definition.Opcode))
                {
                    continue;
                }

                int release = index + saved.Length + 1;
                if (body[release] is not { Definition.Opcode: Deallocate, Operands: [var originalWidth] }
                    || originalWidth != width
                    || body[release + 1] is not { Definition.Opcode: Jump, Operands: [var originalFailure] }
                    || originalFailure != entryFailure)
                {
                    return null;
                }

                for (int register = 0; register < saved.Length; register++)
                {
                    if (body[index + register + 1] is not
                        { Definition.Opcode: Move, Operands: [var from, { Kind: BeamOperandParameters.X, TypeIndex: null } to] }
                        || from != saved[register] || to.Value != register)
                    {
                        return null;
                    }
                }

                return (index, release + 2);
            }

            return null;
        }

        void Copy(int start, int end) => output.Write(code.Span[body[start].Offset..body[end].Offset]);
    }
}
