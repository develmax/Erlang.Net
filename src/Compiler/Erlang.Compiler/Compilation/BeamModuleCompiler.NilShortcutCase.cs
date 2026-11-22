using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNilShortcutCase(Expr.Case decision)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || owner.receiveScratchRegisters is not null
                || decision.Value is not Expr.Binary { Operator: ErlangOperators.OrElse, Left: var first, Right: var second }
                || !IsBooleanCase(decision)
                || ReadNilShortcutProducer(first) is not { } left || ReadNilShortcutProducer(second) is not { } right)
            {
                return false;
            }

            bool trueFirst = decision.Clauses[0].Patterns[0] is global::Erlang.Pattern.Literal { Value: Atom { Name: "true" } };
            Expr whenTrue = decision.Clauses[trueFirst ? 0 : 1].Body;
            Expr whenFalse = decision.Clauses[trueFirst ? 1 : 0].Body;
            if (!IsAvailableResult(whenTrue) || !IsAvailableResult(whenFalse))
            {
                return false;
            }

            // A comparison with [] has two possible values for any producer
            // result. Its only consumer is the case, so use the test edge.
            // Emit the calls in their original order and keep right-side
            // bindings private to the matching short-circuit edge.
            var incomingRecords = checkedRecordBindings;
            int matched = Label();
            int done = Label();
            Expression(left);
            TestNil(matched);
            EmitShortCircuitRight(right, null);
            TestNil(matched);
            Expression(whenFalse);
            JumpTo(done);
            Mark(matched);
            Expression(whenTrue);
            Mark(done);
            checkedRecordBindings = incomingRecords;

            return true;

            bool IsAvailableResult(Expr expression) => AttributeLiteral.TryNormalizeConstant(expression, out _)
                || expression is Expr.Variable variable && variables.ContainsKey(variable.Name)
                    && !deferredPairMembers.ContainsKey(variable.Name);

            void TestNil(int unequal)
            {
                owner.Op(IsNil, LabelTag, unequal);
                owner.Operand(XRegisterTag, 0);
            }
        }

        private static Expr.Call? ReadNilShortcutProducer(Expr expression) =>
            expression is Expr.Binary
            {
                Operator: ErlangOperators.NumericNotEqual or ErlangOperators.ExactNotEqual,
                Left: Expr.Call { DynamicRemote: false } call,
                Right: Expr.Literal { Value: Nil } or Expr.List { Items.Count: 0, Tail: null }
            }
                ? call
                : null;
    }
}
