using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record ConsHeadTupleRow(
            int Branch,
            global::Erlang.Pattern.Tuple Tuple,
            Atom Tag,
            global::Erlang.Pattern.Variable? TagBinding
        );

        private sealed record ConsHeadTupleGroup(int Arity, ConsHeadTupleRow[] Rows, int[] Fields);

        private ConsHeadTupleGroup[]? ReadConsHeadTupleGroups(
            Clause[] clauses,
            int input,
            int consCount,
            bool retainsTail,
            HashSet<string>[] unusedFields
        )
        {
            if (!retainsTail || consCount < 3 || owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled)
            {
                return null;
            }

            var rows = new List<ConsHeadTupleRow>();
            for (int branch = 0; branch < consCount - 1; branch++)
            {
                var list = (global::Erlang.Pattern.List)clauses[branch].Patterns[input];
                if (list is not { Items: [global::Erlang.Pattern.Tuple { Items.Count: > 0 } tuple], Tail: global::Erlang.Pattern.Variable }
                    || clauses[branch].Patterns.Where((_, index) => index != input).Any(pattern =>
                        pattern is not (global::Erlang.Pattern.Variable or global::Erlang.Pattern.Any)))
                {
                    return null;
                }

                switch (tuple.Items[0])
                {
                    case global::Erlang.Pattern.Literal { Value: Atom tag }:
                        rows.Add(new ConsHeadTupleRow(
                            branch,
                            tuple,
                            tag,
                            null
                        ));
                        break;
                    case AliasPattern { Left: global::Erlang.Pattern.Literal { Value: Atom tag }, Right: global::Erlang.Pattern.Variable binding }:
                        rows.Add(new ConsHeadTupleRow(
                            branch,
                            tuple,
                            tag,
                            binding
                        ));
                        break;
                    case AliasPattern { Right: global::Erlang.Pattern.Literal { Value: Atom tag }, Left: global::Erlang.Pattern.Variable binding }:
                        rows.Add(new ConsHeadTupleRow(
                            branch,
                            tuple,
                            tag,
                            binding
                        ));
                        break;
                    default:
                        return null;
                }
            }

            var buckets = rows.GroupBy(row => row.Tuple.Items.Count).OrderByDescending(group => group.Key).ToArray();
            if (buckets.All(bucket => bucket.Count() < 2)
                || buckets.Any(bucket => bucket.Select(row => row.Tag).Distinct().Count() != bucket.Count()))
            {
                return null;
            }

            return buckets.Select(bucket =>
            {
                int[] fields = [0, .. Enumerable.Range(1, bucket.Key - 1).Where(field =>
                    bucket.Any(row => !DiscardStructuralTupleField(row.Tuple.Items[field], unusedFields[row.Branch])))];

                return new ConsHeadTupleGroup(bucket.Key, bucket.ToArray(), fields);
            }).ToArray();
        }

        private void EmitConsHeadTupleGroups(
            ConsHeadTupleGroup[] groups,
            int head,
            int firstScratch,
            int fallback,
            Action<int, int> emitBranch
        )
        {
            int[] labels = groups.Length == 1 ? [] : groups.Select(_ => Label()).ToArray();
            if (groups.Length == 1)
            {
                EmitTupleShapeTest(
                    XRegisterTag,
                    head,
                    groups[0].Arity,
                    fallback
                );
            }
            else
            {
                owner.EmitTupleArityDispatch(
                    fallback,
                    groups.Select((group, index) => (group.Arity, labels[index])).Reverse().ToArray(),
                    head
                );
            }

            var compare = Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            for (int index = 0; index < groups.Length; index++)
            {
                var group = groups[index];
                if (groups.Length > 1)
                {
                    Mark(labels[index]);
                }

                for (int field = 0; field < group.Fields.Length; field++)
                {
                    owner.EmitTuplePart(head, group.Fields[field], firstScratch + field);
                }

                if (group.Rows is [var single])
                {
                    owner.Op(ExactEqualTest, LabelTag, fallback);
                    owner.Operand(XRegisterTag, firstScratch);
                    owner.LiteralOperand(single.Tag);
                    emitBranch(single.Branch, fallback);
                    continue;
                }

                var targets = group.Rows.OrderByDescending(row => (Term)row.Tag, compare)
                    .Select(row => (Row: row, Label: Label())).ToArray();
                owner.EmitMembershipSelect(
                    firstScratch,
                    fallback,
                    targets.Reverse().Select(target => ((Term)target.Row.Tag, target.Label))
                );
                foreach (var target in targets)
                {
                    Mark(target.Label);
                    emitBranch(target.Row.Branch, fallback);
                }
            }
        }

        private static HashSet<string> ReadStructuralEntryUnusedFields(Clause clause)
        {
            var unusedFields = clause.Patterns.SelectMany(Semantics.Variables)
                .GroupBy(name => name, StringComparer.Ordinal)
                .Where(group => group.Count() == 1)
                .Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
            try
            {
                unusedFields.ExceptWith(ClosureCaptures.ReadEntryReferences(clause, unusedFields));
            }
            catch (NotSupportedException)
            {
                unusedFields.Clear();
            }

            return unusedFields;
        }

        private static bool DiscardStructuralTupleField(Pattern member, HashSet<string> unusedFields) =>
            member is global::Erlang.Pattern.Any { } or global::Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard }
            || member is global::Erlang.Pattern.Variable variable && unusedFields.Contains(variable.Name);
    }
}
