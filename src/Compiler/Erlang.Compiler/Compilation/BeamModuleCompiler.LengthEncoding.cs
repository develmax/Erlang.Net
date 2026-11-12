using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record LengthEncoding(Expr.Call Octets, Expr.Call Length, Expr.Binary Header, Expr.Binary Total,
        Expr.Call Exit, Atom Outer, Atom Inner, Atom Reason);

    private sealed record TaggedLengthEncoding(Expr.Call Length, Expr.Call Size, Expr.Binary Sum);
    private sealed record OctetStep(Expr.Binary Shift, Expr.Binary Mask, Expr.Call Recurse);

    private static OctetStep? ReadOctetAccumulator(FunctionDefinition function)
    {
        if (function.Clauses is not [
                { Guard: null, Patterns: [Pattern.Literal { Value: Integer zero }, Pattern.Variable result],
                  Body: Expr.Variable returned }, var step]
            || zero.Value != 0 || result.Name == VariableScopeNames.Wildcard || returned.Name != result.Name)
        {
            return null;
        }

        return ReadOctetStep(step, function.Name);
    }

    private static OctetStep? ReadOctetStep(Clause clause, string function)
    {
        if (clause is not
                { Guard: null, Patterns: [Pattern.Variable value, Pattern.Variable accumulator],
                  Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [
                      Expr.Binary { Operator: ErlangOperators.ShiftRight, Left: Expr.Variable shifted,
                          Right: Expr.Literal { Value: Integer width } } shift,
                      Expr.List { Items: [Expr.Binary { Operator: ErlangOperators.BitwiseAnd, Left: Expr.Variable masked,
                          Right: Expr.Literal { Value: Integer mask } } band], Tail: Expr.Variable tail }] } recurse }
            || !DistinctBindings(value, accumulator) || shifted.Name != value.Name || masked.Name != value.Name
            || tail.Name != accumulator.Name || recurse.Function != function
            || width.Value != BitSyntaxDefaults.BinaryUnit || mask.Value != byte.MaxValue)
        {
            return null;
        }

        return new(shift, band, recurse);
    }

    private static (OctetStep Step, bool Negative)? ReadSignedOctetAccumulator(FunctionDefinition function)
    {
        if (function.Clauses is not [
                { Patterns: [Pattern.Literal { Value: Integer stop }, AliasPattern { Left: Pattern.List
                    { Items: [Pattern.Variable head], Tail: Pattern.Variable tail }, Right: Pattern.Variable accumulator }],
                  Guard: Expr.Binary { Left: Expr.Variable tested, Right: Expr.Literal { Value: Integer limit } } guard,
                  Body: Expr.Variable returned }, var step]
            || !DistinctBindings(head, tail, accumulator) || tested.Name != head.Name || returned.Name != accumulator.Name
            || ReadOctetStep(step, function.Name) is not { } octets)
        {
            return null;
        }

        if (stop.Value == 0 && limit.Value == 128 && guard.Operator == ErlangOperators.Less)
        {
            return (octets, false);
        }
        return stop.Value == -1 && limit.Value == 127 && guard.Operator == ErlangOperators.Greater
            ? (octets, true) : null;
    }

    private void EmitSignedOctetAccumulator(
        OctetStep step, bool negative, bool byteAccumulator, int type, int entry, BeamSourceLocation location
    )
    {
        int advance = nextLabel++;
        Op(ExactEqualTest, LabelTag, advance);
        long? lower = type != 0 && !negative ? 0 : null;
        long? upper = type != 0 && negative ? type == IntegerTypeBits ? -1 : 0 : null;
        EmitOctetValue(type, lower, upper);
        Operand(BeamWire.IntegerTag, negative ? -1 : 0);
        EmitNonemptyList(1, advance);
        Op(GetHead, XRegisterTag, 1);
        Operand(XRegisterTag, 2);
        Op(byteAccumulator ? IsGreaterOrEqual : IsLess, LabelTag, advance);
        if (negative == byteAccumulator)
        {
            if (byteAccumulator)
            {
                EmitTypedRegister(2, IntegerTypeBits, byte.MinValue, byte.MaxValue);
            }
            else
            {
                Operand(XRegisterTag, 2);
            }
            Operand(BeamWire.IntegerTag, 128);
        }
        else
        {
            Operand(BeamWire.IntegerTag, 127);
            if (byteAccumulator)
            {
                EmitTypedRegister(2, IntegerTypeBits, byte.MinValue, byte.MaxValue);
            }
            else
            {
                Operand(XRegisterTag, 2);
            }
        }
        MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
        instructions.WriteByte(Return);
        EmitOctetStep(step, entry, advance, location, type, lower, upper);
    }

    private void EmitOctetAccumulator(OctetStep step, int entry, BeamSourceLocation location, long? lower, int type)
    {
        int advance = nextLabel++;
        long? entryLower = type is NumberTypeBits or AnyTypeBits ? 0 : lower;
        Op(ExactEqualTest, LabelTag, advance);
        EmitOctetValue(type, entryLower, null);
        Operand(BeamWire.IntegerTag, 0);
        MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
        instructions.WriteByte(Return);
        EmitOctetStep(step, entry, advance, location, type, lower.HasValue ? 1 : entryLower, null);
    }

    private void EmitOctetStep(
        OctetStep step, int entry, int advance, BeamSourceLocation location,
        int type, long? lower, long? upper
    )
    {
        Op(Label, BeamWire.UnsignedTag, advance);
        var previous = location;
        EmitArithmeticLine(step.Shift.SourceLine, ref previous);
        EmitOctetArithmetic(step.Shift.Operator, BitSyntaxDefaults.BinaryUnit, 2, 2, type, lower, upper);
        EmitArithmeticLine(step.Mask.SourceLine, ref previous);
        // The preceding shift can only succeed for an integer input.
        EmitOctetArithmetic(
            step.Mask.Operator,
            byte.MaxValue,
            3,
            0,
            IntegerTypeBits,
            lower,
            upper
        );
        Op(TestHeap, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, 3);
        Op(PutList, XRegisterTag, 0);
        Operand(XRegisterTag, 1);
        Operand(XRegisterTag, 1);
        MoveRegister(XRegisterTag, 2, XRegisterTag, 0);
        Op(TailLocalCall, BeamWire.UnsignedTag, 2);
        Operand(LabelTag, entry);
    }

    private void EmitOctetArithmetic(
        string operation, int constant, int live, int destination, int type, long? lower, long? upper
    )
    {
        Op(GcBifTwo, LabelTag, 0);
        Operand(BeamWire.UnsignedTag, live);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, operation, 2));
        EmitOctetValue(type, lower, upper);
        Operand(BeamWire.IntegerTag, constant);
        Operand(XRegisterTag, destination);
    }

    private void EmitOctetValue(int type, long? lower, long? upper)
    {
        if (type != 0)
        {
            EmitTypedRegister(0, type, lower, upper);
        }
        else
        {
            Operand(XRegisterTag, 0);
        }
    }

    private static TaggedLengthEncoding? ReadTaggedLengthEncoding(FunctionDefinition function)
    {
        if (function.Clauses is not [
                { Guard: null, Patterns: [Pattern.List { Items: [Pattern.Variable tag], Tail: Pattern.Variable rest },
                    Pattern.Variable bytes, Pattern.Variable length], Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.Tuple { Items: [Pattern.Variable header, Pattern.Variable count] },
                        Value: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable measured] } encode },
                    Expr.Call { Module: null, DynamicRemote: false, Arguments: [
                        Expr.Variable next, Expr.List { Items: [Expr.Variable prefix, Expr.Variable encoded],
                            Tail: Expr.Variable payload },
                        Expr.Binary { Operator: ErlangOperators.Plus, Left: Expr.Binary
                            { Operator: ErlangOperators.Plus, Left: Expr.Variable current, Right: Expr.Call
                                { Module: null or ErlangModuleNames.Module, DynamicRemote: false, Function: "byte_size",
                                  Arguments: [Expr.Variable sized] } size },
                            Right: Expr.Variable added } sum] } recur] } },
                { Guard: null, Patterns: [var nil, Pattern.Variable finalBytes, Pattern.Variable finalLength],
                  Body: Expr.Tuple { Items: [Expr.Variable returnedBytes, Expr.Variable returnedLength] } }]
            || !DistinctBindings(tag, rest, bytes, length, header, count) || !DistinctBindings(finalBytes, finalLength)
            || measured.Name != length.Name || current.Name != length.Name || sized.Name != tag.Name
            || prefix.Name != tag.Name || encoded.Name != header.Name || added.Name != count.Name
            || payload.Name != bytes.Name || next.Name != rest.Name || recur.Function != function.Name
            || !IsEmptyListPattern(nil) || returnedBytes.Name != finalBytes.Name || returnedLength.Name != finalLength.Name)
        {
            return null;
        }

        return new(encode, size, sum);
    }

    private void EmitTaggedLengthEncoding(
        TaggedLengthEncoding flow,
        int entry,
        int length,
        BeamSourceLocation location,
        long? lower,
        bool numeric
    )
    {
        int functionInfo = nextLabel - 2;
        int empty = nextLabel++;
        EmitNonemptyList(0, empty);
        Op(Allocate, BeamWire.UnsignedTag, 4);
        Operand(BeamWire.UnsignedTag, 3);
        MoveRegister(XRegisterTag, 2, YRegisterTag, 2);
        MoveRegister(XRegisterTag, 1, YRegisterTag, 3);
        EmitListParts(0, YRegisterTag, 1, YRegisterTag, 0);
        MoveRegister(XRegisterTag, 2, XRegisterTag, 0);
        EmitCallLine(flow.Length, location);
        EmitFieldCall(1, length);
        var previous = CallLocation(flow.Length, location);
        EmitCallLine(flow.Size, previous);
        previous = CallLocation(flow.Size, previous);
        Op(GcBifOne, LabelTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, flow.Size.Function, 1));
        Operand(YRegisterTag, 1);
        Operand(XRegisterTag, 1);

        int add = Import(ErlangModuleNames.Module, ErlangOperators.Plus, 2);
        EmitArithmeticLine(flow.Sum.SourceLine, ref previous);
        Op(GcBifTwo, LabelTag, 0);
        Operand(BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, add);
        if (lower.HasValue)
        {
            EmitTypedRegister(2, IntegerTypeBits, lower, registerTag: YRegisterTag);
        }
        else if (numeric)
        {
            EmitTypedRegister(2, NumberTypeBits, registerTag: YRegisterTag);
        }
        else
        {
            Operand(YRegisterTag, 2);
        }
        EmitTypedRegister(1, IntegerTypeBits, 0, MaximumBitSize);
        Operand(XRegisterTag, 1);
        EmitTuplePart(0, 1, 2);
        Op(GcBifTwo, LabelTag, 0);
        Operand(BeamWire.UnsignedTag, 3);
        Operand(BeamWire.UnsignedTag, add);
        if (lower.HasValue)
        {
            EmitTypedRegister(1, IntegerTypeBits, lower);
        }
        else
        {
            EmitTypedRegister(1, NumberTypeBits);
        }
        EmitTypedRegister(2, IntegerTypeBits, lower: 1);
        Operand(XRegisterTag, 1);
        Op(TestHeap, BeamWire.UnsignedTag, 4);
        Operand(BeamWire.UnsignedTag, 2);
        EmitTuplePart(0, 0, 0);
        Op(PutList, XRegisterTag, 0);
        Operand(YRegisterTag, 3);
        Operand(XRegisterTag, 0);
        Op(PutList, YRegisterTag, 1);
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 0);
        MoveRegister(XRegisterTag, 1, XRegisterTag, 2);
        MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
        MoveRegister(YRegisterTag, 0, XRegisterTag, 0);
        Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 3);
        Operand(LabelTag, entry);
        Operand(BeamWire.UnsignedTag, 4);

        Op(Label, BeamWire.UnsignedTag, empty);
        EmitNilTest(XRegisterTag, 0, functionInfo);
        Op(TestHeap, BeamWire.UnsignedTag, 3);
        Operand(BeamWire.UnsignedTag, 3);
        EmitTagMatchTuple(0, (XRegisterTag, 1), (XRegisterTag, 2));
        instructions.WriteByte(Return);
    }

    private static LengthEncoding? ReadLengthEncoding(FunctionDefinition function)
    {
        if (function.Clauses is not [
                { Patterns: [Pattern.Variable input],
                  Guard: Expr.Binary { Operator: ErlangOperators.LessOrEqual, Left: Expr.Variable tested,
                      Right: Expr.Literal { Value: Integer shortLimit } },
                  Body: Expr.Tuple { Items: [Expr.List { Items: [Expr.Variable single], Tail: null },
                      Expr.Literal { Value: Integer shortSize }] } },
                { Guard: null, Patterns: [Pattern.Variable longInput], Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.Variable octets, Value: Expr.Call
                        { Module: null, DynamicRemote: false, Arguments: [Expr.Variable measured] } octetCall },
                    Expr.Match { Pattern: Pattern.Variable length, Value: Expr.Call
                        { Module: null or ErlangModuleNames.Module, Function: "length", DynamicRemote: false,
                          Arguments: [Expr.Variable list] } lengthCall },
                    Expr.If { Clauses: [var accepted, var rejected] }] } }]
            || input.Name == VariableScopeNames.Wildcard || tested.Name != input.Name || single.Name != input.Name
            || shortLimit.Value != 127 || shortSize.Value != 1 || !DistinctBindings(longInput, octets, length)
            || measured.Name != longInput.Name || list.Name != octets.Name
            || accepted is not
            {
                Guard: Expr.Binary { Operator: ErlangOperators.LessOrEqual, Left: Expr.Variable bounded,
                    Right: Expr.Literal { Value: Integer limit } },
                Body: Expr.Tuple { Items: [
                    Expr.List { Items: [Expr.Binary { Operator: ErlangOperators.BitwiseOr,
                        Left: Expr.Literal { Value: Integer marker }, Right: Expr.Variable headerLength } header],
                        Tail: Expr.Variable tail },
                    Expr.Binary { Operator: ErlangOperators.Plus, Left: Expr.Variable size,
                        Right: Expr.Literal { Value: Integer increment } } total] }
            }
            || limit.Value != 126 || marker.Value != 128 || increment.Value != 1 || bounded.Name != length.Name
            || headerLength.Name != length.Name || size.Name != length.Name || tail.Name != octets.Name
            || rejected is not
            {
                Guard: Expr.Literal { Value: Atom { Name: "true" } },
                Body: Expr.Call { Module: null or ErlangModuleNames.Module, Function: "exit", DynamicRemote: false,
                    Arguments: [Expr.Tuple { Items: [Expr.Literal { Value: Atom outer },
                        Expr.Tuple { Items: [Expr.Literal { Value: Atom inner }, Expr.Literal { Value: Atom reason },
                            Expr.Variable observed] }] }] } exit
            }
            || observed.Name != length.Name)
        {
            return null;
        }

        return new(octetCall, lengthCall, header, total, exit, outer, inner, reason);
    }

    private void EmitLengthEncoding(LengthEncoding flow, int octets, BeamSourceLocation location, bool properOctets, long? lower)
    {
        int longForm = nextLabel++;
        int failure = nextLabel++;
        Op(IsGreaterOrEqual, LabelTag, longForm);
        Operand(BeamWire.IntegerTag, 127);
        EmitOctetValue(lower.HasValue ? IntegerTypeBits : 0, lower, null);
        Op(TestHeap, BeamWire.UnsignedTag, 5);
        Operand(BeamWire.UnsignedTag, 1);
        Op(PutList, XRegisterTag, 0);
        Operand(AtomTag, NilAtomIndex);
        Operand(XRegisterTag, 0);
        EmitTagMatchTuple(0, (XRegisterTag, 0), (BeamWire.IntegerTag, 1));
        instructions.WriteByte(Return);

        Op(Label, BeamWire.UnsignedTag, longForm);
        Op(Allocate, BeamWire.UnsignedTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        EmitCallLine(flow.Octets, location);
        EmitFieldCall(1, octets);
        var previous = CallLocation(flow.Octets, location);
        EmitCallLine(flow.Length, previous);
        previous = CallLocation(flow.Length, previous);
        Op(GcBifOne, LabelTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, flow.Length.Function, 1));
        if (properOctets)
        {
            const int listTypeBits = (1 << 2) | (1 << 7); // BEAM_TYPE_CONS | BEAM_TYPE_NIL
            EmitTypedRegister(0, listTypeBits);
        }
        else
        {
            Operand(XRegisterTag, 0);
        }
        Operand(XRegisterTag, 1);
        Op(IsGreaterOrEqual, LabelTag, failure);
        Operand(BeamWire.IntegerTag, 126);
        EmitTypedRegister(1, IntegerTypeBits, 0, MaximumBitSize);
        EmitArithmeticLine(flow.Header.SourceLine, ref previous);
        EmitLengthArithmetic(flow.Header.Operator, 128, 2, 2);
        EmitArithmeticLine(flow.Total.SourceLine, ref previous);
        EmitLengthArithmetic(flow.Total.Operator, 1, 3, 1);
        Op(TestHeap, BeamWire.UnsignedTag, 5);
        Operand(BeamWire.UnsignedTag, 3);
        Op(PutList, XRegisterTag, 2);
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 0);
        EmitTagMatchTuple(0, (XRegisterTag, 0), (XRegisterTag, 1));
        EmitFrameReturn();

        Op(Label, BeamWire.UnsignedTag, failure);
        Op(TestHeap, BeamWire.UnsignedTag, 7);
        Operand(BeamWire.UnsignedTag, 2);
        EmitTagMatchTuple(0, (AtomTag, Intern(flow.Inner.Name)), (AtomTag, Intern(flow.Reason.Name)), (XRegisterTag, 1));
        EmitProtectedPair(AtomTag, Intern(flow.Outer.Name));
        EmitLine(CallLocation(flow.Exit, previous));
        Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, flow.Exit.Function, 1));
        Operand(BeamWire.UnsignedTag, 0);
    }

    private void EmitLengthArithmetic(string operation, int constant, int live, int destination)
    {
        Op(GcBifTwo, LabelTag, 0);
        Operand(BeamWire.UnsignedTag, live);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, operation, 2));
        EmitTypedRegister(1, IntegerTypeBits, 0, 126);
        Operand(BeamWire.IntegerTag, constant);
        Operand(XRegisterTag, destination);
    }
}
