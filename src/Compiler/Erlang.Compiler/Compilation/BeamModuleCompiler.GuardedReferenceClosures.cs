using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGuardedReferenceClosures(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || recursiveClosure is not null
                || function.Arity != 1 || function.Clauses.Count < 2
                || function.Clauses[^1] is not
                {
                    Guard: null, Patterns: [Pattern.Variable fallbackBinding], Body: Expr.Fun { SelfName: null } fallback
                }
                || HasNestedDefinitions(fallback))
            {
                return false;
            }

            var keys = new HashSet<Term>();
            var branches = new List<(Term Key, Expr.FunctionReference Reference, int Entry)>();
            foreach (Clause clause in function.Clauses.SkipLast(1))
            {
                if (clause is not
                    {
                        Patterns: [Pattern.Variable subject],
                        Guard: Expr.Binary
                        {
                            Operator: ErlangOperators.ExactEqual, Left: Expr.Variable candidate, Right: var literal
                        },
                        Body: Expr.FunctionReference reference
                    }
                    || subject.Name == VariableScopeNames.Wildcard || subject.Name != candidate.Name
                    || !AttributeLiteral.TryNormalizeConstant(literal, out Term key)
                    || key is not Integer && (key is not TupleTerm tuple || tuple.Items.Any(field => field is not Integer))
                    || !keys.Add(key)
                    || !entries.TryGetValue((reference.Name, reference.Arity), out int entry))
                {
                    return false;
                }

                branches.Add((key, reference, entry));
            }

            bool integerKeys = branches.All(branch => branch.Key is Integer);
            if (!integerKeys && branches.Any(branch => branch.Key is Integer))
            {
                return false;
            }

            variables = new(StringComparer.Ordinal);
            if (integerKeys && branches.Count > 1)
            {
                // Exact integer guards on the same input can share one switch. Materialize closures in branch order.
                branches.Sort((left, right) => TermOrder.Compare(left.Key, right.Key, exact: true));
                int[] targets = new int[branches.Count];
                for (int index = targets.Length - 1; index >= 0; index--)
                {
                    targets[index] = Label();
                }

                int otherwise = Label();
                EmitAritySelect(0, targets.Length, otherwise);
                for (int index = 0; index < targets.Length; index++)
                {
                    owner.LiteralOperand(branches[index].Key);
                    owner.Operand(LabelTag, targets[index]);
                }

                for (int index = targets.Length - 1; index >= 0; index--)
                {
                    Mark(targets[index]);
                    EmitReference(branches[index].Reference, branches[index].Entry);
                }

                Mark(otherwise);
            }
            else
            {
                foreach (var (key, reference, entry) in branches)
                {
                    int next = Label();
                    owner.Op(ExactEqualTest, LabelTag, next);
                    owner.Operand(XRegisterTag, 0);
                    owner.LiteralOperand(key);
                    EmitReference(reference, entry);
                    Mark(next);
                }
            }

            var captures = fallbackBinding.Name == VariableScopeNames.Wildcard
                ? null
                : new Dictionary<string, int>(StringComparer.Ordinal) { [fallbackBinding.Name] = 0 };
            EmitGeneralClosure(fallback with { CoreOrdinal = fallback.CoreOrdinal ?? 0 }, captureRegisters: captures);
            owner.instructions.WriteByte(Return);

            return true;

            void EmitReference(Expr.FunctionReference reference, int entry)
            {
                owner.EmitClosureAllocation(0, 0, 1);
                owner.EmitDeclaredFunctionReference(reference, entry, 0);
                owner.instructions.WriteByte(Return);
            }
        }
    }
}
