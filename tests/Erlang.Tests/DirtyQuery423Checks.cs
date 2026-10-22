using System.Reflection;

namespace Erlang.Tests;

internal static class DirtyQuery423Checks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(DirtyQuery423Names.Failure);
    }

    private static Task<Term> Query(RuntimeNodeFoundationHostActor observer, Pid pid)
        => observer.InvokeAsync(context => context.Runtime.Modules.Call(
            context,
            DirtyQuery423Names.Module,
            DirtyQuery423Names.Function,
            pid
        ).AsTask());

    internal static Task Cpu() => Running(DirtyNative419Names.Cpu);

    internal static Task Io() => Running(DirtyNative419Names.Io);

    private static async Task Running(string kind)
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDirtyQuery423.Register(runtime, registry));
        await using var first = new RuntimeNodeFoundationHostActor(runtime);
        await using var queued = new RuntimeNodeFoundationHostActor(runtime);
        await using var observer = new RuntimeNodeFoundationHostActor(runtime);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var secondEntered = new ManualResetEventSlim();
        using var secondRelease = new ManualResetEventSlim();
        ProcessContext a = await first.Ready, b = await queued.Ready;
        Task<bool>? work = null;
        Task<Term>? next = null;
        try
        {
            Require((await Query(observer, a.Self)).Equals(Term.A(DirtyQuery423Names.False)));
            work = first.InvokeAsync(async context =>
            {
                await DirtyNative419Access.Schedule(
                    context,
                    kind,
                    [],
                    (_, _) =>
                {
                    entered.Set();
                    Require(release.Wait(DirtyQuery423Names.Deadline));

                    return Nil.Value;
                }
                );

                return true;
            });
            Require(entered.Wait(DirtyQuery423Names.Deadline));
            next = await queued.Invoke(context => DirtyNative419Access.Schedule(
                context,
                kind,
                [],
                (_, _) =>
            {
                secondEntered.Set();
                Require(secondRelease.Wait(DirtyQuery423Names.Deadline));

                return Nil.Value;
            }
            ));
            Require((await Query(observer, a.Self)).Equals(Term.A(DirtyQuery423Names.True)));
            Require((await Query(observer, b.Self)).Equals(Term.A(DirtyQuery423Names.False)));
            Require(!secondEntered.IsSet);
            release.Set();
            Require(secondEntered.Wait(DirtyQuery423Names.Deadline));
            await work;
            Require((await Query(observer, a.Self)).Equals(Term.A(DirtyQuery423Names.False)));
            Require((await Query(observer, b.Self)).Equals(Term.A(DirtyQuery423Names.True)));
            secondRelease.Set();
            await next;
            Require((await Query(observer, b.Self)).Equals(Term.A(DirtyQuery423Names.False)));
        }
        finally
        {
            release.Set();
            secondRelease.Set();
            try
            {
                if (work is not null) await work;
            }
            finally
            {
                if (next is not null) await next;
            }
        }
    }

    internal static async Task Binding()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapDirtyQuery423.Register(runtime, registry);
            RuntimeBootstrapDirtyQuery423.Register(runtime, registry);
        });
        var nativeEntry = typeof(ModuleRegistry).GetMethod(
            DirtyQuery423Names.NativeEntry,
            BindingFlags.NonPublic | BindingFlags.Instance,
            [typeof(string), typeof(string), typeof(int)]
        )!.CreateDelegate<Func<string, string, int, bool>>(runtime.Modules);
        Require(nativeEntry(DirtyQuery423Names.Module, DirtyQuery423Names.Function, 1));
        await using var foreign = new ProcessRuntime();
        await foreign.InitializeModulesAsync(_ => { });
        bool rejected = false;
        try
        {
            RuntimeBootstrapDirtyQuery423.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            rejected = true;
        }
        Require(rejected);
        await using var observer = new RuntimeNodeFoundationHostActor(runtime);
        var ended = new RuntimeNodeFoundationHostActor(runtime);
        Pid dead = (await ended.Ready).Self;
        await ended.DisposeAsync();
        Require((await Query(observer, dead)).Equals(Term.A(DirtyQuery423Names.False)));
        await using var other = new RuntimeNodeFoundationHostActor(foreign);
        Pid alien = (await other.Ready).Self;
        bool badarg = false;
        try
        {
            await Query(observer, alien);
        }
        catch (ErlangException error)
        {
            badarg = error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument));
        }
        Require(badarg);
    }
}
