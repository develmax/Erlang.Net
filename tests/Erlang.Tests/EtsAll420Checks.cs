using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Erlang;
namespace Erlang.Tests;

internal static class EtsAll420Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(EtsAll420Names.Failure);
    }

    private static void Install(ProcessRuntime r)
    {
        RuntimeNativePrimitiveProviders.Register(r, r.Modules, RuntimeNativePrimitives.EtsStorageCore);
        RuntimeBootstrapEtsHeir419.Register(r, r.Modules);
        RuntimeBootstrapEtsRename418.Register(r, r.Modules);
        RuntimeBootstrapEtsAll420.Register(r, r.Modules);
    }

    private static ValueTask<Term> Call(
        ProcessRuntime r,
        ProcessContext c,
        string f,
        params Term[] args
    ) => r.Modules.Call(
        c,
        EtsAll420Names.Ets,
        f,
        args
    );

    private static Task<Term> Bind(RuntimeNodeFoundationHostActor actor, ProcessRuntime r, int lane) => actor.InvokeAsync(async c => await r.Modules.Call(
        c,
        EtsAll420Names.Erlang,
        EtsAll420Names.ProcessFlag,
        Term.A(EtsAll420Names.Scheduler),
        Term.I(lane)
    ));

    private static async Task<List<Term>> Gather(ProcessRuntime r, ProcessContext c)
    {
        var reference = (ReferenceTerm)await Call(r, c, EtsAll420Names.Request);
        var lists = new List<Term>();
        for (int i = 0; i < r.ActorDispatchCapacity; i++)
        {
            var reply = await c.Mailbox.ReceiveAsync<TupleTerm>(v => v is TupleTerm { Items.Count: 2 } t && t.Items[0].Equals(reference) ? t : null, EtsAll420Names.Deadline, c.Cancellation);
            Require(reply is not null);
            lists.Add(reply!.Items[1]);
        }
        Require(await c.Mailbox.ReceiveAsync<Term>(v => v is TupleTerm { Items.Count: 2 } t && t.Items[0].Equals(reference) ? v : null, TimeSpan.Zero, c.Cancellation) is null);

        return lists;
    }

    private static Term[] Items(Term value) => Cons.Items(value).ToArray();

    internal static async Task Binding()
    {
        await using var r = new ProcessRuntime(1);
        Require(!r.Modules.HasExport(EtsAll420Names.Ets, EtsAll420Names.Request, 0));
        RuntimeBootstrapEtsAll420.Register(r, r.Modules);
        RuntimeBootstrapEtsAll420.Register(r, r.Modules);
        Require(r.Modules.HasExport(EtsAll420Names.Ets, EtsAll420Names.Request, 0) && r.Modules.SourceOwner(EtsAll420Names.Ets, EtsAll420Names.Request, 0) is null);
        await using var foreign = new ProcessRuntime(1);
        bool rejected = false;
        try
        {
            RuntimeBootstrapEtsAll420.Register(r, foreign.Modules);
        }
        catch (ArgumentException)
        {
            rejected = true;
        }
        Require(rejected && !foreign.Modules.HasExport(EtsAll420Names.Ets, EtsAll420Names.Request, 0));
        await using var rollback = new ProcessRuntime(1);
        var sentinel = new InvalidOperationException(EtsAll420Names.Failure);
        try
        {
            await rollback.InitializeModulesAsync(registry =>
            {
                RuntimeBootstrapEtsAll420.Register(rollback, registry);
                throw sentinel;
            });
        }
        catch (InvalidOperationException e) when (ReferenceEquals(e, sentinel)) { }
        Require(!rollback.Modules.HasExport(EtsAll420Names.Ets, EtsAll420Names.Request, 0));
        await using var actor = new RuntimeNodeFoundationHostActor(r);
        Require((await actor.InvokeAsync(async c => await Gather(r, c))).Count == 1);
    }

    internal static async Task Directory()
    {
        await using var r = new ProcessRuntime(EtsAll420Names.Capacity);
        Install(r);
        await using var first = new RuntimeNodeFoundationHostActor(r);
        await using var second = new RuntimeNodeFoundationHostActor(r);
        await using var observer = new RuntimeNodeFoundationHostActor(r);
        _ = await Bind(first, r, 1);
        _ = await Bind(second, r, 2);
        Term named = await first.InvokeAsync(async c => await Call(
            r,
            c,
            EtsAll420Names.New,
            Term.A(EtsAll420Names.First),
            Term.List(Term.A(EtsAll420Names.Named), Term.A(EtsAll420Names.Public))
        ));
        Term anonymous = await second.InvokeAsync(async c => await Call(
            r,
            c,
            EtsAll420Names.New,
            Term.A(EtsAll420Names.Anon),
            Nil.Value
        ));
        _ = await first.InvokeAsync(async c => await Call(
            r,
            c,
            EtsAll420Names.Give,
            named,
            second.Process.Pid,
            Nil.Value
        ));
        await second.InvokeAsync(async c =>
      {
          Require(await c.Mailbox.ReceiveAsync<Term>(
              v => v is TupleTerm { Items.Count: 4 } t && t.Items[0].Equals(Term.A(EtsAll420Names.Transfer)) ? v : null,
              EtsAll420Names.Deadline,
              c.Cancellation
          ) is not null);

          return true;
      });
        Term renamed = await second.InvokeAsync(async c => await Call(
            r,
            c,
            EtsAll420Names.Rename,
            named,
            Term.A(EtsAll420Names.Second)
        ));
        ProcessContext target = await observer.Ready;
        var property = typeof(Mailbox).GetProperty(EtsAll420Names.Admitted, Members)!;
        var original = (Action<Term, Term?>?)property.GetValue(target.Mailbox);
        int admitted = 0;
        property.SetValue(
            target.Mailbox,
            (Action<Term, Term?>)((message, from) =>
      {
          if (message is TupleTerm { Items.Count: 2 } t && t.Items[0] is ReferenceTerm)
          {
              Require(from is Atom { Name: EtsAll420Names.SystemSender });
              Interlocked.Increment(ref admitted);
          }
          original?.Invoke(message, from);
      })
        );
        try
        {
            var replies = await observer.InvokeAsync(async c => await Gather(r, c));
            Require(replies.Count == EtsAll420Names.Capacity && replies.Sum(v => Items(v).Length) == 2);
            Require(replies.Count(v => Items(v).Contains(renamed)) == 1 && replies.Count(v => Items(v).Contains(anonymous)) == 1);
            Require(!replies.Any(v => Items(v).Contains(renamed) && Items(v).Contains(anonymous)));
            await (Task)EtsFoundation405Access.Invoke(EtsFoundation405Access.Get(r), EtsAll420Names.Drain)!;
            Require(admitted == EtsAll420Names.Capacity);
            _ = await second.InvokeAsync(async c => await Call(
                r,
                c,
                EtsAll420Names.Delete,
                renamed
            ));
            _ = await second.InvokeAsync(async c => await Call(
                r,
                c,
                EtsAll420Names.Delete,
                anonymous
            ));
            Require((await observer.InvokeAsync(async c => await Gather(r, c))).All(v => v is Nil));
        }
        finally
        {
            property.SetValue(target.Mailbox, original);
        }
    }

    internal static async Task Auxiliary()
    {
        await using var r = new ProcessRuntime(EtsAll420Names.Capacity);
        Install(r);
        RuntimeBootstrapScheduler.Register(r, r.Modules);
        await using var manager = new RuntimeNodeFoundationHostActor(r);
        _ = await Bind(manager, r, 1);
        _ = await manager.InvokeAsync(async c => await r.Modules.Call(
            c,
            EtsAll420Names.Erlang,
            EtsAll420Names.SystemFlag,
            Term.A(EtsAll420Names.SchedulersOnline),
            Term.I(1)
        ));
        Require((await manager.InvokeAsync(async c => await Gather(r, c))).Count == EtsAll420Names.Capacity);
        _ = await manager.InvokeAsync(async c => await r.Modules.Call(
            c,
            EtsAll420Names.Erts,
            EtsAll420Names.WallTime,
            Term.A(EtsAll420Names.True)
        ));
        object dispatcher = typeof(ProcessRuntime).GetField(EtsAll420Names.Dispatcher, Members)!.GetValue(r)!;
        var entered = new ConcurrentDictionary<int, byte>();
        using var release = new ManualResetEventSlim();
        object batch = EtsFoundation405Access.Invoke(
            dispatcher,
            EtsAll420Names.PrepareAux,
            (Action<int>)(identity =>
      {
          entered.TryAdd(identity, 0);
          Require(release.Wait(EtsAll420Names.Deadline));
          var watch = Stopwatch.StartNew();
          while (watch.ElapsedMilliseconds < EtsAll420Names.WorkMilliseconds) Thread.SpinWait(64);
      })
        )!;
        Task completion = (Task)batch.GetType().GetProperty(EtsAll420Names.Completion, Members)!.GetValue(batch)!;
        try
        {
            EtsFoundation405Access.Invoke(batch, EtsAll420Names.Publish);
            Require(SpinWait.SpinUntil(() => entered.Count == EtsAll420Names.Capacity, EtsAll420Names.Deadline));
            var active = r.CaptureManagedSchedulerWallTime();
            Require(active.Enabled && active.Lanes.Count == EtsAll420Names.Capacity);
            Require(active.Lanes.All(lane => lane.ActiveTicks > 0));
        }
        finally
        {
            release.Set();
            await completion.WaitAsync(EtsAll420Names.Deadline);
        }
        Require(entered.Keys.Order().SequenceEqual(new[] { 1, 2, 3 }));
    }

    internal static async Task Close()
    {
        var r = new ProcessRuntime(EtsAll420Names.Capacity);
        Install(r);
        object storage = EtsFoundation405Access.Get(r);
        var actor = new RuntimeNodeFoundationHostActor(r);
        ProcessContext c = await actor.Ready;
        object gate = typeof(ProcessRuntime).GetField(EtsAll420Names.RuntimeGate, Members)!.GetValue(r)!;
        var property = typeof(Mailbox).GetProperty(EtsAll420Names.Incoming, Members)!;
        var original = (Action<Term>?)property.GetValue(c.Mailbox);
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? closing = null;
        property.SetValue(
            c.Mailbox,
            (Action<Term>)(value =>
      {
          if (value is TupleTerm { Items.Count: 2 } t && t.Items[0] is ReferenceTerm)
          {
              Require(!Monitor.IsEntered(gate));
              entered.TrySetResult();
              Require(release.Wait(EtsAll420Names.Deadline));
          }
          original?.Invoke(value);
      })
        );
        try
        {
            _ = await actor.InvokeAsync(async current => await Call(r, current, EtsAll420Names.Request));
            await entered.Task.WaitAsync(EtsAll420Names.Deadline);
            using (ExecutionContext.SuppressFlow()) closing = Task.Run(async () => await r.DisposeAsync());
            Require(SpinWait.SpinUntil(() => (bool)EtsFoundation405Access.Snapshot(storage)[5]!, EtsAll420Names.Deadline));
            Require(!closing.IsCompleted);
            release.Set();
            await closing.WaitAsync(EtsAll420Names.Deadline);
            EtsFoundation405Access.Empty(storage, true);
            object requests = storage.GetType().GetField(EtsAll420Names.AllRequests, Members)!.GetValue(storage)!;
            Require((int)requests.GetType().GetProperty(nameof(ICollection.Count))!.GetValue(requests)! == 0);
        }
        finally
        {
            release.Set();
            property.SetValue(c.Mailbox, original);
            if (closing is not null) await closing.WaitAsync(EtsAll420Names.Deadline);
            await actor.DisposeAsync();
            await r.DisposeAsync();
        }
    }
}
