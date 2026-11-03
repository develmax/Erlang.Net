using System.Reflection;
using Erlang.Differential;
using static Erlang.Tests.CollectionRequestNames;

namespace Erlang.Tests;

internal static class CollectionRequestChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static Task Ordinary() => Run(false, false);

    internal static Task Owned() => Run(true, false);

    internal static Task OrdinaryBeam() => Run(false, true);

    internal static Task OwnedBeam() => Run(true, true);

    private static async Task Run(bool enabled, bool beam)
    {
        // This raw parser entry accepts preprocessed source; expand only the
        // fixed module-name macro, exactly as the original OTP worker does.
        string prepared = Source.Replace(ModuleMacro, GuestModule, StringComparison.Ordinal);
        using var artifact = beam ? null : GeneratedModuleCompiler.Compile(prepared, GuestModule + ".erl");
        await using var runtime = OwnedGuest506RuntimeFactory.Create(enabled);
        if (beam)
        {
            PreparedBeamImage image = runtime.Modules.PrepareBeam(CollectionRequestOriginal.Beam());
            Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
        }
        else
            artifact!.Definition.Register(runtime.Modules);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term observed = await actor.InvokeAsync(async context => await runtime.Modules.Call(context, GuestModule, Entry));
        Require(Term.Tuple(Term.A(Returned), observed).Equals(CollectionRequestOriginal.Outcome()));
        object statistics = typeof(ProcessRuntime).GetProperty(OrdinaryCollectorNames.Statistics, Members)!.GetValue(runtime)!;
        var sample = (TupleTerm)statistics.GetType().GetMethod(OrdinaryCollectorNames.Sample, Members)!.Invoke(statistics, null)!;
        Require(sample.Items[0] is Integer count && count.Value >= 3);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
