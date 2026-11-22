using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (ReadOnlyMemory<byte> Code, int LabelCount)? KeepCaughtResultInTailArguments(
        ReadOnlyMemory<byte> code,
        int labelCount
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TryCase)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var x2 = new BeamOperand(BeamOperandParameters.X, 2);
        var y0 = new BeamOperand(BeamOperandParameters.Y, 0);
        var y1 = new BeamOperand(BeamOperandParameters.Y, 1);
        var y2 = new BeamOperand(BeamOperandParameters.Y, 2);
        var y3 = new BeamOperand(BeamOperandParameters.Y, 3);
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        for (int start = 2; start + 27 < body.Count; start++)
        {
            if (body[start - 2] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var arity] }
                || body[start - 1].Definition.Opcode != Label || arity.Value.Sign <= 0
                || !Has(
                    start,
                    Allocate,
                    4,
                    (int)arity.Value
                )
                || body[start + 1] is not { Definition.Opcode: InitYRegisters, Operands: [{ Value: var initialized }, var first, var second] }
                || initialized != 2 || first != y0 || second != y1
                || !IsMove(body[start + 2], x0 with { Value = arity.Value - 1 }, y2)
                || body[start + 3] is not { Definition.Opcode: TryEnter, Operands: [var installed, var handler] }
                || installed != y3 || !HasLine(start + 4)
                || body[start + 5] is not { Definition.Opcode: LocalCall or ExternalCall, Operands: [var arguments, _] }
                || arguments.Value != arity.Value - 1
                || !HasSlot(start + 6, TryEnd, y3)
                || body[start + 7] is not { Definition.Opcode: Jump, Operands: [var join] }
                || !LabelAt(start + 8, handler) || !HasSlot(start + 9, TryCase, y3)
                || !IsMove(body[start + 10], x1, y1) || !IsMove(body[start + 11], x0, y3)
                || !IsMove(body[start + 12], x2, x0) || !IsMove(body[start + 13], x0, y0)
                || body[start + 14] is not
                { Definition.Opcode: ExactEqualTest, Operands: [var wrap, var selected, { Kind: BeamOperandParameters.Atom } passthrough] }
                || selected != y3 || !IsMove(body[start + 15], y1, x0)
                || body[start + 16] is not { Definition.Opcode: Jump, Operands: [var rejoin] } || rejoin != join
                || !LabelAt(start + 17, wrap) || !Has(
                    start + 18,
                    TestHeap,
                    6,
                    0
                )
                || !ReadTuple(start + 19, y1, out var inner) || !ReadTuple(start + 20, x0, out var outer)
                || !LabelAt(start + 21, join) || !IsMove(body[start + 22], x0, y3)
                || !IsMove(body[start + 23], y3, x1) || !IsMove(body[start + 24], y2, x0) || !HasLine(start + 25)
                || body[start + 26] is not
                {
                    Definition.Opcode: TailLocalCallWithFrame or TailExternalCallWithFrame,
                    Operands: [{ Value: var tailArity }, var tailTarget, { Value: var tailFrame }]
                } || tailArity != 2 || tailFrame != 4)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.TryGetSingleForwardTarget(start + 3, (int)handler.Value, out int caught) || caught != start + 8
                || !flow.TryGetSingleForwardTarget(start + 14, (int)wrap.Value, out int wrapped) || wrapped != start + 17
                || !flow.TryGetExclusiveForwardTarget(
                    start + 7,
                    start + 16,
                    start + 20,
                    (int)join.Value,
                    out int joined
                )
                || joined != start + 21)
            {
                continue;
            }

            // Only the receiver must survive the protected call. Both cold
            // branches consume the reason before any call, so the landing
            // registers can deliver the result directly to the tail argument.
            Copy(copied, start);
            Unsigned(Allocate, 2, (int)arity.Value);
            WriteMove(x0 with { Value = arity.Value - 1 }, y0);
            output.WriteByte(TryEnter);
            WriteRegister(output, y1);
            BeamCompactWriter.Write(output, LabelTag, handler.Value);
            Copy(body[start + 4].Offset, start + 6);
            WriteSlot(TryEnd, y1);
            WriteMove(x0, x1);
            Copy(body[start + 7].Offset, start + 9);
            WriteSlot(TryCase, y1);
            output.WriteByte(ExactNotEqualTest);
            BeamCompactWriter.Write(output, LabelTag, join.Value);
            output.WriteByte(ExtendedTypedRegisterOperand);
            WriteRegister(output, x0);
            int atomType = GetOperandTypeIndex((AtomTypeBits, null, null, null));
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, atomType);
            BeamCompactWriter.Write(output, AtomTag, passthrough.Value);
            Unsigned(TestHeap, 6, 2);
            WriteTuple(x0, inner, x1);
            WriteTuple(x1, outer, x0);
            Copy(body[start + 21].Offset, start + 22);
            WriteMove(y0, x0);
            Copy(body[start + 25].Offset, start + 26);
            output.WriteByte(body[start + 26].Definition.Opcode);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
            BeamCompactWriter.Write(
                output,
                tailTarget.Kind == BeamOperandParameters.Label ? LabelTag : BeamWire.UnsignedTag,
                tailTarget.Value
            );
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
            copied = body[start + 27].Offset;
            start += 26;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        byte[] rewritten = output.ToArray();
        if (OrderFinalPhysicalLabelsCore(
            rewritten,
            labelCount,
            formMultiKeySelections: false,
            relocateOnly: true
        ) is not { } ordered)
        {
            return null;
        }

        return (OrderTypeOperandsByFirstUse(ordered.Code, ordered.LabelCount), ordered.LabelCount);

        void Copy(int from, int until) => output.Write(code.Span[from..body[until].Offset]);

        bool Has(
            int index,
            byte opcode,
            int first,
            int second
        ) =>
            body[index].Definition.Opcode == opcode && body[index].Operands is [var left, var right]
            && left.Value == first && right.Value == second;

        bool HasSlot(int index, byte opcode, BeamOperand slot) =>
            body[index].Definition.Opcode == opcode && body[index].Operands is [var operand] && operand == slot;

        bool HasLine(int index) => body[index] is { Definition.Opcode: Line, Operands: [{ Value.Sign: > 0 }] };

        bool LabelAt(int index, BeamOperand target) =>
            body[index] is { Definition.Opcode: Label, Operands: [var label] } && label.Value == target.Value;

        bool ReadTuple(int index, BeamOperand reason, out BeamOperand tag)
        {
            if (body[index] is
                {
                    Definition.Opcode: PutTuple,
                    Operands: [var destination, { Value: var count }, { Kind: BeamOperandParameters.Atom } literal, var field]
                } && destination == x0 && count == 2 && field == reason)
            {
                tag = literal;

                return true;
            }

            tag = default!;

            return false;
        }

        void Unsigned(byte opcode, int first, int second)
        {
            output.WriteByte(opcode);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, first);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, second);
        }

        void WriteSlot(byte opcode, BeamOperand slot)
        {
            output.WriteByte(opcode);
            WriteRegister(output, slot);
        }

        void WriteMove(BeamOperand source, BeamOperand destination)
        {
            WriteSlot(Move, source);
            WriteRegister(output, destination);
        }

        void WriteTuple(BeamOperand destination, BeamOperand tag, BeamOperand reason)
        {
            WriteSlot(PutTuple, destination);
            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
            BeamCompactWriter.Write(output, AtomTag, tag.Value);
            WriteRegister(output, reason);
        }
    }
}
