using System.Reflection;
using Erlang;
namespace Erlang.Tests;

public static class BeamOnLoadHostChecks
{
    public static async Task Run()
    {
        BeamOnLoadHostScenario[] scenarios =
        [
            new(
                BeamOnLoadHostNames.FreshSuccess,
                BeamOnLoadHostNames.FreshSuccessModule,
                false,
                true
            ),
            new(
                BeamOnLoadHostNames.FreshFailure,
                BeamOnLoadHostNames.FreshFailureModule,
                false,
                false
            ),
            new(
                BeamOnLoadHostNames.ReplacementSuccess,
                BeamOnLoadHostNames.ReplacementSuccessModule,
                true,
                true
            ),
            new(
                BeamOnLoadHostNames.ReplacementFailure,
                BeamOnLoadHostNames.ReplacementFailureModule,
                true,
                false
            )
        ];
        foreach (BeamOnLoadHostScenario scenario in scenarios)
            await Observe(scenario, false);
        await Observe(scenarios[0], true);
        await RetireWithoutStarting(scenarios[0]);
        await HighLevel(scenarios[0]);
        await HighLevel(scenarios[1]);
        await BeamOnLoadDeathRaceChecks.Run(Resource(scenarios[0], BeamOnLoadHostNames.NewRole));
    }

