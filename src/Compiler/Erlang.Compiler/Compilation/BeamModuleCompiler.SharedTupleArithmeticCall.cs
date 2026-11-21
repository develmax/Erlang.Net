using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        // OTP keeps the first clause frameless and places the fallback frame at
        // the call boundary, on the success edge of the preceding arithmetic.
        private bool TryEmitSharedTupleArithmeticCall(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not [
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple
                    {
                        Items: [
                                Pattern.Variable { Name: VariableScopeNames.Wildcard },
                                Pattern.Literal { Value: Integer discriminator }
                            ]
                    }],
                    Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [] } direct
                },
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple
                    {
                        Items: [Pattern.Variable first, Pattern.Variable second]
                    }],
                    Body: Expr.Binary { Left: Expr.Binary initial } result
                }
                ]
                || first.Name == second.Name || first.Name == VariableScopeNames.Wildcard
                || second.Name == VariableScopeNames.Wildcard
                || initial.Left is not Expr.Variable firstArgument || firstArgument.Name != first.Name
                || initial.Right is not Expr.Variable secondArgument || secondArgument.Name != second.Name
                || !IsIntegerRegisterArithmetic(initial.Operator) || initial.Operator == ErlangOperators.BitwiseXor
                || !entries.TryGetValue((direct.Function, 0), out int target))
            {
                return false;
            }

            const string beforeCall = "\0before-call";
            const string callResult = "\0call-result";

            // The left subtree is evaluated before the call. A trailing subtree
            // can begin with the call after reading harmless scalar operands.
            Expr? trailing = RewriteLeadingCall(result.Right, out Expr.Call? call);
            if (trailing is null || call is null || call.Function != direct.Function)
            {
                return false;
            }

            var continuation = result with
            {
                Left = new Expr.Variable(beforeCall),
                Right = trailing
            };
            if (ReadsVariable(continuation, first.Name))
            {
                return false;
            }

            var inputs = new Dictionary<string, int>(StringComparer.Ordinal) { [callResult] = 0 };
            var slots = new Dictionary<int, int>();
            if (ReadsVariable(trailing, second.Name))
            {
                inputs.Add(second.Name, 1);
                slots.Add(1, 0);
            }

            inputs.Add(beforeCall, 2);
            slots.Add(2, slots.Count);
            var facts = slots.Keys.ToDictionary(value => value, _ => PairFacts.Integer);
            var initialPlan = PlanRegisterArithmetic(
                [initial],
                new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    [first.Name] = 0,
                    [second.Name] = 1
                },
                firstResult: 2,
                tail: true
            );
            var resultPlan = PlanRegisterArithmetic(
                [continuation],
                inputs,
                firstResult: 3,
                tail: true,
                inputFacts: facts,
                stackInputs: slots
            );
            if (initialPlan is null || resultPlan is null)
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            int fallback = Label();
            owner.EmitTupleCheck(0, 2, failure);
            owner.EmitTuplePart(0, 1, 1);
            owner.Op(ExactEqualTest, LabelTag, fallback);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(discriminator);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 0);
            owner.Operand(LabelTag, target);
            Mark(fallback);
            owner.EmitTuplePart(0, 0, 0);
            var previous = EmitRegisterArithmetic(initialPlan, location);
            EmitFrameAllocation(slots.Count == 2 ? 2 : 1, slots.Count);
            if (slots.TryGetValue(1, out int secondSlot))
            {
                owner.MoveRegister(
                    XRegisterTag,
                    1,
                    YRegisterTag,
                    secondSlot
                );
            }

            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                slots[2]
            );
            EmitWindowLocal(call, target, ref previous);
            EmitRegisterArithmetic(resultPlan, previous);
            owner.EmitFrameReturn(slots.Count);

            return true;

            static Expr? RewriteLeadingCall(Expr expression, out Expr.Call? call)
            {
                switch (expression)
                {
                    case Expr.Call { Module: null, DynamicRemote: false, Arguments: [] } local:
                        call = local;

                        return new Expr.Variable(callResult);
                    case Expr.Binary binary when IsRegisterArithmeticOperator(binary.Operator)
                        && binary.Operator != ErlangOperators.BitwiseXor:
                        if (RewriteLeadingCall(binary.Left, out call) is { } left)
                        {
                            return binary with { Left = left };
                        }

                        if (binary.Left is Expr.Variable or Expr.Literal
                            && RewriteLeadingCall(binary.Right, out call) is { } right)
                        {
                            return binary with { Right = right };
                        }

                        break;
                }

                call = null;

                return null;
            }

            static bool ReadsVariable(Expr expression, string name) =>
                expression is Expr.Variable variable && variable.Name == name
                || expression is Expr.Binary binary
                    && (ReadsVariable(binary.Left, name) || ReadsVariable(binary.Right, name));
        }
    }
}
