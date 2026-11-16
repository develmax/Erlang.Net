using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? MoveTerminalErrorBlocks(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        int copied = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            byte boundary = body[end].Definition.Opcode;
            if (boundary is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            int tail = end;
            if (boundary == FunctionInfo)
            {
                while (tail > start && body[tail - 1].Definition.Opcode == Line)
                {
                    tail--;
                }

                if (tail == start || body[tail - 1].Definition.Opcode != Label)
                {
                    continue;
                }

                // Leave the next function's info label and line in place.
                tail--;
            }

            if (tail <= start || body[start].Definition.Opcode != Label
                || !(EndsBeamBlock(body[tail - 1].Definition.Opcode) || body[tail - 1].Definition.Opcode == IfEnd))
            {
                continue;
            }

            int line = start >= 2 && body[start - 2].Definition.Opcode == Line ? start - 2 : -1;
            var errors = new List<(int Label, int Resume, int Line)>();
            for (int index = start + 1; index < tail; index++)
            {
                if (body[index].Definition.Opcode == Line)
                {
                    line = index;
                    continue;
                }

                if (body[index].Definition.Opcode is TryEnter or CatchEnter)
                {
                    // Keep the layout of functions with exception handler frames.
                    errors.Clear();
                    break;
                }

                if (line < 0 || index + 3 >= tail
                    || body[index] is not { Definition.Opcode: Jump, Operands: [var target] }
                    || body[index + 1].Definition.Opcode != Label)
                {
                    continue;
                }

                int failure = index + 2;
                bool hasLine = body[failure].Definition.Opcode == Line;
                if (hasLine)
                {
                    failure++;
                }

                if (failure + 1 >= tail
                    || body[failure].Definition.Opcode is not (BadRecord or BadMatch or CaseEnd or IfEnd)
                    || body[failure + 1] is not { Definition.Opcode: Label, Operands: [var resume] }
                    || target.Value != resume.Value)
                {
                    continue;
                }

                errors.Add((index + 1, failure + 1, hasLine ? failure - 1 : line));
                index = failure;
            }

            if (errors.Count == 0)
            {
                continue;
            }

            foreach (var (label, resume, _) in errors)
            {
                output.Write(code.Span[copied..body[label - 1].Offset]);
                copied = body[resume].Offset;
            }

            output.Write(code.Span[copied..body[tail].Offset]);
            copied = body[tail].Offset;
            foreach (var (label, resume, originalLine) in errors)
            {
                output.Write(code.Span[body[label].Offset..body[label + 1].Offset]);
                bool hadLine = body[label + 1].Definition.Opcode == Line;
                bool needsLine = body[line].Operands[0].Value != body[originalLine].Operands[0].Value;
                if (needsLine)
                {
                    output.Write(code.Span[body[originalLine].Offset..body[originalLine + 1].Offset]);
                    line = originalLine;
                }

                lineInstructions += (needsLine ? 1 : 0) - (hadLine ? 1 : 0);
                int failure = label + (hadLine ? 2 : 1);
                output.Write(code.Span[body[failure].Offset..body[resume].Offset]);
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