    private static async Task Observe(BeamOnLoadHostScenario scenario, bool kill)
    {
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle observer = runtime.Spawn(async context =>
        {
            ready.TrySetResult(context);

            return await finish.Task;
        });
        ProcessContext controller = await ready.Task;
        ProcessHandle? callback = null;
        try
        {
            runtime.Register(BeamOnLoadHostNames.Observer, observer.Pid);
            if (scenario.Old)
            {
                PreparedBeamImage previous = runtime.Modules.PrepareBeam(Resource(scenario, BeamOnLoadHostNames.OldRole));
                Require(runtime.Modules.PublishPreparedBeam(previous).Status == BeamPublicationStatus.Published);
            }
            byte[] bytes = Resource(scenario, BeamOnLoadHostNames.NewRole);
            Reject<InvalidDataException>(() => runtime.Modules.PrepareBeam(BeamOnLoadModeledBytes.UnownedY(bytes)));
            Reject<InvalidDataException>(() => runtime.Modules.PrepareBeam(BeamOnLoadModeledBytes.MismatchedFrame(bytes)));
            PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes);
            Require(image.HasOnLoad && !image.IsConsumed);
            Reject<InvalidOperationException>(() => runtime.Modules.PublishPreparedBeam(image));
            BeamOnLoadTicket ticket = runtime.Modules.StagePreparedOnLoad(image);
            Require(image.IsConsumed && image.HasOnLoad && ticket.Phase == BeamOnLoadPhase.Staged);
            Reject<ArgumentException>(() => runtime.Modules.StartPendingOnLoad(foreign, ticket));
            Reject<InvalidOperationException>(() => runtime.Modules.StagePreparedOnLoad(runtime.Modules.PrepareBeam(bytes)));
            callback = runtime.Modules.StartPendingOnLoad(runtime, ticket);
            Reject<InvalidOperationException>(() => runtime.Modules.StartPendingOnLoad(runtime, ticket));
            TupleTerm notice = (TupleTerm)(await controller.ReceiveAsync(
                term => term is TupleTerm tuple
                && tuple.Items.Count == 5 && tuple.Items[0].Equals(Term.A(BeamOnLoadHostNames.Notice)) ? term : null,
                TimeSpan.FromSeconds(BeamOnLoadHostNames.WatchdogSeconds)
            ))!;
            Require(notice.Items[1].Equals(Term.A(scenario.Module)) && notice.Items[2].Equals(callback.Pid));
            Require(ticket.Phase == BeamOnLoadPhase.Running && !ticket.Outcome.IsCompleted);
            Require(runtime.Modules.IsModuleLoaded(scenario.Module) == scenario.Old);
            if (scenario.Old)
                Require((await runtime.Modules.Call(controller, scenario.Module, BeamOnLoadHostNames.Run)).Equals(Term.A(BeamOnLoadHostNames.OldImage)));
            else
                Require(!runtime.Modules.HasExport(scenario.Module, BeamOnLoadHostNames.Run, 0));
            ReferenceTerm monitored = runtime.Monitor(controller, callback.Pid);
            if (kill)
                runtime.Exit(controller, callback.Pid, Term.A(ProcessExitSignals.Kill));
            else
                runtime.Send(callback.Pid, Term.Tuple(Term.A(BeamOnLoadHostNames.Continue), observer.Pid));
            BeamOnLoadCallbackOutcome outcome = await ticket.Outcome.WaitAsync(TimeSpan.FromSeconds(BeamOnLoadHostNames.WatchdogSeconds));
            Term down = await callback.Completion.WaitAsync(TimeSpan.FromSeconds(BeamOnLoadHostNames.WatchdogSeconds));
            await ticket.CallbackQuiescence.WaitAsync(TimeSpan.FromSeconds(BeamOnLoadHostNames.WatchdogSeconds));
            TupleTerm actualDown = (TupleTerm)(await controller.ReceiveAsync(
                term => term is TupleTerm tuple && tuple.Items.Count == 5
                && tuple.Items[0].Equals(Term.A(ProcessMessageTags.MonitorDown)) && tuple.Items[1].Equals(monitored) ? term : null,
                TimeSpan.FromSeconds(BeamOnLoadHostNames.WatchdogSeconds)
            ))!;
            Require(actualDown.Items[3].Equals(callback.Pid) && actualDown.Items[4].Equals(down));
            Directory.CreateDirectory(BeamOnLoadHostNames.ActualArtifacts);
            File.WriteAllText(
                Path.Combine(BeamOnLoadHostNames.ActualArtifacts, scenario.Name + (kill ? "-kill" : "") + BeamOnLoadHostNames.ObservationExtension),
                System.Text.Json.JsonSerializer.Serialize(
                    new
                    {
                        scenario.Name,
                        scenario.Module,
                        scenario.Old,
                        scenario.Success,
                        Kill = kill,
                        Callback = callback.Pid.ToString(),
                        Returned = outcome.Returned?.ToString(),
                        Failure = outcome.Failure?.ToString(),
                        DownReason = down.ToString(),
                        ActualMonitorDown = actualDown.ToString(),
                        Notice = notice.ToString(),
                        PhaseBeforeFinalization = ticket.Phase.ToString(),
                        HasOnLoad = image.HasOnLoad,
                        InputSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)),
                        DisassemblyBoundToGenuineResource = true,
                        OpcodeFailure = outcome.Failure is null ? null : outcome.Failure.StackTrace
                    },
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
                )
            );
            Require(outcome.Callback.Equals(callback.Pid));
            bool keep = !kill && scenario.Success;
            if (kill)
                Require(outcome.Failure is not null && outcome.Returned is null && down.Equals(Term.A(ProcessExitReasons.Killed)));
            else
            {
                Require(outcome.Failure is null && outcome.Returned!.Equals(Term.A(scenario.Success ? BeamOnLoadHostNames.Ok : BeamOnLoadHostNames.NotOk)));
                Require(down.Equals(outcome.Returned));
            }
            Require(runtime.Modules.FinishPendingOnLoad(ticket, keep) == keep);
            Require(ticket.Phase == (keep ? BeamOnLoadPhase.Published : BeamOnLoadPhase.Retired));
            Reject<InvalidOperationException>(() => runtime.Modules.FinishPendingOnLoad(ticket, keep));
            Require(runtime.Modules.IsModuleLoaded(scenario.Module) == (keep || scenario.Old));
            if (keep || scenario.Old)
                Require((await runtime.Modules.Call(controller, scenario.Module, BeamOnLoadHostNames.Run)).Equals(Term.A(keep ? BeamOnLoadHostNames.NewImage : BeamOnLoadHostNames.OldImage)));
            Require(runtime.Modules.HasOldCode(scenario.Module) == (keep && scenario.Old));
            if (!kill)
            {
                Term before = scenario.Old ? Term.Tuple(Term.A(BeamOnLoadHostNames.Value), Term.A(BeamOnLoadHostNames.OldImage))
                    : Term.Tuple(Term.A(BeamOnLoadHostNames.Error), Term.A(BeamOnLoadHostNames.Undef));
                Term during = scenario.Old ? before : Term.A(BeamOnLoadHostNames.NotInvoked);
                Term after = keep ? Term.Tuple(Term.A(BeamOnLoadHostNames.Value), Term.A(BeamOnLoadHostNames.NewImage)) : before;
                Term load = keep ? Term.Tuple(Term.A(BeamOnLoadHostNames.Module), Term.A(scenario.Module))
                    : Term.Tuple(Term.A(BeamOnLoadHostNames.Error), Term.A(BeamOnLoadHostNames.OnLoadFailure));
                Term projected = Term.Tuple(
                    Term.A(BeamOnLoadHostNames.Observed),
                    load,
                    down,
                    before,
                    during,
                    after
                );
                byte[] encoded = ExternalTermFormat.Encode(Term.Tuple(Term.A(BeamOnLoadHostNames.Ok), projected));
                File.WriteAllBytes(Path.Combine(BeamOnLoadHostNames.ActualArtifacts, scenario.Name + ".etf"), encoded);
                File.WriteAllText(Path.Combine(BeamOnLoadHostNames.ActualArtifacts, scenario.Name + ".term"), projected.ToString());
                Require(encoded.AsSpan().SequenceEqual(Summary(scenario)));
            }
        }
        finally
        {
            if (callback is not null && !callback.Completion.IsCompleted)
            {
                runtime.Exit(controller, callback.Pid, Term.A(ProcessExitSignals.Kill));
                _ = await callback.Completion.WaitAsync(TimeSpan.FromSeconds(BeamOnLoadHostNames.WatchdogSeconds));
            }
            finish.TrySetResult(Term.A(BeamOnLoadHostNames.Ok));
            _ = await observer.Completion;
        }
    }

    private static async Task RetireWithoutStarting(BeamOnLoadHostScenario scenario)
    {
        await using var runtime = new ProcessRuntime();
        PreparedBeamImage image = runtime.Modules.PrepareBeam(Resource(scenario, BeamOnLoadHostNames.NewRole));
        BeamOnLoadTicket ticket = runtime.Modules.StagePreparedOnLoad(image);
        Require(!runtime.Modules.FinishPendingOnLoad(ticket, false));
        Require(ticket.Phase == BeamOnLoadPhase.Retired && image.IsConsumed && image.HasOnLoad);
    }

    private static async Task HighLevel(BeamOnLoadHostScenario scenario)
    {
        await using var runtime = new ProcessRuntime();
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle observer = runtime.Spawn(async context =>
        {
            ready.TrySetResult(context);

            return await finish.Task;
        });
        ProcessContext controller = await ready.Task;
        try
        {
            runtime.Register(BeamOnLoadHostNames.Observer, observer.Pid);
            byte[] bytes = Resource(scenario, BeamOnLoadHostNames.NewRole);
            Reject<NotSupportedException>(() => runtime.Modules.PrepareBeam(BeamMetadataModeledBytes.WithAttributes(bytes, Term.A(BeamOnLoadHostNames.UnknownAttribute))));
            Task<BeamOnLoadResult> pending = runtime.Modules.LoadPreparedOnLoadAsync(runtime, runtime.Modules.PrepareBeam(bytes));
            TupleTerm notice = (TupleTerm)(await controller.ReceiveAsync(
                term => term is TupleTerm tuple && tuple.Items.Count == 5
                && tuple.Items[0].Equals(Term.A(BeamOnLoadHostNames.Notice)) ? term : null,
                TimeSpan.FromSeconds(BeamOnLoadHostNames.WatchdogSeconds)
            ))!;
            runtime.Send((Pid)notice.Items[2], Term.Tuple(Term.A(BeamOnLoadHostNames.Continue), observer.Pid));
            BeamOnLoadResult result = await pending.WaitAsync(TimeSpan.FromSeconds(BeamOnLoadHostNames.WatchdogSeconds));
            Require(result.Published == scenario.Success && result.Callback.Equals(notice.Items[2])
                && result.Outcome.Failure is null && result.DownReason.Equals(result.Outcome.Returned));
        }
        finally
        {
            finish.TrySetResult(Term.A(BeamOnLoadHostNames.Ok));
            _ = await observer.Completion;
        }
    }

    private static byte[] Summary(BeamOnLoadHostScenario scenario)
    {
        string suffix = string.Join(
            '.',
            BeamOnLoadHostNames.ResourcePrefix,
            scenario.Name.Replace('-', '_'),
            BeamOnLoadHostNames.ActualSummary
        );
        Assembly assembly = typeof(BeamOnLoadHostChecks).Assembly;
        using Stream stream = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().Single(value => value.EndsWith(suffix, StringComparison.Ordinal)))!;
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);

        return bytes.ToArray();
    }

    private static byte[] Resource(BeamOnLoadHostScenario scenario, string role)
    {
        string suffix = string.Join(
            '.',
            BeamOnLoadHostNames.ResourcePrefix,
            scenario.Name.Replace('-', '_'),
            role,
            scenario.Module + BeamOnLoadHostNames.Extension
        );
        Assembly assembly = typeof(BeamOnLoadHostChecks).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(suffix, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);

        return bytes.ToArray();
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(BeamOnLoadHostNames.ProofFailure);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException(BeamOnLoadHostNames.ProofFailure);
    }
}
