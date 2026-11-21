using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGuardedTupleMembershipForward(FunctionDefinition function)
        {
            if (!CanUseEntryTypeFacts() || function.Arity >= FirstReservedXRegister
                || function.Clauses is not
                [
                {
                    Guard: Expr.Binary
                    {
                        Operator: ErlangOperators.AndAlso,
                        Left: var prefix,
                        Right: Expr.Binary { Operator: ErlangOperators.OrElse or ErlangOperators.Or } alternatives
                    },
                    Body: Expr.Call { DynamicRemote: false } call
                } clause
                ]
                || ReadForwardParameters(clause) is not { } forward)
            {
                return false;
            }

            var (parameters, shapes, fields) = forward;
            if (shapes is not [(var tuple, var width)]
                || !call.Arguments.SequenceEqual(parameters.Select(name => new Expr.Variable(name)))
                || owner.ResolvesErlangBif(call)
                || ReadEntryCall(call, parameters) is not { } target
                || ReadForwardGuard(prefix, parameters, fields) is not { } common)
            {
                return false;
            }

            ForwardGuard[] tests = FlattenForwardConjunction(common).ToArray();
            if (tests.Any(test => test is not ForwardGuard.FieldTest))
            {
                return false;
            }

            var keys = new List<Atom>();
            foreach (var (name, field) in fields)
            {
                keys.Clear();
                if (field.Register != tuple || !ReadAtomGuardKeys(alternatives, name, keys)
                    || keys.Count is < 2 or > MaximumFiniteAtomSetSize || keys.Distinct().Count() != keys.Count)
                {
                    continue;
                }

                int failure = owner.nextLabel - 2;
                int accepted = Label();
                int scratch = function.Arity;
                owner.EmitTupleCheck(tuple, width, failure);
                foreach (var test in tests)
                {
                    EmitForwardGuard(test, failure);
                }

                // Exact and numeric equality with an atom have the same success set.
                // All failed checks retain the original arguments for function_clause.
                owner.EmitTuplePart(tuple, field.Index, scratch);
                var order = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
                owner.EmitMembershipSelect(scratch, failure, keys.Order(order).Select(key => ((Term)key, accepted)));
                Mark(accepted);
                EmitRegisterTailCall(target);

                return true;
            }

            return false;
        }
    }
}
