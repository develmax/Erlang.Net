using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private readonly record struct NestedArmCheck(
        int First,
        int End,
        BeamOperand Input,
        int Arity,
        int Failure
    );

    private static byte[]? DelayNestedCheckedArmFrame(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int start = 0;
        BeamProjectionFlow? flow = null;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            for (int index = start + 2; index + 6 < end; index++)
            {
                if (body[index] is not
                    {
                        Definition.Opcode: Allocate,
                        Operands: [{ Value.Sign: > 0 } width, { Value.Sign: > 0 } live]
                    }
                    || live.Value >= FirstReservedXRegister - 1
                    || body[index - 1].Definition.Opcode != Label
                    || !EndsBeamBlock(body[index - 2].Definition.Opcode))
                {
                    continue;
                }

                int outerStart = index + 1;
                var saved = new Dictionary<int, BeamOperand>();
                for (; outerStart < end; outerStart++)
                {
                    var instruction = body[outerStart];
                    if (instruction is
                        {
                            Definition.Opcode: Move,
                            Operands: [
                            { Kind: BeamOperandParameters.X, TypeIndex: null } from,
                            { Kind: BeamOperandParameters.Y, TypeIndex: null } to
                            ]
                        } && from.Value < live.Value && to.Value < width.Value)
                    {
                        saved[(int)to.Value] = from;
                    }
                    else if (instruction.Definition.Opcode == InitYRegisters)
                    {
                        foreach (BeamOperand slot in instruction.Operands.Skip(1))
                        {
                            saved.Remove((int)slot.Value);
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if (ReadNestedArmCheck(body, outerStart, end) is not { } outer
                    || !saved.TryGetValue((int)outer.Input.Value, out var original))
                {
                    continue;
                }

                int innerStart = outer.End;
                var projections = new Dictionary<int, int>();
                for (; innerStart < end; innerStart++)
                {
                    if (body[innerStart] is not
                        {
                            Definition.Opcode: GetTupleElement,
                            Operands: [
                                var carrier, { Kind: BeamOperandParameters.Unsigned } field,
                            { Kind: BeamOperandParameters.Y, TypeIndex: null } destination
                            ]
                        } || carrier != outer.Input || field.Value >= outer.Arity || destination.Value >= width.Value
                        || saved.ContainsKey((int)destination.Value)
                        || !projections.TryAdd((int)destination.Value, innerStart))
                    {
                        break;
                    }
                }

                if (ReadNestedArmCheck(body, innerStart, end) is not { } inner || outer.Failure != inner.Failure
                    || !projections.TryGetValue((int)inner.Input.Value, out int selected)
                    || ReadDisjointFrameOwnership(body, start, end) is not { } ownership || ownership.Uses.Count != 1
                    || ownership.Owners[outer.First - start] != index
                    || ownership.Owners[inner.First - start] != index
                    || !(flow ??= new BeamProjectionFlow(body)).TryGetNestedArmLiteralReturn(
                        outer,
                        inner,
                        (int)width.Value,
                        end,
                        out int release
                    ))
                {
                    continue;
                }

                // The tests consume one projected value. Leave the other
                // fields in their original destinations on the successful edge.
                var temporary = new BeamOperand(BeamOperandParameters.X, live.Value);
                output.Write(code.Span[copied..body[index].Offset]);
                WriteCheck(outer, original);
                output.WriteByte(GetTupleElement);
                WriteRegister(output, original);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, body[selected].Operands[1].Value);
                WriteRegister(output, temporary);
                WriteCheck(inner, temporary);
                output.WriteByte(Allocate);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, width.Value);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live.Value + 1);
                output.Write(code.Span[body[index + 1].Offset..body[outer.First].Offset]);
                output.WriteByte(Move);
                WriteRegister(output, temporary);
                WriteRegister(output, inner.Input);
                for (int projection = outer.End; projection < inner.First; projection++)
                {
                    if (projection != selected)
                    {
                        output.Write(code.Span[body[projection].Offset..body[projection + 1].Offset]);
                    }
                }

                output.Write(code.Span[body[inner.End].Offset..body[release].Offset]);
                copied = body[release + 1].Offset;
                break;
            }

            start = end + 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void WriteCheck(NestedArmCheck check, BeamOperand register)
        {
            for (int index = check.First; index < check.End; index++)
            {
                var instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                output.WriteByte(opcode);
                BeamCompactWriter.Write(output, LabelTag, check.Failure);
                WriteRegister(output, register);
                if (opcode != IsTuple)
                {
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, check.Arity);
                }

                if (opcode == IsTaggedTuple)
                {
                    BeamCompactWriter.Write(output, AtomTag, instruction.Operands[3].Value);
                }
            }
        }
    }

    private static NestedArmCheck? ReadNestedArmCheck(IReadOnlyList<BeamInstruction> body, int first, int end)
    {
        if (first >= end)
        {
            return null;
        }

        var instruction = body[first];
        if (instruction is
            {
                Definition.Opcode: IsTaggedTuple,
                Operands: [
                { Kind: BeamOperandParameters.Label, Value.Sign: > 0 } taggedFailure,
                { Kind: BeamOperandParameters.Y, TypeIndex: null } taggedInput,
                { Kind: BeamOperandParameters.Unsigned } taggedWidth,
                { Kind: BeamOperandParameters.Atom }
                ]
            })
        {
            return new(
                first,
                first + 1,
                taggedInput,
                (int)taggedWidth.Value,
                (int)taggedFailure.Value
            );
        }

        if (first + 1 < end && instruction is
            {
                Definition.Opcode: IsTuple,
                Operands: [
                { Kind: BeamOperandParameters.Label, Value.Sign: > 0 } failure,
                { Kind: BeamOperandParameters.Y, TypeIndex: null } input
                ]
            } && body[first + 1] is
            {
                Definition.Opcode: TestArity,
                Operands: [var arityFailure, var subject, { Kind: BeamOperandParameters.Unsigned } width]
            } && arityFailure == failure && subject == input)
        {
            return new(
                first,
                first + 2,
                input,
                (int)width.Value,
                (int)failure.Value
            );
        }

        return null;
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool TryGetNestedArmLiteralReturn(
            NestedArmCheck outer,
            NestedArmCheck inner,
            int width,
            int end,
            out int release
        )
        {
            release = -1;
            if (!labels.TryGetValue(outer.Failure, out int label) || label < inner.End || label + 3 >= end)
            {
                return false;
            }

            int move = label + 1;
            while (move < end && body[move].Definition.Opcode == Line)
            {
                move++;
            }

            if (move + 2 >= end || body[move] is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    {
                        Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer
                            or BeamOperandParameters.Nil or BeamOperandParameters.LiteralValue
                    },
                        var destination
                    ]
                } || !IsBareX0(destination)
                || body[move + 1] is not { Definition.Opcode: Deallocate, Operands: [var released] }
                || released.Value != width || body[move + 2].Definition.Opcode != Return
                || predecessors[label].Count != outer.End - outer.First + inner.End - inner.First
                || predecessors[label].Any(index =>
                    (index < outer.First || index >= outer.End) && (index < inner.First || index >= inner.End)))
            {
                return false;
            }

            release = move + 1;

            return true;
        }
    }
}
