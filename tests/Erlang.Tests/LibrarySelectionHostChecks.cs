using static Erlang.Tests.LibrarySelectionHostData;

namespace Erlang.Tests;

internal static class LibrarySelectionHostChecks
{
    internal static async Task Defaults()
    {
        await using var ordinary = new ProcessRuntime();
        await using var empty = await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([]));
        await using var managed = await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([
            RuntimeModuleSelection.Managed(ListBeamHostNames.Module),
            RuntimeModuleSelection.Managed(QueueModuleNames.Module)
        ]));
        Require(ordinary.LibrarySelection.Modules.Count == 0 && empty.LibrarySelection.Modules.Count == 0);
        Require(managed.LibrarySelection.Modules.Count == 2);
        foreach (RuntimeLibraryModuleSnapshot selected in managed.LibrarySelection.Modules)
        {
            Require(selected.Backend == RuntimeModuleBackend.Managed && selected.InputSha256 is null);
            Require(ReferenceEquals(selected.Image, managed.Modules.CurrentCodeImage(selected.Module)));
            Require(selected.NativeMd5.Equals(ordinary.Modules.ModuleInformation(selected.Module, BeamPreparedHostNames.Md5)));
            Require(selected.ManagedAssemblies.Count > 0 && selected.ManagedAssemblies.All(value => value.ModuleVersionId != Guid.Empty));
            Require(!managed.Modules.HasOldCode(selected.Module));
        }
        await RoundTrip(managed);
    }

    internal static async Task BothOrders()
    {
        foreach (bool reverse in new[] { false, true })
        {
            await using var runtime = await ProcessRuntime.CreateAsync(Both(reverse));
            Require(runtime.LibrarySelection.Modules.Select(value => value.Module).SequenceEqual(new[] { ListBeamHostNames.Module, QueueModuleNames.Module }));
            foreach (RuntimeLibraryModuleSnapshot selected in runtime.LibrarySelection.Modules)
            {
                Require(selected.Backend == RuntimeModuleBackend.Beam);
                Require(selected.InputSha256 == (selected.Module == ListBeamHostNames.Module ? ListBeamHostNames.Sha256 : QueueBeamAdmissionHostNames.Sha256));
                Require(ReferenceEquals(selected.Image, runtime.Modules.CurrentCodeImage(selected.Module)));
                Require(!runtime.Modules.HasOldCode(selected.Module));
                foreach (BeamFunctionSignature export in selected.Exports)
                {
                    Require(runtime.Modules.SourceOwner(selected.Module, export.Name, export.Arity) is PreparedBeamImage);
                    Require(ReferenceEquals(runtime.Modules.SourceEntry(selected.Module, export.Name, export.Arity)!.Image, selected.Image));
                }
            }
            RuntimeLibraryModuleSnapshot lists = runtime.LibrarySelection.Modules.Single(value => value.Module == ListBeamHostNames.Module);
            Require(lists.NativePrimitives.Count == 5 && lists.Exports.Count == 93);
            RuntimeLibraryModuleSnapshot queue = runtime.LibrarySelection.Modules.Single(value => value.Module == QueueModuleNames.Module);
            Require(queue.NativePrimitives.Count == 0 && queue.Exports.Count == 40);
            Require(queue.Dependencies.Count == 1 && queue.Dependencies[0].Module == ListBeamHostNames.Module && queue.Dependencies[0].Backend == RuntimeModuleBackend.Beam);
            await RoundTrip(runtime);
        }
    }

    internal static async Task Mixed()
    {
        await using var runtime = await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([
            RuntimeModuleSelection.Managed(ListBeamHostNames.Module),
            RuntimeModuleSelection.Beam(QueueModuleNames.Module, Queue(), [ListBeamHostNames.Module])
        ]));
        RuntimeLibraryModuleSnapshot queue = runtime.LibrarySelection.Modules.Single(value => value.Module == QueueModuleNames.Module);
        Require(queue.Dependencies.Single().Backend == RuntimeModuleBackend.Managed);
        Require(runtime.Modules.SourceOwner(ListBeamHostNames.Module, ListBeamHostNames.Sort, 1) is null);
        Require(runtime.Modules.SourceOwner(QueueModuleNames.Module, QueueModuleNames.New, 0) is PreparedBeamImage);
        await RoundTrip(runtime);
        await using var reverse = await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([
            RuntimeModuleSelection.Beam(ListBeamHostNames.Module, Lists()),
            RuntimeModuleSelection.Managed(QueueModuleNames.Module)
        ]));
        Require(reverse.Modules.SourceOwner(ListBeamHostNames.Module, ListBeamHostNames.Sort, 1) is PreparedBeamImage);
        Require(reverse.Modules.SourceOwner(QueueModuleNames.Module, QueueModuleNames.New, 0) is null);
        await RoundTrip(reverse);
        var caller = Erlang.Differential.GeneratedModuleCompiler.Compile(
            LibrarySelectionHostNames.CallerSource,
            LibrarySelectionHostNames.CallerFile
        );
        caller.Definition.Register(reverse.Modules);
        await using var actor = new RuntimeNodeFoundationHostActor(reverse);
        await actor.InvokeAsync(async context =>
        {
            Require((await reverse.Modules.Call(context, LibrarySelectionHostNames.CallerModule, LibrarySelectionHostNames.CallerRun))
                .Equals(Term.List(Term.I(1), Term.I(2), Term.I(3))));

            return true;
        });
        Require(reverse.Modules.SourceOwner(ListBeamHostNames.Module, ListBeamHostNames.Sort, 1) is PreparedBeamImage);
        Require(reverse.Modules.SourceOwner(QueueModuleNames.Module, QueueModuleNames.New, 0) is null);
    }

    internal static async Task Immutable()
    {
        byte[] lists = Lists();
        byte[] queue = Queue();
        var dependencies = new List<string> { ListBeamHostNames.Module };
        var choices = new List<RuntimeModuleSelection>
        {
            RuntimeModuleSelection.Beam(ListBeamHostNames.Module, lists),
            RuntimeModuleSelection.Beam(QueueModuleNames.Module, queue, dependencies)
        };
        var request = new RuntimeLibrarySelection(choices);
        lists[0] ^= byte.MaxValue;
        queue[0] ^= byte.MaxValue;
        dependencies.Clear();
        choices.Clear();
        await using var beam = await ProcessRuntime.CreateAsync(request);
        await using var managed = await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([RuntimeModuleSelection.Managed(ListBeamHostNames.Module)]));
        Require(beam.LibrarySelection.Modules.Count == 2 && request.Modules.Count == 2);
        Require(request.Modules.Single(value => value.Module == QueueModuleNames.Module).Dependencies.SequenceEqual([ListBeamHostNames.Module]));
        Require(beam.Modules.SourceOwner(ListBeamHostNames.Module, ListBeamHostNames.Sort, 1) is PreparedBeamImage);
        Require(managed.Modules.SourceOwner(ListBeamHostNames.Module, ListBeamHostNames.Sort, 1) is null);
        Require(beam.LibrarySelection.Modules is System.Collections.IList { IsReadOnly: true });
        Require(request.Modules is System.Collections.IList { IsReadOnly: true });
        await RoundTrip(beam);
        await RoundTrip(managed);
    }

    internal static async Task Refusals()
    {
        Reject(() => RuntimeModuleSelection.Managed(LibrarySelectionPolicyControlNames.StringModule));
        Reject(() => RuntimeModuleSelection.Managed(LibrarySelectionHostNames.Missing));
        Reject(() => new RuntimeLibrarySelection([RuntimeModuleSelection.Managed(ListBeamHostNames.Module), RuntimeModuleSelection.Managed(ListBeamHostNames.Module)]));
        Reject(() => new RuntimeLibrarySelection([RuntimeModuleSelection.Beam(QueueModuleNames.Module, Queue(), [ListBeamHostNames.Module])]));
        await RejectStartup(new RuntimeLibrarySelection([RuntimeModuleSelection.Beam(QueueModuleNames.Module, Queue())]));
        await RejectStartup(new RuntimeLibrarySelection([RuntimeModuleSelection.Beam(QueueModuleNames.Module, Lists())]));
        byte[] invalid = Queue();
        invalid[0] ^= byte.MaxValue;
        await RejectStartup(new RuntimeLibrarySelection([
            RuntimeModuleSelection.Beam(ListBeamHostNames.Module, Lists()),
            RuntimeModuleSelection.Beam(QueueModuleNames.Module, invalid, [ListBeamHostNames.Module])
        ]));
        // Failure must throw rather than return a runtime silently using Managed.
        await using var valid = await ProcessRuntime.CreateAsync(Both());
        await RoundTrip(valid);
    }

    internal static async Task Reload()
    {
        await using var runtime = await ProcessRuntime.CreateAsync(Both());
        RuntimeLibrarySelectionSnapshot receipt = runtime.LibrarySelection;
        RuntimeLibraryModuleSnapshot original = receipt.Modules.Single(value => value.Module == ListBeamHostNames.Module);
        Require(!runtime.Modules.HasOldCode(ListBeamHostNames.Module));
        PreparedBeamImage next = runtime.Modules.PrepareBeam(Lists(), new BeamDependencySelection([ListBeamHostNames.Module]));
        Require(runtime.Modules.PublishPreparedBeam(next).Status == BeamPublicationStatus.Published);
        Require(runtime.Modules.HasOldCode(ListBeamHostNames.Module));
        Require(ReferenceEquals(runtime.LibrarySelection, receipt) && ReferenceEquals(receipt.Modules.Single(value => value.Module == ListBeamHostNames.Module), original));
        Require(!ReferenceEquals(original.Image, runtime.Modules.CurrentCodeImage(ListBeamHostNames.Module)));
        await RoundTrip(runtime);
    }

    internal static async Task Cancellation()
    {
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        bool refused = false;
        try
        {
            await using var unexpected = await ProcessRuntime.CreateAsync(Both(), canceled.Token);
        }
        catch (OperationCanceledException)
        {
            refused = true;
        }
        Require(refused);
        using var lifetime = new CancellationTokenSource();
        await using var runtime = await ProcessRuntime.CreateAsync(Both(), lifetime.Token);
        lifetime.Cancel();
        await RoundTrip(runtime);
    }

    private static async Task RoundTrip(ProcessRuntime runtime)
    {
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term empty = await runtime.Modules.Call(context, QueueModuleNames.Module, QueueModuleNames.New);
            Term queue = await runtime.Modules.Call(
                context,
                QueueModuleNames.Module,
                QueueModuleNames.In,
                Term.I(7),
                empty
            );
            Require((await runtime.Modules.Call(
                context,
                QueueModuleNames.Module,
                QueueModuleNames.ToList,
                queue
            )).Equals(Term.List(Term.I(7))));
            Require((await runtime.Modules.Call(
                context,
                ListBeamHostNames.Module,
                ListBeamHostNames.Sort,
                Term.List(Term.I(3), Term.I(1), Term.I(2))
            )).Equals(Term.List(Term.I(1), Term.I(2), Term.I(3))));

            return true;
        });
    }

    private static void Reject(Action action)
    {
        bool refused = false;
        try
        {
            action();
        }
        catch (ArgumentException)
        {
            refused = true;
        }
        Require(refused);
    }

    private static async Task RejectStartup(RuntimeLibrarySelection selection)
    {
        bool refused = false;
        try
        {
            await using var unexpected = await ProcessRuntime.CreateAsync(selection);
        }
        catch (InvalidOperationException)
        {
            refused = true;
        }
        Require(refused);
    }
}
