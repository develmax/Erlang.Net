using Erlang;
using Erlang.Differential;

namespace Erlang.Tests;

internal static class NonTailStackLifecycleHostChecks
{
    internal static async Task Run()
    {
        await RunNormal();
        await RunError();
        await RunCancellation();
    }

    internal static async Task RunNormal()
    {
        foreach (bool anonymous in new[] { false, true })
            await Check(anonymous, false, false);
    }

    internal static async Task RunError()
    {
        foreach (bool anonymous in new[] { false, true })
            await Check(anonymous, true, false);
    }

    internal static async Task RunCancellation()
    {
        foreach (bool anonymous in new[] { false, true })
            await Check(anonymous, false, true);
    }

    private static async Task Check(bool anonymous, bool fail, bool cancel)
    {
        using var source = GeneratedModuleCompiler.Compile(anonymous ? NonTailStackLifecycleHostNames.AnonymousSource : NonTailStackLifecycleHostNames.NamedSource);
        using var replacement = GeneratedModuleCompiler.Compile(NonTailStackLifecycleHostNames.ReplacementSource);
        await using var runtime = new ProcessRuntime();
        source.Definition.Register(runtime.Modules);
        ModuleCodeImage image = runtime.Modules.CurrentCodeImage(source.Definition.Name)!;
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var bridgeUnwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        int heldCalls = 0;
        int cleanups = 0;
        ErlangFunction held = async (context, arguments) =>
        {
            try
            {
                Require(arguments.Count == 0 && ReferenceEquals(context.ExecutingSourceImage(source.Definition), image) && Active(context, image));
                Interlocked.Increment(ref heldCalls);
                await Task.Yield();
                Require(ReferenceEquals(context.ExecutingSourceImage(source.Definition), image) && Active(context, image));
                entered.TrySetResult(context);
                await release.Task.WaitAsync(context.Cancellation);
                Require(ReferenceEquals(context.ExecutingSourceImage(source.Definition), image) && Active(context, image));
                if (fail)
                    throw new ErlangException(Term.A(NonTailStackLifecycleHostNames.Fault));

                return Term.I(0);
            }
            catch (Exception failure)
            {
                entered.TrySetException(failure);
                throw;
            }
            finally
            {
                bridgeUnwound.TrySetResult();
            }
        };
        ErlangFunction cleanup = (context, arguments) =>
        {
            Require(arguments.Count == 0 && ReferenceEquals(context.ExecutingSourceImage(source.Definition), image) && Active(context, image));
            Interlocked.Increment(ref cleanups);

            return ValueTask.FromResult<Term>(Nil.Value);
        };
        NifAdmissionHostBridge.Call(
            runtime.Modules,
            NonTailStackLifecycleHostNames.RegisterIntrinsic,
            NonTailStackLifecycleHostNames.Bridge,
            NonTailStackLifecycleHostNames.Held,
            0,
            held
        );
        NifAdmissionHostBridge.Call(
            runtime.Modules,
            NonTailStackLifecycleHostNames.RegisterIntrinsic,
            NonTailStackLifecycleHostNames.Bridge,
            NonTailStackLifecycleHostNames.Cleanup,
            0,
            cleanup
        );
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                result.TrySetResult(await runtime.Modules.Call(
                    context,
                    NonTailStackLifecycleHostNames.Module,
                    NonTailStackLifecycleHostNames.Run,
                    Term.I(NonTailStackLifecycleHostNames.Depth)
                ));
            }
            catch (Exception failure)
            {
                result.TrySetException(failure);
                throw;
            }

