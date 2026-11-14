using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? TrimLinearStackFrames(ReadOnlyMemory<byte> code)
    {
        var stackOperands = new List<(int Start, int End, int Slot)>();
        IReadOnlyList<BeamInstruction> body;
        try
        {
            var header = new BeamCodeHeader(
                BeamWire.FormatVersion,
                highestOpcode,
                nextLabel,
                exports.Count + locals.Count,
                code
            );
            body = BeamInstructionReader.Read(
                header,
                atoms.Count + 1,
                literalBytes.Count,
                recordStackOperand: (start, end, slot) => stackOperands.Add((start, end, slot))
            );
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        MemoryStream? output = null;
        int copied = 0;
        int stackIndex = 0;
        bool hasExceptionHandler = false;
        for (int end = 0; end + 1 < body.Count; end++)
        {
            byte currentOpcode = body[end].Definition.Opcode;
            if (currentOpcode == FunctionInfo)
            {
                hasExceptionHandler = false;
            }
            else if (currentOpcode is CatchEnter or TryEnter)
            {
                hasExceptionHandler = true;
            }

            if (hasExceptionHandler || body[end] is not { Definition.Opcode: Deallocate, Operands: [var frame] }
                || body[end + 1].Definition.Opcode != Return)
            {
                continue;
            }

            // Match beam_trim's straight-line liveness window. Opcode-specific
            // control flow and existing frame changes terminate the window.
            int start = end - 1;
            var used = new HashSet<int>();
            bool hasCall = false;
            bool localCallFirst = false;
            int candidate = -1;
            int drop = 0;
            List<(int Source, int Destination)>? moves = null;
            for (; start >= 0; start--)
            {
                BeamInstruction instruction = body[start];
                byte opcode = instruction.Definition.Opcode;
                if (opcode is LocalCall or ExternalCall or DynamicApply or CallFun or CallFunTyped)
                {
                    hasCall = true;
                    localCallFirst = opcode != ExternalCall;
                }
                else if (opcode is not (Line or Move or Swap or InitYRegisters
                    or TestHeap or PutTuple or PutList or GetTupleElement
                    or GetList or GetHead or GetTail))
                {
                    break;
                }

                if (opcode == InitYRegisters && hasCall)
                {
                    var killed = instruction.Operands.Skip(1).Select(operand => (int)operand.Value).ToHashSet();
                    int last = (int)frame.Value - 1;
                    var relocations = new List<(int Source, int Destination)>();
                    for (int trim = 1; trim <= last + 1; trim++)
                    {
                        int slot = trim - 1;
                        if (used.Contains(slot))
                        {
                            while (last >= trim && used.Contains(last))
                            {
                                last--;
                            }

                            if (last < trim || killed.Contains(slot))
                            {
                                break;
                            }

                            relocations.Add((slot, last--));
                        }

                        var remainingKills = killed.Count(slot => slot >= trim
                            && relocations.All(move => move.Destination != slot));
                        int cost = remainingKills + relocations.Count;
                        if (!localCallFirst)
                        {
                            cost += relocations.Count == 0 ? 1 : 2;
                        }

                        if (cost <= killed.Count)
                        {
                            candidate = start;
                            drop = trim;
                            moves = [.. relocations];
                        }
                    }
                }

                // Any use or write to a slot in the suffix prevents removing it
                // without introducing a relocation or dropping an instruction.
                foreach (var operand in instruction.Operands)
                {
                    if (operand.Kind == BeamOperandParameters.Y)
                    {
                        used.Add((int)operand.Value);
                    }
                }
            }

            if (moves is null)
            {
                continue;
            }

            output ??= new MemoryStream(code.Length);
            output.Write(code.Span[copied..body[candidate].Offset]);
            var retained = body[candidate].Operands.Skip(1)
                .Where(slot => slot.Value >= drop && moves.All(move => move.Destination != slot.Value))
                .ToArray();
            if (retained.Length != 0)
            {
                output.WriteByte(InitYRegisters);
                output.WriteByte(ExtendedListOperand);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, retained.Length);
                foreach (var slot in retained)
                {
                    BeamCompactWriter.Write(output, YRegisterTag, slot.Value);
                }
            }

            foreach (var (source, destination) in moves)
            {
                output.WriteByte(Move);
                BeamCompactWriter.Write(output, YRegisterTag, source);
                BeamCompactWriter.Write(output, YRegisterTag, destination);
            }

            output.WriteByte(TrimFrame);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, drop);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, frame.Value - drop);

            copied = body[candidate + 1].Offset;
            while (stackIndex < stackOperands.Count && stackOperands[stackIndex].Start < copied)
            {
                stackIndex++;
            }

            while (stackIndex < stackOperands.Count && stackOperands[stackIndex].Start < body[end].Offset)
            {
                var (offset, operandEnd, slot) = stackOperands[stackIndex++];
                output.Write(code.Span[copied..offset]);
                int mapped = slot < drop ? moves.First(move => move.Source == slot).Destination : slot;
                BeamCompactWriter.Write(output, YRegisterTag, mapped - drop);
                copied = operandEnd;
            }

            output.Write(code.Span[copied..body[end].Offset]);
            output.WriteByte(Deallocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, frame.Value - drop);
            copied = body[end + 1].Offset;
        }

        if (output is null)
        {
            return null;
        }

        using (output)
        {
            output.Write(code.Span[copied..]);
            return output.ToArray();
        }
    }
}
