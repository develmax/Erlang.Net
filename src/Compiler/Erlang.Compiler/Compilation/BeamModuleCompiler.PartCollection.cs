using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record PartCollection(Expr.Call Nested, Expr.Call Bits, Expr.Call Reverse, Expr.Call Finish, Integer BitTag);

    private sealed record BitPartCollection(Expr.Call Recur, Expr.Binary Sum, Expr.Call Reverse, Expr.Call Finish, Integer Tag);

    private static (Expr.Call Read, Expr.Call Collect, Expr.Call Guard, Expr.Case Decision)? ReadMatchAndCollect(
        FunctionDefinition function
    )
    {
        if (function.Clauses is not [{
                Guard: null, Patterns: [Pattern.Variable first, Pattern.Variable second],
                Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.Variable result, Value: Expr.Call
                        { Module: null, DynamicRemote: false, Arguments: [Expr.Variable left, Expr.Variable right] } read },
                    Expr.Case { Value: Expr.Variable input, Clauses: [
                        { Guard: null, Patterns: [AliasPattern { Left: Pattern.List
                            { Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                              Tail: Pattern.Variable { Name: VariableScopeNames.Wildcard } },
                            Right: Pattern.Variable parts }],
                          Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable collected] } collect },
                        { Patterns: [Pattern.Variable binary], Guard: Expr.Call
                            { Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                              Function: "is_binary", Arguments: [Expr.Variable tested] } guard,
                          Body: Expr.Variable returned }
                    ] } decision] }
            }]
            || !DistinctBindings(first, second, result, parts)
            || !DistinctBindings(first, second, result, binary)
            || left.Name != first.Name || right.Name != second.Name || input.Name != result.Name
            || collected.Name != parts.Name || tested.Name != binary.Name || returned.Name != binary.Name)
        {
            return null;
        }

        return (read, collect, guard, decision);
    }

    private void EmitMatchAndCollect(
        Expr.Call read,
        Expr.Case decision,
        int reader,
        int collector,
        BeamSourceLocation location
    )
    {
        int binary = nextLabel++;
        int failure = nextLabel++;
        Op(Allocate, BeamWire.UnsignedTag, 0);
        Operand(BeamWire.UnsignedTag, 2);
        EmitCallLine(read, location);
        EmitFieldCall(2, reader);
        EmitNonemptyList(0, binary);
        Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 1);
        Operand(LabelTag, collector);
        Operand(BeamWire.UnsignedTag, 0);
        Op(Label, BeamWire.UnsignedTag, binary);
        Op(IsBinary, LabelTag, failure);
        Operand(XRegisterTag, 0);
        EmitFrameReturn();
        Op(Label, BeamWire.UnsignedTag, failure);
        if (decision.SourceLine != 0)
        {
            EmitLine(new(decision.PreprocessorFile, decision.SourceLine));
        }
        Op(CaseEnd, XRegisterTag, 0);
    }

    private static BitPartCollection? ReadBitPartCollection(FunctionDefinition function)
    {
        if (function.Clauses is not [
                { Guard: null, Patterns: [Pattern.List { Items: [Pattern.Tuple { Items:
                    [Pattern.Literal { Value: Integer tag }, BitPattern { Segments: [var prefix, var suffix] }] }],
                    Tail: Pattern.Variable rest }, Pattern.Variable acc, Pattern.Variable total],
                  Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [
                      Expr.Variable next, Expr.List { Items: [Expr.Variable bytes], Tail: Expr.Variable previous },
                      Expr.Binary { Operator: ErlangOperators.Plus, Left: Expr.Variable unused,
                          Right: Expr.Variable accumulated } sum] } recur },
                { Guard: null, Patterns: [var nil, Pattern.Variable remaining, Pattern.Variable finalCount],
                  Body: Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                      Function: "list_to_binary", Arguments: [
                          Expr.List { Items: [Expr.Variable count], Tail: Expr.Call
                              { Module: "lists", DynamicRemote: false, Function: "reverse",
                                Arguments: [Expr.Variable reversed] } reverse }] } finish }]
            || prefix is not { Value: Pattern.Variable unusedByte,
                Specification: { Type: BitSegmentTypes.Integer, Size: null, Unit: 1, Signed: false, Endian: BitByteOrders.Big } }
            || suffix is not { Value: Pattern.Variable binary,
                Specification: { Type: BitSegmentTypes.Binary, Size: null, Unit: BitSyntaxDefaults.BinaryUnit } }
            || tag.Value < int.MinValue || tag.Value > int.MaxValue
            || !DistinctBindings(rest, acc, total, unusedByte, binary) || !DistinctBindings(remaining, finalCount)
            || recur.Function != function.Name || next.Name != rest.Name || bytes.Name != binary.Name
            || previous.Name != acc.Name || unused.Name != unusedByte.Name || accumulated.Name != total.Name
            || !IsEmptyListPattern(nil) || count.Name != finalCount.Name || reversed.Name != remaining.Name)
        {
            return null;
        }

        return new(recur, sum, reverse, finish, tag);
    }

    private static PartCollection? ReadPartCollection(FunctionDefinition function)
    {
        if (function.Clauses is not [var nested, var bits, var plain, var empty]
            || nested is not
            {
                Patterns: [Pattern.List { Items: [Pattern.Tuple { Items:
                    [Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable list] }],
                    Tail: Pattern.Variable nestedTail }, Pattern.Variable nestedAcc],
                Guard: Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                    Function: "is_list", Arguments: [Expr.Variable tested] },
                Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable next,
                    Expr.List { Items: [Expr.Call { Module: null, DynamicRemote: false,
                        Arguments: [Expr.Variable child] } collect], Tail: Expr.Variable acc }] } recur
            }
            || !DistinctBindings(list, nestedTail, nestedAcc) || tested.Name != list.Name || child.Name != list.Name
            || next.Name != nestedTail.Name || acc.Name != nestedAcc.Name || recur.Function != function.Name
            || bits is not
            {
                Guard: null,
                Patterns: [Pattern.List { Items: [Pattern.Tuple { Items: [
                    Pattern.Literal { Value: Integer tag }, BitPattern { Segments: [var prefix, var suffix] }] }],
                    Tail: Pattern.Variable bitsTail }, Pattern.Variable ignored],
                Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable rest,
                    Expr.List { Items: [Expr.Variable bytes], Tail: null }, Expr.Variable unused] } collectBits
            }
            || tag.Value < int.MinValue || tag.Value > int.MaxValue
            || prefix is not { Value: Pattern.Variable count,
                Specification: { Type: BitSegmentTypes.Integer, Size: null, Unit: 1, Signed: false, Endian: BitByteOrders.Big } }
            || suffix is not { Value: Pattern.Variable binary,
                Specification: { Type: BitSegmentTypes.Binary, Size: null, Unit: BitSyntaxDefaults.BinaryUnit } }
            || !DistinctBindings(count, binary, bitsTail) || ignored.Name != VariableScopeNames.Wildcard
                && (ignored.Name == count.Name || ignored.Name == binary.Name || ignored.Name == bitsTail.Name)
            || rest.Name != bitsTail.Name || bytes.Name != binary.Name || unused.Name != count.Name
            || plain is not
            {
                Guard: null,
                Patterns: [Pattern.List { Items: [Pattern.Tuple { Items: [Pattern.Variable plainTag, Pattern.Variable value] }],
                    Tail: Pattern.Variable plainTail }, Pattern.Variable plainAcc],
                Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable plainNext,
                    Expr.List { Items: [Expr.Variable item], Tail: Expr.Variable accumulated }] } plainRecur
            }
            || !DistinctBindings(value, plainTail, plainAcc) || plainTag.Name != VariableScopeNames.Wildcard
                && (plainTag.Name == value.Name || plainTag.Name == plainTail.Name || plainTag.Name == plainAcc.Name)
            || plainRecur.Function != function.Name || plainNext.Name != plainTail.Name
            || item.Name != value.Name || accumulated.Name != plainAcc.Name
            || empty is not
            {
                Guard: null, Patterns: [var nil, Pattern.Variable last],
                Body: Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false, Function: "list_to_binary",
                    Arguments: [Expr.Call { Module: "lists", DynamicRemote: false, Function: "reverse",
                        Arguments: [Expr.Variable reversed] } reverse] } finish
            }
            || !IsEmptyListPattern(nil) || last.Name == VariableScopeNames.Wildcard || reversed.Name != last.Name)
        {
            return null;
        }

        return new(collect, collectBits, reverse, finish, tag);
    }

    private void EmitPartCollection(
        PartCollection flow, int entry, int nested, int bits, BeamSourceLocation location
    )
    {
        int functionInfo = nextLabel - 2;
        int binary = nextLabel++;
        int plain = nextLabel++;
        int empty = nextLabel++;
        EmitNonemptyList(0, empty);
        EmitListParts(0, XRegisterTag, 2, XRegisterTag, 3);
        EmitTupleCheck(2, 2, functionInfo);
        EmitTuplePart(2, 1, 0);
        Op(IsList, LabelTag, binary);
        Operand(XRegisterTag, 0);
        Op(Allocate, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, 4);
        MoveRegister(XRegisterTag, 3, YRegisterTag, 0);
        MoveRegister(XRegisterTag, 1, YRegisterTag, 1);
        EmitCallLine(flow.Nested, location);
        EmitFieldCall(1, nested);
        Op(TestHeap, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, 1);
        Op(PutList, XRegisterTag, 0);
        Operand(YRegisterTag, 1);
        Operand(XRegisterTag, 1);
        MoveRegister(YRegisterTag, 0, XRegisterTag, 0);
        Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 2);
        Operand(LabelTag, entry);
        Operand(BeamWire.UnsignedTag, 2);

        Op(Label, BeamWire.UnsignedTag, binary);
        EmitTuplePart(2, 0, 2);
        Op(ExactEqualTest, LabelTag, plain);
        Operand(XRegisterTag, 2);
        LiteralOperand(flow.BitTag);
        EmitPartBinaryMatch(plain, input: 0, context: 2, live: 4, tail: 1);
        Op(TestHeap, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, 4);
        Op(PutList, XRegisterTag, 1);
        Operand(AtomTag, NilAtomIndex);
        Operand(XRegisterTag, 1);
        MoveRegister(XRegisterTag, 0, XRegisterTag, 2);
        MoveRegister(XRegisterTag, 3, XRegisterTag, 0);
        Op(TailLocalCall, BeamWire.UnsignedTag, 3);
        Operand(LabelTag, bits);

        Op(Label, BeamWire.UnsignedTag, plain);
        Op(TestHeap, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, 4);
        Op(PutList, XRegisterTag, 0);
        Operand(XRegisterTag, 1);
        Operand(XRegisterTag, 1);
        MoveRegister(XRegisterTag, 3, XRegisterTag, 0);
        Op(TailLocalCall, BeamWire.UnsignedTag, 2);
        Operand(LabelTag, entry);

        Op(Label, BeamWire.UnsignedTag, empty);
        EmitNilTest(XRegisterTag, 0, functionInfo);
        Op(Allocate, BeamWire.UnsignedTag, 0);
        Operand(BeamWire.UnsignedTag, 2);
        MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
        EmitCallLine(flow.Reverse, location);
        Op(ExternalCall, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(flow.Reverse.Module!, flow.Reverse.Function, 1));
        EmitCallLine(flow.Finish, CallLocation(flow.Reverse, location));
        Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, flow.Finish.Function, 1));
        Operand(BeamWire.UnsignedTag, 0);
    }

    private void EmitBitPartCollection(BitPartCollection flow, int entry, BeamSourceLocation location, bool naturalTotal)
    {
        int functionInfo = nextLabel - 2;
        int empty = nextLabel++;
        EmitNonemptyList(0, empty);
        EmitListParts(0, XRegisterTag, 3, XRegisterTag, 4);
        EmitTupleCheck(3, 2, functionInfo);
        EmitTuplePart(3, 0, 5);
        Op(ExactEqualTest, LabelTag, functionInfo);
        Operand(XRegisterTag, 5);
        LiteralOperand(flow.Tag);
        EmitTuplePart(3, 1, 3);
        EmitPartBinaryMatch(functionInfo, input: 3, context: 3, live: 5, tail: 3);
        var previous = location;
        EmitArithmeticLine(flow.Sum.SourceLine, ref previous);
        Op(GcBifTwo, LabelTag, 0);
        Operand(BeamWire.UnsignedTag, 5);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, flow.Sum.Operator, 2));
        EmitTypedRegister(0, IntegerTypeBits, byte.MinValue, byte.MaxValue);
        if (naturalTotal)
        {
            EmitTypedRegister(2, IntegerTypeBits, lower: 0);
        }
        else
        {
            Operand(XRegisterTag, 2);
        }
        Operand(XRegisterTag, 2);
        Op(TestHeap, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, 5);
        Op(PutList, XRegisterTag, 3);
        Operand(XRegisterTag, 1);
        Operand(XRegisterTag, 1);
        MoveRegister(XRegisterTag, 4, XRegisterTag, 0);
        Op(TailLocalCall, BeamWire.UnsignedTag, 3);
        Operand(LabelTag, entry);

        Op(Label, BeamWire.UnsignedTag, empty);
        EmitNilTest(XRegisterTag, 0, functionInfo);
        Op(Allocate, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 3);
        MoveRegister(XRegisterTag, 2, YRegisterTag, 0);
        MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
        EmitCallLine(flow.Reverse, location);
        Op(ExternalCall, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(flow.Reverse.Module!, flow.Reverse.Function, 1));
        Op(TestHeap, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, 1);
        Op(PutList, YRegisterTag, 0);
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 0);
        EmitCallLine(flow.Finish, CallLocation(flow.Reverse, location));
        Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, flow.Finish.Function, 1));
        Operand(BeamWire.UnsignedTag, 1);
    }

    private void EmitPartBinaryMatch(int failure, int input, int context, int live, int tail)
    {
        const int byteWidth = BitSyntaxDefaults.BinaryUnit;
        Op(StartBinaryMatch, LabelTag, failure);
        Operand(XRegisterTag, input);
        Operand(BeamWire.UnsignedTag, live);
        Operand(XRegisterTag, context);
        Op(BinaryMatch, LabelTag, failure);
        Operand(XRegisterTag, context);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 13);
        Operand(AtomTag, Intern("ensure_at_least"));
        Operand(BeamWire.UnsignedTag, byteWidth);
        Operand(BeamWire.UnsignedTag, byteWidth);
        Operand(AtomTag, Intern("integer"));
        Operand(BeamWire.UnsignedTag, live);
        Operand(AtomTag, NilAtomIndex);
        Operand(BeamWire.UnsignedTag, byteWidth);
        Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.NumericUnit);
        Operand(XRegisterTag, 0);
        Operand(AtomTag, Intern("get_tail"));
        Operand(BeamWire.UnsignedTag, live);
        Operand(BeamWire.UnsignedTag, byteWidth);
        Operand(XRegisterTag, tail);
    }
}
