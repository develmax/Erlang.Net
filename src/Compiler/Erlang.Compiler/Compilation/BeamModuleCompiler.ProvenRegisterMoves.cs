using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RemoveProvenRegisterMoves(ReadOnlyMemory<byte> code)
    {
        IReadOnlyList<BeamInstruction> body;
        try
        {
            body = BeamInstructionReader.Read(
                new BeamCodeHeader(BeamWire.FormatVersion, highestOpcode, nextLabel, exports.Count + locals.Count, code),
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
        return output.ToArray();

        static BeamOperand Untyped(BeamOperand operand) =>
            operand.TypeIndex is null ? operand : operand with { TypeIndex = null };

        static bool IsExactMoveConstant(BeamOperand operand) =>
            operand.Kind is BeamOperandParameters.Atom or BeamOperandParameters.Nil or BeamOperandParameters.Integer;
    }
}
