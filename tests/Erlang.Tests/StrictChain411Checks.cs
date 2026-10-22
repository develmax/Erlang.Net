namespace Erlang.Tests;

// Root supplies bytes compiled from the five unchanged sibling Erlang files.
// No expected native result is manufactured and no source import is projected away.
internal static class StrictChain411Checks
{
    internal static Task OrderAndMixedImports() => OrderAndMixedImports(StrictChain411Resources.Image);

    internal static Task Refusals() => Refusals(StrictChain411Resources.Image);

    internal static Task PreCanceled() => PreCanceled(StrictChain411Resources.Image);

    internal static Task CancelActualBlockedCallback() => CancelActualBlockedCallback(StrictChain411Resources.Image);

    internal static async Task OrderAndMixedImports(Func<string, byte[]> image)
    {
        string[] mutable = [StrictChain411Names.B, StrictChain411Names.A];
        var options = new RuntimeLibraryStartupOptions(onLoadOrder: mutable);
        mutable[0] = StrictChain411Names.Failure;
        await using var runtime = await ProcessRuntime.CreateWithOptionsAsync(Selection(image), options, default)
            .WaitAsync(StrictChain411Names.Deadline);
        Require(options.OnLoadOrder[0] == StrictChain411Names.B);
        Require(runtime.LibrarySelection.Modules.Count == 4);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term answer = await actor.InvokeAsync(async c => await runtime.Modules.Call(c, StrictChain411Names.A, StrictChain411Names.Run));
        Require(answer.Equals(Term.A(StrictChain411Names.ReadyA)));
        Require(runtime.Modules.SourceOwner(StrictChain411Names.A, StrictChain411Names.Run, 0) is PreparedBeamImage);
        Require(runtime.Modules.SourceOwner(StrictChain411Names.B, StrictChain411Names.Run, 0) is PreparedBeamImage);
        // The complete import graph has Ordinary -> B -> Ordinary. It is legal.
    }

    internal static async Task Refusals(Func<string, byte[]> image)
    {
        await Refuse(() => ProcessRuntime.CreateAsync(Selection(image)));
        await Refuse(() => ProcessRuntime.CreateWithOptionsAsync(Selection(image),
            new RuntimeLibraryStartupOptions(onLoadOrder: [StrictChain411Names.B]), default));
        await Refuse(() => ProcessRuntime.CreateWithOptionsAsync(
            Selection(image),
            new RuntimeLibraryStartupOptions(onLoadOrder: [StrictChain411Names.B, StrictChain411Names.A, StrictChain411Names.Ordinary]),
            default
        ));
        await Refuse(() => ProcessRuntime.CreateWithOptionsAsync(Selection(image),
            new RuntimeLibraryStartupOptions(onLoadOrder: [StrictChain411Names.A, StrictChain411Names.B]), default));
        await Refuse(() => ProcessRuntime.CreateWithOptionsAsync(
            Selection(image, omitDependency: true),
            new RuntimeLibraryStartupOptions(onLoadOrder: [StrictChain411Names.B, StrictChain411Names.A]),
            default
        ));
        await Refuse(() => ProcessRuntime.CreateWithOptionsAsync(
            new RuntimeLibrarySelection([
            RuntimeModuleSelection.Managed(StrictChain411Names.Erlang),
            RuntimeModuleSelection.Beam(StrictChain411Names.Failure, image(StrictChain411Names.Failure), [StrictChain411Names.Erlang])
        ]),
            new RuntimeLibraryStartupOptions(onLoadOrder: [StrictChain411Names.Failure]),
            default
        ));
    }

