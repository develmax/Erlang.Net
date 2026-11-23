using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? BuildTupleInProtectedSendArgument(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(Send)
            || !code.Span.Contains(PutTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamOperand x0 = new(BeamOperandParameters.X, 0);
        BeamOperand x1 = new(BeamOperandParameters.X, 1);
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
            if (start + 10 >= end || body[start].Definition.Opcode != Label
                || body[start + 1] is not { Definition.Opcode: Allocate, Operands: [var width, _] }
                || body[start + 2] is not { Definition.Opcode: InitYRegisters } initialized)
            {
                continue;
            }

            for (int tuple = start + 4; tuple + 7 < end; tuple++)
            {
                if (body[tuple] is not { Definition.Opcode: PutTuple, Operands: [var build, var count, ..] } constructor
                    || build != x0 || body[tuple - 1] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsZero: true }] }
                    || ReadHeapWords(code, body[tuple - 1]) is not { Words: var reserved } || reserved != count.Value + 1
                    || constructor.Operands.Skip(2).Any(item => item.Kind is not
                        (BeamOperandParameters.Y or BeamOperandParameters.Atom or BeamOperandParameters.Integer
                         or BeamOperandParameters.LiteralValue or BeamOperandParameters.Nil))
                    || body[tuple + 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var produced, { Kind: BeamOperandParameters.Y, TypeIndex: null } message]
                    } || produced != x0
                    || body[tuple + 2] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } receiver, var target]
                    } || target != x0 || receiver == message || !IsMove(body[tuple + 3], message, x1)
                    || !initialized.Operands.Contains(message))
                {
                    continue;
                }

                int send = tuple + 4;
                while (send < end && body[send].Definition.Opcode == Line)
                {
                    send++;
                }

                if (send + 3 >= end || body[send].Definition.Opcode != Send
                    || body[send + 1].Definition.Opcode != TryEnd
                    || body[send + 2] is not { Definition.Opcode: Deallocate, Operands: [var released] }
                    || released != width || body[send + 3].Definition.Opcode != Return
                    || Enumerable.Range(start + 3, end - start - 3)
                        .Any(at => body[at].Definition.Opcode is Allocate or AllocateHeap or TrimFrame)
                    || Enumerable.Range(start + 3, end - start - 3)
                        .Where(at => body[at].Definition.Opcode != InitYRegisters)
                        .Sum(at => body[at].Operands.Count(item => SameProjectionRegister(item, message))) != 2)
                {
                    continue;
                }

                // The tuple has only literal and saved inputs. Construct it in
                // the final send argument without changing the handler or roots.
                int destination = body[tuple].Offset + 1;
                var cursor = new BeamCursor(code[destination..]);
                _ = BeamCompactReader.Read(cursor);
                output.Write(code.Span[copied..destination]);
                WriteRegister(output, x1);
                output.Write(code.Span[(destination + cursor.Position)..body[tuple + 1].Offset]);
                output.Write(code.Span[body[tuple + 2].Offset..body[tuple + 3].Offset]);
                copied = body[tuple + 4].Offset;
                tuple = send;
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
