using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? FinalizeCheckedPairCallReturns(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || OnLoadFunction is not null
            || sourceModule is not { DeclaredFunctions: var functions }
            || !code.Span.Contains(IsTaggedTuple) || !code.Span.Contains(TestHeap)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        Func<int, bool>? proof = null;

        return RewriteCheckedPairCallReturns(
            code,
            body,
            label => (proof ??= ReadLocalFixedPairResultProof(functions, body, emittedAtomIndices: true))(label)
        );
    }

    private byte[]? RewriteCheckedPairCallReturns(
        ReadOnlyMemory<byte> code,
        int labelCount,
        Func<int, bool> hasPair
    ) => DecodeLateAtomicProjectionSlots(code, labelCount) is { Body: var body }
        ? RewriteCheckedPairCallReturns(code, body, hasPair) : null;

    private byte[]? RewriteCheckedPairCallReturns(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        Func<int, bool> hasPair
    )
    {
        using var output = new MemoryStream();
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var y0 = new BeamOperand(BeamOperandParameters.Y, 0);
        var y1 = new BeamOperand(BeamOperandParameters.Y, 1);
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
            for (int call = start; call + 15 < end; call++)
            {
                if (body[call] is not { Definition.Opcode: LocalCall, Operands: [_, var callee] }
                    || body[call + 1] is not
                    {
                        Definition.Opcode: IsTaggedTuple,
                        Operands: [{ Kind: BeamOperandParameters.Label } failure, var result, { Value: var arity }, { Kind: BeamOperandParameters.Atom } tag]
                    } || result != x0 || arity != 2
                    || !IsField(
                        call + 2,
                        x0,
                        1,
                        x1
                    ) || !IsField(
                        call + 3,
                        x1,
                        0,
                        y1
                    ) || !IsField(
                        call + 4,
                        x1,
                        1,
                        y0
                    ))
                {
                    continue;
                }

                int unary = SkipLines(call + 5, end);
                if (unary + 8 >= end || body[unary] is not
                    {
                        Definition.Opcode: GcBifOne,
                        Operands: [{ Kind: BeamOperandParameters.Failure }, { Value.IsZero: true }, _, var argument, var measured]
                    } || argument != y0 || measured != x0
                    || body[unary + 1] is not { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value.IsOne: true }, _] }
                    || body[unary + 2] is not { Definition.Opcode: Move, Operands: [var list, var outgoing] }
                    || list != y0 || outgoing != x1)
                {
                    continue;
                }

                int last = SkipLines(unary + 3, end);
                if (last + 6 >= end
                    || body[last] is not { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value: var lastArity }, _] }
                    || lastArity != 2 || body[last + 1] is not
                    { Definition.Opcode: TestHeap, Operands: [{ Value: var words }, { Value.IsOne: true }] }
                    || words != 6 || body[last + 2] is not
                    {
                        Definition.Opcode: PutTuple,
                        Operands: [var inner, { Value: var innerArity }, var surviving, var value]
                    } || inner != x0 || innerArity != 2 || surviving != y1 || value != x0
                    || body[last + 3] is not
                    {
                        Definition.Opcode: PutTuple,
                        Operands: [var outer, { Value: var outerArity }, var outerTag, var nested]
                    } || outer != x0 || outerArity != 2 || outerTag != tag || nested != x0
                    || body[last + 4] is not { Definition.Opcode: Label, Operands: [var label] } || label.Value != failure.Value
                    || body[last + 5] is not { Definition.Opcode: Deallocate, Operands: [{ Value: var width }] }
                    || width != 2 || body[last + 6].Definition.Opcode != Return
                    || !hasPair((int)callee.Value)
                    || ReadDisjointFrameOwnership(
                        body,
                        start,
                        end,
                        trackSiblingTrimEpochs: true
                    ) is not { } frames)
                {
                    continue;
                }

                int allocation = frames.Owners[call - start];
                if (allocation < start || frames.Uses[allocation].Length != 2
                    || frames.Owners[last + 4 - start] != allocation
                    || !Enumerable.Range(call, last + 6 - call).All(index => frames.Owners[index - start] == allocation))
                {
                    continue;
                }

                // The failure edge keeps X0 and the original frame. The
                // straight success edge owns the inner carrier and its two Ys.
                output.Write(code.Span[copied..body[call + 1].Offset]);
                output.WriteByte(IsTaggedTuple);
                BeamCompactWriter.Write(output, LabelTag, failure.Value);
                output.WriteByte(ExtendedTypedRegisterOperand);
                WriteRegister(output, x0);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, GetOperandTypeIndex((TupleTypeBits, null, null, null)));
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, arity);
                BeamCompactWriter.Write(output, AtomTag, tag.Value);
                WriteField(1, x0);
                WriteField(0, y1);
                WriteField(1, y0);
                output.Write(code.Span[body[call + 5].Offset..body[unary + 3].Offset]);
                output.WriteByte(TrimFrame);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                output.Write(code.Span[body[unary + 3].Offset..body[last + 2].Offset]);
                output.WriteByte(PutTuple);
                WriteRegister(output, x0);
                output.WriteByte(ExtendedListOperand);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
                WriteRegister(output, y0);
                WriteRegister(output, x0);
                output.Write(code.Span[body[last + 3].Offset..body[last + 4].Offset]);
                output.WriteByte(Deallocate);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                output.WriteByte(Return);
                copied = body[last + 4].Offset;
                call = last + 6;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool IsField(
            int index,
            BeamOperand owner,
            int offset,
            BeamOperand destination
        ) => body[index] is { Definition.Opcode: GetTupleElement, Operands: [var source, var field, var target] }
            && source == owner && field.Value == offset && target == destination;

        void WriteField(int field, BeamOperand target)
        {
            output.WriteByte(GetTupleElement);
            WriteRegister(output, x0);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field);
            WriteRegister(output, target);
        }

        int SkipLines(int index, int limit)
        {
            while (index < limit && body[index].Definition.Opcode == Line)
            {
                index++;
            }

            return index;
        }
    }
}
