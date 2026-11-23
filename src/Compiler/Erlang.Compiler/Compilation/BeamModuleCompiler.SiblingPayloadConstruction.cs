using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record SiblingPayloadConstruction(
        int First,
        int Save,
        BeamOperand Position,
        BeamOperand CountTail,
        BeamOperand Children
    );

    private static SiblingPayloadConstruction? ReadSiblingPayloadConstruction(
        IReadOnlyList<BeamInstruction> body,
        int first,
        int save,
        BeamOperand position,
        BeamOperand countTail,
        BeamOperand children,
        BeamOperand state,
        BeamOperand payload,
        int copyHint
    )
    {
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        if (first + 8 >= body.Count || IsBareX0(state)
            || !IsExactSiblingCons(
                body[first],
                position,
                countTail,
                x0
            )
            || !IsMove(body[first + 1], x0, position)
            || body[first + 2] is not { Definition.Opcode: PutList, Operands: [var head, var childTail, var list] }
            || head != payload || childTail == payload || childTail == x0 || !IsBareX(childTail) || list != x0
            || SameProjectionRegister(childTail, countTail) || SameProjectionRegister(childTail, position)
            || body[first + 3] is not { Definition.Opcode: PutTuple, Operands: [var tuple, _, ..] } rebuild
            || tuple != x0 || rebuild.Operands.Skip(2).Count(value => SameProjectionRegister(value, x0)) != 1
            || rebuild.Operands.Skip(2).Any(value => SameProjectionRegister(value, position)
                || SameProjectionRegister(value, countTail) || SameProjectionRegister(value, payload))
            || !IsMove(body[first + 4], x0, countTail)
            || !IsExactSiblingCons(
                body[first + 5],
                countTail,
                children,
                x0
            )
            || !IsMove(body[first + 6], x0, countTail)
            || body[first + 7] is not
            {
                Definition.Opcode: UpdateRecord,
                Operands: [var hint, _, var carrier, var updated, _, _, var counted, _, var reconstructed]
            } || hint.Kind != BeamOperandParameters.Atom || hint.Value != copyHint
            || !SameProjectionRegister(carrier, state) || updated != x0
            || !SameProjectionRegister(counted, position) || !SameProjectionRegister(reconstructed, countTail)
            || body[first + 8].Definition.Opcode != Return)
        {
            return null;
        }

        return new(
            first,
            save,
            position,
            countTail,
            children
        );
    }

    private static bool IsExactSiblingCons(
        BeamInstruction instruction,
        BeamOperand head,
        BeamOperand tail,
        BeamOperand destination
    ) => instruction is { Definition.Opcode: PutList, Operands: [var a, var b, var c] }
        && a == head && b == tail && c == destination;

    private bool WriteSiblingPayloadConstruction(
        Stream output,
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        SiblingListProjectionGroup plan
    )
    {
        if (plan.Payload is not { } payload)
        {
            return true;
        }

        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var first = body[payload.First + 2].Operands;
        Put(Mapped(payload.Position), payload.CountTail, payload.CountTail);
        Put(x0, Mapped(first[1]), x0);
        if (!WriteDeferredRecordInstruction(
            output,
            code,
            body,
            payload.First + 3,
            -1,
            0,
            plan.Registers
        ))
        {
            return false;
        }

        Put(x0, Mapped(payload.Children), x0);
        var record = body[payload.First + 7].Operands;
        output.WriteByte(UpdateRecord);
        BeamCompactWriter.Write(output, AtomTag, record[0].Value);
        UInt(record[1].Value);
        EmitRegister(record[2]);
        WriteRegister(output, x0);
        output.WriteByte(ExtendedListOperand);
        UInt(record[4].Value);
        int consType = GetOperandTypeIndex((ConsTypeBits, null, null, null));
        UInt(record[5].Value);
        EmitRegister(payload.CountTail with { TypeIndex = consType });
        UInt(record[7].Value);
        EmitRegister(x0 with { TypeIndex = consType });

        return true;

        BeamOperand Mapped(BeamOperand operand) => plan.Registers.TryGetValue((int)operand.Value, out int mapped)
            ? operand with { Value = mapped }
            : operand;

        void Put(BeamOperand head, BeamOperand tail, BeamOperand destination)
        {
            output.WriteByte(PutList);
            WriteRegister(output, head);
            WriteRegister(output, tail);
            WriteRegister(output, destination);
        }

        void EmitRegister(BeamOperand register)
        {
            if (register.TypeIndex is int type)
            {
                output.WriteByte(ExtendedTypedRegisterOperand);
                WriteRegister(output, register);
                UInt(type);
            }
            else
            {
                WriteRegister(output, register);
            }
        }

        void UInt(System.Numerics.BigInteger operand) => BeamCompactWriter.Write(output, BeamWire.UnsignedTag, operand);
    }
}
