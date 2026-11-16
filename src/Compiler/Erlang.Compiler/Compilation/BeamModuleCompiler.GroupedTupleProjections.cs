using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGroupedTupleProjections(
            FunctionDefinition function,
            IReadOnlyList<IGrouping<int, Clause>> groups,
            Clause? fallback,
            int functionInfo
        )
        {
            Term? defaultValue = null;
            if (function.Arity != 1 || fallback is not null
                && !AttributeLiteral.TryNormalizeConstant(fallback.Body, out defaultValue))
            {
                return false;
            }

            var atomOrder = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            var projections = new Dictionary<int, SortedDictionary<Atom, int>>();
            var lastUse = new Dictionary<int, int>();
            var useCounts = new Dictionary<int, int>();
            foreach (var group in groups)
            {
                var tags = new SortedDictionary<Atom, int>(atomOrder);
                foreach (Clause clause in group)
                {
                    if (clause is not
                        {
                            Guard: null,
                            Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, ..] } tuple],
                            Body: Expr.Variable result
                        })
                    {
                        return false;
                    }

                    int field = -1;
                    for (int index = 1; index < tuple.Items.Count; index++)
                    {
                        if (tuple.Items[index] is not Pattern.Variable variable)
                        {
                            return false;
                        }

                        if (variable.Name == VariableScopeNames.Wildcard)
                        {
                            continue;
                        }

                        if (field >= 0 || variable.Name != result.Name)
                        {
                            return false;
                        }

                        field = index;
                    }

                    if (field < 0)
                    {
                        return false;
                    }

                    tags.TryAdd(tag, field);
                }

                foreach (int field in tags.Values)
                {
                    lastUse.TryAdd(field, group.Key);
                    useCounts[field] = useCounts.GetValueOrDefault(field) + 1;
                }

                projections.Add(group.Key, tags);
            }

            var labels = new Dictionary<int, int>();
            var resultLabels = new Dictionary<int, int>();
            var results = new Dictionary<int, int[]>();
            foreach (var group in groups.Reverse())
            {
                int arity = group.Key;
                var tags = projections[arity];
                labels.Add(arity, Label());
                int[] localResults = tags.Reverse().Select(tag => tag.Value)
                    .Distinct().Where(field => lastUse[field] == arity).ToArray();
                foreach (int field in localResults)
                {
                    resultLabels.Add(field, tags.Count > 1 || useCounts[field] > 1 ? Label() : 0);
                }

                results.Add(arity, localResults);
            }

            int failure = defaultValue is null ? functionInfo : Label();
            owner.EmitTupleArityDispatch(failure, groups.Select(group => (group.Key, labels[group.Key])).ToArray());
            foreach (var group in groups.Reverse())
            {
                int arity = group.Key;
                var tags = projections[arity];
                Mark(labels[arity]);
                owner.EmitTuplePart(0, 0, 1);
                if (tags.Count == 1)
                {
                    var (tag, field) = tags.First();
                    owner.EmitAtomTest(1, tag.Name, failure);
                    if (lastUse[field] != arity)
                    {
                        owner.Op(Jump, LabelTag, resultLabels[field]);
                    }
                }
                else
                {
                    EmitAritySelect(1, tags.Count, failure);
                    foreach (var (tag, field) in tags)
                    {
                        owner.Operand(AtomTag, owner.Intern(tag.Name));
                        owner.Operand(LabelTag, resultLabels[field]);
                    }
                }

                foreach (int field in results[arity])
                {
                    if (resultLabels[field] is int label && label != 0)
                    {
                        Mark(label);
                    }

                    owner.EmitTuplePart(0, field, 0);
                    owner.instructions.WriteByte(Return);
                }
            }

            if (defaultValue is not null)
            {
                Mark(failure);
                owner.EmitConstant(defaultValue, 0);
                owner.instructions.WriteByte(Return);
            }

            return true;
        }
    }
}
