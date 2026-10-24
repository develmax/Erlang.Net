namespace Erlang.Tests;

internal static class DirtyNative419Checks
{
    internal static async Task Lanes()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(_ => { });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            int normal = Environment.CurrentManagedThreadId, cpu = 0, io = 0;
            await DirtyNative419Access.Schedule(
                context,
                DirtyNative419Names.Cpu,
                [],
                (c, _) =>
      {
          cpu = Environment.CurrentManagedThreadId;
          DirtyNative419Access.Require(cpu != normal && DirtyNative419Access.Kind(c) == DirtyNative419Names.Cpu && !DirtyNative419Access.NormalTurn(c));
          DirtyNative419Access.Require(DirtyNative419Access.NativeSchedulerIdentity(c).Equals(Term.I(0)));
          DirtyNative419Access.Require(runtime.Modules.Call(
              c,
              DirtyNative419Names.Runtime,
              DirtyNative419Names.SystemInfo,
              Term.A(DirtyNative419Names.SchedulerIdentity)
          ).GetAwaiter().GetResult() is Integer publicIdentity && publicIdentity.Value > 0);

          return Nil.Value;
      }
            );
            await DirtyNative419Access.Schedule(
                context,
                DirtyNative419Names.Io,
                [],
                (c, _) =>
      {
          io = Environment.CurrentManagedThreadId;
          DirtyNative419Access.Require(io != normal && io != cpu && DirtyNative419Access.Kind(c) == DirtyNative419Names.Io && !DirtyNative419Access.NormalTurn(c));

          return Nil.Value;
      }
            );
            foreach (string query in DirtyNative419Names.Counts)
                DirtyNative419Access.Require((await runtime.Modules.Call(
                    context,
                    DirtyNative419Names.Runtime,
                    DirtyNative419Names.SystemInfo,
                    Term.A(query)
                )).Equals(Term.I(1)));
            DirtyNative419Access.Require(DirtyNative419Access.Kind(context) == DirtyNative419Names.Normal && DirtyNative419Access.NormalTurn(context));

