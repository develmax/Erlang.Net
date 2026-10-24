using Erlang;
using System.Collections;
using System.Reflection;

namespace Erlang.Tests;

internal static class DistributionEntryCreationHostChecks
{
    internal static async Task FreshAndLazyBudget()
    {
        await using var runtime = new ProcessRuntime();
        await using var reader = new RuntimeNodeFoundationHostActor(runtime);
        Check((await Read(runtime, reader)).Equals(DistributionState402Atoms.Undefined));
        // Reading a DistEntry creation must not eagerly consume pending-service initialization.
        Check(runtime.InitializeDistributionPending(DistributionEntryCreationHostNames.MaximumNodes).MaximumNodes ==
            DistributionEntryCreationHostNames.MaximumNodes);
    }

    internal static async Task StartedUnsigned()
    {
        foreach (uint creation in new uint[] { DistributionEntryCreationHostNames.RequestedCreation, uint.MaxValue })
        {
            await using var runtime = new ProcessRuntime();
            await using var reader = new RuntimeNodeFoundationHostActor(runtime);
            await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
            await Start(runtime, kernel, creation);
            Check((await Read(runtime, reader)).Equals(new Integer(creation)));
        }
    }

    internal static async Task RetainedDormantAndGeneration()
    {
        await using var runtime = new ProcessRuntime();
        runtime.InitializeDistributionPending(DistributionEntryCreationHostNames.MaximumNodes);
        await using var reader = new RuntimeNodeFoundationHostActor(runtime);
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await Start(runtime, kernel, DistributionEntryCreationHostNames.RequestedCreation);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        var dormant = Term.A(ProcessNodeDefaults.Local);
        var held = runtime.DistributionPending.BeginPending(dormant);
        await controller.InvokeAsync(async context => await DistributionControllerAdapters.SetNode(
            context,
            dormant,
            context.Self,
            Term.Tuple(new Integer(DistributionPassThroughFlags.Mandatory),
                new Integer(DistributionEntryCreationHostNames.RemoteCreation))
        ));
        await controller.DisposeAsync();
        await kernel.DisposeAsync();
        Check(runtime.Node == ProcessNodeDefaults.Local);
        Check(!runtime.IsDistributionAlive);
        // Same actual controller-retired, handle-rooted state as the native one-shot witness.
        Check((await Read(runtime, reader)).Equals(new Integer(DistributionEntryCreationHostNames.RemoteCreation)));
        await using var nextKernel = new RuntimeNodeFoundationHostActor(runtime);
        await Start(runtime, nextKernel, DistributionEntryCreationHostNames.NextCreation);
        var next = runtime.DistributionPending.BeginPending(dormant);
        Check(next.Items[1].Equals(held.Items[1]));
        uint previousId = (uint)((Integer)held.Items[0]).Value;
        Check(next.Items[0].Equals(new Integer((previousId + 1) & DistributionEntryCreationHostNames.ConnectionIdMask)));
        Check((await Read(runtime, reader)).Equals(new Integer(DistributionEntryCreationHostNames.NextCreation)));
        GC.KeepAlive(held);
    }

    internal static async Task ClosedReleasesRuntimeRoots()
    {
        var runtime = new ProcessRuntime();
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await Start(runtime, kernel, DistributionEntryCreationHostNames.RequestedCreation);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        var dormant = Term.A(ProcessNodeDefaults.Local);
        var held = runtime.DistributionPending.BeginPending(dormant);
        await controller.InvokeAsync(async context => await DistributionControllerAdapters.SetNode(
            context,
            dormant,
            context.Self,
            Term.Tuple(new Integer(DistributionPassThroughFlags.Mandatory),
                new Integer(DistributionEntryCreationHostNames.RemoteCreation))
        ));
        await controller.DisposeAsync();
        await kernel.DisposeAsync();
        Check(Field(runtime, DistributionEntryCreationHostNames.CurrentField) is not null);
        await runtime.DisposeAsync();
        Check(Field(runtime, DistributionEntryCreationHostNames.CurrentField) is null);
        Check(((ICollection)Field(runtime, DistributionEntryCreationHostNames.RegistryField)!).Count == 0);
        Check(runtime.ReferenceResources.LiveResources == 0);
        // The retained public term is not rewritten; the owner explicitly invalidates its payload.
        Check(held.Items[1] is ResourceReferenceTerm);
        GC.KeepAlive(held);
    }

    private static Task<Term> Read(ProcessRuntime runtime, RuntimeNodeFoundationHostActor actor)
        => actor.InvokeAsync(async context => await runtime.Modules.Call(context,
            DistributionEntryCreationHostNames.PrimitiveModule, DistributionEntryCreationHostNames.Getter));

    private static Task<Term> Start(ProcessRuntime runtime, RuntimeNodeFoundationHostActor kernel, uint creation)
        => kernel.InvokeAsync(async context =>
        {
            runtime.Register(DistributionEntryCreationHostNames.Kernel, context.Self);

            return await runtime.Modules.Call(
                context,
                DistributionEntryCreationHostNames.ErlangModule,
                DistributionEntryCreationHostNames.SetNode,
                Term.A(DistributionEntryCreationHostNames.Local),
                new Integer(creation)
            );
        });

    private static object? Field(ProcessRuntime runtime, string name)
        => typeof(ProcessRuntime).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(runtime);

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(DistributionEntryCreationHostNames.Failure);
    }
}