    internal static async Task PreCanceled(Func<string, byte[]> image)
    {
        // Previously valid default-literal call sites must remain compile-unambiguous.
        await using (var legacyTwo = await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([]), default))
            Require(legacyTwo.LibrarySelection.Modules.Count == 0);
        await using (var legacyThree = await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([]), default, default))
            Require(legacyThree.LibrarySelection.Modules.Count == 0);
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try
        {
            await using var unexpected = await ProcessRuntime.CreateWithOptionsAsync(Selection(image),
                new RuntimeLibraryStartupOptions(onLoadOrder: [StrictChain411Names.B, StrictChain411Names.A]), stop.Token);
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }
        Require(canceled);
    }

    internal static async Task CancelActualBlockedCallback(Func<string, byte[]> image)
    {
        using var stop = new CancellationTokenSource();
        var held = new TaskCompletionSource<Pid>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessRuntime? owned = null;
        var observer = LibrarySelectionPublicCancellationAccess.Observer();
        Action<string, ProcessRuntime, ProcessContext?>? previous = observer.Value;
        observer.Value = (phase, runtime, _) =>
        {
            if (phase != LibrarySelectionPublicCancellationNames.SetupPhase) return;
            owned = runtime;
            ProcessHandle listener = runtime.Spawn(async context =>
            {
                Term? notice = await context.ReceiveAsync(
                    term => term is TupleTerm tuple
                    && tuple.Items.Count == 2 && tuple.Items[0].Equals(Term.A(StrictChain411Names.HeldNotice))
                    ? term : null,
                    StrictChain411Names.Deadline
                );
                Require(notice is TupleTerm { Items.Count: 2 });
                held.TrySetResult((Pid)((TupleTerm)notice!).Items[1]);

                return Nil.Value;
            });
            runtime.Register(StrictChain411Names.Observer, listener.Pid);
        };
        Task<ProcessRuntime> factory;
        try
        {
            factory = ProcessRuntime.CreateWithOptionsAsync(
                new RuntimeLibrarySelection([
                RuntimeModuleSelection.Managed(StrictChain411Names.Erlang),
                RuntimeModuleSelection.Beam(StrictChain411Names.Held, image(StrictChain411Names.Held), [StrictChain411Names.Erlang])
            ]),
                new RuntimeLibraryStartupOptions(onLoadOrder: [StrictChain411Names.Held]),
                stop.Token
            );
        }
        finally
        {
            observer.Value = previous;
        }
        try
        {
            Pid callback = await held.Task.WaitAsync(StrictChain411Names.Deadline);
            Require(owned is not null && owned.IsAlive(callback) && !factory.IsCompleted);
            stop.Cancel();
            bool canceled = false;
            try
            {
                await using var unexpected = await factory.WaitAsync(StrictChain411Names.Deadline);
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }
            Require(canceled);
            Require(LibrarySelectionPublicCancellationAccess.Disposed(owned!));
            Require(LibrarySelectionPublicCancellationAccess.Dispatcher(owned!).IsCompletedSuccessfully);
            Require(!owned!.IsAlive(callback) && owned.LibrarySelection.Modules.Count == 0);
        }
        finally
        {
            stop.Cancel();
            try
            {
                if (owned is not null) await owned.DisposeAsync().AsTask().WaitAsync(StrictChain411Names.Deadline);
            }
            finally
            {
                try
                {
                    _ = await factory.WaitAsync(StrictChain411Names.Deadline);
                }
                catch (OperationCanceledException) { }
            }
        }
    }

    private static RuntimeLibrarySelection Selection(Func<string, byte[]> image, bool omitDependency = false) => new([
        RuntimeModuleSelection.Managed(StrictChain411Names.Erlang),
        RuntimeModuleSelection.Beam(StrictChain411Names.Ordinary, image(StrictChain411Names.Ordinary),
            omitDependency ? [] : [StrictChain411Names.B]),
        RuntimeModuleSelection.Beam(StrictChain411Names.A, image(StrictChain411Names.A), [StrictChain411Names.Erlang, StrictChain411Names.Ordinary]),
        RuntimeModuleSelection.Beam(StrictChain411Names.B, image(StrictChain411Names.B), [StrictChain411Names.Erlang, StrictChain411Names.Ordinary])
    ]);

    private static async Task Refuse(Func<Task<ProcessRuntime>> action)
    {
        bool refused = false;
        try
        {
            await using var unexpected = await action().WaitAsync(StrictChain411Names.Deadline);
        }
        catch (Exception error) when (error is not TimeoutException && error is not OperationCanceledException)
        {
            refused = true;
        }
        Require(refused);
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(StrictChain411Names.Error);
    }
}
