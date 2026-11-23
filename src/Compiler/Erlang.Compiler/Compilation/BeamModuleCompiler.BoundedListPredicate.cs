using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitBoundedListPredicate(FunctionDefinition function)
        {
            if (!CanUseEntryTypeFacts() || recursiveClosure is not null || owner.IsDeclaredNative((function.Name, function.Arity))
                || function.Arity != 1 || function.Clauses is not
                [
                {
                    Patterns: [Pattern.Literal { Value: Nil } or Pattern.List { Items: [], Tail: null }],
                    Guard: null, Body: Expr.Literal { Value: Atom { Name: ErlangBooleanAtoms.True } }
                },
                {
                    Patterns: [Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable tail }],
                    Guard: Expr.Binary
                    {
                        Operator: ErlangOperators.AndAlso,
                        Left: Expr.Binary
                        {
                            Operator: ErlangOperators.AndAlso,
                            Left: Expr.Call
                            {
                                Module: null or ErlangModuleNames.Module,
                                DynamicRemote: false,
                                Function: AutoImportNames.IsInteger,
                                Arguments: [Expr.Variable tested]
                            } integerTest,
                            Right: Expr lowerTest
                        },
                        Right: Expr upperTest
                    },
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments: [Expr.Variable next]
                    } recur
                },
                {
                    Patterns: [global::Erlang.Pattern.Variable or global::Erlang.Pattern.Any],
                    Guard: null, Body: Expr.Literal { Value: Atom { Name: ErlangBooleanAtoms.False } }
                }
                ]
                || !DistinctBindings(head, tail) || tested.Name != head.Name || next.Name != tail.Name
                || recur.Function != function.Name || !owner.ResolvesErlangBif(integerTest)
                || !entries.TryGetValue((function.Name, 1), out int entry)
                || ReadInclusiveListIntegerBound(lowerTest, head.Name, lower: true) is not long lower
                || ReadInclusiveListIntegerBound(upperTest, head.Name, lower: false) is not long upper
                || lower > upper)
            {
                return false;
            }

            int failedGuard = Label();
            int nonCons = Label();
            owner.EmitNonemptyList(0, nonCons);
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );

            owner.Op(IsInteger, LabelTag, failedGuard);
            owner.Operand(XRegisterTag, 1);
            owner.Op(IsGreaterOrEqual, LabelTag, failedGuard);
            owner.EmitTypedRegister(1, IntegerTypeBits);
            owner.LiteralOperand(new Integer(lower));
            owner.Op(IsGreaterOrEqual, LabelTag, failedGuard);
            owner.LiteralOperand(new Integer(upper));
            owner.EmitTypedRegister(1, IntegerTypeBits, lower);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, entry);

            Mark(failedGuard);
            owner.EmitConstant(Term.A(ErlangBooleanAtoms.False), 0);
            owner.instructions.WriteByte(Return);

            // Only non-cons values reach here, with the original argument still in X0.
            Mark(nonCons);
            owner.Op(BifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.ExactEqual, 2));
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private static long? ReadInclusiveListIntegerBound(Expr expression, string input, bool lower)
        {
            if (expression is not Expr.Binary
                {
                    Operator: ErlangOperators.GreaterOrEqual or ErlangOperators.LessOrEqual
                } test)
            {
                return null;
            }

            bool variableFirst = lower == (test.Operator == ErlangOperators.GreaterOrEqual);
            Expr variable = variableFirst ? test.Left : test.Right;
            Expr literal = variableFirst ? test.Right : test.Left;

            return variable is Expr.Variable operand && operand.Name == input
                && literal is Expr.Literal { Value: Integer value }
                && value.Value >= long.MinValue && value.Value <= long.MaxValue ? (long)value.Value : null;
        }
    }
}
