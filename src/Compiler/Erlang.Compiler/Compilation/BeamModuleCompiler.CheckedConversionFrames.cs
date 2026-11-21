using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? SplitCheckedConversionFrames(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int functionInfo = 0; functionInfo + 4 < body.Count; functionInfo++)
        {
            if (body[functionInfo].Definition.Opcode != FunctionInfo
                || !body[functionInfo].Operands[^1].Value.IsOne)
            {
                continue;
            }

            int entry = functionInfo + 1;
            int allocation = entry + 1;
            if (body[entry] is not { Definition.Opcode: Label, Operands: [var entryLabel] }
                || body[allocation] is not
                {
                    Definition.Opcode: Allocate,
                    Operands: [{ Value.Sign: > 0 } width, { Value.IsOne: true }]
                })
            {
                continue;
            }

            int save = allocation + 1;
            if (body[save].Definition.Opcode == InitYRegisters)
            {
                save++;
            }

            int check = save + 1;
            if (body[save] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null },
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                }
                || body[check] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label } rejected, var tested, var arity, var tag]
                }
                || tested != saved || saved.Value >= width.Value
                || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(check, (int)rejected.Value, out int fallback))
            {
                continue;
            }

            int end = check + 1;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            int restore = fallback + 1;
            if (restore >= end || body[restore] is not { Definition.Opcode: Move, Operands: [var original, var x0] }
                || original != saved || !IsBareX0(x0)
                || body.Skip(check + 1).Take(end - check - 1)
                    .Any(instruction => instruction.Definition.Opcode is TryEnter or CatchEnter or DebugLine or ExecutableLine))
            {
                continue;
            }

            int call = SkipLine(restore + 1);
            if (call >= end || body[call] is not
                { Definition.Opcode: LocalCall, Operands: [{ Value.IsOne: true }, var conversion] }
                || conversion.Value == entryLabel.Value)
            {
                continue;
            }

            int tail = SkipLine(call + 1);
            int store = -1;
            if (tail < end && body[tail] is { Definition.Opcode: Move, Operands: [var result, var overwritten] }
                && result == x0 && overwritten == saved)
            {
                store = tail;
                tail = SkipLine(tail + 1);
            }

            if (tail >= end || body[tail] is not
                {
                    Definition.Opcode: TailLocalCallWithFrame,
                    Operands: [{ Value.IsOne: true }, var target, var deallocate]
                }
                || target.Value != entryLabel.Value || deallocate.Value != width.Value)
            {
                continue;
            }

            // A rejected tag reaches the converter with the original X0 and no live saved values.
            // The accepted path still owns its existing frame and all of its initialized slots.
            output.Write(code.Span[copied..body[allocation].Offset]);
            output.WriteByte(IsTaggedTuple);
            BeamCompactWriter.Write(output, LabelTag, rejected.Value);
            WriteRegister(output, x0);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, arity.Value);
            BeamCompactWriter.Write(output, AtomTag, tag.Value);
            output.Write(code.Span[body[allocation].Offset..body[check].Offset]);
            output.Write(code.Span[body[check + 1].Offset..body[restore].Offset]);

            output.WriteByte(Allocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            if (store >= 0)
            {
                output.Write(code.Span[body[restore + 1].Offset..body[store].Offset]);
                output.Write(code.Span[body[store + 1].Offset..body[tail].Offset]);
            }
            else
            {
                output.Write(code.Span[body[restore + 1].Offset..body[tail].Offset]);
            }
            output.WriteByte(TailLocalCallWithFrame);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            BeamCompactWriter.Write(output, LabelTag, entryLabel.Value);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
            copied = body[tail + 1].Offset;
            functionInfo = end - 1;

            int SkipLine(int index)
            {
                while (index < end && body[index].Definition.Opcode == Line)
                {
                    index++;
                }

                return index;
            }
        }

        if (copied == 0)
        {
            return null;
        }
        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
