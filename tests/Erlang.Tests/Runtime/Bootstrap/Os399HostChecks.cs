using Erlang;
using static Erlang.Tests.Os399HostAccess;

namespace Erlang.Tests;

internal static class Os399HostChecks
{
    internal static Task Codec()
    {
        string Read(Term value, bool key = false) => (string)Call(
            null,
            Os399HostNames.Codec,
            "Read",
            value,
            key,
            CancellationToken.None
        )!;
        Require(Read(Term.List(Text("A"), Term.List(Term.I(0xD800)), Nil.Value)) == "A\uD800");
        Require(Read(Text("=Z:"), true) == "=Z:");
        Require(Read(Nil.Value, true) == string.Empty);
        foreach (Term bad in new Term[] { Term.A("atom"), new BitString(new byte[] { 65 }), Term.List(Term.I(0)), Term.List(Term.I(0x10000)), new Cons(Term.I(65), Term.A("tail")) })
        {
            try
            {
                Read(bad);
                throw new InvalidOperationException(Os399HostNames.Invariant);
            }
            catch (ErlangException e) when (e.Reason.Equals(Term.A(Os399HostNames.BadArgument))) { }
        }
        try
        {
            Read(Text("A=B"), true);
            throw new InvalidOperationException(Os399HostNames.Invariant);
        }
        catch (ErlangException e) when (e.Reason.Equals(Term.A(Os399HostNames.BadArgument))) { }
        Require(((Term)Call(
            null,
            Os399HostNames.Codec,
            "Write",
            "\uD83D\uDE42",
            CancellationToken.None
        )!).Equals(Term.List(Term.I(0xD83D), Term.I(0xDE42))));

        return Task.CompletedTask;
    }

    internal static Task StoreCancelClose()
    {
        if (!OperatingSystem.IsWindows()) return Task.CompletedTask;
        object store = Store();
        using var dispose = (IDisposable)store;
        Term key = Text("A"), value = Text("first");
        StoreCall(
            store,
            "Put",
            key,
            value,
            CancellationToken.None
        );
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        try
        {
            StoreCall(
                store,
                "Put",
                key,
                Text("changed"),
                cancelled.Token
            );
            throw new InvalidOperationException(Os399HostNames.Invariant);
        }
        catch (OperationCanceledException) { }
        Require(StoreCall(
            store,
            "Get",
            key,
            CancellationToken.None
        ).Equals(value));
        try
        {
            StoreCall(
                store,
                "Unset",
                key,
                cancelled.Token
            );
            throw new InvalidOperationException(Os399HostNames.Invariant);
        }
        catch (OperationCanceledException) { }
        Require(StoreCall(
            store,
            "Get",
            key,
            CancellationToken.None
        ).Equals(value));
        object independent = Store();
        using var disposeIndependent = (IDisposable)independent;
        Require(StoreCall(
            independent,
            "Get",
            key,
            CancellationToken.None
        ).Equals(Term.A("false")));
        dispose.Dispose();
        dispose.Dispose();
        try
        {
            StoreCall(
                store,
                "Get",
                key,
                CancellationToken.None
            );
            throw new InvalidOperationException(Os399HostNames.Invariant);
        }
        catch (ObjectDisposedException) { }

        return Task.CompletedTask;
    }

    internal static Task Tree()
    {
        if (!OperatingSystem.IsWindows()) return Task.CompletedTask;
        object store = Store();
        using var dispose = (IDisposable)store;
        foreach (string key in new[] { "D", "B", "F", "A", "C", "E", "G" }) StoreCall(
            store,
            "Put",
            Text(key),
            Text(key),
            CancellationToken.None
        );
        foreach (string key in new[] { "D", "F", "B" }) StoreCall(
            store,
            "Unset",
            Text(key),
            CancellationToken.None
        );
        foreach (string key in new[] { "A", "C", "E", "G" }) Require(StoreCall(
            store,
            "Get",
            Text(key),
            CancellationToken.None
        ).Equals(Text(key)));
        foreach (string key in new[] { "D", "F", "B" }) Require(StoreCall(
            store,
            "Get",
            Text(key),
            CancellationToken.None
        ).Equals(Term.A("false")));
        StoreCall(
            store,
            "Put",
            Text("a"),
            Text("replacement"),
            CancellationToken.None
        );
        Require(StoreCall(
            store,
            "Get",
            Text("A"),
            CancellationToken.None
        ).Equals(Text("replacement")));
        Term all = StoreCall(store, "All", CancellationToken.None);
        var rows = Cons.Items(all).Cast<TupleTerm>().ToArray();
        Require(rows.Length == 4);
        Require(rows.Count(t => t.Items[0].Equals(Text("a")) && t.Items[1].Equals(Text("replacement"))) == 1);

        return Task.CompletedTask;
    }

    internal static async Task Owner()
    {
        Require(Capture(
            null,
            Os399HostNames.Module,
            "env",
            0
        ) is null);
        await using var owner = new ProcessRuntime();
        Require(!owner.Modules.HasExport(Os399HostNames.Module, "env", 0));
        foreach (var wrong in new[] { ("other", "env", 0), (Os399HostNames.Module, "getenv", 2), (Os399HostNames.Module, "unknown", 0) })
            Require(Capture(
                owner,
                wrong.Item1,
                wrong.Item2,
                wrong.Item3
            ) is null);
        ErlangFunction? body = Capture(
            owner,
            Os399HostNames.Module,
            "env",
            0
        );
        if (!OperatingSystem.IsWindows())
        {
            Require(body is null);

            return;
        }
        Require(body is not null);
        await using var foreign = new ProcessRuntime();
        await using (var actor = new RuntimeNodeFoundationHostActor(foreign))
        {
            ProcessContext context = await actor.Ready;
            try
            {
                await body!(context, []);
                throw new InvalidOperationException(Os399HostNames.Invariant);
            }
            catch (InvalidOperationException failure) when (failure.Message == Os399HostNames.Foreign) { }
        }
        ErlangFunction put = Capture(
            owner,
            Os399HostNames.Module,
            "putenv",
            2
        )!;
        ProcessContext cancelledContext;
        await using (var actor = new RuntimeNodeFoundationHostActor(owner))
        {
            cancelledContext = await actor.Ready;
            var stop = (CancellationTokenSource)typeof(ProcessContext).GetProperty("Stop", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(cancelledContext)!;
            stop.Cancel();
            await actor.Process.Completion;
        }
        try
        {
            await put(cancelledContext, [Text("ERLANG399_CANCEL_NOT_COMMITTED"), Text("bad")]);
            throw new InvalidOperationException(Os399HostNames.Invariant);
        }
        catch (OperationCanceledException) { }
        ErlangFunction get = Capture(
            owner,
            Os399HostNames.Module,
            "getenv",
            1
        )!;
        await using (var actor = new RuntimeNodeFoundationHostActor(owner))
            Require((await actor.InvokeAsync(async c => await get(c, [Text("ERLANG399_CANCEL_NOT_COMMITTED")]))).Equals(Term.A("false")));
        ProcessContext saved;
        await using (var actor = new RuntimeNodeFoundationHostActor(owner)) saved = await actor.Ready;
        await owner.DisposeAsync();
        try
        {
            await body!(saved, []);
            throw new InvalidOperationException(Os399HostNames.Invariant);
        }
        catch (ObjectDisposedException) { }
        catch (OperationCanceledException) { }
    }
}
