using System.Collections.ObjectModel;

namespace Erlang.Compiler;

internal static class BeamFramePlacement
{
    internal static BeamFramePlan? Place(int entry, IReadOnlyList<BeamFrameBlock> blocks, int? sharedException = null)
    {
        var byLabel = new Dictionary<int, BeamFrameBlock>();
        foreach (BeamFrameBlock block in blocks)
        {
            if (!byLabel.TryAdd(block.Label, block))
            {
                return null;
            }
        }

        if (!byLabel.ContainsKey(entry) || sharedException is int exception
            && (exception == entry || !byLabel.TryGetValue(exception, out BeamFrameBlock? terminal)
                || terminal.RequiresFrame || terminal.IsReceiveLoopHeader
                || terminal.Successors.Count != 0 || terminal.PhiPredecessors.Count != 0))
        {
            return null;
        }

        foreach (BeamFrameBlock block in blocks)
        {
            if (block.Successors.Any(successor => !byLabel.ContainsKey(successor))
                || block.PhiPredecessors.Any(predecessor => !byLabel.TryGetValue(predecessor, out BeamFrameBlock? incoming)
                    || !incoming.Successors.Contains(block.Label)))
            {
                return null;
            }
        }

        var order = ReversePostorder(entry, byLabel);
        var ordinal = order.Select((label, index) => (label, index)).ToDictionary(pair => pair.label, pair => pair.index);
        if (order.Any(label => byLabel[label].PhiPredecessors.Any(predecessor => !ordinal.ContainsKey(predecessor))))
        {
            return null;
        }

        int[] immediate = FindImmediateDominators(order, byLabel, ordinal);
        var attempted = new HashSet<int>();
        var frames = new List<int>();
        var ancestors = new Stack<(int Index, int FrameCount)>();
        int current = 0;
        while (current < order.Count)
        {
            if (!byLabel[order[current]].RequiresFrame)
            {
                ancestors.Push((current, frames.Count));
                current++;
                continue;
            }

            if (TryPlace(current))
            {
                current++;
                continue;
            }

            int required = current;
            bool placed = false;
            // A failed demand unwinds to the closest eligible earlier
            // block in the RPO walk. Restore that block's frame snapshot,
            // keep the attempted set, and revisit its suffix.
            while (ancestors.TryPop(out var ancestor))
            {
                if (!Dominates(ancestor.Index, required))
                {
                    continue;
                }

                frames.RemoveRange(ancestor.FrameCount, frames.Count - ancestor.FrameCount);
                if (TryPlace(ancestor.Index))
                {
                    current = ancestor.Index + 1;
                    placed = true;
                    break;
                }

                required = ancestor.Index;
            }

            if (!placed)
            {
                return null;
            }
        }

        var selected = frames.ToHashSet();
        var owners = new Dictionary<int, int?>();
        for (int index = 0; index < order.Count; index++)
        {
            int label = order[index];
            if (label == sharedException)
            {
                continue;
            }

            int? inherited = index == 0 ? null : owners[order[immediate[index]]];
            bool beginsFrame = selected.Contains(index);
            if (beginsFrame && inherited is not null || !beginsFrame && inherited is null && byLabel[label].RequiresFrame)
            {
                return null;
            }

            owners.Add(label, beginsFrame ? label : inherited);
        }

        foreach (var (label, owner) in owners)
        {
            foreach (int successor in byLabel[label].Successors)
            {
                if (successor == sharedException)
                {
                    continue;
                }

                // The first entry of an owner must be from the unframed
                // state. Every ordinary join keeps the same active frame.
                bool entersFrame = selected.Contains(ordinal[successor]);
                if (entersFrame ? owner.HasValue : owners[successor] != owner)
                {
                    return null;
                }
            }
        }

        return new(
            frames.AsEnumerable().Reverse().Select(index => order[index]).ToList().AsReadOnly(),
            new ReadOnlyDictionary<int, int?>(owners)
        );

        bool TryPlace(int index)
        {
            if (!attempted.Add(index) || frames.Any(frame => Dominates(frame, index)))
            {
                return true;
            }

            BeamFrameBlock candidate = byLabel[order[index]];
            if (candidate.IsReceiveLoopHeader || candidate.PhiPredecessors.Any(predecessor => !Owns(predecessor)))
            {
                return false;
            }

            var pending = new Stack<int>();
            var visited = new HashSet<int>();
            pending.Push(candidate.Label);
            while (pending.TryPop(out int label))
            {
                if (!visited.Add(label) || label == sharedException)
                {
                    continue;
                }

                if (!Owns(label))
                {
                    return false;
                }

                foreach (int successor in byLabel[label].Successors)
                {
                    pending.Push(successor);
                }
            }

            frames.Add(index);

            return true;

            bool Owns(int label) => label == sharedException || Dominates(index, ordinal[label]);
        }

        bool Dominates(int ancestor, int block)
        {
            while (block > ancestor)
            {
                block = immediate[block];
            }

            return block == ancestor;
        }
    }

    private static List<int> ReversePostorder(int entry, IReadOnlyDictionary<int, BeamFrameBlock> blocks)
    {
        var postorder = new List<int>();
        var visited = new HashSet<int> { entry };
        var pending = new Stack<(int Label, int Edge)>();
        pending.Push((entry, 0));
        while (pending.TryPop(out var next))
        {
            IReadOnlyList<int> successors = blocks[next.Label].Successors;
            if (next.Edge == successors.Count)
            {
                postorder.Add(next.Label);
                continue;
            }

            pending.Push((next.Label, next.Edge + 1));
            int successor = successors[next.Edge];
            if (visited.Add(successor))
            {
                pending.Push((successor, 0));
            }
        }

        postorder.Reverse();

        return postorder;
    }

    private static int[] FindImmediateDominators(
        IReadOnlyList<int> order,
        IReadOnlyDictionary<int, BeamFrameBlock> blocks,
        IReadOnlyDictionary<int, int> ordinal
    )
    {
        var predecessors = new List<int>[order.Count];
        for (int index = 0; index < order.Count; index++)
        {
            predecessors[index] = [];
        }

        for (int index = 0; index < order.Count; index++)
        {
            foreach (int successor in blocks[order[index]].Successors)
            {
                predecessors[ordinal[successor]].Add(index);
            }
        }

        var immediate = new int[order.Count];
        Array.Fill(immediate, -1);
        immediate[0] = 0;
        bool changed;
        do
        {
            changed = false;
            for (int index = 1; index < order.Count; index++)
            {
                int common = -1;
                foreach (int predecessor in predecessors[index])
                {
                    if (immediate[predecessor] >= 0)
                    {
                        common = common < 0 ? predecessor : Intersect(common, predecessor, immediate);
                    }
                }

                if (immediate[index] != common)
                {
                    immediate[index] = common;
                    changed = true;
                }
            }
        } while (changed);

        return immediate;
    }

    private static int Intersect(int left, int right, int[] immediate)
    {
        while (left != right)
        {
            if (left > right)
            {
                left = immediate[left];
            }
            else
            {
                right = immediate[right];
            }
        }

        return left;
    }
}
