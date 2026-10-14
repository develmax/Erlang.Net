using System.Reflection;

namespace Erlang.Tests;

public static class BeamLoaderAdmissionRaceChecks
{
    public static async Task Run(byte[] bytes)
    {
        await CompetingConsume(bytes);
        await ClosedBeforeAdmission(bytes);
    }

    private static async Task CompetingConsume(byte[] bytes)
    {
        await using var runtime = new ProcessRuntime();
        PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes);
        ResourceReferenceTerm reference = runtime.ReferenceResources.CreatePrepared(image);
        using var started = new ManualResetEventSlim();
        object moduleGate = typeof(ModuleRegistry).GetField(BeamLoaderAdmissionRaceNames.ModuleGate, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime.Modules)!;
        Task<Term> competing;
        Monitor.Enter(moduleGate);
        try
        {
            competing = Task.Run(() =>
            {
                started.Set();

                return Finish(runtime.ReferenceResources, reference);
            });
            Require(started.Wait(TimeSpan.FromSeconds(BeamLoaderAdmissionRaceNames.WatchdogSeconds)));
            Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
        }
        finally
        {
            Monitor.Exit(moduleGate);
        }
        await Badarg(competing);
        Require(image.IsConsumed);
    }

    private static async Task ClosedBeforeAdmission(byte[] bytes)
    {
        var runtime = new ProcessRuntime();
        PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes);
        ResourceReferenceTerm reference = runtime.ReferenceResources.CreatePrepared(image);
        using var started = new ManualResetEventSlim();
        object moduleGate = typeof(ModuleRegistry).GetField(BeamLoaderAdmissionRaceNames.ModuleGate, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime.Modules)!;
        Task<Term> competing;
        Monitor.Enter(moduleGate);
        try
        {
            competing = Task.Run(() =>
            {
                started.Set();

                return Finish(runtime.ReferenceResources, reference);
            });
            Require(started.Wait(TimeSpan.FromSeconds(BeamLoaderAdmissionRaceNames.WatchdogSeconds)));
            runtime.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        finally
        {
            Monitor.Exit(moduleGate);
        }
        await Badarg(competing);
        Require(!image.IsConsumed && runtime.ReferenceResources.LiveResources == 0);
    }

    private static Term Finish(RuntimeReferenceResources resources, ReferenceTerm reference)
    {
        MethodInfo method = typeof(RuntimeReferenceResources).GetMethod(BeamLoaderAdmissionRaceNames.FinishReferences, BindingFlags.Instance | BindingFlags.NonPublic)!;
        try
        {
            return (Term)method.Invoke(resources, [new ReferenceTerm[] { reference }])!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is ErlangException error)
        {
            throw error;
        }
    }

    private static async Task Badarg(Task<Term> task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(BeamLoaderAdmissionRaceNames.WatchdogSeconds));
        }
        catch (ErlangException exception)
        {
            Require(exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));

            return;
        }

        throw new InvalidOperationException(BeamLoaderAdmissionRaceNames.Failure);
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(BeamLoaderAdmissionRaceNames.Failure);
    }
}
