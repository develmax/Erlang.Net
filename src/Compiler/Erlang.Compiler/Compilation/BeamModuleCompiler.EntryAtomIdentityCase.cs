using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool SameSourceLine(BeamSourceLocation left, BeamSourceLocation right) =>
        left.Line == right.Line
        && (left.File ?? defaultLineFile ?? implicitLineFile) == (right.File ?? defaultLineFile ?? implicitLineFile);

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitEntryAtomIdentityCase(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [Pattern.Variable first, ..],
                    Body: Expr.Case { Value: Expr.Variable selector, Clauses: [_, _, ..] } decision
                } clause
                ]
                || first.Name == VariableScopeNames.Wildcard || first.Name != selector.Name
                || !HasUnconditionalParameters(clause) || IsBooleanCase(decision)
                || ReadAtomEntryType(function, 0) != 0)
            {
                return false;
            }

            var keys = new HashSet<Atom>();
            foreach (Clause branch in decision.Clauses)
            {
                if (branch is not
                    {
                        Guard: null,
                        Patterns: [Pattern.Literal { Value: Atom key }],
                        Body: Expr.Literal { Value: Atom returned }
                    }
                    || !key.Equals(returned) || !keys.Add(key))
                {
                    return false;
                }
            }

            // Once each return literal is already in X0, the success blocks share one return.
            int success = Label();
            int failure = Label();
            var comparer = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            owner.EmitMembershipSelect(0, failure, keys.Order(comparer).Select(key => ((Term)key, success)));
            Mark(success);
            owner.instructions.WriteByte(Return);
            var failureLocation = CaseSourceLocation(decision);
            // The selector and shared return carry no intervening source line.
            if (failureLocation is { } source && owner.SameSourceLine(source, location))
            {
                failureLocation = null;
            }

            EmitBooleanCaseFailure(failure, failureLocation == owner.lastEmittedLine ? null : failureLocation);

            return true;
        }
    }
}