            return Nil.Value;
        });
        object? purge = null;
        Exception? primary = null;
        try
        {
            ProcessContext context = await entered.Task.WaitAsync(NonTailStackLifecycleHostNames.Deadline);
            Require(heldCalls == 1 && Active(context, image) && cleanups == 0);
            Require(runtime.Modules.DeleteModule(source.Definition.Name).Equals(Term.A(ErlangBooleanAtoms.True)));
            replacement.Definition.Register(runtime.Modules);
            Require(!ReferenceEquals(runtime.Modules.CurrentCodeImage(source.Definition.Name), image));
            purge = NifAdmissionHostBridge.Call(runtime.Modules, NonTailStackLifecycleHostNames.PreparePurge, source.Definition.Name)!;
            Require(purge is not null && runtime.Modules.PendingPurge(image) is { IsCompleted: false } && Active(context, image));
            if (cancel)
                await Stop(runtime, actor);
            else
                release.TrySetResult();
            await bridgeUnwound.Task.WaitAsync(NonTailStackLifecycleHostNames.Deadline);
            Term reason = await actor.Completion.WaitAsync(NonTailStackLifecycleHostNames.Deadline);
            var unwind = (TaskCompletionSource)NifAdmissionHostBridge.Property(context, NonTailStackLifecycleHostNames.Unwound);
            await unwind.Task.WaitAsync(NonTailStackLifecycleHostNames.Deadline);
            Require(heldCalls == 1 && !Active(context, image));
            if (cancel)
            {
                Require(reason.Equals(Term.A(NonTailStackLifecycleHostNames.Killed)) && cleanups == 0);
                try
                {
                    await result.Task.WaitAsync(NonTailStackLifecycleHostNames.Deadline);
                    throw new InvalidOperationException(NonTailStackLifecycleHostNames.Broken);
                }
                catch (OperationCanceledException) { }
            }
            else if (fail)
            {
                try
                {
                    await result.Task.WaitAsync(NonTailStackLifecycleHostNames.Deadline);
                    throw new InvalidOperationException(NonTailStackLifecycleHostNames.Broken);
                }
                catch (ErlangException failure)
                {
                    Require(failure.Reason.Equals(Term.A(NonTailStackLifecycleHostNames.Fault)) && cleanups == NonTailStackLifecycleHostNames.Depth);
                }
            }
            else
                Require((await result.Task.WaitAsync(NonTailStackLifecycleHostNames.Deadline)).Equals(Term.I(NonTailStackLifecycleHostNames.Depth)) && cleanups == NonTailStackLifecycleHostNames.Depth);
            Require(runtime.Modules.PendingPurge(image) is { IsCompleted: false });
            Require((bool)NifAdmissionHostBridge.Call(
                runtime.Modules,
                NonTailStackLifecycleHostNames.FinishPurge,
                purge,
                true
            )!);
            purge = null;
            Require(runtime.Modules.PendingPurge(image) is null);
        }
        catch (Exception failure)
        {
            primary = failure;
            throw;
        }
        finally
        {
            release.TrySetResult();
            try
            {
                if (purge is not null && runtime.Modules.PendingPurge(image) is not null)
                    NifAdmissionHostBridge.Call(
                        runtime.Modules,
                        NonTailStackLifecycleHostNames.FinishPurge,
                        purge,
                        false
                    );
                if (!actor.Completion.IsCompleted)
                    await Stop(runtime, actor);
                await actor.Completion.WaitAsync(NonTailStackLifecycleHostNames.Deadline);
                if (result.Task.IsCompleted)
                    _ = result.Task.Exception;
            }
            catch (Exception cleanupFailure) when (primary is not null)
            {
                throw new AggregateException(primary, cleanupFailure);
            }
        }
    }

    private static bool Active(ProcessContext context, ModuleCodeImage image) =>
        (bool)NifAdmissionHostBridge.Call(context, NonTailStackLifecycleHostNames.IsExecutingCode, image)!;

    private static Task Stop(ProcessRuntime runtime, ProcessHandle actor) => NifAdmissionHostBridge.Context(
        runtime,
        observer =>
    {
        runtime.Exit(observer, actor.Pid, Term.A(NonTailStackLifecycleHostNames.Kill));

        return Task.CompletedTask;
    }
    );

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(NonTailStackLifecycleHostNames.Broken);
    }
}
