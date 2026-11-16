using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const int IntegerTypeBits = 0x0020;
    private const int NumberTypeBits = 0x0028;
    private const int MatchableTypeBits = 0x0002;
    private const long MaximumBitSize = (1L << 58) - 1;
    private readonly HashSet<(string Name, int Arity)> numericWidthFunctions = [];

    private sealed record SignedWrite(Expr.Call Width, Expr.Bits Bits);

    private static SignedWrite? ReadSignedWriter(Clause clause)
    {
        if (clause is
            {
                Patterns: [Pattern.Variable input],
                Guard: Expr.Call { Module: null or ErlangModuleNames.Module, Function: "is_integer",
                    DynamicRemote: false, Arguments: [Expr.Variable guarded] },
                Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.Variable width, Value: Expr.Call { Module: null,
                        Arguments: [Expr.Variable value, Expr.Literal { Value: Integer initial }] } call },
                    Expr.Bits { Segments: [var segment] } bits
                ] }
            } && segment is
            {
                Value: Expr.Variable written, Size: Expr.Variable size, Type: BitSegmentTypes.Integer,
                Unit: 1, Endian: BitByteOrders.Big, Signed: true
            }
            && input.Name == guarded.Name && input.Name == value.Name && input.Name == written.Name
            && IsFresh(clause, width) && size.Name == width.Name && initial.Value == 8)
        {
            return new(call, bits);
        }

        return null;
    }

    private void EmitSignedWriter(
        SignedWrite flow,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        if (!entries.TryGetValue((flow.Width.Function, 2), out int width))
        {
            throw new NotSupportedException(Unsupported);
        }

        Op(IsInteger, LabelTag, nextLabel - 2);
        Operand(XRegisterTag, 0);
        Op(Allocate, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 1);
        MoveRegister(XRegisterTag, 0, YRegisterTag, 0);
        EmitConstant(Term.I(8), 1);
        EmitCallLine(flow.Width, location);
        Op(LocalCall, BeamWire.UnsignedTag, 2);
        Operand(LabelTag, width);

        var previous = CallLocation(flow.Width, location);
        EmitArithmeticLine(flow.Bits.SourceLine, ref previous);
        Op(CreateBinary, LabelTag, 0);
        Operand(BeamWire.UnsignedTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(XRegisterTag, 0);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 6);
        Operand(AtomTag, Intern("integer"));
        Operand(BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(AtomTag, NilAtomIndex);
        EmitTypedRegister(0, IntegerTypeBits, registerTag: YRegisterTag);
        if (numericWidthFunctions.Contains((flow.Width.Function, 2)))
        {
            EmitTypedRegister(0, NumberTypeBits);
        }
        else
        {
            Operand(XRegisterTag, 0);
        }
        EmitFrameReturn(1);
    }

    private sealed record SignedRead(Expr.Call Size, int MatchLine);

    private static SignedRead? ReadSignedInteger(Clause clause)
    {
        if (clause is
            {
                Guard: null, Patterns: [Pattern.Variable input],
                Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.Variable width, Value: Expr.Call { Module: null or ErlangModuleNames.Module,
                        DynamicRemote: false, Function: "bit_size", Arguments: [Expr.Variable bytes] } size },
                    Expr.Match { Pattern: BitPattern { Segments: [var segment] }, Value: Expr.Variable matched } match,
                    Expr.Variable returned
                ] }
            } && segment is
            {
                Value: Pattern.Variable value,
                Specification: { Type: BitSegmentTypes.Integer, Unit: 1, Endian: BitByteOrders.Big, Signed: true, Size: Expr.Variable bits }
            }
            && IsFresh(clause, width) && IsFresh(clause, value) && width.Name != value.Name
            && input.Name == bytes.Name && input.Name == matched.Name
            && width.Name == bits.Name && value.Name == returned.Name)
        {
            return new(size, match.SourceLine);
        }

        return null;
    }

    private void EmitSignedInteger(
        SignedRead read,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        if (read.Size.Module is null && entries.ContainsKey(("bit_size", 1)))
        {
            throw new NotSupportedException(Unsupported);
        }

        int failure = nextLabel++;
        EmitCallLine(read.Size, location);
        Op(GcBifOne, LabelTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, "bit_size", 1));
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 1);

        Op(StartBinaryMatchKnown, AtomTag, Intern("no_fail"));
        Operand(BeamWire.UnsignedTag, 2);
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 2);
        Op(GetBinaryInteger, LabelTag, failure);
        EmitTypedRegister(2, MatchableTypeBits);
        Operand(BeamWire.UnsignedTag, 3);
        EmitTypedRegister(1, IntegerTypeBits, 0, MaximumBitSize);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 4); // signed, big-endian
        Operand(XRegisterTag, 1);
        Op(BinaryMatch, LabelTag, failure);
        Operand(XRegisterTag, 2);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 2);
        Operand(AtomTag, Intern("ensure_exactly"));
        Operand(BeamWire.UnsignedTag, 0);
        MoveToX0(XRegisterTag, 1);
        instructions.WriteByte(Return);

        Op(Label, BeamWire.UnsignedTag, failure);
        var previous = CallLocation(read.Size, location);
        EmitArithmeticLine(read.MatchLine, ref previous);
        Op(BadMatch, XRegisterTag, 0);
    }

    private sealed record SignedWidth(Expr.Binary Shift, Expr.Binary Difference, Expr.Unary Negate, Expr.Binary Next, Expr.Call Recur);

    private static SignedWidth? ReadSignedWidth(Clause clause)
    {
        if (clause is
            {
                Guard: null, Patterns: [Pattern.Variable value, Pattern.Variable width],
                Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.Variable upper, Value: Expr.Binary { Operator: ErlangOperators.ShiftLeft,
                        Left: Expr.Literal { Value: Integer one }, Right: Expr.Binary { Operator: ErlangOperators.Minus,
                            Left: Expr.Variable shiftWidth, Right: Expr.Literal { Value: Integer subtract } } difference } shift },
                    Expr.Case { Value: Expr.Binary { Operator: ErlangOperators.AndAlso,
                        Left: Expr.Binary { Operator: ">=", Left: Expr.Variable checked1, Right: Expr.Unary
                            { Operator: ErlangOperators.Minus, Operand: Expr.Variable negative } negate },
                        Right: Expr.Binary { Operator: "<", Left: Expr.Variable checked2, Right: Expr.Variable positive } },
                        Clauses: [
                            { Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "true" } }], Body: Expr.Variable result },
                            { Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "false" } }],
                                Body: Expr.Call { Module: null, Arguments: [Expr.Variable passed, Expr.Binary { Operator: ErlangOperators.Plus,
                                    Left: Expr.Variable oldWidth, Right: Expr.Literal { Value: Integer step } } next] } recur }
                        ] }
                ] }
            } && HasUnconditionalParameters(clause) && IsFresh(clause, upper)
            && one.Value == 1 && subtract.Value == 1 && step.Value == 8
            && width.Name == shiftWidth.Name && width.Name == result.Name && width.Name == oldWidth.Name
            && value.Name == checked1.Name && value.Name == checked2.Name && value.Name == passed.Name
            && upper.Name == negative.Name && upper.Name == positive.Name)
        {
            return new(shift, difference, negate, next, recur);
        }

        return null;
    }

    private void EmitSignedWidth(
        SignedWidth flow,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        if (!entries.TryGetValue((flow.Recur.Function, 2), out int recur))
        {
            throw new NotSupportedException(Unsupported);
        }

        int advance = nextLabel++;
        var arithmetic = location;
        EmitArithmeticLine(flow.Difference.SourceLine, ref arithmetic);
        GcArithmetic(GcBifTwo, "-", 2, 2, (XRegisterTag, 1, 0), (BeamWire.IntegerTag, 1, 0));
        EmitArithmeticLine(flow.Shift.SourceLine, ref arithmetic);
        GcArithmetic(GcBifTwo, "bsl", 3, 2, (BeamWire.IntegerTag, 1, 0), (XRegisterTag, 2, NumberTypeBits));
        EmitArithmeticLine(flow.Negate.SourceLine, ref arithmetic);
        GcArithmetic(GcBifOne, "-", 3, 3, (XRegisterTag, 2, IntegerTypeBits));
        Op(IsGreaterOrEqual, LabelTag, advance);
        Operand(XRegisterTag, 0);
        EmitTypedRegister(3, IntegerTypeBits);
        Op(IsLess, LabelTag, advance);
        Operand(XRegisterTag, 0);
        EmitTypedRegister(2, IntegerTypeBits);
        MoveToX0(XRegisterTag, 1);
        instructions.WriteByte(Return);
        Op(Label, BeamWire.UnsignedTag, advance);
        EmitArithmeticLine(flow.Next.SourceLine, ref arithmetic);
        GcArithmetic(GcBifTwo, "+", 2, 1, (XRegisterTag, 1, NumberTypeBits), (BeamWire.IntegerTag, 8, 0));
        Op(TailLocalCall, BeamWire.UnsignedTag, 2);
        Operand(LabelTag, recur);
    }

    private void EmitArithmeticLine(int line, ref BeamSourceLocation previous)
    {
        if (line != 0 && line != previous.Line)
        {
            previous = new(previous.File, line);
            EmitLine(previous);
        }
    }

    private void GcArithmetic(byte opcode, string function, int live, int destination, params (int Tag, int Value, int Type)[] operands)
    {
        Op(opcode, LabelTag, 0);
        Operand(BeamWire.UnsignedTag, live);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, function, operands.Length));
        foreach (var (tag, value, type) in operands)
        {
            if (type == 0)
            {
                Operand(tag, value);
            }
            else
            {
                EmitTypedRegister(value, type);
            }
        }
        Operand(XRegisterTag, destination);
    }
}
