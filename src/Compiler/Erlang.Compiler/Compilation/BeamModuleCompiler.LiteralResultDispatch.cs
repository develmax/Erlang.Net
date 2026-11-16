using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitLiteralResultDispatch(FunctionDefinition function)
        {
            if (function.Arity != 1)
            {
                return false;
            }

            if (function.Clauses is
                [
                {
                    Guard: null,
                    Patterns: [Pattern.Variable selector],
                    Body: Expr.If { Clauses: [_, .., { Guard: Expr.Literal { Value: Atom { Name: "true" } } }] } conditional
                }
                ] && selector.Name != VariableScopeNames.Wildcard)
            {
                var alternatives = conditional.Clauses.Select(clause => clause with { Patterns = [selector] }).ToArray();
                alternatives[^1] = alternatives[^1] with { Guard = null };

                return TryEmitAtomGuardResultDispatch(alternatives, function);
            }

            Expr.Case? decision = null;
            Clause? caseInput = null;
            IReadOnlyList<Clause> clauses = function.Clauses;
            if (clauses is
                [
                {
                    Guard: null,
                    Patterns: [Pattern.Variable parameter],
                    Body: Expr.Case { Value: Expr.Variable input } body
                }]
                && parameter.Name != VariableScopeNames.Wildcard && parameter.Name == input.Name)
            {
                decision = body;
                caseInput = function.Clauses[0];
                clauses = body.Clauses;
            }

            if (TryEmitAtomGuardResultDispatch(clauses, function))
            {
                return true;
            }

            if (clauses.Count < 2)
            {
                return false;
            }

            Clause fallback = clauses[^1];
            bool hasFallback = fallback.Patterns is [Erlang.Pattern.Variable];
            int keyCount = clauses.Count - (hasFallback ? 1 : 0);
            if (keyCount < 2 || !HasLiteralDispatchKeys(clauses.Take(keyCount)))
            {
                return false;
            }

            var guard = ReadEntryTypeGuard(fallback, fallback.Guard);
            if (guard is null && caseInput is not null && hasFallback)
            {
                guard = ReadEntryTypeGuard(caseInput, fallback.Guard);
            }

            bool identity = hasFallback && fallback.Body is Expr.Variable value
                && (fallback.Patterns is [Pattern.Variable name] && name.Name == value.Name
                    || caseInput?.Patterns is [Pattern.Variable original] && original.Name == value.Name)
                && value.Name != VariableScopeNames.Wildcard;
            bool constant = AttributeLiteral.TryNormalizeConstant(fallback.Body, out Term result);
            Expr.Call? error = fallback.Body is Expr.Call call && IsConstantErrorCall(call) ? call : null;
            if (fallback.Guard is not null && guard is null
                || hasFallback && !identity && !constant && error is null
                || decision is null && (!hasFallback || guard is null && error is null))
            {
                return false;
            }

            BeamSourceLocation? caseLocation = decision is { SourceLine: > 0 }
                ? new BeamSourceLocation(decision.PreprocessorFile, decision.SourceLine)
                : null;
            int functionInfo = owner.nextLabel - 2;
            owner.EmitLiteralDispatch(
                clauses,
                location,
                emitComputedFallback: () =>
                {
                    int failure = decision is not null && guard.HasValue ? Label() : functionInfo;
                    if (guard is { } test)
                    {
                        owner.Op(test.Opcode, LabelTag, failure);
                        owner.Operand(XRegisterTag, test.Register);
                    }

                    if (!hasFallback)
                    {
                        EmitCaseFailure();
                    }
                    else if (error is not null)
                    {
                        owner.EmitCall(
                            fallback,
                            location,
                            error,
                            entries
                        );
                    }
                    else
                    {
                        if (constant)
                        {
                            owner.EmitConstant(result, 0);
                        }

                        owner.instructions.WriteByte(Return);
                    }

                    if (decision is not null && guard.HasValue)
                    {
                        Mark(failure);
                        EmitCaseFailure();
                    }
                },
                hasFallbackClause: hasFallback,
                selectorType: ReadAtomEntryType(function, 0)
            );

            return true;

            void EmitCaseFailure()
            {
                if (caseLocation is { } source)
                {
                    owner.EmitLine(source);
                }

                owner.Op(CaseEnd, XRegisterTag, 0);
            }
        }

        private bool TryEmitAtomGuardResultDispatch(IReadOnlyList<Clause> clauses, FunctionDefinition function)
        {
            if (clauses is not [_, .., { Guard: null, Patterns: [Erlang.Pattern.Variable] } fallback]
                || !AttributeLiteral.TryNormalizeConstant(fallback.Body, out Term defaultResult))
            {
                return false;
            }

            var seen = new HashSet<Atom>();
            var branches = new List<Clause>();
            bool foldedGuard = false;
            foreach (Clause clause in clauses.SkipLast(1))
            {
                if (!AttributeLiteral.TryNormalizeConstant(clause.Body, out Term result))
                {
                    return false;
                }

                if (clause is { Guard: null, Patterns: [Pattern.Literal { Value: Atom key }] })
                {
                    AddKey(key, result);
                    continue;
                }

                var keys = new List<Atom>();
                if (clause is not { Guard: { } guard, Patterns: [Pattern.Variable variable] }
                    || variable.Name == VariableScopeNames.Wildcard || !ReadAtomGuardKeys(guard, variable.Name, keys))
                {
                    return false;
                }

                foldedGuard = true;
                foreach (Atom atom in keys)
                {
                    AddKey(atom, result);
                }
            }

            if (!foldedGuard || branches.Count < 2)
            {
                return false;
            }

            branches.Add(fallback with { Body = new Expr.Literal(defaultResult) });
            owner.EmitLiteralDispatch(branches, location, selectorType: ReadAtomEntryType(function, 0));

            return true;

            void AddKey(Atom key, Term result)
            {
                if (seen.Add(key) && !result.Equals(defaultResult))
                {
                    branches.Add(new Clause([new Pattern.Literal(key)], null, new Expr.Literal(result)));
                }
            }
        }

        private static bool ReadAtomGuardKeys(Expr guard, string input, List<Atom> keys)
        {
            if (guard is Expr.GuardAlternatives alternatives)
            {
                return alternatives.Items.All(alternative => ReadAtomGuardKeys(alternative, input, keys));
            }

            if (guard is Expr.Binary { Operator: ErlangOperators.OrElse or ErlangOperators.Or } disjunction)
            {
                return ReadAtomGuardKeys(disjunction.Left, input, keys) && ReadAtomGuardKeys(disjunction.Right, input, keys);
            }

            if (guard is not Expr.Binary { Operator: ErlangOperators.NumericEqual or ErlangOperators.ExactEqual } comparison)
            {
                return false;
            }

            Atom? key = ComparedAtom(comparison.Left, comparison.Right) ?? ComparedAtom(comparison.Right, comparison.Left);
            if (key is null)
            {
                return false;
            }

            keys.Add(key);

            return true;

            Atom? ComparedAtom(Expr variable, Expr constant) =>
                variable is Expr.Variable tested && tested.Name == input && constant is Expr.Literal { Value: Atom atom }
                    ? atom
                    : null;
        }
    }
}
