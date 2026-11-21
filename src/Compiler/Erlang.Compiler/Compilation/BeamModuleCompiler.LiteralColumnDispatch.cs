using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record LiteralColumnRow(Term?[] Keys, Term Result);

        private abstract record LiteralColumnNode(int Identity);
        private sealed record LiteralColumnLeaf(int Identity, Term Result) : LiteralColumnNode(Identity);
        private sealed record LiteralColumnSelect(
            int Identity,
            int Register,
            LiteralColumnNode Failure,
            (Term Key, LiteralColumnNode Target)[] Arms
        ) : LiteralColumnNode(Identity);

        private bool TryEmitLiteralColumnDispatch(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || function.Arity < 2
                || function.Clauses is not [_, _, .., { Guard: null } fallback]
                || !HasUnconditionalParameters(fallback) || !AttributeLiteral.TryNormalizeConstant(fallback.Body, out Term defaultResult))
            {
                return false;
            }

            var rows = new List<LiteralColumnRow>();
            foreach (Clause clause in function.Clauses.SkipLast(1))
            {
                if (clause.Guard is not (null or Expr.Literal { Value: Atom { Name: ErlangBooleanAtoms.True } })
                    || !AttributeLiteral.TryNormalizeConstant(clause.Body, out Term result))
                {
                    return false;
                }

                var bindings = new HashSet<string>(StringComparer.Ordinal);
                bool sawVariable = false;
                var keys = new Term?[function.Arity];
                for (int index = 0; index < keys.Length; index++)
                {
                    switch (clause.Patterns[index])
                    {
                        case Pattern.Literal { Value: Atom or Integer } literal when !sawVariable:
                            keys[index] = literal.Value;
                            break;
                        case Pattern.Variable variable
                            when variable.Name == VariableScopeNames.Wildcard || bindings.Add(variable.Name):
                            sawVariable = true;
                            break;
                        default:
                            return false;
                    }
                }

                if (keys[0] is null)
                {
                    return false;
                }

                rows.Add(new LiteralColumnRow(keys, result));
            }

            int nextIdentity = 0;
            var leaves = new Dictionary<Term, LiteralColumnLeaf>();
            var branches = new Dictionary<(int Register, int Failure, Term Edges), LiteralColumnSelect>();
            var termOrder = Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            if (Build(0, rows, Leaf(defaultResult)) is not LiteralColumnSelect { Arms.Length: > 1 } root)
            {
                return false;
            }

            // Preserve grouped Boolean arity dispatch, which returns singleton comparisons directly.
            if (TryEmitGroupedArityDispatch(function))
            {
                return true;
            }

            var blocks = new List<LiteralColumnNode>();
            var seen = new HashSet<int>();
            Visit(root);
            var labels = blocks.Where(node => node != root).ToDictionary(node => node.Identity, _ => Label());
            for (int index = blocks.Count - 1; index >= 0; index--)
            {
                LiteralColumnNode block = blocks[index];
                if (block != root)
                {
                    Mark(labels[block.Identity]);
                }

                if (block is LiteralColumnLeaf leaf)
                {
                    owner.EmitConstant(leaf.Result, 0);
                    owner.instructions.WriteByte(Return);
                    continue;
                }

                var select = (LiteralColumnSelect)block;
                int failure = labels[select.Failure.Identity];
                if (select.Arms is [var only])
                {
                    owner.Op(ExactEqualTest, LabelTag, failure);
                    owner.Operand(XRegisterTag, select.Register);
                    owner.LiteralOperand(only.Key);
                    owner.Op(Jump, LabelTag, labels[only.Target.Identity]);
                }
                else
                {
                    owner.EmitMembershipSelect(select.Register, failure, select.Arms.Select(arm =>
                        (arm.Key, labels[arm.Target.Identity])));
                }
            }

            return true;

            LiteralColumnLeaf Leaf(Term value)
            {
                if (!leaves.TryGetValue(value, out var leaf))
                {
                    leaf = new LiteralColumnLeaf(nextIdentity++, value);
                    leaves.Add(value, leaf);
                }

                return leaf;
            }

            LiteralColumnNode? Build(int column, IReadOnlyList<LiteralColumnRow> remaining, LiteralColumnNode failure)
            {
                if (column == function.Arity || remaining[0].Keys[column] is null)
                {
                    return remaining.Count == 1 ? Leaf(remaining[0].Result) : null;
                }

                if (remaining[^1].Keys[column] is null)
                {
                    failure = Leaf(remaining[^1].Result);
                    remaining = remaining.Take(remaining.Count - 1).ToArray();
                }

                bool atoms = remaining[0].Keys[column] is Atom;
                if (remaining.Any(row => atoms ? row.Keys[column] is not Atom : row.Keys[column] is not Integer))
                {
                    return null;
                }

                var arms = new List<(Term Key, LiteralColumnNode Target)>();
                foreach (var group in remaining.GroupBy(row => row.Keys[column]!).OrderBy(group => group.Key, termOrder))
                {
                    if (Build(column + 1, group.ToArray(), failure) is not { } target)
                    {
                        return null;
                    }

                    if (target.Identity != failure.Identity)
                    {
                        arms.Add((group.Key, target));
                    }
                }

                if (arms.Count == 0)
                {
                    return failure;
                }

                Term edges = Term.Tuple(arms.Select(arm => Term.Tuple(arm.Key, Term.I(arm.Target.Identity))).ToArray());
                var key = (column, failure.Identity, edges);
                if (!branches.TryGetValue(key, out var branch))
                {
                    branch = new LiteralColumnSelect(
                        nextIdentity++,
                        column,
                        failure,
                        arms.ToArray()
                    );
                    branches.Add(key, branch);
                }

                return branch;
            }

            void Visit(LiteralColumnNode node)
            {
                if (!seen.Add(node.Identity))
                {
                    return;
                }

                if (node is LiteralColumnSelect select)
                {
                    Visit(select.Failure);
                    foreach (var (_, target) in select.Arms)
                    {
                        Visit(target);
                    }
                }

                blocks.Add(node);
            }
        }
    }
}
