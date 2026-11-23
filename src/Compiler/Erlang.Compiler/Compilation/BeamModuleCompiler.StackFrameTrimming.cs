using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? TrimLinearStackFrames(
        ReadOnlyMemory<byte> code,
        int labelCount,
        IReadOnlySet<int>? eligibleInitializations = null
    )
    {
        var stackOperands = new List<(int Start, int End, int Slot)>();
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

        var imported = imports.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        string[] atomNames = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();

        var safeFailures = new HashSet<int>();
        for (int index = body.Count - 2; index >= 0; index--)
        {
            if (body[index].Definition.Opcode != Label)
            {
                continue;
            }

            int next = index + 1;
            while (body[next].Definition.Opcode is Line or Move or Swap
                && body[next].Operands.All(operand => operand.Kind != BeamOperandParameters.Y))
            {
                next++;
            }

            BeamInstruction target = body[next];
            bool exits = target.Definition.Opcode is BadMatch or CaseEnd or TryCaseEnd or IfEnd or BadRecord;
            if (target.Definition.Opcode == ExternalCall)
            {
                var import = imported[(int)target.Operands[1].Value];
                exits = BeamTrimBifs.IsExit(atomNames[import.Module - 1], atomNames[import.Function - 1], import.Arity);
            }

            if (exits && target.Operands.All(operand => operand.Kind != BeamOperandParameters.Y))
            {
                safeFailures.Add((int)body[index].Operands[0].Value);
            }
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

            int frameOperand = currentOpcode switch
            {
                Deallocate when body[end + 1].Definition.Opcode == Return => 0,
                TailLocalCallWithFrame or TailExternalCallWithFrame => 2,
                TailDynamicApply => 1,
                _ => -1
            };
            if (hasExceptionHandler || frameOperand < 0)
            {
                continue;
            }

            BeamOperand frame = body[end].Operands[frameOperand];

            // The suffix may branch to errors that do not access the frame.
            int start = end - 1;
            var used = new HashSet<int>();
            var live = new HashSet<int>();
            bool hasCall = frameOperand != 0;
            bool useStrictCost = currentOpcode == TailExternalCallWithFrame
                && IsPureImport((int)body[end].Operands[1].Value);
            int candidate = -1;
            int drop = 0;
            List<(int Source, int Destination, bool Copy)>? moves = null;
            for (; start >= 0; start--)
            {
                BeamInstruction instruction = body[start];
                byte opcode = instruction.Definition.Opcode;
                if (opcode is LocalCall or ExternalCall or DynamicApply or CallFun or CallFunTyped)
                {
                    hasCall = true;
                    useStrictCost = false;
                    if (opcode == ExternalCall)
                    {
                        useStrictCost = IsPureImport((int)instruction.Operands[1].Value);
                    }
                }
                else if (opcode is not (Line or Move or Swap or InitYRegisters
                    or TestHeap or PutTuple or PutList or GetTupleElement
                    or GetList or GetHead or GetTail or BifZero or MakeFun or UpdateRecord)
                    && !IsSafeTrimTest(instruction, safeFailures))
                {
                    break;
                }

                if (opcode == InitYRegisters && hasCall
                    && (eligibleInitializations is null || eligibleInitializations.Contains(instruction.Offset)))
                {
                    var killed = instruction.Operands.Skip(1).Select(operand => (int)operand.Value).ToHashSet();
                    int last = (int)frame.Value - 1;
                    var relocations = new List<(int Source, int Destination, bool Copy)>();
                    for (int trim = 1; trim <= last + 1; trim++)
                    {
                        int slot = trim - 1;
                        if (used.Contains(slot))
                        {
                            while (last >= trim && (frameOperand == 0 ? used : live).Contains(last))
                            {
                                last--;
                            }

                            if (last < trim || killed.Contains(slot) || used.Contains(last))
                            {
                                break;
                            }

                            // The suffix still needs the slot number when its first use is a write.
                            relocations.Add((slot, last--, live.Contains(slot)));
                        }

                        var remainingKills = killed.Count(slot => slot >= trim
                            && relocations.All(move => !move.Copy || move.Destination != slot));
                        int cost = remainingKills + relocations.Count(move => move.Copy);
                        if (useStrictCost)
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

                // beam_trim looks through ordinary blocks and line instructions to the next call.
                // A guard test, a record update or another initialization ends that lookahead.
                if (opcode is InitYRegisters or UpdateRecord
                    || IsSafeTrimTest(instruction, safeFailures) && !IsTrimBlockBif(instruction))
                {
                    useStrictCost = false;
                }

                TrackTrimLiveness(instruction, live);

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
            bool resizeAllocation = moves.Count == 0 && candidate > 0
                && body[candidate - 1].Offset >= copied
                && body[candidate - 1].Definition.Opcode is (Allocate or AllocateHeap)
                && body[candidate - 1].Operands[0].Value == frame.Value;
            if (resizeAllocation)
            {
                int firstOperand = body[candidate - 1].Offset + 1;
                var cursor = new BeamCursor(code[firstOperand..]);
                _ = BeamCompactReader.Read(cursor);
                output.Write(code.Span[copied..firstOperand]);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, frame.Value - drop);
                output.Write(code.Span[(firstOperand + cursor.Position)..body[candidate].Offset]);
            }
            else
            {
                output.Write(code.Span[copied..body[candidate].Offset]);
            }
            int[] retained = body[candidate].Operands.Skip(1)
                .Where(slot => slot.Value >= drop && moves.All(move => !move.Copy || move.Destination != slot.Value))
                .Select(slot => (int)slot.Value)
                .Concat(moves.Where(move => !move.Copy).Select(move => move.Destination))
                .Distinct().Order().ToArray();
            if (retained.Length != 0)
            {
                output.WriteByte(InitYRegisters);
                output.WriteByte(ExtendedListOperand);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, retained.Length);
                foreach (var slot in retained)
                {
                    BeamCompactWriter.Write(output, YRegisterTag, resizeAllocation ? slot - drop : slot);
                }
            }

            foreach (var (source, destination, copy) in moves)
            {
                if (copy)
                {
                    output.WriteByte(Move);
                    BeamCompactWriter.Write(output, YRegisterTag, source);
                    BeamCompactWriter.Write(output, YRegisterTag, destination);
                }
            }

            if (!resizeAllocation)
            {
                output.WriteByte(TrimFrame);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, drop);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, frame.Value - drop);
            }

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

            int operandStart = body[end].Offset + 1;
            var exitOperands = new BeamCursor(code[operandStart..]);
            for (int operand = 0; operand < frameOperand; operand++)
            {
                _ = BeamCompactReader.Read(exitOperands);
            }

            output.Write(code.Span[copied..(operandStart + exitOperands.Position)]);
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

        bool IsPureImport(int index)
        {
            var import = imported[index];

            return BeamTrimBifs.IsPure(atomNames[import.Module - 1], atomNames[import.Function - 1], import.Arity);
        }
    }

    private static void TrackTrimLiveness(BeamInstruction instruction, HashSet<int> live)
    {
        var operands = instruction.Operands;
        int destination = instruction.Definition.Opcode switch
        {
            Move or GetHead or GetTail => 1,
            PutList or GetTupleElement => 2,
            PutTuple => 0,
            UpdateRecord => 4,
            BifZero or BifOne or BifTwo or BifThree or GcBifOne or GcBifTwo or GcBifThree => operands.Count - 1,
            _ => -1
        };

        if (destination >= 0 && operands[destination].Kind == BeamOperandParameters.Y)
        {
            live.Remove((int)operands[destination].Value);
        }

        if (instruction.Definition.Opcode == InitYRegisters)
        {
            live.ExceptWith(operands.Skip(1).Select(operand => (int)operand.Value));

            return;
        }

        for (int index = 0; index < operands.Count; index++)
        {
            if (index != destination && operands[index].Kind == BeamOperandParameters.Y)
            {
                live.Add((int)operands[index].Value);
            }
        }
    }

    private static bool IsTrimBlockBif(BeamInstruction instruction) =>
        instruction.Definition.Opcode is BifOne or BifTwo or BifThree or GcBifOne or GcBifTwo or GcBifThree
        && instruction.Operands[0].Kind == BeamOperandParameters.Failure;

    private static bool IsTrimTest(byte opcode) =>
        opcode is IsLess or IsGreaterOrEqual
            or NumericEqualTest or NumericNotEqualTest or ExactEqualTest or ExactNotEqualTest
            or IsInteger or IsFloat or IsNumber or IsAtom or IsPid or IsReference or IsPort
            or IsNil or IsBinary or IsBitString or IsList or IsNonemptyList or IsTuple or TestArity
            or IsFunction or IsBoolean or IsFunctionWithArity or IsMap or HasMapFields or GetMapElements
            or IsTaggedTuple or IsAnyNativeRecord or BifOne or BifTwo or BifThree
            or GcBifOne or GcBifTwo or GcBifThree;

    private static bool IsSafeTrimTest(BeamInstruction instruction, HashSet<int> safeFailures) =>
        IsTrimTest(instruction.Definition.Opcode)
        && (instruction.Operands[0].Kind == BeamOperandParameters.Failure
            || instruction.Operands[0].Kind == BeamOperandParameters.Label
                && safeFailures.Contains((int)instruction.Operands[0].Value));
}
