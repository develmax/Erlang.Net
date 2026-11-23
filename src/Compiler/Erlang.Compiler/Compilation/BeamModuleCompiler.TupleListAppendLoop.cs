using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTupleListAppendLoop(FunctionDefinition function, int functionInfo)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || function.Arity != 2
                || function.Clauses is not [var finish, var skip, var join]
                || finish is not
                {
                    Guard: null, Patterns: [var empty, Pattern.Variable retained],
                    Body: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable forwarded] } final
                }
                || ListEntryTest(empty) != IsNil || retained.Name == VariableScopeNames.Wildcard || retained.Name != forwarded.Name
                || ReadEntryCall(final, [VariableScopeNames.Wildcard, retained.Name], allowControlTransfer: true) is not { } terminal
                || skip is not
                {
                    Guard: null,
                    Patterns:
                    [
                        Pattern.List
                    {
                        Tail: Pattern.Variable tail,
                        Items: [Pattern.Tuple { Items: [var emptyFirst, var emptySecond] }]
                    },
                        Pattern.Variable prior
                    ],
                    Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable remaining, Expr.Variable carried] } repeat
                }
                || ListEntryTest(emptyFirst) != IsNil || ListEntryTest(emptySecond) != IsNil || !DistinctBindings(tail, prior)
                || repeat.Function != function.Name || remaining.Name != tail.Name || carried.Name != prior.Name
                || join is not
                {
                    Guard: null,
                    Patterns:
                    [
                        Pattern.List
                    {
                        Tail: Pattern.Variable rest,
                        Items: [Pattern.Tuple { Items: [Pattern.Variable left, Pattern.Variable right] }]
                    },
                        Pattern.Variable accumulated
                    ],
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments: [Expr.Variable next, Expr.List
                        {
                            Items: [Expr.Binary { Operator: ErlangOperators.Append } concatenate],
                            Tail: Expr.Variable accumulator
                        }]
                    } recur
                }
                || !DistinctBindings(
                    rest,
                    left,
                    right,
                    accumulated
                ) || next.Name != rest.Name
                || accumulated.Name != accumulator.Name || recur.Function != function.Name
                || !entries.TryGetValue((function.Name, function.Arity), out int entry)
                || concatenate.Left is not Expr.Variable prefix || prefix.Name != left.Name
                || concatenate.Right is not Expr.Binary
                {
                    Operator: ErlangOperators.Append, Left: var delimiter,
                    Right: Expr.Binary { Operator: ErlangOperators.Append, Left: Expr.Variable last, Right: var suffix } inner
                }
                || last.Name != right.Name || AppendPrefix(delimiter) is not [var item]
                || !AttributeLiteral.TryNormalizeConstant(item, out Term separator)
                || !AttributeLiteral.TryNormalizeConstant(suffix, out Term ending)
                || !ProvesProperListAppendSuffix(ending))
            {
                return false;
            }

            const int tuple = 2;
            const int restRegister = 3;
            int concatenateEntry = Label();
            int emptyEntry = Label();
            owner.EmitNonemptyList(0, emptyEntry);
            owner.EmitListParts(
                0,
                XRegisterTag,
                tuple,
                XRegisterTag,
                restRegister
            );
            owner.Op(IsTuple, LabelTag, functionInfo);
            owner.Operand(XRegisterTag, tuple);
            owner.Op(TestArity, LabelTag, functionInfo);
            owner.Operand(XRegisterTag, tuple);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitTuplePart(tuple, 0, 0);
            owner.EmitTuplePart(tuple, 1, tuple);
            owner.EmitNilTest(XRegisterTag, 0, concatenateEntry);
            owner.EmitNilTest(XRegisterTag, tuple, concatenateEntry);
            owner.MoveToX0(XRegisterTag, restRegister);
            EmitRecur(0);

            Mark(concatenateEntry);
            EmitFrameAllocation(restRegister + 1, 3);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                restRegister,
                YRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                2
            );
            owner.EmitConstant(ending, 1);
            owner.MoveToX0(XRegisterTag, tuple);
            var previous = location;
            owner.EmitArithmeticLine(inner.SourceLine, ref previous);
            Call(ErlangModuleNames.Module, ErlangOperators.Append, 2);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.instructions.WriteByte(PutList);
            owner.LiteralOperand(separator);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            Load(0);
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitArithmeticLine(concatenate.SourceLine, ref previous);
            Call(ErlangModuleNames.Module, ErlangOperators.Append, 2);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(YRegisterTag, 1);
            owner.Operand(XRegisterTag, 1);
            Load(0);
            EmitRecur(2);

            Mark(emptyEntry);
            owner.EmitNilTest(XRegisterTag, 0, functionInfo);
            EmitRegisterTailCall(terminal, previous);

            return true;

            void EmitRecur(int frame)
            {
                owner.Op(frame == 0 ? TailLocalCall : TailLocalCallWithFrame, BeamWire.UnsignedTag, function.Arity);
                owner.Operand(LabelTag, entry);
                if (frame != 0)
                {
                    owner.Operand(BeamWire.UnsignedTag, frame);
                }
            }
        }

        private static bool ProvesProperListAppendSuffix(Term suffix)
        {
            while (suffix is Cons cons)
            {
                suffix = cons.Tail;
            }

            return suffix is Nil;
        }
    }
}
