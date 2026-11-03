using System.Reflection;
using System.Security.Cryptography;
using Erlang.Differential;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.RuntimeFunctionMarker364Names;

namespace Erlang.Tests;

internal static class RuntimeFunctionMarker364Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task<FunctionTerm> CreateRefused(
        ProcessRuntime target,
        RuntimeNodeFoundationHostActor actor,
        object heap,
        object frame
    )
    {
        using var source = GeneratedModuleCompiler.Compile(Source);
        source.Definition.Register(target.Modules);
        await using var foreign = OwnedGuest506RuntimeFactory.Create();
        source.Definition.Register(foreign.Modules);
        await Prepare(target);
        await Prepare(foreign);
        await using var foreignActor = new RuntimeNodeFoundationHostActor(foreign);
        var localSource = (FunctionTerm)await actor.InvokeAsync(context => target.Modules.Call(
            context,
            RuntimeFunctionMarker364Names.Module,
            Make,
            Term.I(Seed)
        ).AsTask());
        var localBeam = (FunctionTerm)await actor.InvokeAsync(context => target.Modules.Call(
            context,
            RuntimeFunctionMarker364Names.BeamModule,
            Make,
            Term.I(Seed)
        ).AsTask());
        var foreignSource = (FunctionTerm)await foreignActor.InvokeAsync(context => foreign.Modules.Call(
            context,
            RuntimeFunctionMarker364Names.Module,
            Make,
            Term.I(Seed)
        ).AsTask());
        var foreignBeam = (FunctionTerm)await foreignActor.InvokeAsync(context => foreign.Modules.Call(
            context,
            RuntimeFunctionMarker364Names.BeamModule,
            Make,
            Term.I(Seed)
        ).AsTask());
        foreach (FunctionTerm local in new[] { localSource, localBeam })
        {
            Require((bool)Property(local, Descriptor));
            var copied = (FunctionTerm)await actor.Invoke(context => (Term)Call(
                heap,
                CountersExportRollback596Names.Export,
                frame,
                local
            )!);
            Require((bool)Property(copied, Descriptor));
            Require((await actor.InvokeAsync(context => copied.Invoke(context, [Term.I(Argument)]).AsTask())).Equals(Term.I(Result)));
        }
        int lazyReads = 0;
        var identity = new LocalFunctionIdentity(
            Term.A(RuntimeFunctionMarker364Names.Module),
            null,
            0,
            0,
            new BitString(new byte[16])
        );
        var opaque = new FunctionTerm(
            () =>
 {
     lazyReads++;

     return identity;
 },
            0,
            [Nil.Value],
            static (_, _) => ValueTask.FromResult<Term>(Nil.Value)
        );
        var copiedOpaque = (FunctionTerm)await actor.Invoke(context => (Term)Call(
            heap,
            CountersExportRollback596Names.Export,
            frame,
            opaque
        )!);
        Require(lazyReads == 0 && !(bool)Property(opaque, Descriptor) && !(bool)Property(copiedOpaque, Descriptor));
        foreach (FunctionTerm refused in new[] { foreignSource, foreignBeam }) await Refuse(
            actor,
            heap,
            frame,
            refused
        );
        target.Modules.DeleteModule(RuntimeFunctionMarker364Names.Module);
        target.Modules.DeleteModule(RuntimeFunctionMarker364Names.BeamModule);
        foreach (FunctionTerm refused in new[] { foreignSource, foreignBeam }) await Refuse(
            actor,
            heap,
            frame,
            refused
        );
        await foreignActor.DisposeAsync();
        await foreign.DisposeAsync();
        // Detached scalar environment and by-value UID remain data after actual
        // source VM closure; its real runtime descriptor still cannot downgrade.
        foreach (FunctionTerm refused in new[] { foreignSource, foreignBeam }) await Refuse(
            actor,
            heap,
            frame,
            refused
        );

        return foreignSource;
    }

    private static async Task Refuse(
        RuntimeNodeFoundationHostActor actor,
        object heap,
        object frame,
        FunctionTerm function
    )
    {
        Require((bool)Property(function, Descriptor));
        bool refused = false;
        try
        {
            _ = await actor.Invoke(context => (Term)Call(
                heap,
                CountersExportRollback596Names.Export,
                frame,
                function
            )!);
        }
        catch (NotSupportedException error) when (error.Message == RuntimeFunctionMarker364Names.Refused)
        {
            refused = true;
        }
        Require(refused);
    }

    private static async Task Prepare(ProcessRuntime runtime)
    {
        string name = typeof(RuntimeFunctionMarker364Checks).Assembly.GetManifestResourceNames().Single(value => value.EndsWith(Separator + RuntimeFunctionMarker364Names.BeamModule + BeamSuffix, StringComparison.Ordinal));
        using Stream source = typeof(RuntimeFunctionMarker364Checks).Assembly.GetManifestResourceStream(name)!;
        using var output = new MemoryStream();
        source.CopyTo(output);
        byte[] bytes = output.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == BeamHash);
        await PreparedBeamFixtureScope171.RunAsync(
            runtime,
            (registry, scope) =>
        {
            PreparedBeamImage image = scope.Prepare(() => registry.PrepareBeam(bytes, new BeamDependencySelection([ErlangModuleNames.Module])));

            return ValueTask.FromResult<Action<ModuleRegistry>>(target => Require(target.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published));
        }
        );
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