            return true;
        });
    }

    internal static async Task Cancellation()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(_ => { });
        await using var held = new RuntimeNodeFoundationHostActor(runtime);
        await using var queued = new RuntimeNodeFoundationHostActor(runtime);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        int ran = 0;
        Task<bool>? first = null;
        Task<Term>? second = null;
        try
        {
            first = held.InvokeAsync(async context =>
         {
             await DirtyNative419Access.Schedule(
                 context,
                 DirtyNative419Names.Cpu,
                 [],
                 (_, _) =>
    {
        entered.Set();
        DirtyNative419Access.Require(release.Wait(DirtyNative419Names.Deadline));

        return Nil.Value;
    }
             );

             return true;
         });
            DirtyNative419Access.Require(entered.Wait(DirtyNative419Names.Deadline));
            ProcessContext q = await queued.Ready;
            second = await queued.Invoke(context => DirtyNative419Access.Schedule(
                context,
                DirtyNative419Names.Cpu,
                [],
                (_, _) =>
         {
             Interlocked.Increment(ref ran);

             return Nil.Value;
         }
            ));
            DirtyNative419Access.Cancel(q);
            release.Set();
            bool canceled = false;
            try
            {
                await second;
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }
            await first;
            DirtyNative419Access.Require(canceled && Volatile.Read(ref ran) == 0);
        }
        finally
        {
            release.Set();
            if (first is not null) await first;
        }
    }

    internal static async Task Close()
    {
        var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(_ => { });
        var actor = new RuntimeNodeFoundationHostActor(runtime);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Task<bool>? work = null;
        Task? closing = null;
        try
        {
            work = actor.InvokeAsync(async context =>
         {
             await DirtyNative419Access.Schedule(
                 context,
                 DirtyNative419Names.Io,
                 [],
                 (_, _) =>
    {
        entered.Set();
        DirtyNative419Access.Require(release.Wait(DirtyNative419Names.Deadline));

        return Nil.Value;
    }
             );

             return true;
         });
            DirtyNative419Access.Require(entered.Wait(DirtyNative419Names.Deadline));
            ProcessContext owner = await actor.Ready;
            var physical = DebugDirtyQueue424Access.PhysicalWork(runtime, owner);
            closing = runtime.DisposeAsync().AsTask();
            DirtyNative419Access.Require(!closing.IsCompleted && !actor.Process.Completion.IsCompleted);
            release.Set();
            await closing.WaitAsync(DirtyNative419Names.Deadline);
            DirtyNative419Access.Require((await physical.Outcome).Equals(Nil.Value));
            await physical.Settled;
            await RequireGuestCancellation(work, owner);
            object state = DirtyNative419Access.Snapshot(runtime);
            DirtyNative419Access.Require(DirtyNative419Access.Field<bool>(state, DirtyNative419Names.Joined) && !DirtyNative419Access.Field<bool>(state, DirtyNative419Names.CpuAlive) && !DirtyNative419Access.Field<bool>(state, DirtyNative419Names.IoAlive) && DirtyNative419Access.Field<int>(state, DirtyNative419Names.Pending) == 0 && DirtyNative419Access.Field<int>(state, DirtyNative419Names.Running) == 0);
        }
        finally
        {
            release.Set();
            if (closing is not null) await closing;
            await runtime.DisposeAsync();
        }
    }

    internal static async Task Shutdown()
    {
        await ShutdownScenario(true);
        await ShutdownScenario(false);
    }

    private static async Task ShutdownScenario(bool halt)
    {
        var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(_ => { });
        var actor = new RuntimeNodeFoundationHostActor(runtime);
        var killer = new RuntimeNodeFoundationHostActor(runtime);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Task<bool>? work = null;
        Task? closing = null;
        try
        {
            work = actor.InvokeAsync(async context =>
         {
             await DirtyNative419Access.Schedule(
                 context,
                 DirtyNative419Names.Cpu,
                 [],
                 (_, _) =>
    {
        entered.Set();
        DirtyNative419Access.Require(release.Wait(DirtyNative419Names.Deadline));

        return Nil.Value;
    }
             );

             return true;
         });
            DirtyNative419Access.Require(entered.Wait(DirtyNative419Names.Deadline));
            ProcessContext owner = await actor.Ready;
            var physical = DebugDirtyQueue424Access.PhysicalWork(runtime, owner);
            if (halt) closing = Task.WhenAll(runtime.RequestImmediateHalt(0), runtime.HaltQuiescence);
            else
            {
                await killer.Invoke(context =>
            {
                runtime.Exit(context, runtime.InitializationPid, Term.A(DirtyNative419Names.Kill));

                return true;
            });
                closing = runtime.DisposeAsync().AsTask();
            }
            object state = DirtyNative419Access.Snapshot(runtime);
            DirtyNative419Access.Require(DirtyNative419Access.Field<bool>(state, DirtyNative419Names.Closed) && !closing.IsCompleted && !actor.Process.Completion.IsCompleted);
            release.Set();
            await closing.WaitAsync(DirtyNative419Names.Deadline);
            DirtyNative419Access.Require((await physical.Outcome).Equals(Nil.Value));
            await physical.Settled;
            await RequireGuestCancellation(work, owner);
            state = DirtyNative419Access.Snapshot(runtime);
            DirtyNative419Access.Require(DirtyNative419Access.Field<bool>(state, DirtyNative419Names.Joined) && !DirtyNative419Access.Field<bool>(state, DirtyNative419Names.CpuAlive) && !DirtyNative419Access.Field<bool>(state, DirtyNative419Names.IoAlive));
        }
        finally
        {
            release.Set();
            if (closing is not null) await closing;
            await runtime.DisposeAsync();
        }
    }

    private static async Task RequireGuestCancellation(Task work, ProcessContext owner)
    {
        bool canceled = false;
        try
        {
            await work;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }
        DirtyNative419Access.Require(canceled && owner.Cancellation.IsCancellationRequested);
    }

    internal static async Task Roots()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(_ => { });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var weak = await actor.InvokeAsync(async context =>
      {
          var created = DirtyNative419Access.CreateWeakWork(context);
          await created.Work;

          return (created.Argument, created.Ambient);
      });
        // A subsequent turn ensures the previous command/result continuation has left
        // the actor dispatcher before checking the permanently idle native worker.
        await actor.Invoke(_ => true);
        for (int i = 0; i < 8; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            await Task.Yield();
        }
        DirtyNative419Access.Require(!weak.Argument.TryGetTarget(out _) && !weak.Ambient.TryGetTarget(out _));
    }
}
