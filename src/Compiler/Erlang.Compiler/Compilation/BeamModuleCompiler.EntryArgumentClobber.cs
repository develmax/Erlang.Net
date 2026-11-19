using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? SinkEntryArgumentClobbers(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
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
            if (ReadClobber(start, end) is not { } plan)
            {
                continue;
            }

            int save = body[plan.Save].Offset;
            int clobber = body[plan.Clobber].Offset;
            int clobberEnd = body[plan.Clobber + 1].Offset;
            int reload = body[plan.Reload].Offset;
            output.Write(code.Span[copied..save]);
            output.WriteByte(InitYRegisters);
            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            WriteRegister(output, body[plan.Save].Operands[1]);
            output.Write(code.Span[body[plan.Save + 1].Offset..clobber]);
            output.Write(code.Span[clobberEnd..reload]);
            output.WriteByte(Move);
            WriteRegister(output, body[plan.Save].Operands[0]);
            WriteRegister(output, body[plan.Reload].Operands[1]);
            output.Write(code.Span[clobber..clobberEnd]);
            copied = body[plan.Reload + 1].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        (int Save, int Clobber, int Reload)? ReadClobber(int start, int end)
        {
            if (start + 2 >= end || body[start].Definition.Opcode != Label
                || body[start + 1] is not { Definition.Opcode: Allocate, Operands: [var slots, var live] })
            {
                return null;
            }

            int prep = start + 2;
            while (prep < end && body[prep] is
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null }, { Kind: BeamOperandParameters.Y }]
                })
            {
                prep++;
            }

            int reload = prep;
            while (reload < end && body[reload] is
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null }, { Kind: BeamOperandParameters.X, TypeIndex: null }]
                })
            {
                reload++;
            }

            if (reload <= prep || reload >= end || body[reload] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } slot, { Kind: BeamOperandParameters.X, TypeIndex: null } target]
                }
                || slot.Value >= slots.Value)
            {
                return null;
            }

            int call = reload + 1;
            while (call < end && body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (call >= end || body[call].Definition.Opcode != LocalCall || target.Value >= body[call].Operands[0].Value)
            {
                return null;
            }

            int save = -1;
            for (int index = start + 2; index < prep; index++)
            {
                if (SameProjectionRegister(body[index].Operands[1], slot))
                {
                    if (save >= 0)
                    {
                        return null;
                    }

                    save = index;
                }
            }

            if (save < 0)
            {
                return null;
            }

            BeamOperand source = body[save].Operands[0];
            if (source.Value >= live.Value || SameProjectionRegister(source, target))
            {
                return null;
            }

            int clobber = prep;
            while (clobber < reload && !SameProjectionRegister(body[clobber].Operands[1], source))
            {
                clobber++;
            }

            if (clobber + 1 >= reload)
            {
                return null;
            }

            BeamOperand input = body[clobber].Operands[0];
            if (input.Value >= live.Value || SameProjectionRegister(input, target) || SameProjectionRegister(input, source))
            {
                return null;
            }

            bool readsTarget = false;
            for (int index = clobber + 1; index < reload; index++)
            {
                var operands = body[index].Operands;
                if (operands.Any(operand => SameProjectionRegister(operand, source) || SameProjectionRegister(operand, input)))
                {
                    return null;
                }

                readsTarget |= SameProjectionRegister(operands[0], target);
            }

            if (!readsTarget)
            {
                return null;
            }

            // No remaining instruction observes the temporary, its stack index or
            // a handler/debug frame. The clobber is independent of the intervening
            // moves and can follow the replacement reload.
            for (int index = start + 2; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                if (instruction.Definition.Opcode is Allocate or AllocateHeap or TrimFrame or TryEnter or CatchEnter
                    || instruction.Definition.Name is "debug_line" or "executable_line"
                    || index != save && index != reload && instruction.Operands.Any(operand => SameProjectionRegister(operand, slot)))
                {
                    return null;
                }
            }

            return (save, clobber, reload);
        }
    }
}
