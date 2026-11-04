using System.Text.Json;
using Erlang;

internal sealed class GenStatemRetentionChecks
{
    private int generations;
    private ProcessContext? machineContext;
    private int terminations;



    internal void Register(ModuleRegistry registry)
    {
        registry.Register(
            GenStatemRetentionNames.CallbackModule,
            GenStatemRetentionNames.Init,
            1,
            (context, _) =>
 {
     machineContext = context;

     return ValueTask.FromResult<Term>(Term.Tuple(Term.A(GenStatemRetentionNames.Ok), Term.A(GenStatemRetentionNames.State), Nil.Value));
 }
        );
        registry.Register(
            GenStatemRetentionNames.CallbackModule,
            GenStatemRetentionNames.CallbackMode,
            0,
            (_, _) => ValueTask.FromResult<Term>(Term.A(GenStatemRetentionNames.Mode))
        );
        registry.Register(
            GenStatemRetentionNames.CallbackModule,
            GenStatemRetentionNames.HandleEvent,
            4,
            (_, arguments) =>
            {
                var caller = (TupleTerm)arguments[0];
                Term state = CreateState();

                return ValueTask.FromResult<Term>(Term.Tuple(
                    Term.A(GenStatemRetentionNames.KeepState),
                    state,
                    Term.List(Term.Tuple(Term.A(GenStatemRetentionNames.Reply), caller.Items[1], Term.A(GenStatemRetentionNames.Ok)))
                ));
            }
        );
        registry.Register(
            GenStatemRetentionNames.CallbackModule,
            GenStatemRetentionNames.Terminate,
            3,
            (_, _) =>
            {
                terminations++;

                return ValueTask.FromResult<Term>(Term.A(GenStatemRetentionNames.Ok));
            }
        );
    }

    private Term CreateState() => Term.I(generations++);

    internal async Task Run(ProcessRuntime runtime, string directory)
    {
        File.Delete(Path.Combine(directory, GenStatemRetentionNames.Report));
        var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle controller = runtime.Spawn(async context =>
        {
            Pid? machine = null;
            try
            {
                var started = (TupleTerm)await runtime.Modules.Call(
                    context,
                    GenStatemModule.Name,
                    GenStatemRetentionNames.Start,
                    Term.A(GenStatemRetentionNames.CallbackModule),
                    Nil.Value,
                    Nil.Value
                );
                machine = (Pid)started.Items[1];
                for (int round = 0; round < GenStatemRetentionNames.Rounds; round++)
                {
                    _ = await runtime.Modules.Call(
                        context,
                        GenStatemModule.Name,
                        GenStatemRetentionNames.Call,
                        machine,
                        Term.A(GenStatemRetentionNames.Install)
                    );
                    _ = await runtime.Modules.Call(
                        context,
                        GenStatemValidationModules.Sys,
                        GenStatemRetentionNames.GetState,
                        machine
                    );
                    _ = await runtime.Modules.Call(
                        context,
                        GenStatemValidationModules.Sys,
                        GenStatemRetentionNames.GetStatus,
                        machine
                    );
                    _ = await runtime.Modules.Call(
                        context,
                        GenStatemValidationModules.Sys,
                        GenStatemRetentionNames.Suspend,
                        machine
                    );
                    _ = await runtime.Modules.Call(
                        context,
                        GenStatemValidationModules.Sys,
                        GenStatemRetentionNames.Resume,
                        machine
                    );
                }
                _ = await runtime.Modules.Call(
                    context,
                    GenStatemValidationModules.Erlang,
                    GenStatemRetentionNames.GarbageCollect,
                    machine
                );
                int live;
                using var observation = new CancellationTokenSource(GenStatemValidationLimits.OperationDeadline);
                while ((live = GenStatemContinuationGraph.CountMachines(machineContext!)) == 0)
                {
                    // A sys reply can arrive just before system_continue attaches
                    // the successor's mailbox awaiter. Observe the idle loop itself.
                    await Task.Delay(GenStatemRetentionNames.ObservationPollInterval, observation.Token);
                }
                var report = new
                {
                    Rounds = generations,
                    RetainedManagedMachines = live,
                    MaximumRetainedMachines = GenStatemRetentionNames.MaximumRetainedMachines,
                    Passed = live <= GenStatemRetentionNames.MaximumRetainedMachines,
                    ManagedAssemblySha256 = GenStatemAdmission.Hash(typeof(GenStatemModule).Assembly.Location),
                    RuntimeAssemblySha256 = GenStatemAdmission.Hash(typeof(ProcessRuntime).Assembly.Location)
                };
                File.WriteAllText(
                    Path.Combine(directory, GenStatemRetentionNames.Report),
                    JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })
                );
                if (!report.Passed)
                {
                    throw new InvalidOperationException(GenStatemRetentionNames.Failure);
                }
            }
            catch (Exception error)
            {
                complete.TrySetException(error);
            }
            finally
            {
                if (machine is not null)
                {
                    try
                    {
                        _ = await runtime.Modules.Call(
                            context,
                            GenStatemModule.Name,
                            GenStatemRetentionNames.Stop,
                            machine
                        );
                        if (terminations != 1)
                        {
                            throw new InvalidOperationException(GenStatemRetentionNames.TerminationFailure);
                        }
                    }
                    catch (Exception error)
                    {
                        complete.TrySetException(error);
                    }
                }
                complete.TrySetResult();
            }

            return Nil.Value;
        });
        await complete.Task.WaitAsync(GenStatemValidationLimits.RetentionDeadline);
        await controller.Completion.WaitAsync(GenStatemValidationLimits.OperationDeadline);
    }

}
