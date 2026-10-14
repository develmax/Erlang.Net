namespace Erlang;

internal static class BeamInstructionReader
{
    public static IReadOnlyList<BeamInstruction> Read(BeamStructure structure)
    {
        Require(structure.Code.MaximumOpcode >= BeamOperandParameters.MinimumCompatibleOpcode);
        Require(structure.Code.MaximumOpcode <= BeamOpcodeCatalog.All.Count);
        var cursor = new BeamCursor(structure.Code.Instructions);
        var result = new List<BeamInstruction>();
        bool terminated = false;
        while (cursor.Remaining != 0)
        {
            int offset = cursor.Position;
            int opcode = cursor.ReadByte();
            if (opcode == 0 || opcode > BeamOpcodeCatalog.All.Count)
            {
                throw new InvalidDataException(BeamOperandParameters.InvalidCode);
            }
            BeamOpcodeDefinition definition = BeamOpcodeCatalog.All[opcode - 1];
            int count = definition.Arity;
            var operands = new List<BeamOperand>();
            for (int index = 0; index < count; index++)
            {
                BeamCompactValue raw = BeamCompactReader.Read(cursor);
                if (raw.WireTag == BeamOperandParameters.ExtendedTag)
                {
                    int extension = Index(raw);
                    if (extension == BeamOperandParameters.ArgumentList)
                    {
                        if (index + 1 != count)
                        {
                            throw new InvalidDataException(BeamOperandParameters.InvalidCode);
                        }
                        int extra = Unsigned(cursor);
                        if (extra > BeamOperandParameters.MaximumManagedArguments - count)
                        {
                            throw new InvalidDataException(BeamOperandParameters.InvalidCode);
                        }
                        count += extra;
                        operands.Add(new(BeamOperandParameters.Count, extra));
                        continue;
                    }
                    if (extension == BeamOperandParameters.Literal)
                    {
                        int literal = Unsigned(cursor);
                        Require(literal < structure.Literals.Count);
                        operands.Add(new(BeamOperandParameters.LiteralValue, literal));
                        continue;
                    }
                    if (extension == BeamOperandParameters.FloatRegister)
                    {
                        int register = Unsigned(cursor);
                        Require(register < BeamOperandParameters.MaximumRegister);
                        operands.Add(new(BeamOperandParameters.Float, register));
                        continue;
                    }
                    if (extension == BeamOperandParameters.TypedRegister)
                    {
                        BeamCompactValue register = BeamCompactReader.Read(cursor);
                        Require(register.WireTag is BeamOperandParameters.XTag or BeamOperandParameters.YTag);
                        BeamOperand operand = Scalar(register, structure);
                        int type = Unsigned(cursor);
                        operands.Add(operand with { TypeIndex = type });
                        continue;
                    }
                    throw new NotSupportedException(BeamOperandParameters.Unsupported);
                }
                operands.Add(Scalar(raw, structure));
            }
            result.Add(new(offset, definition, operands.AsReadOnly()));
            if (opcode == BeamOperandParameters.EndOfCodeOpcode)
            {
                Require(cursor.Remaining == 0);
                terminated = true;
                break;
            }
        }

        Require(terminated);

        return result.AsReadOnly();
    }

    private static BeamOperand Scalar(BeamCompactValue raw, BeamStructure structure)
    {
        string kind;
        switch (raw.WireTag)
        {
            case BeamWire.IntegerTag:
                Require(BeamIntegerNormalization.IsNormalized(raw));
                kind = BeamOperandParameters.Integer;
                break;
            case BeamWire.UnsignedTag:
                _ = Index(raw);
                kind = BeamOperandParameters.Unsigned;
                break;
            case BeamOperandParameters.AtomTag:
                int atom = Index(raw);
                Require(atom < structure.Atoms.Count);
                kind = atom == 0 ? BeamOperandParameters.Nil : BeamOperandParameters.Atom;
                break;
            case BeamOperandParameters.LabelTag:
                int label = Index(raw);
                Require(label == 0 || label < structure.Code.LabelCount);
                kind = label == 0 ? BeamOperandParameters.Failure : BeamOperandParameters.Label;
                break;
            case BeamOperandParameters.XTag:
            case BeamOperandParameters.YTag:
                Require(Index(raw) < BeamOperandParameters.MaximumRegister);
                kind = raw.WireTag == BeamOperandParameters.XTag ? BeamOperandParameters.X : BeamOperandParameters.Y;
                break;
            default:
                throw new InvalidDataException(BeamOperandParameters.Unsupported);
        }

        return new(kind, raw.Value);
    }

    private static int Unsigned(BeamCursor cursor)
    {
        BeamCompactValue value = BeamCompactReader.Read(cursor);
        Require(value.WireTag == BeamWire.UnsignedTag);

        return Index(value);
    }

    private static int Index(BeamCompactValue value)
    {
        Require(value.FitsNativeWord && value.Value >= 0 && value.Value <= int.MaxValue);

        return (int)value.Value;
    }

    private static void Require(bool condition)
    {
        if (!condition)
        {
            throw new InvalidDataException(BeamOperandParameters.InvalidCode);
        }
    }
}
