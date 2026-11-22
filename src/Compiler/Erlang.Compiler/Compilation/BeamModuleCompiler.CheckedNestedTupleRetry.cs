using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ShareCheckedNestedTupleRetryPrefix(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var edits = new List<CodeEdit>();
        BeamProjectionFlow? flow = null;
        for (int first = 0; first + 6 < body.Count; first++)
        {
            if (body[first] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label } retryLabel,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } carrier, var width, _]
                }
                || body[first + 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var source, var tagField, { Kind: BeamOperandParameters.X, TypeIndex: null } scratch]
                } || source != carrier || scratch == carrier || tagField.Value >= width.Value
                || body[first + 2] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [var tagFailure, var tested, { Kind: BeamOperandParameters.Atom }]
                } || tagFailure != retryLabel || tested != scratch
                || body[first + 3] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var sameSource, var payload, var result]
                } || sameSource != carrier || result != scratch || payload == tagField || payload.Value >= width.Value
                || body[first + 4] is not { Definition.Opcode: IsTuple, Operands: [var tupleFailure, var tuple] }
                || tupleFailure != retryLabel || tuple != scratch
                || body[first + 5] is not { Definition.Opcode: TestArity, Operands: [var arityFailure, var sized, _] }
                || arityFailure != retryLabel || sized != scratch)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.TryGetNestedTupleRetry(first, (int)retryLabel.Value, out int retry)
                || retry + 5 >= body.Count
                || body[retry + 1] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label } failed, ..]
                } || failed == retryLabel
                || !Repeats(0, skipFailure: true) || !Repeats(1, skipFailure: false)
                || !Repeats(2, skipFailure: true) || !Repeats(3, skipFailure: false)
                || body[retry + 3].Operands[0] != failed
                || !flow.TryGetForwardLabel(retry + 4, (int)failed.Value, out int final)
                || !HasUnprotectedCheckedFailureRegion(body, first, final))
            {
                continue;
            }

            // The two outer failures would fail the repeated check with
            // the identical registers. Only the nested checks retry.
            Retarget(first, (int)failed.Value);
            Retarget(first + 2, (int)failed.Value);
            edits.Add(new(
                body[retry + 1].Offset,
                body[retry + 5].Offset,
                0,
                0,
                IsRemoval: true
            ));
            first = retry + 4;

            bool Repeats(int offset, bool skipFailure)
            {
                var original = body[first + offset];
                var repeated = body[retry + 1 + offset];
                int skip = skipFailure ? 1 : 0;

                return original.Definition.Opcode == repeated.Definition.Opcode
                    && original.Operands.Skip(skip).SequenceEqual(repeated.Operands.Skip(skip));
            }
        }

        return ApplyDuplicatedFrameReadEdits(code, edits);

        void Retarget(int index, int destination)
        {
            int operand = body[index].Offset + 1;
            var cursor = new BeamCursor(code[operand..]);
            _ = BeamCompactReader.Read(cursor);
            edits.Add(new(
                operand,
                operand + cursor.Position,
                LabelTag,
                destination
            ));
        }
    }

    private byte[]? OrderCheckedNestedTupleReads(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TestArity)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamOperand outer = new(BeamOperandParameters.X, 0);
        BeamOperand inner = new(BeamOperandParameters.X, 1);
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        for (int start = 0; start + 10 < body.Count; start++)
        {
            if (body[start] is not { Definition.Opcode: IsTuple, Operands: [var failed, var testedOuter] }
                || testedOuter != outer
                || body[start + 1] is not { Definition.Opcode: TestArity, Operands: [var failedOuter, var sized, var outerArity] }
                || failedOuter != failed || sized != outer
                || body[start + 2] is not { Definition.Opcode: GetTupleElement, Operands: [var parent, var field, var child] }
                || parent != outer || child != inner || field.Value >= outerArity.Value
                || body[start + 3] is not { Definition.Opcode: IsTuple, Operands: [var failedInner, var testedInner] }
                || failedInner != failed || testedInner != inner
                || body[start + 4] is not { Definition.Opcode: TestArity, Operands: [var failure, var checkedInner, var innerArity] }
                || failure != failed || checkedInner != inner
                || body[start + 5] is not { Definition.Opcode: Allocate, Operands: [_, { Value: var live }] }
                || live != 2
                || body[start + 6] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var innerSource, { Kind: BeamOperandParameters.Y, TypeIndex: null } savedInner]
                }
                || innerSource != inner
                || body[start + 7] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var outerSource, { Kind: BeamOperandParameters.Y, TypeIndex: null } savedOuter]
                }
                || outerSource != outer || savedOuter == savedInner)
            {
                continue;
            }

            int prefix = start + 8;
            int innerStart = prefix;
            while (innerStart < body.Count && body[innerStart] is
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var source, var element, { Kind: BeamOperandParameters.Y, TypeIndex: null } slot]
                }
                && source == savedOuter && element.Value < outerArity.Value
                && slot != savedOuter && slot != savedInner)
            {
                innerStart++;
            }

            int end = innerStart;
            while (end < body.Count && body[end] is
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var innerInput, var innerField, { Kind: BeamOperandParameters.X, TypeIndex: null } target]
                }
                && innerInput == savedInner && innerField.Value == end - innerStart && innerField.Value < innerArity.Value
                && target.Value > inner.Value)
            {
                end++;
            }

            if (prefix == innerStart || end == innerStart || end >= body.Count || end - innerStart != innerArity.Value)
            {
                continue;
            }

            // Both arity checks dominate these reads. Keep the saved carriers
            // for later blocks, and read the still-live original X registers here.
            output.Write(code.Span[copied..body[prefix].Offset]);
            WriteReads(innerStart, end, inner);
            WriteReads(prefix, innerStart, outer);
            copied = body[end].Offset;
            start = end - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void WriteReads(int first, int last, BeamOperand source)
        {
            for (int index = first; index < last; index++)
            {
                int operand = body[index].Offset + 1;
                var cursor = new BeamCursor(code[operand..]);
                _ = BeamCompactReader.Read(cursor);
                output.WriteByte(GetTupleElement);
                WriteRegister(output, source);
                output.Write(code.Span[(operand + cursor.Position)..body[index + 1].Offset]);
            }
        }
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool TryGetNestedTupleRetry(int first, int label, out int retry) =>
            TryGetForwardLabel(first + 5, label, out retry)
            && predecessors[retry] is [var tag, var literal, var tuple, var arity]
            && tag == first && literal == first + 2 && tuple == first + 4 && arity == first + 5;
    }
}
