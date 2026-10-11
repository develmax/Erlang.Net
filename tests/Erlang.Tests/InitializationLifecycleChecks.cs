using Erlang;

internal static class InitializationLifecycleChecks
{
    private static void Require(bool value, string name)
    {
        if (!value)
            throw new InvalidDataException(name);
    }

    public static async Task<int> Run()
    {
        try
        {
            await using var runtime = new ProcessRuntime();
            Require(runtime.IsAlive(runtime.InitializationPid), "initial owner alive");
            Require(runtime.WhereIs("init").Equals(runtime.InitializationPid), "initial owner registered");
            var snapshot = runtime.Modules.Exports;
            await runtime.InitializeModulesAsync(registry =>
            {
                registry.Register(
                    "host_probe",
                    "outer",
                    0,
                    (_, _) => ValueTask.FromResult<Term>(Term.I(1))
                );
                runtime.InitializeModulesAsync(nested => nested.Register(
                    "host_probe",
                    "inner",
                    0,
                    (_, _) => ValueTask.FromResult<Term>(Term.I(2))
                )).GetAwaiter().GetResult();
            }).AsTask().WaitAsync(TimeSpan.FromSeconds(10));
            Require(
                runtime.Modules.HasExport("host_probe", "outer", 0) && runtime.Modules.HasExport("host_probe", "inner", 0),
                "nested atomic configuration"
            );
            Require(!snapshot.Any(key => key.Module == "host_probe"), "immutable prior export snapshot");
            bool rejected = false;
            try
            {
                await runtime.InitializeModulesAsync(registry =>
                {
                    registry.Register(
                        "host_probe",
                        "rolled_back",
                        0,
                        (_, _) => ValueTask.FromResult<Term>(Term.I(3))
                    );
                    registry.Register(
                        "host_probe",
                        "outer",
                        0,
                        (_, _) => ValueTask.FromResult<Term>(Term.I(4))
                    );
                });
            }
            catch (ArgumentException)
            {
                rejected = true;
            }
            Require(
                rejected && !runtime.Modules.HasExport("host_probe", "rolled_back", 0) && runtime.Modules.HasExport("host_probe", "inner", 0),
                "failed registration rollback"
            );
            await Task.WhenAll(Enumerable.Range(0, 100).Select(index => runtime.InitializeModulesAsync(registry => registry.Register(
                "host_probe",
                "queued_" + index,
                0,
                (_, _) => ValueTask.FromResult<Term>(Term.I(index))
            )).AsTask())).WaitAsync(TimeSpan.FromSeconds(10));
            Require(runtime.Modules.Exports.Count(key => key.Module == "host_probe") == 102, "queue applied all configurations");
            var process = runtime.Spawn(async context =>
            {
                Require(context.Self.Id == 1, "first user pid unchanged");
                Term value = await runtime.Modules.Call(context, "host_probe", "inner");
                Require(value.Equals(Term.I(2)), "registered callback invoked");
                context.Runtime.Send(context.Runtime.InitializationPid, Term.Tuple(context.Self, Term.A("get_status")));
                Term status = (await context.ReceiveAsync(value => value).AsTask().WaitAsync(TimeSpan.FromSeconds(10)))!;
                Require(status.Equals(Term.Tuple(Term.A("init"), Term.Tuple(Term.A("started"), Term.A("started")))), "owner mailbox status");

                return Term.A("done");
            });
            await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
            Require(runtime.IsAlive(runtime.InitializationPid), "owner persists after child exit");
            await runtime.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
            Require(!runtime.IsAlive(runtime.InitializationPid), "owner shutdown completed");
            bool canceled = false;
            try
            {
                await runtime.InitializeModulesAsync(_ => throw new InvalidDataException(Erlang.Tests.InitializationCheckDiagnostics.CallbackMustNotRun));
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }
            Require(canceled, "post-shutdown configuration canceled");
            await ShutdownPending();
            await OwnerDeath();
            Console.WriteLine("Local initialization ownership, nested queue, rollback, snapshot, 100 work items, status and disposal passed; native unrun");

            return 0;
        }
        catch (Exception exception)
        {
            Console.WriteLine("INITIALIZATION FAIL " + exception);

            return 1;
        }
    }

    private static async Task ShutdownPending()
    {
        await using var runtime = new ProcessRuntime();
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var running = runtime.InitializeModulesAsync(_ =>
        {
            entered.SetResult();
            if (!release.Wait(TimeSpan.FromSeconds(10)))
                throw new TimeoutException();
        }).AsTask();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        int applied = 0;
        var queued = runtime.InitializeModulesAsync(_ => Interlocked.Increment(ref applied)).AsTask();
        var disposal = runtime.DisposeAsync().AsTask();
        release.Set();
        await running.WaitAsync(TimeSpan.FromSeconds(10));
        await disposal.WaitAsync(TimeSpan.FromSeconds(10));
        bool canceled = false;
        try
        {
            await queued.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }
        Require(canceled && applied == 0, "queued work canceled during owner shutdown");
    }

    private static async Task OwnerDeath()
    {
        await using var runtime = new ProcessRuntime();
        var child = runtime.Spawn(async context =>
        {
            await context.ReceiveAsync(value => value);

            return Term.A("done");
        });
        var killer = runtime.Spawn(context =>
        {
            runtime.Exit(context, runtime.InitializationPid, Term.A("kill"));

            return ValueTask.FromResult<Term>(Term.A("done"));
        });
        await killer.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term childExit = await child.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Require(childExit.Equals(Term.A("shutdown")) && !runtime.IsAlive(runtime.InitializationPid), "owner death stops VM children");
    }
}
