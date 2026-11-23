using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNestedListTypePredicate(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || owner.OnLoadFunction is not null || owner.IsDeclaredNative((function.Name, function.Arity))
                || function.Clauses is not
                [
                {
                    Patterns: [var list], Guard: null,
                    Body: Expr.Unary
                    {
                        Operator: ErlangOperators.Not,
                        Operand: Expr.Call { Arguments: [Expr.Variable value] } predicate
                    }
                },
                {
                    Patterns: [Pattern.Variable fallback],
                    Guard: Expr.Call { Function: AutoImportNames.IsList, Arguments: [Expr.Variable tested] } listGuard,
                    Body: Expr.Literal { Value: Atom result }
                }
                ] || result.Name != ErlangBooleanAtoms.False || tested.Name != fallback.Name
                || fallback.Name == VariableScopeNames.Wildcard || !owner.ResolvesErlangBif(listGuard)
                || !owner.ResolvesErlangBif(predicate) || TypeGuardOpcode(predicate) == 0
                || !IsSafeExternalBif(predicate.Function, 1))
            {
                return false;
            }

            int depth = 0;
            Pattern current = list;
            while (current is Pattern.List { Items: [var head], Tail: { } tail } && IgnoresPattern(tail))
            {
                depth++;
                current = head;
            }

            if (depth < 2 || current is not Pattern.Variable innermost || innermost.Name != value.Name
                || innermost.Name == VariableScopeNames.Wildcard)
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            bool excludesNil = HasOnlyLocalCalls(function, 1, CallerExcludesNilBeforeListCheck);
            int empty = excludesNil ? failure : Label();
            int rejectedHead = Label();
            owner.EmitNonemptyList(0, empty);
            for (int index = 0; index < depth; index++)
            {
                if (index != 0)
                {
                    owner.EmitNonemptyList(0, rejectedHead);
                }

                owner.Op(GetHead, XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
            }

            owner.Op(BifOne, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, predicate.Function, 1));
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.Op(BifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.ExactEqual, 2));
            owner.EmitTypedRegister(0, AtomTypeBits);
            owner.LiteralOperand(result);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(Return);

            if (!excludesNil)
            {
                Mark(empty);
                owner.EmitNilTest(XRegisterTag, 0, failure);
            }

            Mark(rejectedHead);
            owner.EmitConstant(result, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private static bool CallerExcludesNilBeforeListCheck(FunctionDefinition caller, Expr.Call target)
        {
            if (target.Arguments is not [Expr.Variable value] || value.Name == VariableScopeNames.Wildcard)
            {
                return false;
            }

            for (int index = 1; index < caller.Clauses.Count; index++)
            {
                Clause clause = caller.Clauses[index];
                Expr first = clause.Body is Expr.Sequence { Items.Count: > 0 } sequence ? sequence.Items[0] : clause.Body;
                Expr reached = first is Expr.Match match ? match.Value : first;
                if (!ReferenceEquals(reached, target))
                {
                    continue;
                }

                for (int argument = 0; argument < clause.Patterns.Count; argument++)
                {
                    if (clause.Patterns[argument] is Pattern.Variable binding && binding.Name == value.Name
                        && caller.Clauses.Take(index).Any(earlier => ExitsOnNilArgument(earlier, argument)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool ExitsOnNilArgument(Clause earlier, int index)
        {
            if (earlier.Guard is not null || earlier.Patterns[index] is not
                (Pattern.Literal { Value: Nil } or Pattern.List { Items.Count: 0, Tail: null }))
            {
                return false;
            }

            var names = new HashSet<string>(StringComparer.Ordinal);
            for (int position = 0; position < earlier.Patterns.Count; position++)
            {
                Pattern pattern = earlier.Patterns[position];
                if (position != index && !IgnoresPattern(pattern)
                    && (pattern is not Pattern.Variable binding || !names.Add(binding.Name)))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IgnoresPattern(Pattern pattern) =>
            pattern is global::Erlang.Pattern.Any or Pattern.Variable { Name: VariableScopeNames.Wildcard };
    }
}
