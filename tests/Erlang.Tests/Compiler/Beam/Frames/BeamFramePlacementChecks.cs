using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamFramePlacementChecks
{
    internal const string TestName = "compiler/beam/places-frames-on-owned-virtual-control-flow";

    internal static Task Run()
    {
        CheckDisjointDemands();
        CheckMixedOwners();
        CheckPhiPredecessors();
        CheckExceptionEdges();
        CheckReceiveLoopHeader();

        return Task.CompletedTask;
    }

    private static void CheckDisjointDemands()
    {
        BeamFrameBlock[] graph =
        [
            Block(0, [20, 10, 90]),
            Block(10, [11], requiresFrame: true),
            Block(11),
            Block(20, [21], requiresFrame: true),
            Block(21),
            Block(90),
            Block(99, requiresFrame: true)
        ];
        BeamFramePlan plan = RequirePlan(graph, [20, 10]);
        Require(plan.Owners[0] is null && plan.Owners[10] == 10 && plan.Owners[11] == 10
            && plan.Owners[20] == 20 && plan.Owners[21] == 20 && plan.Owners[90] is null
            && !plan.Owners.ContainsKey(99));
    }

    private static void CheckMixedOwners()
    {
        BeamFrameBlock[] graph =
        [
            Block(0, [2, 1]),
            Block(1, [3], requiresFrame: true),
            Block(2, [3]),
            Block(3)
        ];
        BeamFramePlan plan = RequirePlan(graph, [0]);
        Require(plan.Owners.Values.All(owner => owner == 0));

        // The condition dominates the join and provides a nearer legal
        // allocation than function entry. Its sibling needs no frame.
        BeamFrameBlock[] nested =
        [
            Block(0, [100, 8]),
            Block(8, [2, 1]),
            Block(1, [3], requiresFrame: true),
            Block(2, [3]),
            Block(3),
            Block(100)
        ];
        BeamFramePlan nestedPlan = RequirePlan(nested, [8]);
        Require(nestedPlan.Owners[100] is null && nestedPlan.Owners[3] == 8);
    }

    private static void CheckPhiPredecessors()
    {
        BeamFrameBlock[] graph =
        [
            Block(0, [2, 1]),
            Block(1, [3]),
            Block(2, [3]),
            Block(3, [4], requiresFrame: true),
            Block(4)
        ];
        BeamFramePlan independentJoin = RequirePlan(graph, [3]);
        Require(independentJoin.Owners[1] is null && independentJoin.Owners[2] is null
            && independentJoin.Owners[3] == 3);
        graph[3] = graph[3] with { PhiPredecessors = [1, 2] };
        BeamFramePlan phiJoin = RequirePlan(graph, [0]);
        Require(phiJoin.Owners[1] == 0 && phiJoin.Owners[2] == 0 && phiJoin.Owners[3] == 0);
        graph[3] = graph[3] with { PhiPredecessors = [0, 2] };
        Require(BeamFramePlacement.Place(0, graph) is null);
    }

    private static void CheckExceptionEdges()
    {
        BeamFrameBlock[] graph =
        [
            Block(0, [2, 1]),
            Block(1, [4, 3], requiresFrame: true),
            Block(2, [4]),
            Block(3),
            Block(4)
        ];
        BeamFramePlan shared = RequirePlan(graph, [1], sharedException: 4);
        Require(shared.Owners[1] == 1 && shared.Owners[3] == 1 && shared.Owners[2] is null
            && !shared.Owners.ContainsKey(4));

        BeamFramePlan ordinary = RequirePlan(graph, [0]);
        Require(ordinary.Owners[4] == 0);
        graph[4] = graph[4] with { RequiresFrame = true };
        Require(BeamFramePlacement.Place(0, graph, sharedException: 4) is null);

        // Lowering classified both try-tag installation and handler entry
        // as demands. The handler remains an ordinary dominated block.
        graph[2] = Block(2);
        BeamFramePlan protectedRegion = RequirePlan(graph, [1]);
        Require(protectedRegion.Owners[4] == 1 && protectedRegion.Owners[2] is null);
    }

    private static void CheckReceiveLoopHeader()
    {
        BeamFrameBlock[] graph =
        [
            Block(0, [10]),
            Block(10, [20, 30], requiresFrame: true) with { IsReceiveLoopHeader = true },
            Block(20, [10]),
            Block(30)
        ];
        BeamFramePlan plan = RequirePlan(graph, [0]);
        Require(plan.Owners[10] == 0 && plan.Owners[20] == 0 && plan.Owners[30] == 0);
    }

    private static BeamFrameBlock Block(int label, int[]? successors = null, bool requiresFrame = false) => new(
        label,
        successors ?? [],
        [],
        requiresFrame
    );

    private static BeamFramePlan RequirePlan(
        IReadOnlyList<BeamFrameBlock> graph,
        int[] origins,
        int? sharedException = null
    )
    {
        BeamFramePlan? plan = BeamFramePlacement.Place(0, graph, sharedException);
        if (plan is null || !plan.Origins.SequenceEqual(origins))
        {
            throw new InvalidOperationException("The frame origins disagree with pinned control-flow placement.");
        }

        return plan;
    }

    private static void Require(bool valid)
    {
        if (!valid)
        {
            throw new InvalidOperationException("The active frame does not agree with the virtual control-flow edge.");
        }
    }
}
