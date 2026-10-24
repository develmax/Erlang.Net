using static Erlang.Tests.BeamLoaderDependencies410Access;

namespace Erlang.Tests;

internal static class BeamLoaderDependencies410Checks
{
    internal static async Task Declared()
    {
        await using var runtime = Runtime();
        PreparedBeamImage outer = Caller(runtime, BeamLoaderDependencies410Names.OuterModule, true);
        ModuleCodeImage code = Code(runtime, outer);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            using SourceCodeLease lease = runtime.Modules.ReserveSourceCode(context, code);
            using IDisposable flow = lease.Attach(outer);
            Require(ReferenceEquals(context.ExecutingSourceImage(outer), code));
            Term prepared = await Prepare(context);
            Require(prepared is ResourceReferenceTerm);
            using (var reference = runtime.ReferenceResources.AcquirePrepared((ReferenceTerm)prepared))
            {
                Require(reference is not null && reference.Value.Name == BeamLoaderDependencies410Names.TargetModule
                    && reference.Value.RegistryModuleDependencies.SetEquals(new[] {
                        BeamLoaderDependencies410Names.DependencyModule, BeamLoaderDependencies410Names.TargetModule }));
            }
            Require((await Native(context, BeamLoaderDependencies410Names.HasOnLoad, prepared)).Equals(Term.A(ErlangBooleanAtoms.False)));
            Require((await Native(context, BeamLoaderDependencies410Names.Finish, Term.List(prepared)))
                .Equals(Term.A(BeamLoaderDependencies410Names.Ok)));
            Require(runtime.ReferenceResources.AcquirePrepared((ReferenceTerm)prepared) is null);
            Require(runtime.Modules.SourceOwner(BeamLoaderDependencies410Names.TargetModule,
                BeamLoaderDependencies410Names.Run, 0) is PreparedBeamImage);
            Require((await runtime.Modules.Call(context, BeamLoaderDependencies410Names.TargetModule,
                BeamLoaderDependencies410Names.Run)).Equals(Term.I(BeamLoaderDependencies410Names.Expected)));

            return true;
        });
    }

    internal static async Task Undeclared()
    {
        await using var runtime = Runtime();
        PreparedBeamImage outer = Caller(runtime, BeamLoaderDependencies410Names.OuterModule, false);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            using SourceCodeLease lease = runtime.Modules.ReserveSourceCode(context, Code(runtime, outer));
            using IDisposable flow = lease.Attach(outer);
            await Refuses(context);

            return true;
        });
    }

    internal static async Task Host()
    {
        await using var runtime = Runtime();
        PreparedBeamImage broad = Caller(runtime, BeamLoaderDependencies410Names.OuterModule, true);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            await Refuses(context);
            // Same actual erlang native-bridge image, arbitrary attached owner.
            // Image.ExecutionOwner (not this owner object) is the authority.
            ModuleCodeImage hostImage = runtime.Modules.CurrentCodeImage(BeamLoaderDependencies410Names.Erlang)!;
            using SourceCodeLease lease = runtime.Modules.ReserveSourceCode(context, hostImage);
            using IDisposable flow = lease.Attach(broad);
            await Refuses(context);

            return true;
        });
    }

    internal static async Task Foreign()
    {
        await using var runtime = Runtime();
        await using var foreign = Runtime();
        PreparedBeamImage broad = Caller(foreign, BeamLoaderDependencies410Names.OuterModule, true);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            using SourceCodeLease lease = foreign.Modules.ReserveSourceCode(context, Code(foreign, broad));
            using IDisposable flow = lease.Attach(broad);
            Require(ReferenceEquals(context.ExecutingSourceImage(broad), lease.Image));
            await Refuses(context);

            return true;
        });
    }

    internal static async Task Ended()
    {
        await using var runtime = Runtime();
        PreparedBeamImage broad = Caller(runtime, BeamLoaderDependencies410Names.OuterModule, true);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            using SourceCodeLease lease = runtime.Modules.ReserveSourceCode(context, Code(runtime, broad));
            using IDisposable flow = lease.Attach(broad);
            lease.Dispose();
            // Prove stale AsyncLocal flow still exists; it must be filtered by
            // actual active lease membership rather than absence of the record.
            Require(ReferenceEquals(context.ExecutingSourceImage(broad), lease.Image));
            await Refuses(context);

            return true;
        });
    }

    internal static async Task Nearest()
    {
        await using var runtime = Runtime();
        PreparedBeamImage broad = Caller(runtime, BeamLoaderDependencies410Names.OuterModule, true);
        PreparedBeamImage empty = Caller(runtime, BeamLoaderDependencies410Names.InnerModule, false);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            using SourceCodeLease outer = runtime.Modules.ReserveSourceCode(context, Code(runtime, broad));
            using IDisposable outerFlow = outer.Attach(broad);
            using SourceCodeLease inner = runtime.Modules.ReserveSourceCode(context, Code(runtime, empty));
            using IDisposable innerFlow = inner.Attach(empty);
            Require(ReferenceEquals(context.ExecutingSourceImage(empty), inner.Image));
            await Refuses(context);

            return true;
        });
    }
}
