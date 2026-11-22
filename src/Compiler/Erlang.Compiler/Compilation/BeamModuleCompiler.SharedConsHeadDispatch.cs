using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record SharedConsHeadDispatch(
            int Fallback,
            int[] Order,
            (int Entry, int Failure)[] Arms,
            Dictionary<int, (int Entry, (Term Key, int Label)[] Tags)> ArityStarts
        );

        private SharedConsHeadDispatch EmitSharedConsHeadDispatch(
            IReadOnlyList<(int Arity, Atom Tag)> shapes,
            int nonCons,
            bool checkCons
        )
        {
            const int input = 0;
            const int head = 1;
            int fallback = Label();
            var order = new List<int>(shapes.Count);
            var arms = new (int Entry, int Failure)[shapes.Count];
            var starts = new Dictionary<int, (int Entry, (Term Key, int Label)[] Tags)>();
            var arities = new List<(int Arity, int Label)>();
            var byArity = shapes.Select((shape, branch) => (shape.Arity, shape.Tag, Branch: branch))
                .GroupBy(shape => shape.Arity).OrderByDescending(group => group.Key);
            var compareTags = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));

            // Tuple tags and arities are disjoint. Prefixes that use the same
            // tag still retry in source order with their original list parts.
            foreach (var arity in byArity)
            {
                int start = order.Count;
                int entry = Label();
                arities.Add((arity.Key, entry));
                var tags = new List<(Term Key, int Label)>();
                foreach (var tag in arity.GroupBy(shape => shape.Tag).OrderByDescending(group => group.Key, compareTags))
                {
                    int tagEntry = Label();
                    int armEntry = tagEntry;
                    int[] tagArms = tag.Select(shape => shape.Branch).ToArray();
                    tags.Add((tag.Key, tagEntry));
                    for (int index = 0; index < tagArms.Length; index++)
                    {
                        int branch = tagArms[index];
                        int failure = index + 1 < tagArms.Length ? Label() : fallback;
                        order.Add(branch);
                        arms[branch] = (armEntry, failure);
                        armEntry = failure;
                    }
                }

                tags.Reverse();
                starts.Add(order[start], (entry, tags.ToArray()));
            }

            if (checkCons)
            {
                owner.EmitNonemptyList(input, nonCons);
            }

            owner.EmitListParts(
                input,
                XRegisterTag,
                head,
                XRegisterTag,
                input
            );
            arities.Reverse();
            owner.EmitTupleArityDispatch(fallback, arities, head);

            return new(
                fallback,
                order.ToArray(),
                arms,
                starts
            );
        }

        private void EmitSharedConsHeadEntry(SharedConsHeadDispatch dispatch, int branch)
        {
            if (dispatch.ArityStarts.TryGetValue(branch, out var arity))
            {
                const int head = 1;
                const int tag = 2;
                Mark(arity.Entry);
                owner.EmitTuplePart(head, 0, tag);
                if (arity.Tags is [(Atom key, _)])
                {
                    owner.EmitAtomTest(tag, key.Name, dispatch.Fallback);
                }
                else
                {
                    owner.EmitMembershipSelect(tag, dispatch.Fallback, arity.Tags);
                }
            }

            Mark(dispatch.Arms[branch].Entry);
        }
    }
}
