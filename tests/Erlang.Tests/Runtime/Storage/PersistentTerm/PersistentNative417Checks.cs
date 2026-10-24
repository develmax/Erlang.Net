namespace Erlang.Tests;

internal static class PersistentNative417Checks
{
    internal static Task HaltHeld() => PersistentLifecycle408Checks.Scenario(true, false, true);

    internal static Task InitializerHeld() => PersistentLifecycle408Checks.Scenario(false, true, true);

    private static long Field(MapTerm info, string key)
        => (long)((Integer)info.Entries.Single(pair => pair.Key.Equals(Term.A(key))).Value).Value;

    internal static async Task OriginalImage()
    {
        await using var runtime = new ProcessRuntime(1);
        PersistentTerm408HostAccess.Require(!runtime.Modules.HasExport(PersistentNative417Names.Module, PersistentNative417Names.Info, 0));
        PreparedBeamImage image = runtime.Modules.PrepareBeam(
            NativeProvider408Access.Resource(PersistentNative417Names.Image),
            new BeamDependencySelection([NativeProvider408Names.RuntimeModule])
        );
        PersistentTerm408HostAccess.Require(image.NativePrimitiveExports.Count == PersistentNative417Names.NativeCount
            && PersistentNative417Names.Signatures.All(signature => image.NativePrimitiveExports.Contains(signature))
            && !runtime.Modules.HasExport(PersistentNative417Names.Module, PersistentNative417Names.Info, 0));
        PersistentTerm408HostAccess.Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published
            && ReferenceEquals(runtime.Modules.SourceOwner(PersistentNative417Names.Module, PersistentNative417Names.Info, 0), image));
        foreach (var signature in PersistentNative417Names.Signatures)
            PersistentTerm408HostAccess.Require(ReferenceEquals(runtime.Modules.SourceOwner(PersistentNative417Names.Module,
                signature.Name, signature.Arity), image));
        object store = PersistentTerm408HostAccess.Store(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            async ValueTask<Term> Call(string name, params Term[] args)
                => await runtime.Modules.Call(
                    context,
                    PersistentNative417Names.Module,
                    name,
                    args
                );
            var before = (MapTerm)await Call(PersistentNative417Names.Info);
            Term key = Term.A(PersistentTerm408HostNames.Key);
            Term value = Term.Tuple(Term.I(417), new BitString(new byte[] { 4, 1, 7 }));
            await Call(PersistentNative417Names.Put, key, value);
            var after = (MapTerm)await Call(PersistentNative417Names.Info);
            object report = PersistentTerm408HostAccess.Call(store, PersistentTerm408HostNames.Report)!;
            PersistentTerm408HostAccess.Require(after.Entries.Count == 2 && Field(after, PersistentNative417Names.Count) == 1
                && Field(before, PersistentNative417Names.Count) == 0
                && Field(after, PersistentNative417Names.Memory) == PersistentArena416HostAccess.Number(report, PersistentNative417Names.ActiveTable)
                    + PersistentArena416HostAccess.Number(report, PersistentNative417Names.ActivePair)
                && Field(after, PersistentNative417Names.Memory) > Field(before, PersistentNative417Names.Memory));
            PersistentTerm408HostAccess.Require((await Call(PersistentNative417Names.Get, key)).Equals(value));
            Term fallback = Term.Tuple(Term.I(0));
            PersistentTerm408HostAccess.Require(ReferenceEquals(await Call(PersistentNative417Names.Get, Term.I(-1), fallback), fallback));
            await Call(PersistentNative417Names.PutNew, key, value);
            PersistentTerm408HostAccess.Require((await Call(PersistentNative417Names.Info)).Equals(after)
                && Cons.Items(await Call(PersistentNative417Names.Get)).OfType<TupleTerm>().Single().Items[1].Equals(value));
            await Call(PersistentNative417Names.Erase, key);
            var empty = (MapTerm)await Call(PersistentNative417Names.Info);
            PersistentTerm408HostAccess.Require(Field(empty, PersistentNative417Names.Count) == 0
                && Field(empty, PersistentNative417Names.Memory) == Field(before, PersistentNative417Names.Memory));
            bool rejected = false;
            try
            {
                _ = await Call(PersistentNative417Names.Get, key);
            }
            catch (ErlangException error)
            {
                rejected = NativeProvider408Access.NativeError(error, PersistentNative417Names.Get)
                    && error.StackTraceTerm is Cons { Head: TupleTerm frame } && frame.Items[2].Equals(Term.List(key));
            }
            PersistentTerm408HostAccess.Require(rejected);

            return true;
        });
    }

    internal static async Task Owner()
    {
        var runtime = new ProcessRuntime(1);
        await using var foreign = new ProcessRuntime(1);
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            object?[] args = [runtime, PersistentNative417Names.Module, PersistentNative417Names.Info, 0, null];
            bool found = (bool)typeof(ProcessRuntime).Assembly.GetType(PersistentNative417Names.Type, true)!
                .GetMethod(PersistentNative417Names.Capture, PersistentNative417Names.Static)!.Invoke(null, args)!;
            PersistentTerm408HostAccess.Require(found);
            var body = (ErlangFunction)args[4]!;
            await using (var other = new RuntimeNodeFoundationHostActor(foreign))
                await other.InvokeAsync(async context =>
                {
                    bool refused = false;
                    try
                    {
                        _ = await body(context, []);
                    }
                    catch (InvalidOperationException)
                    {
                        refused = true;
                    }
                    PersistentTerm408HostAccess.Require(refused);

                    return true;
                });
            actor = new(runtime);
            ProcessContext saved = await actor.Ready;
            await actor.InvokeAsync(async context =>
            {
                _ = await body(context, []);
                object store = PersistentTerm408HostAccess.Store(runtime);
                object before = PersistentTerm408HostAccess.Call(store, PersistentTerm408HostNames.Report)!;
                ((CancellationTokenSource)typeof(ProcessContext).GetProperty(PersistentArena416HostNames.Stop,
                    PersistentArena416HostNames.Members)!.GetValue(context)!).Cancel();
                bool refused = false;
                try
                {
                    _ = await body(context, []);
                }
                catch (OperationCanceledException)
                {
                    refused = true;
                }
                PersistentTerm408HostAccess.Require(refused && before.Equals(PersistentTerm408HostAccess.Call(store, PersistentTerm408HostNames.Report)!));

                return true;
            });
            await runtime.DisposeAsync();
            bool closed = false;
            try
            {
                _ = await body(saved, []);
            }
            catch (ObjectDisposedException)
            {
                closed = true;
            }
            PersistentTerm408HostAccess.Require(closed);
        }
        finally
        {
            try
            {
                if (actor is not null) await actor.DisposeAsync();
            }
            finally
            {
                await runtime.DisposeAsync();
            }
        }
    }

    internal static async Task WaitRetained(object store, long count)
    {
        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        while (true)
        {
            object report = PersistentTerm408HostAccess.Call(store, PersistentTerm408HostNames.Report)!;
            if ((bool)report.GetType().GetProperty(PersistentLifecycle408Names.Closed)!.GetValue(report)!
                && (long)report.GetType().GetProperty(PersistentTerm408HostNames.Payloads)!.GetValue(report)! == count) return;
            if (System.Diagnostics.Stopwatch.GetElapsedTime(start) > PersistentLifecycle408Names.Deadline)
                throw new TimeoutException(PersistentTerm408HostNames.Failure);
            await Task.Delay(PersistentNative417Names.PollMilliseconds);
        }
    }
}
