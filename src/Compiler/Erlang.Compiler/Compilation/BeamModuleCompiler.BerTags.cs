using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private readonly HashSet<string> resumableBerFunctions = new(StringComparer.Ordinal);

    private sealed record BerTag(Expr.Call Long, int ShortLine);
    private sealed record BerContinuation(Expr.Call Recur, int LastLine, int ContinueLine);

    private static BerTag? ReadBerTag(IReadOnlyList<Clause> clauses)
    {
        if (clauses is [
            { Guard: null, Patterns: [BitPattern { Segments: [var c1, var skip1, var marker, var tail] }],
                Body: Expr.Binary { Operator: ErlangOperators.BitwiseOr, Left: var prefix1,
                    Right: Expr.Call { Module: null, Arguments: [Expr.Variable remainder, Expr.Literal { Value: Integer zero }] } call } },
            { Guard: null, Patterns: [BitPattern { Segments: [var c2, var skip2, var tag] }],
                Body: Expr.Binary { Operator: ErlangOperators.BitwiseOr, Left: var prefix2, Right: Expr.Variable result } shortForm }
        ] && zero.Value == 0
            && IsShiftedField(c1, prefix1, 2, 16) && IsShiftedField(c2, prefix2, 2, 16)
            && IsFixedBits(skip1, 1) && skip1.Value is Pattern.Variable { Name: VariableScopeNames.Wildcard }
            && IsFixedBits(skip2, 1) && skip2.Value is Pattern.Variable { Name: VariableScopeNames.Wildcard }
            && IsFixedBits(marker, 5) && marker.Value is Pattern.Literal { Value: Integer { Value: var markerValue } } && markerValue == 31
            && IsBinaryTail(tail, remainder.Name) && IsFixedBits(tag, 5) && IsBoundField(tag, result.Name)
            && c1.Value is Pattern.Variable firstClass && firstClass.Name != remainder.Name
            && c2.Value is Pattern.Variable secondClass && secondClass.Name != result.Name)
        {
            return new(call, shortForm.SourceLine);
        }

        return null;
    }

    private static BerContinuation? ReadBerContinuation(IReadOnlyList<Clause> clauses)
    {
        if (clauses is [
            { Guard: null, Patterns: [BitPattern { Segments: [var more, var part1, var tail] }, Pattern.Variable acc1],
                Body: Expr.Call { Module: null, Arguments: [Expr.Variable remainder, Expr.Binary continued] } recur },
            { Guard: null, Patterns: [BitPattern { Segments: [var last, var part2] }, Pattern.Variable acc2],
                Body: Expr.Binary complete }
        ] && IsFixedBits(more, 1) && more.Value is Pattern.Literal { Value: Integer { Value: var moreValue } } && moreValue == 1
            && IsFixedBits(last, 1) && last.Value is Pattern.Literal { Value: Integer { Value: var lastValue } } && lastValue == 0
            && IsBinaryTail(tail, remainder.Name)
            && IsBerAccumulator(part1, acc1, continued) && IsBerAccumulator(part2, acc2, complete)
            && acc1.Name != remainder.Name && part1.Value is Pattern.Variable part && part.Name != remainder.Name)
        {
            return new(recur, complete.SourceLine, continued.SourceLine);
        }

        return null;
    }

    private static bool IsBerAccumulator(BitPatternSegment segment, Pattern.Variable accumulator, Expr.Binary expression) =>
        expression is
        {
            Operator: ErlangOperators.BitwiseOr,
            Left: Expr.Binary { Operator: ErlangOperators.ShiftLeft, Left: Expr.Variable previous,
                Right: Expr.Literal { Value: Integer shift } },
            Right: Expr.Variable part
        } && shift.Value == 7 && previous.Name == accumulator.Name && accumulator.Name != VariableScopeNames.Wildcard
        && IsFixedBits(segment, 7) && IsBoundField(segment, part.Name) && accumulator.Name != part.Name;

    private static bool IsShiftedField(BitPatternSegment segment, Expr expression, int width, int shift) =>
        expression is Expr.Binary { Operator: ErlangOperators.ShiftLeft, Left: Expr.Variable field,
            Right: Expr.Literal { Value: Integer count } }
        && count.Value == shift && IsFixedBits(segment, width) && IsBoundField(segment, field.Name);

    private static bool IsBoundField(BitPatternSegment segment, string name) =>
        segment.Value is Pattern.Variable variable && variable.Name == name && name != VariableScopeNames.Wildcard;

    private static bool IsFixedBits(BitPatternSegment segment, int width) =>
        segment.Specification is { Type: BitSegmentTypes.Integer, Unit: 1, Endian: BitByteOrders.Big,
            Signed: false, Size: Expr.Literal { Value: Integer size } } && size.Value == width;

    private static bool IsBinaryTail(BitPatternSegment segment, string name) =>
        segment.Specification is { Type: BitSegmentTypes.Binary, Unit: 8, Size: null }
        && IsBoundField(segment, name);

    private void EmitBerTag(BerTag tag, BeamSourceLocation location, IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        if (!entries.TryGetValue((tag.Long.Function, 2), out int longTag))
        {
            throw new NotSupportedException(Unsupported);
        }

        int functionInfo = nextLabel - 2;
        int shortTag = nextLabel++;
        int restore = nextLabel++;
        BeginBerMatch(functionInfo, 1, 1);
        BerMatch(restore, 1, 17);
        BerEnsure(8, 1);
        BerRead(2, 2, 2);
        Operand(AtomTag, Intern("skip"));
        Operand(BeamWire.UnsignedTag, 1);
        BerRead(3, 5, 3);

        Op(ExactEqualTest, LabelTag, shortTag);
        EmitTypedRegister(3, IntegerTypeBits, 0, 31);
        Operand(BeamWire.IntegerTag, 31);
        BerMatch(shortTag, 1, 3);
        BerEnsure(0, 8);
        Op(Allocate, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 3);
        InitStackSlot(0);
        BeginBerArithmetic("bsl", 3);
        EmitTypedRegister(2, IntegerTypeBits, 0, 3);
        Operand(BeamWire.IntegerTag, 16);
        Operand(YRegisterTag, 0);
        bool passContext = resumableBerFunctions.Contains(tag.Long.Function);
        MoveBerTailToArgument(1, 2, passContext);
        EmitConstant(Term.I(0), 1);
        Op(LocalCall, BeamWire.UnsignedTag, 2);
        Operand(LabelTag, longTag);
        BeginBerArithmetic("bor", 1);
        EmitTypedRegister(0, IntegerTypeBits, 0, 196608, YRegisterTag);
        if (passContext)
        {
            EmitTypedRegister(0, IntegerTypeBits);
        }
        else
        {
            Operand(XRegisterTag, 0);
        }
        Operand(XRegisterTag, 0);
        EmitFrameReturn(1);

        Op(Label, BeamWire.UnsignedTag, shortTag);
        BerMatch(restore, 1, 2);
        BerEnsure(0);
        EmitArithmeticLine(tag.ShortLine, ref location);
        BeginBerArithmetic("bsl", 4);
        EmitTypedRegister(2, IntegerTypeBits, 0, 3);
        Operand(BeamWire.IntegerTag, 16);
        Operand(XRegisterTag, 0);
        BeginBerArithmetic("bor", 4);
        EmitTypedRegister(0, IntegerTypeBits, 0, 196608);
        EmitTypedRegister(3, IntegerTypeBits, 0, 31);
        Operand(XRegisterTag, 0);
        instructions.WriteByte(Return);
        RestoreBerInput(restore, functionInfo, 1, 2);
    }

    private void EmitBerContinuation(BerContinuation flow, BeamSourceLocation location, IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        if (!entries.TryGetValue((flow.Recur.Function, 2), out int recur))
        {
            throw new NotSupportedException(Unsupported);
        }

        int functionInfo = nextLabel - 2;
        int more = nextLabel++;
        int restore = nextLabel++;
        BeginBerMatch(functionInfo, 2, 2);
        BerMatch(restore, 2, 9);
        BerEnsure(1, 1);
        BerRead(3, 1, 3);
        Op(ExactEqualTest, LabelTag, more);
        Operand(XRegisterTag, 3);
        Operand(BeamWire.IntegerTag, 0);
        BerMatch(restore, 2, 8);
        BerEnsure(7);
        BerRead(3, 7, 3);
        EmitArithmeticLine(flow.LastLine, ref location);
        EmitBerAccumulate(4, 0, 3);
        instructions.WriteByte(Return);

        Op(Label, BeamWire.UnsignedTag, more);
        BerMatch(restore, 2, 9);
        BerEnsure(7, 8);
        BerRead(3, 7, 0);
        EmitArithmeticLine(flow.ContinueLine, ref location);
        EmitBerAccumulate(3, 1, 0);
        MoveBerTailToArgument(2, 3, resumableBerFunctions.Contains(flow.Recur.Function));
        Op(TailLocalCall, BeamWire.UnsignedTag, 2);
        Operand(LabelTag, recur);
        RestoreBerInput(restore, functionInfo, 2, 3);
    }

    private void MoveBerTailToArgument(int context, int live, bool passContext)
    {
        if (passContext)
        {
            MoveRegister(XRegisterTag, context, XRegisterTag, 0);
        }
        else
        {
            Op(GetBinaryTail, XRegisterTag, context);
            Operand(XRegisterTag, 0);
            Operand(BeamWire.UnsignedTag, live);
        }
    }

    private void EmitBerAccumulate(int live, int destination, int part)
    {
        BeginBerArithmetic("bsl", live);
        Operand(XRegisterTag, 1);
        Operand(BeamWire.IntegerTag, 7);
        Operand(XRegisterTag, destination);
        BeginBerArithmetic("bor", live);
        EmitTypedRegister(destination, IntegerTypeBits);
        EmitTypedRegister(part, IntegerTypeBits, 0, 127);
        Operand(XRegisterTag, destination);
    }

    private void BeginBerArithmetic(string op, int live)
    {
        Op(GcBifTwo, LabelTag, 0);
        Operand(BeamWire.UnsignedTag, live);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, op, 2));
    }

    private void BeginBerMatch(int failure, int context, int live)
    {
        Op(StartBinaryMatch, LabelTag, failure);
        Operand(XRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, live);
        Operand(XRegisterTag, context);
        Op(GetBinaryPosition, XRegisterTag, context);
        Operand(XRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, live + 1);
    }

    private void RestoreBerInput(int restore, int failure, int context, int live)
    {
        Op(Label, BeamWire.UnsignedTag, restore);
        Op(SetBinaryPosition, XRegisterTag, context);
        Operand(XRegisterTag, 0);
        Op(GetBinaryTail, XRegisterTag, context);
        Operand(XRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, live);
        Op(Jump, LabelTag, failure);
    }

    private void BerMatch(int failure, int context, int count)
    {
        Op(BinaryMatch, LabelTag, failure);
        Operand(XRegisterTag, context);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, count);
    }

    private void BerEnsure(int bits, int? unit = null)
    {
        Operand(AtomTag, Intern(unit.HasValue ? "ensure_at_least" : "ensure_exactly"));
        Operand(BeamWire.UnsignedTag, bits);
        if (unit is int stride)
        {
            Operand(BeamWire.UnsignedTag, stride);
        }
    }

    private void BerRead(int live, int bits, int destination)
    {
        Operand(AtomTag, Intern("integer"));
        Operand(BeamWire.UnsignedTag, live);
        Operand(AtomTag, NilAtomIndex);
        Operand(BeamWire.UnsignedTag, bits);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(XRegisterTag, destination);
    }
}
