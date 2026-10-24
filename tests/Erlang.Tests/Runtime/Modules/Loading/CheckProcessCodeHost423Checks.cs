using Erlang.Compiler;
using System.Reflection;

namespace Erlang.Tests;

internal static class CheckProcessCodeHost423Checks
{
    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(CheckProcessCodeHost423Names.Failure);
    }

    private static Task<Term> Query(ProcessRuntime owner, RuntimeNodeFoundationHostActor actor, string module)
        => actor.InvokeAsync(context => owner.Modules.Call(
            context,
            CheckProcessCodeHost423Names.Module,
            CheckProcessCodeHost423Names.Function,
            Term.A(module)
        ).AsTask());

    internal static async Task Bindings()
    {
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapCheckProcessCode423.Register(runtime, registry);
            RuntimeBootstrapCheckProcessCode423.Register(runtime, registry);
        });
        var native = typeof(ModuleRegistry).GetMethod(
            CheckProcessCodeHost423Names.NativeEntry,
            BindingFlags.NonPublic | BindingFlags.Instance,
            [typeof(string), typeof(string), typeof(int)]
        )!
            .CreateDelegate<Func<string, string, int, bool>>(runtime.Modules);
        Require(native(CheckProcessCodeHost423Names.Module, CheckProcessCodeHost423Names.Function, 1));
        Require(runtime.Modules.SourceOwner(CheckProcessCodeHost423Names.Module, CheckProcessCodeHost423Names.Function, 1) is null);
        bool rejected = false;
        try
        {
            RuntimeBootstrapCheckProcessCode423.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            rejected = true;
        }
        Require(rejected);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Require((await Query(runtime, actor, CheckProcessCodeHost423Names.Missing)).Equals(Term.A(CheckProcessCodeHost423Names.False)));
        bool cancelled = false;
        try
        {
            await actor.InvokeAsync(async context =>
            {
                DirtyNative419Access.Cancel(context);

                return await runtime.Modules.Call(
                    context,
                    CheckProcessCodeHost423Names.Module,
                    CheckProcessCodeHost423Names.Function,
                    Term.A(CheckProcessCodeHost423Names.Missing)
                );
            });
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }
        Require(cancelled);
    }

    internal static Task Ownership() => Held(false);

    internal static Task Exit() => Held(true);

    private static async Task Held(bool kill)
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapCheckProcessCode423.Register(runtime, registry));
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        runtime.Modules.Register(
            CheckProcessCodeHost423Names.Callback,
            CheckProcessCodeHost423Names.Hold,
            0,
            async (context, _) =>
            {
                entered.TrySetResult(context);
                await release.Task.WaitAsync(context.Cancellation);
                Term value = await runtime.Modules.Call(
                    context,
                    CheckProcessCodeHost423Names.Module,
                    CheckProcessCodeHost423Names.Function,
                    Term.A(CheckProcessCodeHost423Names.Subject)
                );
                result.TrySetResult(value);

                return value;
            }
        );
        new Parser(CheckProcessCodeHost423Source.First).ParseModule().Register(runtime.Modules);
        var first = runtime.Modules.CurrentCodeImage(CheckProcessCodeHost423Names.Subject)!;
        var sourceResult = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle target = runtime.Spawn(async context =>
        {
            Term value = await runtime.Modules.Call(context, CheckProcessCodeHost423Names.Subject, CheckProcessCodeHost423Names.Run);
            sourceResult.TrySetResult(value);

            return value;
        });
        try
        {
            ProcessContext targetContext = await entered.Task.WaitAsync(RuntimeNodeFoundationHostActorParameters.Deadline);
            Require(!runtime.Modules.IsExecutingOldCode(targetContext, CheckProcessCodeHost423Names.Subject));
            Require(runtime.Modules.DeleteModule(CheckProcessCodeHost423Names.Subject).Equals(Term.A(CheckProcessCodeHost423Names.True)));
            new Parser(CheckProcessCodeHost423Source.Second).ParseModule().Register(runtime.Modules);
            Require(!ReferenceEquals(first, runtime.Modules.CurrentCodeImage(CheckProcessCodeHost423Names.Subject)));
            Require(runtime.Modules.IsExecutingOldCode(targetContext, CheckProcessCodeHost423Names.Subject));
            Require((await Query(runtime, controller, CheckProcessCodeHost423Names.Subject)).Equals(Term.A(CheckProcessCodeHost423Names.False)));
            if (kill)
            {
                await controller.Invoke(context =>
                {
                    runtime.Exit(context, target.Pid, Term.A(CheckProcessCodeHost423Names.Kill));

                    return true;
                });
            }
            else
            {
                release.TrySetResult();
                Require((await result.Task.WaitAsync(RuntimeNodeFoundationHostActorParameters.Deadline)).Equals(Term.A(CheckProcessCodeHost423Names.True)));
            }
            Term completion = await target.Completion.WaitAsync(RuntimeNodeFoundationHostActorParameters.Deadline);
            if (!kill) Require(completion.Equals(Term.A(CheckProcessCodeHost423Names.Normal)));
            if (!kill) Require((await sourceResult.Task.WaitAsync(RuntimeNodeFoundationHostActorParameters.Deadline))
                .Equals(Term.A(CheckProcessCodeHost423Names.First)));
            var unwound = (TaskCompletionSource)typeof(ProcessContext).GetProperty(
                CheckProcessCodeHost423Names.Unwound,
                BindingFlags.NonPublic | BindingFlags.Instance
            )!.GetValue(targetContext)!;
            await unwound.Task.WaitAsync(RuntimeNodeFoundationHostActorParameters.Deadline);
            Require(!runtime.Modules.IsExecutingOldCode(targetContext, CheckProcessCodeHost423Names.Subject));
            Require((await Query(runtime, controller, CheckProcessCodeHost423Names.Subject)).Equals(Term.A(CheckProcessCodeHost423Names.False)));
        }
        finally
        {
            release.TrySetResult();
            await target.Completion.WaitAsync(RuntimeNodeFoundationHostActorParameters.Deadline);
        }
    }
}
