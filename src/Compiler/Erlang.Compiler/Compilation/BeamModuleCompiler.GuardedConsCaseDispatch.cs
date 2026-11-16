using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGuardedConsCaseDispatch(FunctionDefinition function, int functionInfo)
        {
            if (function.Arity >= FirstReservedXRegister || function.Clauses.Count is < 3 or > 4)
            {
                return false;
            }

            var clauses = function.Clauses;
            var empty = clauses.FirstOrDefault(clause => clause.Guard is null
                && clause.Patterns.Count(pattern => ListEntryTest(pattern) == IsNil) == 1
                && clause.Patterns.All(pattern => pattern is Pattern.Variable || ListEntryTest(pattern) == IsNil));
            if (empty is null)
            {
                return false;
            }

            var arms = clauses.Where(clause => !ReferenceEquals(clause, empty)).ToArray();
            int input = Array.FindIndex(empty.Patterns.ToArray(), pattern => ListEntryTest(pattern) == IsNil);
            if (arms[0] is not { Guard: { } guard, Body: Expr.Call tail }
                || arms[1] is not { Guard: null, Body: Expr.Case { Value: Expr.Call } }
                || ReadParameters(arms[0]) is not { } guarded
                || ReadParameters(arms[1]) is not { } decision
                || ReadEntryListReturn([arms[0], arms[1], empty]) is not { } emptyResult)
            {
                return false;
            }

            Term? fallback = null;
            if (arms.Length == 3 && (arms[2].Guard is not null || !HasUnconditionalParameters(arms[2])
                || !AttributeLiteral.TryNormalizeConstant(arms[2].Body, out fallback)
                || !ReferenceEquals(clauses[^1], arms[2])))
            {
                return false;
            }

            var tests = new List<(byte Opcode, int Register)>();
            if (!ReadNegatedTests(guard)
                || ReadEntryCall(owner.ResolveImportedCall(tail, entries), Names(guarded), allowControlTransfer: true) is not { } target
                || ReadEntryRegisterCase(decision, listParts: (function.Arity, input)) is not
                {
                    PreserveSelector: false,
                    Case: { Opcode: ExactEqualTest, Tests: null, Match: { TupleCall: null }, Fallback.TupleCall: null }
                } body)
            {
                return false;
            }

            int[] next = tests.SkipLast(1).Select(_ => Label()).ToArray();
            int caseEntry = next.Length == 0 ? 0 : Label();
            int caseFailure = Label();
            int guardedEntry = Label();
            int noCons = Label();
            int invalid = fallback is null ? functionInfo : Label();
            owner.EmitNonemptyList(input, noCons);
            owner.EmitListParts(
                input,
                XRegisterTag,
                function.Arity,
                XRegisterTag,
                input
            );
            for (int index = 0; index < tests.Count; index++)
            {
                var (opcode, register) = tests[index];
                owner.Op(opcode, LabelTag, index == next.Length ? guardedEntry : next[index]);
                owner.Operand(XRegisterTag, register);
                if (index != next.Length)
                {
                    JumpTo(caseEntry);
                    Mark(next[index]);
                }
            }

            if (caseEntry != 0)
            {
                Mark(caseEntry);
            }

            EmitEntryRegisterCase(body, caseFailure);
            Mark(guardedEntry);
            EmitRegisterTailCall(target);
            EmitEmptyListReturn(
                emptyResult,
                noCons,
                invalid,
                properList: false
            );
            if (fallback is not null)
            {
                Mark(invalid);
                owner.EmitConstant(fallback, 0);
                owner.instructions.WriteByte(Return);
            }

            return true;

            Clause? ReadParameters(Clause clause)
            {
                if (clause.Patterns[input] is not Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable rest }
                    || clause.Patterns.Where((_, index) => index != input).Any(pattern => pattern is not Erlang.Pattern.Variable))
                {
                    return null;
                }

                Pattern[] bindings = [.. clause.Patterns, head];
                bindings[input] = rest;
                Clause prepared = clause with { Patterns = bindings };

                return HasUnconditionalParameters(prepared) ? prepared : null;
            }

            bool ReadNegatedTests(Expr expression)
            {
                if (expression is Expr.Binary { Operator: ErlangOperators.AndAlso } conjunction)
                {
                    return ReadNegatedTests(conjunction.Left) && ReadNegatedTests(conjunction.Right);
                }

                if (expression is not Expr.Binary { Operator: ErlangOperators.ExactEqual, Left: var left, Right: var right })
                {
                    return false;
                }

                var tested = left is Expr.Literal { Value: Atom { Name: ErlangBooleanAtoms.False } } ? right
                    : right is Expr.Literal { Value: Atom { Name: ErlangBooleanAtoms.False } } ? left : null;
                if (ReadEntryTypeGuard(guarded, tested) is not { } test)
                {
                    return false;
                }

                tests.Add(test);

                return true;
            }

            static string[] Names(Clause clause) => clause.Patterns.Cast<Pattern.Variable>().Select(variable => variable.Name).ToArray();
        }
    }
}
