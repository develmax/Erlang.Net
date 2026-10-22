namespace Erlang.Tests;

internal static class HamtIndex429HostChecks
{
    private static void Require(bool value) => HamtIndex429HostAccess.Require(value);

    internal static async Task Graph()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(_ => { });
        MapTerm map = HamtIndex429HostAccess.Map(false);
        using IDisposable first = HamtIndex429HostAccess.Acquire(runtime, map);
        using IDisposable second = HamtIndex429HostAccess.Acquire(runtime, map);
        object index = HamtIndex429HostAccess.Property(first, HamtIndex429HostNames.Index);
        Require(ReferenceEquals(index, HamtIndex429HostAccess.Property(second, HamtIndex429HostNames.Index)));
        Term[] children = Cons.Items(HamtIndex429HostAccess.Protocol(runtime, HamtIndex429HostNames.Children, map)).ToArray();
        Term[] again = Cons.Items(HamtIndex429HostAccess.Protocol(runtime, HamtIndex429HostNames.Children, map)).ToArray();
        Require(children.Length == again.Length && children.Zip(again).All(pair => ReferenceEquals(pair.First, pair.Second)));
        var stack = new Stack<Term>(children);
        int leafs = 0, nodes = 0;
        while (stack.TryPop(out var node))
        {
            if (node is Cons pair)
            {
                Require(map.TryGet(pair.Head, out var stored) && ReferenceEquals(stored, pair.Tail));
                leafs++;
            }
            else if (node is TupleTerm collision)
            {
                foreach (Term item in collision.Items) stack.Push(item);
            }
            else
            {
                Require(node.GetType().Name == HamtIndex429HostNames.Node);
                foreach (Term child in Cons.Items(HamtIndex429HostAccess.Protocol(runtime, HamtIndex429HostNames.Children, node))) stack.Push(child);
                nodes++;
                Require(node.Equals(node) && node.GetHashCode() == node.GetHashCode());
            }
        }
        Require(leafs == map.Entries.Count && nodes > 0);
        Term info = HamtIndex429HostAccess.Protocol(runtime, HamtIndex429HostNames.Info, map);
        var leafInfo = (TupleTerm)Cons.Items(info).Single(value => value is TupleTerm tuple && tuple.Items[0].Equals(Term.A("leafs")));
        Require(leafInfo.Items[1].Equals(Term.I(leafs)));
        IReadOnlyList<KeyValuePair<Term, Term>> actual = MapIteration.Entries(map, runtime);
        Require(actual.Count == map.Entries.Count && actual.All(pair => map.Get(pair.Key).Equals(pair.Value)));
        Term ordered = MapIteration.Iterator(map, HamtIndex429HostNames.Ordered);
        Require(MapIteration.Entries(ordered, runtime).Count == map.Entries.Count);
        using IDisposable after = HamtIndex429HostAccess.Acquire(runtime, map);
        Require(ReferenceEquals(index, HamtIndex429HostAccess.Property(after, HamtIndex429HostNames.Index)));
        await using var foreign = new ProcessRuntime();
        await foreign.InitializeModulesAsync(_ => { });
        bool rejected = false;
        try
        {
            foreign.ReserveExternalTerm(children.First(value => value.GetType().Name == HamtIndex429HostNames.Node));
        }
        catch (ArgumentException)
        {
            rejected = true;
        }
        Require(rejected);
    }

    internal static async Task Lifetime()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(_ => { });
        var holder = HamtIndex429HostAccess.CreateHolder(runtime);
        HamtIndex429HostAccess.CheckHeld(runtime, holder);
        holder[0] = default;
        HamtIndex429HostAccess.Collect();
        Require(HamtIndex429HostAccess.Rows(runtime) == 0 && HamtIndex429HostAccess.ActiveEpochScopes(runtime) == 0);
    }

    internal static async Task Close()
    {
        var runtime = new ProcessRuntime();
        try
        {
            await runtime.InitializeModulesAsync(_ => { });
            MapTerm map = HamtIndex429HostAccess.Map(true);
            IDisposable[] reads = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => HamtIndex429HostAccess.Acquire(runtime, map))));
            try
            {
                object winner = HamtIndex429HostAccess.Property(reads[0], HamtIndex429HostNames.Index);
                Require(reads.All(read => ReferenceEquals(winner, HamtIndex429HostAccess.Property(read, HamtIndex429HostNames.Index))));
                Require(HamtIndex429HostAccess.Rows(runtime) == 1 && HamtIndex429HostAccess.ActiveEpochScopes(runtime) == 1);
                Task shutdown = runtime.DisposeAsync().AsTask();
                await Task.Delay(30);
                Require(!shutdown.IsCompleted && (bool)HamtIndex429HostAccess.Property(HamtIndex429HostAccess.Owner(runtime), HamtIndex429HostNames.Closing));
                bool rejected = false;
                try
                {
                    using IDisposable invalid = HamtIndex429HostAccess.Acquire(runtime, map);
                }
                catch (ObjectDisposedException)
                {
                    rejected = true;
                }
                Require(rejected);
                foreach (var read in reads) read.Dispose();
                await shutdown.WaitAsync(TimeSpan.FromSeconds(10));
                Require(HamtIndex429HostAccess.Rows(runtime) == 0 && HamtIndex429HostAccess.ActiveEpochScopes(runtime) == 0);
            }
            finally
            {
                foreach (var read in reads) read.Dispose();
            }
        }
        finally
        {
            await runtime.DisposeAsync();
        }
    }
}
