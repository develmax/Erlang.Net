using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record DigitList(
        Expr.Call First,
        Expr.Call Second,
        Integer Radix,
        Term[] Suffix,
        Expr.Call? Recur
    );

    private static DigitList? ReadDigitList(Clause clause, bool recursive)
    {
        if (clause is not { Guard: null, Patterns: [BitPattern bits, var counter], Body: Expr.List list }
            || bits.Segments.Count != (recursive ? 3 : 2)
            || ReadDigit(bits.Segments[0]) is not { } high || ReadDigit(bits.Segments[1]) is not { } low
            || !DistinctBindings(high, low)
            || list.Items is not [
                Expr.Call
            {
                Module: null or ErlangModuleNames.Module, Function: "integer_to_list", DynamicRemote: false,
                Arguments: [Expr.Variable first, Expr.Literal { Value: Integer radix }]
            } firstCall,
                Expr.Call
            {
                Module: null or ErlangModuleNames.Module, Function: "integer_to_list", DynamicRemote: false,
                Arguments: [Expr.Variable second, Expr.Literal { Value: Integer secondRadix }]
            } secondCall, ..]
            || high.Name != first.Name || low.Name != second.Name || radix.Value != secondRadix.Value)
        {
            return null;
        }

        if (!recursive)
        {
            return counter is Pattern.Variable { Name: VariableScopeNames.Wildcard }
                && list.Tail is null && list.Items.Count == 2 ? new(
                    firstCall,
                    secondCall,
                    radix,
                    [],
                    null
                ) : null;
        }
        if (bits.Segments[2] is not
            {
                Value: Pattern.Variable rest, Specification:
                { Type: BitSegmentTypes.Binary, Size: null, Unit: BitSyntaxDefaults.BinaryUnit }
            }
            || !DistinctBindings(high, low, rest)
            || list.Tail is not Expr.Call { Module: null, Arguments: [Expr.Variable tail, _] } recur || tail.Name != rest.Name
            || counter is Pattern.Variable variable && !DistinctBindings(
                high,
                low,
                rest,
                variable
            ))
        {
            return null;
        }

        var suffix = new List<Term>();
        foreach (Expr item in list.Items.Skip(2))
        {
            if (!AttributeLiteral.TryNormalizeConstant(item, out Term literal))
            {
                return null;
            }
            suffix.Add(literal);
        }

        return new(
            firstCall,
            secondCall,
            radix,
            suffix.ToArray(),
            recur
        );
    }

    private static Pattern.Variable? ReadDigit(BitPatternSegment segment) => segment is
    {
        Value: Pattern.Variable variable, Specification:
        {
            Type: BitSegmentTypes.Integer, Signed: false,
            Endian: BitByteOrders.Big, Unit: BitSyntaxDefaults.NumericUnit, Size: Expr.Literal { Value: Integer bits }
        }
    } && bits.Value == 4 ? variable : null;

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitBinaryDigitList(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [BitPattern { Segments.Count: 0 }, Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Body: var empty
                },
                var single,
                { Patterns: [_, Pattern.Literal { Value: Integer zero }] } restart,
                { Patterns: [_, Pattern.Variable counter] } advance
            ] || !zero.Value.IsZero || !AttributeLiteral.TryNormalizeConstant(empty, out Term emptyList) || emptyList is not Nil
                || ReadDigitList(single, recursive: false) is not { } last
                || ReadDigitList(restart, recursive: true) is not { Recur: { } reset } wrap
                || ReadDigitList(advance, recursive: true) is not { Recur: { } next } more
                || !AttributeLiteral.TryNormalizeConstant(reset.Arguments[1], out Term start) || start is not Integer
                || next.Arguments[1] is not Expr.Binary
                {
                    Operator: ErlangOperators.Minus, Left: Expr.Variable current,
                    Right: Expr.Literal { Value: Integer step }
                } decrement || step.Value != 1 || counter.Name != current.Name
                || reset.Function != function.Name || next.Function != function.Name
                || !entries.TryGetValue((function.Name, 2), out int entry)
                || new[] { last, wrap, more }.Any(branch => !owner.ResolvesErlangBif(branch.First) || !owner.ResolvesErlangBif(branch.Second))
                || !HasOnlyLocalCalls(
                    function,
                    2,
                    (_, call) => ReferenceEquals(call, next)
                    || AttributeLiteral.TryNormalizeConstant(call.Arguments[1], out Term seed) && seed is Integer
                ))
            {
                return false;
            }

            int info = owner.nextLabel - 2;
            int nonempty = Label();
            int remainder = Label();
            int counting = Label();
            int invalid = Label();
            owner.BeginBerMatch(info, 2, 2);
            owner.BerMatch(nonempty, 2, 2);
            owner.BerEnsure(0);
            owner.EmitConstant(emptyList, 0);
            owner.instructions.WriteByte(Return);
            Mark(nonempty);
            owner.BerMatch(invalid, 2, 15);
            owner.BerEnsure(8, 1);
            owner.BerRead(3, 4, 3);
            owner.BerRead(4, 4, 4);
            owner.BerMatch(remainder, 2, 2);
            owner.BerEnsure(0);
            EmitDigitPair(last, 1);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 0);
            EmitDigitCons(YRegisterTag, 0);
            owner.EmitFrameReturn(1);

            Mark(remainder);
            owner.BerMatch(invalid, 2, 3);
            owner.BerEnsure(0, BitSyntaxDefaults.BinaryUnit);
            owner.Op(ExactEqualTest, LabelTag, counting);
            owner.EmitTypedRegister(1, IntegerTypeBits);
            owner.LiteralOperand(zero);
            EmitDigitPair(wrap, 2);
            owner.Op(Swap, YRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.EmitConstant(start, 1);
            owner.EmitCallLine(reset, CallLocation(wrap.Second, location));
            owner.EmitFieldCall(2, entry);
            EmitDigitListTail(wrap);

            Mark(counting);
            EmitDigitPair(more, 3);
            var previous = CallLocation(more.Second, location);
            owner.EmitArithmeticLine(decrement.SourceLine, ref previous);
            owner.Op(GcBifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Minus, 2));
            owner.EmitTypedRegister(2, IntegerTypeBits, registerTag: YRegisterTag);
            owner.LiteralOperand(step);
            owner.Operand(XRegisterTag, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                2
            );
            Load(1);
            owner.MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                1
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitCallLine(next, previous);
            owner.EmitFieldCall(2, entry);
            EmitDigitListTail(more);
            owner.RestoreBerInput(
                invalid,
                info,
                2,
                3
            );

            return true;
        }

        private void EmitDigitPair(DigitList branch, int frame)
        {
            owner.Op(Allocate, BeamWire.UnsignedTag, frame);
            owner.Operand(BeamWire.UnsignedTag, 5);
            owner.MoveRegister(
                XRegisterTag,
                4,
                YRegisterTag,
                0
            );
            if (frame >= 2)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    2,
                    YRegisterTag,
                    1
                );
            }
            if (frame == 3)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    1,
                    YRegisterTag,
                    2
                );
            }
            owner.EmitConstant(branch.Radix, 1);
            owner.MoveRegister(
                XRegisterTag,
                3,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(branch.First, location);
            owner.EmitStaticRemoteCall(branch.First with { Module = ErlangModuleNames.Module });
            owner.Op(Swap, YRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitConstant(branch.Radix, 1);
            owner.EmitCallLine(branch.Second, CallLocation(branch.First, location));
            owner.EmitStaticRemoteCall(branch.Second with { Module = ErlangModuleNames.Module });
        }

        private void EmitDigitListTail(DigitList branch)
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, (branch.Suffix.Length + 2) * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            foreach (Term separator in branch.Suffix.Reverse())
            {
                owner.instructions.WriteByte(PutList);
                owner.LiteralOperand(separator);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
            }
            EmitDigitCons(YRegisterTag, 1);
            EmitDigitCons(YRegisterTag, 0);
            owner.EmitFrameReturn(2);
        }

        private void EmitDigitCons(int tag, int register)
        {
            owner.Op(PutList, tag, register);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
        }
    }
}
