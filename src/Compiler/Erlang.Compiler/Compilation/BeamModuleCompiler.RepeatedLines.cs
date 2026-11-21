using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private ReadOnlyMemory<byte> RemoveRepeatedLines(ReadOnlyMemory<byte> code, int labelCount)
    {
        IReadOnlyList<BeamInstruction> body;
        try
        {
            var header = new BeamCodeHeader(
                BeamWire.FormatVersion,
                highestOpcode,
                labelCount,
                exports.Count + locals.Count,
                code
            );
            body = BeamInstructionReader.Read(header, atoms.Count + 1, literalBytes.Count);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return code;
        }

        // Debug and executable source markers have separate location references.
        bool hasExecutionMarkers = body.Any(instruction => instruction.Definition.Opcode is DebugLine or ExecutableLine);
        var safeImports = hasExecutionMarkers ? [] : ReadLocationNeutralExternalImports();
        var starts = new List<int>();
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (body[index].Definition.Opcode != FunctionInfo)
            {
                continue;
            }

            // The function's source location is emitted before its func_info.
            int start = index;
            while (start > 0 && body[start - 1].Definition.Opcode is Line or DebugLine or ExecutableLine)
            {
                start--;
            }

            starts.Add(start > 0 && body[start - 1].Definition.Opcode == Label ? start - 1 : start);
        }

        starts.Add(body.Count - 1);
        using var output = new MemoryStream();
        int copied = 0;
        int removed = 0;
        bool removedCallLine = false;
        for (int function = 0; function + 1 < starts.Count; function++)
        {
            int start = starts[function];
            int end = starts[function + 1];
            // Exception-handler functions can require a fresh position at a labeled continuation.
            bool hasHandler = body.Skip(start).Take(end - start)
                .Any(instruction => instruction.Definition.Opcode is TryEnter or CatchEnter);
            int? previousLine = null;
            for (int index = start; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                if (opcode is DebugLine or ExecutableLine || hasHandler && opcode is Label or FunctionInfo)
                {
                    previousLine = null;
                }
                else if (opcode == Line)
                {
                    int line = (int)instruction.Operands[0].Value;
                    // A local tail call discards its caller. OTP's call_line/3 omits that caller's location.
                    bool redundantCallLine = !hasHandler && !hasExecutionMarkers && index + 1 < end
                        && (body[index + 1].Definition.Opcode is TailLocalCall or TailLocalCallWithFrame
                            || safeImports.Count > 0 && CanDropSafeCallLine(body, index + 1, safeImports));
                    if (line == previousLine || redundantCallLine)
                    {
                        output.Write(code.Span[copied..instruction.Offset]);
                        copied = body[index + 1].Offset;
                        removed++;
                        removedCallLine |= redundantCallLine;
                    }

                    if (!redundantCallLine)
                    {
                        previousLine = line;
                    }
                }
            }
        }

        if (removed == 0)
        {
            return code;
        }

        output.Write(code.Span[copied..]);
        lineInstructions -= removed;

        byte[] rewritten = output.ToArray();

        return removedCallLine ? RebuildLineTable(rewritten, labelCount) : rewritten;
    }
}
