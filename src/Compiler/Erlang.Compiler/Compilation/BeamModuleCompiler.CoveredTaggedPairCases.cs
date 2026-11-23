using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record CoveredTaggedPairGuard(
            string Source,
            int Arity,
            int Field,
            Atom Marker,
            Atom Fallback
        );

        private CoveredTaggedPairGuard? ReadCoveredTaggedPairGuard(Expr? guard)
        {
            if (guard is not Expr.GuardAlternatives
                {
                    Items: [
                        Expr.Binary
                    {
                        Operator: ErlangOperators.AndAlso,
                        Left: Expr.Binary { Left: Expr.Call measure } size,
                        Right: Expr.Binary
                        {
                            Operator: ErlangOperators.ExactEqual,
                            Left: Expr.Call
                            {
                                Arguments: [Expr.Literal { Value: Integer field }, Expr.Variable source]
                            } read,
                            Right: Expr.Literal { Value: Atom marker }
                        }
                    },
                        Expr.Binary
                    {
                        Operator: ErlangOperators.ExactEqual,
                        Left: Expr.Variable fallbackSource,
                        Right: Expr.Literal { Value: Atom fallback }
                    }
                    ]
                }
                || ReadTupleArityGuard(size) is not { Value: Expr.Variable measured, Arity: var arity }
                || !owner.ResolvesErlangBif(measure) || !IsElementGuardCall(read) || !owner.ResolvesErlangBif(read)
                || measured.Name != source.Name || fallbackSource.Name != source.Name
                || field.Value < 1 || field.Value > arity)
            {
                return null;
            }

            return new(
                source.Name,
                arity,
                (int)field.Value,
                marker,
                fallback
            );
        }

        private bool TryEmitCoveredTaggedPairGuard(CoveredTaggedPairGuard guard, int failure)
        {
            if (!variables.TryGetValue(guard.Source, out int slot))
            {
                return false;
            }

            // A guarded call-pair arm reads its bindings from the frame. The
            // producer call and the saved payload give the guard its own scratch.
            Load(slot);
            EmitGuardedTupleCheck(
                guard.Arity,
                guard.Field,
                guard.Marker,
                guard.Fallback,
                failure,
                elementRegister: 2
            );

            return true;
        }

        private bool TryEmitCoveredTaggedPairPattern(
            Pattern pattern,
            CoveredTaggedPairGuard guard,
            int carrier,
            int failure
        )
        {
            Pattern.Variable? binding = null;
            if (pattern is AliasPattern alias)
            {
                if (!TryReadPatternAlias(alias, out var paired, out pattern)
                    || paired.Name == VariableScopeNames.Wildcard || variables.ContainsKey(paired.Name)
                    || deferredPairMembers.ContainsKey(paired.Name))
                {
                    return false;
                }

                binding = paired;
            }

            if (pattern is not Pattern.Tuple tuple || !HasTupleTag(tuple, out Atom tag))
            {
                return false;
            }

            var bound = new HashSet<string>(StringComparer.Ordinal);
            int guardedField = -1;
            int literalField = -1;
            for (int index = 1; index < tuple.Items.Count; index++)
            {
                switch (tuple.Items[index])
                {
                    case global::Erlang.Pattern.Any or Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                        break;
                    case Pattern.Variable fresh when bound.Add(fresh.Name) && !variables.ContainsKey(fresh.Name)
                        && !deferredPairMembers.ContainsKey(fresh.Name) && fresh.Name != binding?.Name:
                        if (fresh.Name == guard.Source)
                        {
                            guardedField = index;
                        }

                        break;
                    case Pattern.Literal { Value: Atom or Integer } when literalField < 0:
                        literalField = index;
                        break;
                    default:
                        return false;
                }
            }

            if (guardedField < 0 || literalField <= guardedField)
            {
                return false;
            }

            // The tag and arity check make field reads total. Only the literal
            // column can now reject, so the guard's field need not be read yet.
            owner.EmitRecordTest(
                failure,
                YRegisterTag,
                carrier,
                tag,
                tuple.Items.Count
            );
            owner.Op(GetTupleElement, YRegisterTag, carrier);
            owner.Operand(BeamWire.UnsignedTag, literalField);
            owner.Operand(XRegisterTag, 0);
            Pattern(
                tuple.Items[literalField],
                0,
                failure,
                valueTag: XRegisterTag
            );
            Pattern(
                tuple,
                carrier,
                failure,
                checkedShape: tuple,
                checkedTupleLiterals: [literalField]
            );
            if (binding is not null)
            {
                Pattern(binding, carrier, failure);
            }

            return true;
        }

        private bool HasCoveredTaggedCallPair(Expr.Case decision)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || decision.Value is not Expr.Call { Module: not null, DynamicRemote: false }
                || decision.Clauses.All(clause => clause.Guard is null))
            {
                return false;
            }

            var covered = new Dictionary<Atom, bool>();
            foreach (Clause clause in decision.Clauses)
            {
                if (clause.Patterns is not [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, var payload] }])
                {
                    return false;
                }

                // A tag must have a final irrefutable payload arm. It must not
                // need the original pair again if an earlier guard rejects.
                covered[tag] = clause.Guard is null && (payload is Pattern.Any
                    || payload is Pattern.Variable variable && (variable.Name == VariableScopeNames.Wildcard
                        || !variables.ContainsKey(variable.Name) && !deferredPairMembers.ContainsKey(variable.Name)));
            }

            return covered.Count > 1 && covered.Values.All(complete => complete);
        }
    }
}
