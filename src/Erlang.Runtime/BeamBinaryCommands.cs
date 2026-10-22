using System.Numerics;

namespace Erlang;

internal static class BeamBinaryCommands
{
    internal static IReadOnlyList<BeamBinaryCommand> Read(BeamStructure structure, IReadOnlyList<BeamOperand> operands)
    {
        Require(operands.Count >= BeamBinaryNames.MatchPrefix + BeamBinaryNames.MinimumMatchCommandOperands && operands[0].Kind == BeamOperandParameters.Label
            && Register(operands[1]) && operands[2].Kind == BeamOperandParameters.Count
            && operands[2].Value == operands.Count - 3);
        var commands = new List<BeamBinaryCommand>();
        for (int cursor = 3; cursor < operands.Count;)
        {
            string name = Atom(operands[cursor++]);
            if (name == BeamBinaryNames.EnsureAtLeast)
            {
                Need(2);
                int bits = Number(operands[cursor++]);
                int unit = Unit(operands[cursor++]);
                commands.Add(new(
                    BeamBinaryCommandKind.EnsureAtLeast,
                    -1,
                    bits,
                    unit,
                    BigInteger.Zero,
                    null
                ));
            }
            else if (name is BeamBinaryNames.EnsureExactly or BeamBinaryNames.SkipCommand)
            {
                Need(1);
                int bits = Number(operands[cursor++]);
                commands.Add(new(
                    name == BeamBinaryNames.EnsureExactly ? BeamBinaryCommandKind.EnsureExactly : BeamBinaryCommandKind.Skip,
                    -1,
                    bits,
                    1,
                    BigInteger.Zero,
                    null
                ));
            }
            else if (name == BeamBinaryNames.EqualCommand)
            {
                Need(3);
                Require(operands[cursor++].Kind == BeamOperandParameters.Nil);
                int bits = Number(operands[cursor++]);
                BeamOperand pattern = operands[cursor++];
                Require(pattern.Kind == BeamOperandParameters.Unsigned && pattern.Value >= 0
                    && bits <= BeamBinaryNames.MaximumEqualityBits && pattern.Value < (BigInteger.One << bits));
                BigInteger expected = pattern.Value;
                commands.Add(new(
                    BeamBinaryCommandKind.Equal,
                    -1,
                    bits,
                    1,
                    expected,
                    null
                ));
            }
            else if (name is BeamBinaryNames.IntegerSegment or BeamBinaryNames.BinarySegment)
            {
                Need(5);
                int live = Live(operands[cursor++]);
                BeamOperand flagOperand = operands[cursor++];
                int flags = name == BeamBinaryNames.IntegerSegment ? BeamBinaryWireValues.ListFlags(structure, flagOperand, true) : 0;
                Require(name == BeamBinaryNames.IntegerSegment || flagOperand.Kind == BeamOperandParameters.Nil);
                int size = Number(operands[cursor++]);
                int unit = Unit(operands[cursor++]);
                BeamOperand destination = operands[cursor++];
                bool binary = name == BeamBinaryNames.BinarySegment;
                BigInteger bits = (BigInteger)size * unit;
                Require(Register(destination) && bits <= (binary ? int.MaxValue : BeamBinaryNames.MaximumIntegerCommandBits));
                commands.Add(new(
                    binary ? BeamBinaryCommandKind.Binary : BeamBinaryCommandKind.Integer,
                    live,
                    (int)bits,
                    unit,
                    BigInteger.Zero,
                    destination,
                    flags
                ));
            }
            else if (name == BeamBinaryNames.GetTail)
            {
                Need(3);
                int live = Live(operands[cursor++]);
                int unit = Unit(operands[cursor++]);
                BeamOperand destination = operands[cursor++];
                Require(Register(destination));
                commands.Add(new(
                    BeamBinaryCommandKind.Tail,
                    live,
                    0,
                    unit,
                    BigInteger.Zero,
                    destination
                ));
            }
            else
                throw new NotSupportedException(BeamBinaryNames.Unsupported);

            void Need(int count) => Require(cursor + count <= operands.Count);
        }
        Require(commands.Count != 0);

        return commands;

        string Atom(BeamOperand operand)
        {
            Require(operand.Kind == BeamOperandParameters.Atom);

            return structure.Atoms[(int)operand.Value]!;
        }
    }

    private static int Number(BeamOperand value)
    {
        Require(value.Kind == BeamOperandParameters.Unsigned && value.Value >= 0 && value.Value <= int.MaxValue);

        return (int)value.Value;
    }

    private static int Live(BeamOperand value)
    {
        int live = Number(value);
        Require(live <= BeamOperandParameters.MaximumRegister);

        return live;
    }

    private static int Unit(BeamOperand value)
    {
        int unit = Number(value);
        Require(unit > 0 && unit <= BeamBinaryNames.MaximumUnit);

        return unit;
    }

    private static bool Register(BeamOperand value)
        => value.Kind is BeamOperandParameters.X or BeamOperandParameters.Y;

    private static void Require(bool value)
    {
        if (!value)
            throw new NotSupportedException(BeamBinaryNames.Unsupported);
    }
}
