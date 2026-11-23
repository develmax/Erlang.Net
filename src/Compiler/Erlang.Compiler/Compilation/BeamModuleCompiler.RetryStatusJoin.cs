using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (ReadOnlyMemory<byte> Code, int LabelCount)? RetainRetryStatusInFinalSlot(
        ReadOnlyMemory<byte> code,
        int labelCount
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TrimFrame)
            || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var y0 = new BeamOperand(BeamOperandParameters.Y, 0);
        var nil = new BeamOperand(BeamOperandParameters.Nil, NilAtomIndex);
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        int nextLabel = labelCount;
        for (int index = 11; index + 33 < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: Jump, Operands: [var join] }
                || body[index + 1].Definition.Opcode != Label || body[index + 2] is not
                {
                    Definition.Opcode: ExactNotEqualTest,
                    Operands: [var atomJoin, var atomSource, { Kind: BeamOperandParameters.Atom } success]
                }
                || atomJoin != join || atomSource != x0 || body[index + 3] is not
                { Definition.Opcode: Label, Operands: [var restore] }
                || body[index + 4] is not
                { Definition.Opcode: Move, Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } original, var restored] }
                || restored != x0 || !LabelAt(index + 5, join) || body[index + 6] is not
                { Definition.Opcode: Move, Operands: [var result, { Kind: BeamOperandParameters.Y, TypeIndex: null } status] }
                || result != x0 || status == original || body[index + 7] is not
                { Definition.Opcode: IsTuple, Operands: [var atomCase, var tested] }
                || tested != status || body[index + 8] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [var failure, { TypeIndex: not null } typedStatus, { Value: var tagWidth }, { Kind: BeamOperandParameters.Atom }]
                }
                || !SameProjectionRegister(typedStatus, status) || tagWidth != 2 || !LabelAt(index + 25, atomCase)
                || !LabelAt(index + 30, failure) || body[index + 26] is not
                { Definition.Opcode: ExactEqualTest, Operands: [var atomFailure, var otherTest, var otherSuccess] }
                || otherTest != status || otherSuccess != success || atomFailure != failure
                || !IsMove(body[index + 27], success, x0) || !Has(index + 29, Return)
                || !HasLine(index + 31) || !Has(index + 32, CaseEnd, status))
            {
                continue;
            }

            if (body[index + 18] is not
                { Definition.Opcode: Move, Operands: [var beforeTrim, { Kind: BeamOperandParameters.Y, TypeIndex: null } retained] }
                || beforeTrim != status || retained == status || retained == original || body[index + 19] is not
                { Definition.Opcode: TrimFrame, Operands: [var dropped, { Value.IsOne: true }] }
                || dropped.Value != retained.Value || !HasDeallocation(index + 23, 1)
                || !HasDeallocation(index + 28, dropped.Value + 1)
                || !HasLine(index + 20) || !HasOrdinaryCall(index + 21, 2)
                || !IsMove(body[index + 22], y0, x0) || !Has(index + 24, Return)
                || !Has(
                    index + 9,
                    GetTupleElement,
                    status,
                    U(1),
                    x0
                )
                || !Has(
                    index + 10,
                    IsTuple,
                    failure,
                    x0
                )
                || !Has(
                    index + 11,
                    TestArity,
                    failure,
                    x0,
                    U(2)
                )
                || !Has(
                    index + 12,
                    TestHeap,
                    U(2 * ConsHeapWords),
                    U(1)
                )
                || !Has(
                    index + 13,
                    GetTupleElement,
                    x0,
                    U(0),
                    x1
                )
                || !Has(
                    index + 14,
                    GetTupleElement,
                    x0,
                    U(1),
                    x0
                )
                || !Has(
                    index + 15,
                    PutList,
                    x0,
                    nil,
                    x0
                )
                || !Has(
                    index + 16,
                    PutList,
                    x1,
                    x0,
                    x1
                )
                || body[index + 17] is not { Definition.Opcode: Move, Operands: [var message, var messageTarget] }
                || !IsNonRegisterValue(message) || messageTarget != x0)
            {
                continue;
            }

            int width = (int)retained.Value + 1;
            if (!HasOrdinaryCall(index - 1, 2) || !HasLine(index - 2)
                || body[index - 3] is not { Definition.Opcode: Move, Operands: [var constant, var argument] }
                || !IsNonRegisterValue(constant) || argument != x1
                || !HasOrdinaryCall(index - 4, 1) || !HasLine(index - 5)
                || body[index - 6] is not { Definition.Opcode: InitYRegisters, Operands: [var count, ..] }
                || count.Value != width || !body[index - 6].Operands.Skip(1).SequenceEqual(
                    Enumerable.Range(0, width).Select(slot => new BeamOperand(BeamOperandParameters.Y, slot))
                )
                || !Has(
                    index - 10,
                    ExactNotEqualTest,
                    restore with { Kind = BeamOperandParameters.Label },
                    x0,
                    nil
                ))
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.HasClosedRetryStatusCorridor(index, (int)join.Value) || !HasFrame(index, width))
            {
                continue;
            }

            // The retry arrived from a call with all earlier Y roots cleared.
            // The other edge only reads the already saved status. Both define
            // the final Y slot before any shared check or allocating instruction.
            int successLabel = nextLabel++;
            Copy(copied, index);
            Emit(Move, x0, retained);
            Copy(body[index].Offset, index + 2);
            Emit(
                ExactNotEqualTest,
                new BeamOperand(BeamOperandParameters.Label, successLabel),
                x0,
                success
            );
            Copy(body[index + 3].Offset, index + 4);
            Emit(Move, original, retained);
            Copy(body[index + 5].Offset, index + 6);
            Emit(IsTuple, atomCase, retained);
            Emit(
                IsTaggedTuple,
                failure,
                retained with { TypeIndex = typedStatus.TypeIndex },
                U(2),
                body[index + 8].Operands[3]
            );
            Emit(
                GetTupleElement,
                retained,
                U(1),
                x0
            );
            Copy(body[index + 10].Offset, index + 18);
            Copy(body[index + 20].Offset, index + 22);
            Emit(Move, retained, x0);
            Emit(Deallocate, U(width));
            Copy(body[index + 24].Offset, index + 26);
            Emit(
                ExactEqualTest,
                failure,
                retained,
                success
            );
            Emit(Label, U(successLabel));
            Copy(body[index + 27].Offset, index + 32);
            Emit(CaseEnd, retained);
            copied = body[index + 33].Offset;
            index += 32;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return OrderFinalPhysicalLabelsCore(
            output.ToArray(),
            nextLabel,
            formMultiKeySelections: false,
            relocateOnly: true
        );

        static BeamOperand U(int value) => new(BeamOperandParameters.Unsigned, value);

        bool Has(int index, byte opcode, params BeamOperand[] operands) =>
            body[index].Definition.Opcode == opcode && body[index].Operands.SequenceEqual(operands);

        bool LabelAt(int index, BeamOperand target) =>
            body[index] is { Definition.Opcode: Label, Operands: [var label] } && label.Value == target.Value;

        bool HasDeallocation(int index, System.Numerics.BigInteger width) =>
            body[index] is { Definition.Opcode: Deallocate, Operands: [var size] } && size.Value == width;

        bool HasLine(int index) => body[index] is { Definition.Opcode: Line, Operands: [{ Value.Sign: > 0 }] };

        bool HasOrdinaryCall(int index, int arity) =>
            body[index] is { Definition.Opcode: LocalCall or ExternalCall, Operands: [var arguments, _] }
            && arguments.Value == arity;

        bool HasFrame(int index, int width)
        {
            int start = index;
            while (start > 0 && body[start - 1].Definition.Opcode != FunctionInfo)
            {
                start--;
            }

            int end = index + 33;
            while (end + 1 < body.Count && body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            if (ReadDisjointFrameOwnership(
                body,
                start,
                end,
                trackSiblingTrimEpochs: true
            ) is not { } frame
                || frame.Uses.Count != 2)
            {
                return false;
            }

            int owner = frame.Owners[index - start];

            return frame.Uses.TryGetValue(owner, out var slots) && slots.Length == width
                && frame.Owners[index - 10 - start] == owner && frame.Owners[index + 2 - start] == owner
                && frame.Owners[index + 4 - start] == owner && frame.Owners[index + 19 - start] == owner
                && frame.Owners[index + 20 - start] == index + 19 && frame.Owners[index + 26 - start] == owner
                && frame.Owners[index + 32 - start] == owner;
        }

        void Copy(int from, int until) => output.Write(code.Span[from..body[until].Offset]);

        void Emit(byte opcode, params BeamOperand[] operands)
        {
            output.WriteByte(opcode);
            foreach (BeamOperand operand in operands)
            {
                if (operand.Kind is BeamOperandParameters.X or BeamOperandParameters.Y)
                {
                    if (operand.TypeIndex is int type)
                    {
                        output.WriteByte(ExtendedTypedRegisterOperand);
                        WriteRegister(output, operand);
                        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type);
                    }
                    else
                    {
                        WriteRegister(output, operand);
                    }
                }
                else
                {
                    byte tag = operand.Kind switch
                    {
                        BeamOperandParameters.Label => LabelTag,
                        BeamOperandParameters.Atom => AtomTag,
                        _ => BeamWire.UnsignedTag
                    };
                    BeamCompactWriter.Write(output, tag, operand.Value);
                }
            }
        }
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool HasClosedRetryStatusCorridor(int entry, int joinLabel)
        {
            if (!TryGetExclusiveForwardTarget(
                entry,
                entry + 2,
                entry + 4,
                joinLabel,
                out int joined
            )
                || joined != entry + 5)
            {
                return false;
            }

            for (int index = entry - 9; index <= entry; index++)
            {
                if (predecessors[index] is not [var previous] || previous != index - 1)
                {
                    return false;
                }
            }

            for (int index = joined + 1; index <= entry + 32; index++)
            {
                if (predecessors[index].Count == 0 || predecessors[index].Any(previous => previous < joined || previous >= index))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
