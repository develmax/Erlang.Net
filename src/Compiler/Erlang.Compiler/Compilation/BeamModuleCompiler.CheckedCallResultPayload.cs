using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ShareCheckedCallResultPayload(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(SelectValue)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var x2 = new BeamOperand(BeamOperandParameters.X, 2);
        var labels = body.Select((instruction, index) => (instruction, index))
            .Where(pair => pair.instruction.Definition.Opcode == Label)
            .ToDictionary(pair => pair.instruction.Operands[0].Value, pair => pair.index);
        var uses = body.Where(instruction => instruction.Definition.Opcode != Label)
            .SelectMany(instruction => instruction.Operands)
            .Where(operand => operand.Kind == BeamOperandParameters.Label)
            .GroupBy(operand => operand.Value).ToDictionary(group => group.Key, group => group.Count());
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int functionEnd = 0;
        bool protectedFunction = false;
        for (int index = 1; index + 10 < body.Count; index++)
        {
            if (body[index].Definition.Opcode == FunctionInfo)
            {
                functionEnd = index + 1;
                protectedFunction = false;
                while (body[functionEnd].Definition.Opcode is not (FunctionInfo or EndCode))
                {
                    protectedFunction |= body[functionEnd].Definition.Opcode is TryEnter or CatchEnter or DebugLine or ExecutableLine;
                    functionEnd++;
                }
            }

            if (protectedFunction || index + 10 >= functionEnd || body[index - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[index] is not { Definition.Opcode: IsTuple, Operands: [var failure, var input] }
                || input != x0
                || body[index + 1] is not
                {
                    Definition.Opcode: TestArity,
                    Operands: [var rejected, var tuple, { Value: var resultArity }]
                } || rejected != failure || tuple != x0 || resultArity != 2
                || !ReadsField(
                    index + 2,
                    x0,
                    0,
                    x1
                )
                || body[index + 3] is not
                {
                    Definition.Opcode: SelectValue,
                    Operands: [var selector, var invalid, _, { Kind: BeamOperandParameters.Atom }, var left,
                    { Kind: BeamOperandParameters.Atom }, var right]
                } || selector != x1 || invalid != failure || left == right
                || !labels.TryGetValue(left.Value, out int leftEntry)
                || !labels.TryGetValue(right.Value, out int rightEntry)
                || uses.GetValueOrDefault(left.Value) != 1 || uses.GetValueOrDefault(right.Value) != 1
                || Math.Min(leftEntry, rightEntry) != index + 4
                || !labels.TryGetValue(invalid.Value, out int failureEntry)
                || failureEntry >= functionEnd || !IsCaseFailure(failureEntry))
            {
                continue;
            }

            int guardArm = index + 4;
            int otherArm = Math.Max(leftEntry, rightEntry);
            if (!ReadsField(
                guardArm + 1,
                x0,
                1,
                x1
            )
                || body[guardArm + 2] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [var alternative, var payload, var recordArity, { Kind: BeamOperandParameters.Atom } recordTag]
                } || payload != x1
                || body[guardArm + 3] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var record, var field, var checkedValue]
                } || record != x1 || checkedValue != x2
                || body[guardArm + 4] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [var otherwise, var compared, { Kind: BeamOperandParameters.Atom } expected]
                } || otherwise != alternative || compared != x2
                || !labels.TryGetValue(alternative.Value, out int otherwiseArm)
                || uses.GetValueOrDefault(alternative.Value) != 2
                || otherwiseArm <= guardArm + 4 || otherArm <= otherwiseArm || otherArm + 2 >= functionEnd
                || !EndsBeamBlock(body[otherwiseArm - 1].Definition.Opcode)
                || !EndsBeamBlock(body[otherArm - 1].Definition.Opcode)
                || body[otherwiseArm + 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var rawResult, { Value.IsOne: true }, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                } || rawResult != x0 || !ReadsField(
                    otherArm + 1,
                    x0,
                    1,
                    saved
                ))
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            int compare = guardArm + 4;
            if (!flow.IsEntryTemporaryDead(compare + 1, x0)
                || !flow.IsBranchTemporaryDead(otherwiseArm + 1, x0)
                || !flow.IsBranchTemporaryDead(compare, x1)
                || !flow.IsBranchTemporaryDead(compare, x2))
            {
                continue;
            }

            // The arity check makes the shared payload projection safe. All
            // reachable record paths previously consumed the same second field.
            output.Write(code.Span[copied..body[index + 3].Offset]);
            WriteField(x0, 1, saved);
            Copy(index + 3, guardArm + 1);
            output.WriteByte(IsTaggedTuple);
            BeamCompactWriter.Write(output, LabelTag, alternative.Value);
            WriteRegister(output, saved);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, recordArity.Value);
            BeamCompactWriter.Write(output, AtomTag, recordTag.Value);
            WriteField(saved, (int)field.Value, x0);
            output.WriteByte(ExactEqualTest);
            BeamCompactWriter.Write(output, LabelTag, alternative.Value);
            WriteRegister(output, x0);
            BeamCompactWriter.Write(output, AtomTag, expected.Value);
            Copy(guardArm + 5, otherwiseArm + 1);
            Copy(otherwiseArm + 2, otherArm + 1);
            copied = body[otherArm + 2].Offset;
            index = otherArm + 1;
            if (FindNestedRecordFailure(
                otherwiseArm,
                otherArm,
                failureEntry,
                saved
            ) is { } cold)
            {
                // The cold record check owns the inner error. The outer
                // selector's default is emitted after that branch's exits.
                Copy(otherArm + 2, failureEntry);
                Copy(cold.Start, cold.End);
                Copy(failureEntry, cold.Start);
                copied = body[cold.End].Offset;
                index = cold.End - 1;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool ReadsField(
            int index,
            BeamOperand source,
            int offset,
            BeamOperand destination
        ) => body[index] is
        {
            Definition.Opcode: GetTupleElement,
            Operands: [var input, var field, var result]
        } && input == source && field.Value == offset && result == destination;

        bool IsCaseFailure(int label)
        {
            int index = label + 1;
            if (body[index].Definition.Opcode == Line)
            {
                index++;
            }

            return body[index] is { Definition.Opcode: CaseEnd, Operands: [var result] } && result == x0;
        }

        (int Start, int End)? FindNestedRecordFailure(
            int normal,
            int alternative,
            int outer,
            BeamOperand payload
        )
        {
            int nested = SkipLine(outer + 1) + 1;
            if (outer <= alternative || nested + 2 >= body.Count
                || !EndsBeamBlock(body[outer - 1].Definition.Opcode)
                || body[nested] is not { Definition.Opcode: Label, Operands: [var entry] }
                || uses.GetValueOrDefault(entry.Value) != 1)
            {
                return null;
            }

            int exit = SkipLine(nested + 1);
            if (exit >= body.Count - 1
                || body[exit] is not { Definition.Opcode: BadRecord, Operands: [var rejected] } || rejected != payload
                || !Enumerable.Range(normal + 2, alternative - normal - 2).Any(index => body[index] is
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [var failure, var source, _, _]
                } && failure.Value == entry.Value && source == payload))
            {
                return null;
            }

            return (nested, exit + 1);

            int SkipLine(int index) => body[index].Definition.Opcode == Line ? index + 1 : index;
        }

        void WriteField(BeamOperand source, int field, BeamOperand destination)
        {
            output.WriteByte(GetTupleElement);
            WriteRegister(output, source);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field);
            WriteRegister(output, destination);
        }

        void Copy(int start, int end) => output.Write(code.Span[body[start].Offset..body[end].Offset]);
    }
}
