using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static void CollectIdentityMoves(IReadOnlyList<BeamInstruction> body, List<CodeEdit> edits)
    {
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (body[index] is { Definition.Opcode: Move, Operands: [var source, var destination] }
                && source.Kind is (BeamOperandParameters.X or BeamOperandParameters.Y)
                && source == destination)
            {
                edits.Add(new(
                    body[index].Offset,
                    body[index + 1].Offset,
                    0,
                    0
                ));
            }
        }
    }

    private Dictionary<int, BeamOperand> ReadSingletonCallResults(
        IReadOnlyList<FunctionDefinition> functions,
        IReadOnlyDictionary<(string Name, int Arity), int> localEntries
    )
    {
        var results = new Dictionary<int, BeamOperand>();
        if (ModuleOptimizationDisabled || OnLoadFunction is not null)
        {
            return results;
        }

        foreach (FunctionDefinition function in functions)
        {
            var signature = (function.Name, function.Arity);
            if (function.IsInlineFailure || IsDeclaredNative(signature)
                || !localEntries.TryGetValue(signature, out int entry))
            {
                continue;
            }

            BeamOperand? value = ReadSingletonResult(function) switch
            {
                Atom atom when atoms.TryGetValue(atom.Name, out int atomIndex) => new(BeamOperandParameters.Atom, atomIndex),
                Nil => new(BeamOperandParameters.Nil, 0),
                Integer integer => new(BeamOperandParameters.Integer, integer.Value),
                _ => null
            };
            if (value is not null && (entry > 0 || deferredEntries.TryGetValue(entry, out entry)))
            {
                results.Add(entry, value);
            }
        }

        return results;
    }

    private byte[]? RemoveProvenRegisterMoves(ReadOnlyMemory<byte> code, IReadOnlyDictionary<int, BeamOperand> returnValues)
    {
        IReadOnlyList<BeamInstruction> body;
        try
        {
            body = BeamInstructionReader.Read(
                new BeamCodeHeader(
                    BeamWire.FormatVersion,
                    highestOpcode,
                    nextLabel,
                    exports.Count + locals.Count,
                    code
                ),
                atoms.Count + 1,
                literalBytes.Count
            );
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        var incoming = new Dictionary<int, (BeamOperand Register, BeamOperand Value)?>();
        foreach (BeamInstruction instruction in body)
        {
            bool valueSelection = instruction.Definition.Opcode == SelectValue;
            for (int index = 0; index < instruction.Operands.Count; index++)
            {
                var target = instruction.Operands[index];
                if (target.Kind != BeamOperandParameters.Label)
                {
                    continue;
                }

                (BeamOperand Register, BeamOperand Value)? fact = null;
                if (valueSelection && index >= 4 && index % 2 == 0
                    && IsExactMoveConstant(instruction.Operands[index - 1]))
                {
                    fact = (Untyped(instruction.Operands[0]), instruction.Operands[index - 1]);
                }

                if (instruction is { Definition.Opcode: ExactNotEqualTest, Operands: [_, var tested, var constant] }
                    && IsSwapRegister(Untyped(tested)) && IsExactMoveConstant(constant))
                {
                    fact = (Untyped(tested), constant);
                }

                int label = (int)target.Value;
                if (incoming.TryGetValue(label, out var previous) && previous != fact)
                {
                    fact = null;
                }

                incoming[label] = fact;
            }
        }

        using var output = new MemoryStream();
        int copied = 0;
        bool removedLine = false;
        (BeamOperand Register, BeamOperand Value)? known = null;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            switch (instruction)
            {
                case { Definition.Opcode: ExactEqualTest, Operands: [_, var register, var value] }
                    when IsSwapRegister(Untyped(register)) && IsExactMoveConstant(value):
                    known = (Untyped(register), value);
                    break;
                case { Definition.Opcode: IsNil, Operands: [_, var register] }
                    when IsSwapRegister(Untyped(register)):
                    known = (Untyped(register), new BeamOperand(BeamOperandParameters.Nil, 0));
                    break;
                case { Definition.Opcode: Label, Operands: [var label] }:
                    known = index > 0 && EndsBeamBlock(body[index - 1].Definition.Opcode)
                        ? incoming.GetValueOrDefault((int)label.Value)
                        : null;
                    break;
                case { Definition.Opcode: Move, Operands: [var source, var destination] }:
                    if (known is { } fact && SameProjectionRegister(destination, fact.Register))
                    {
                        if (source == fact.Value)
                        {
                            output.Write(code.Span[copied..instruction.Offset]);
                            copied = body[index + 1].Offset;
                        }
                        else
                        {
                            known = null;
                        }
                    }

                    break;
                case { Definition.Opcode: LocalCall, Operands: [_, var entry] }
                    when returnValues.TryGetValue((int)entry.Value, out var result):
                    var returnRegister = new BeamOperand(BeamOperandParameters.X, 0);
                    int next = index + 1;
                    while (body[next] is { Definition.Opcode: Move, Operands: [var constant, var target] }
                        && constant == result && target == returnRegister)
                    {
                        next++;
                    }

                    if (next > index + 1 && body[next] is { Definition.Opcode: Deallocate }
                        && body[next + 1].Definition.Opcode == Return)
                    {
                        int callLine = index > 0 && body[index - 1].Definition.Opcode == Line ? index - 1 : index;
                        int allocation = body[next].Operands[0].Value.IsZero
                            ? FindEmptyEntryFrame(body, callLine) : -1;
                        if (allocation >= 0 && body[allocation].Offset >= copied)
                        {
                            output.Write(code.Span[copied..body[allocation].Offset]);
                            output.Write(code.Span[body[allocation + 1].Offset..body[callLine].Offset]);
                            output.WriteByte(TailLocalCall);
                            removedLine |= callLine != index;
                        }
                        else
                        {
                            output.Write(code.Span[copied..instruction.Offset]);
                            output.WriteByte(TailLocalCallWithFrame);
                        }

                        output.Write(code.Span[(instruction.Offset + 1)..body[index + 1].Offset]);
                        if (allocation < 0 || body[allocation].Offset < copied)
                        {
                            output.Write(code.Span[(body[next].Offset + 1)..body[next + 1].Offset]);
                        }
                        copied = body[next + 2].Offset;
                        index = next + 1;
                        known = null;
                        break;
                    }

                    if (next > index + 1 && body[next].Definition.Opcode == Return)
                    {
                        output.Write(code.Span[copied..instruction.Offset]);
                        output.WriteByte(TailLocalCall);
                        output.Write(code.Span[(instruction.Offset + 1)..body[index + 1].Offset]);
                        copied = body[next + 1].Offset;
                        index = next;
                        known = null;
                        break;
                    }

                    known = (returnRegister, result);
                    break;
                case { Definition.Opcode: Line }:
                    break;
                default:
                    known = null;
                    break;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        byte[] rewritten = output.ToArray();

        return removedLine ? RebuildProvenMoveLineTable(rewritten) : rewritten;

        static BeamOperand Untyped(BeamOperand operand) =>
            operand.TypeIndex is null ? operand : operand with { TypeIndex = null };

        static bool IsExactMoveConstant(BeamOperand operand) =>
            operand.Kind is BeamOperandParameters.Atom or BeamOperandParameters.Nil or BeamOperandParameters.Integer;
    }

    private static int FindEmptyEntryFrame(IReadOnlyList<BeamInstruction> body, int end)
    {
        int index = end - 1;
        while (index >= 0 && body[index].Definition.Opcode is Move or Swap
            && body[index].Operands.All(operand => operand.Kind != BeamOperandParameters.Y))
        {
            index--;
        }

        return index >= 2
            && body[index] is { Definition.Opcode: Allocate, Operands: [var slots, _] }
            && slots.Value.IsZero
            && body[index - 1].Definition.Opcode == Label
            && body[index - 2].Definition.Opcode == FunctionInfo ? index : -1;
    }

    private byte[] RebuildProvenMoveLineTable(byte[] code)
    {
        var header = new BeamCodeHeader(
            BeamWire.FormatVersion,
            highestOpcode,
            nextLabel,
            exports.Count + locals.Count,
            code
        );
        var body = BeamInstructionReader.Read(header, atoms.Count + 1, literalBytes.Count);
        var previous = lines.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        lines.Clear();
        filenames.Clear();
        lineInstructions = 0;

        using var output = new MemoryStream(code.Length);
        int copied = 0;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: Line, Operands: [var operand] } instruction)
            {
                continue;
            }

            lineInstructions++;
            if (operand.Value.IsZero)
            {
                continue;
            }

            var location = previous[(int)operand.Value - 1];
            if (location.File is not null)
            {
                filenames.TryAdd(location.File, filenames.Count + 1);
            }

            lines.TryAdd(location, lines.Count + 1);
            output.Write(code.AsSpan(copied, instruction.Offset + 1 - copied));
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, lines[location]);
            copied = body[index + 1].Offset;
        }

        output.Write(code.AsSpan(copied));

        return output.ToArray();
    }
}
