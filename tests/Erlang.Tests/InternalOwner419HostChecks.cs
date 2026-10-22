namespace Erlang.Tests;

internal static class InternalOwner419HostChecks
{
    private static ErlangFunction Body(ProcessRuntime owner, string name, int arity)
    {
        object?[] args = [owner, InternalOwnerOriginal419Names.Internal, name, arity, null];
        bool found = (bool)typeof(ProcessRuntime).Assembly.GetType(InternalOwner419HostNames.Type, true)!
            .GetMethod(InternalOwner419HostNames.Capture, InternalOwner419HostNames.Static)!.Invoke(null, args)!;
        InternalOwnerOriginal419Access.Require(found);

        return (ErlangFunction)args[^1]!;
    }

    internal static async Task Reset()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        object store = PersistentTerm408HostAccess.Store(runtime);
        ErlangFunction reset = Body(runtime, InternalOwner419HostNames.Erase, 0);
        object? snapshot = null;
        Term key = Term.A(InternalOwner419HostNames.Key);
        Term original = Term.Tuple(Term.I(419), new BitString(new byte[] { 4, 1, 9 }));
        try
        {
            await actor.InvokeAsync(async context =>
            {
                await PersistentTerm408HostAccess.Put(
                    store,
                    context,
                    key,
                    original
                );
                snapshot = PersistentTerm408HostAccess.Call(store, PersistentTerm408HostNames.Snapshot)!;
                InternalOwnerOriginal419Access.Require((await reset(context, [])).Equals(Term.A(InternalOwner419HostNames.True)));
                InternalOwnerOriginal419Access.Require(PersistentTerm408HostAccess.Call(store, PersistentTerm408HostNames.GetAll, context) is Nil);
                object report = PersistentTerm408HostAccess.Call(store, PersistentTerm408HostNames.Report)!;
                InternalOwnerOriginal419Access.Require(PersistentArena416HostAccess.Number(report, PersistentTerm408HostNames.Payloads) == 1);
                var retained = (TupleTerm?[])PersistentTerm408HostAccess.Call(snapshot, PersistentTerm408HostNames.Materialize, CancellationToken.None)!;
                InternalOwnerOriginal419Access.Require(retained.Length == 1 && retained[0]!.Items[0].Equals(key) && retained[0]!.Items[1].Equals(original));
                await PersistentTerm408HostAccess.Put(
                    store,
                    context,
                    key,
                    Term.I(420)
                );
                InternalOwnerOriginal419Access.Require(PersistentArena416HostAccess.Number(PersistentTerm408HostAccess.Call(store, PersistentTerm408HostNames.Report)!, PersistentTerm408HostNames.Payloads) == 2);
                ((IDisposable)snapshot).Dispose();
                snapshot = null;
                InternalOwnerOriginal419Access.Require(PersistentArena416HostAccess.Number(PersistentTerm408HostAccess.Call(store, PersistentTerm408HostNames.Report)!, PersistentTerm408HostNames.Payloads) == 1);
                _ = await reset(context, []);
                InternalOwnerOriginal419Access.Require(PersistentArena416HostAccess.Number(PersistentTerm408HostAccess.Call(store, PersistentTerm408HostNames.Report)!, PersistentTerm408HostNames.Payloads) == 0);

                return true;
            });
            await using var foreign = new ProcessRuntime();
            await using var foreignActor = new RuntimeNodeFoundationHostActor(foreign);
            await foreignActor.InvokeAsync(async context =>
            {
                bool rejected = false;
                try
                {
                    _ = await reset(context, []);
                }
                catch (InvalidOperationException)
                {
                    rejected = true;
                }
                InternalOwnerOriginal419Access.Require(rejected);

                return true;
            });
        }
        finally
        {
            (snapshot as IDisposable)?.Dispose();
        }
    }

    internal static async Task Cursor()
    {
        await using var runtime = new ProcessRuntime(maximumProcesses: InternalOwner419HostNames.Capacity);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ErlangFunction next = Body(runtime, InternalOwner419HostNames.Next, 1);
        await actor.InvokeAsync(async context =>
        {
            var expected = runtime.ProcessSnapshot().ToHashSet();
            var actual = new HashSet<Pid>();
            int cursor = 0;
            while (true)
            {
                Term result = await next(context, [Term.I(cursor)]);
                if (result.Equals(Term.A(InternalOwner419HostNames.None))) break;
                InternalOwnerOriginal419Access.Require(result is TupleTerm { Items.Count: 2 });
                var batch = (TupleTerm)result;
                int following = (int)((Integer)batch.Items[0]).Value;
                InternalOwnerOriginal419Access.Require(following > cursor && following <= InternalOwner419HostNames.Capacity);
                foreach (Term pid in Cons.Items(batch.Items[1]))
                    InternalOwnerOriginal419Access.Require(pid is Pid identity && actual.Add(identity));
                cursor = following;
            }
            InternalOwnerOriginal419Access.Require(cursor == InternalOwner419HostNames.Capacity && expected.SetEquals(actual));

            return true;
        });
    }
}
