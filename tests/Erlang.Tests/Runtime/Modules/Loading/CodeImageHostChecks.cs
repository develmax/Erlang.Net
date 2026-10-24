using Erlang.Compiler;

namespace Erlang.Tests;

public static class CodeImageHostChecks
{
    public static async Task Run()
    {
        await VerifyTransfer(false);
        await VerifyTransfer(true);
        await VerifyCleanup(false);
        await VerifyCleanup(true);
    }

    private static async Task VerifyTransfer(bool tail)
    {
        await using var runtime = new ProcessRuntime();
        var registry = runtime.Modules;
        string privateBody = tail ? "F(Expected)" : "{kept,F(Expected)}";
        var module = new Parser("-module(code_image_host). -export([run/2,make/0]). run(F,Expected)->code_image_callback:pause(),private(F,Expected). private(F,Expected)->code_image_callback:observe()," + privateBody + ". make()->fun run/2.").ParseModule();
        var target = new Parser("-module(code_image_target). -export([run/1]). run(Expected)->code_image_callback:observe_fun(Expected).").ParseModule();
        module.Register(registry);
        target.Register(registry);
        ModuleCodeImage first = registry.CurrentCodeImage(CodeImageHostNames.Module)!;
        try
        {
            registry.Apply(current =>
            {
                current.DeleteModule(CodeImageHostNames.Module);
                module.Register(current);
                throw new InvalidOperationException(CodeImageHostNames.Broken);
            });
        }
        catch (InvalidOperationException exception) when (exception.Message == CodeImageHostNames.Broken)
        {
        }
        if (!ReferenceEquals(first, registry.CurrentCodeImage(CodeImageHostNames.Module)) || registry.HasOldCode(CodeImageHostNames.Module))
            throw new InvalidOperationException(CodeImageHostNames.Broken);
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        int oldObservations = 0;
        registry.Register(
            CodeImageHostNames.Callback,
            CodeImageHostNames.Pause,
            0,
            async (context, _) =>
            {
                if (Interlocked.Increment(ref calls) == 1)
                {
                    entered.SetResult(context);
                    await release.Task.WaitAsync(context.Cancellation);
                }

                return Term.A(CodeImageHostNames.Kept);
            }
        );
        registry.Register(
            CodeImageHostNames.Callback,
            CodeImageHostNames.Observe,
            0,
            (context, _) =>
            {
                if (registry.IsExecutingOldCode(context, CodeImageHostNames.Module))
                    Interlocked.Increment(ref oldObservations);

                return ValueTask.FromResult<Term>(Term.A(CodeImageHostNames.Kept));
            }
        );
        registry.Register(
            CodeImageHostNames.Callback,
            CodeImageHostNames.ObserveFun,
            1,
            (context, arguments) =>
            {
                bool expected = arguments[0].Equals(Term.A(CodeImageHostNames.Old));
                if (registry.IsExecutingOldCode(context, CodeImageHostNames.Module) != expected || registry.IsExecutingOldCode(context, CodeImageHostNames.Target))
                    throw new InvalidOperationException(CodeImageHostNames.Broken);

                return ValueTask.FromResult<Term>(Term.A(CodeImageHostNames.Kept));
            }
        );
        Exception? failure = null;
        FunctionTerm? held = null;
        var external = runtime.Functions.External(runtime, new ExternalFunctionIdentity(Term.A(CodeImageHostNames.Target), Term.A(CodeImageHostNames.Run), 1));
        var old = runtime.Spawn(async context =>
        {
            try
            {
                held = (FunctionTerm)await registry.Call(context, CodeImageHostNames.Module, CodeImageHostNames.Make);

                return await Execution.InvokeAsync(
                    module,
                    CodeImageHostNames.Run,
                    context,
                    [external, Term.A(tail ? CodeImageHostNames.Current : CodeImageHostNames.Old)]
                );
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }
        });
        ProcessContext oldContext = await entered.Task.WaitAsync(CodeImageHostParameters.Deadline);
        registry.DeleteModule(CodeImageHostNames.Module);
        module.Register(registry);
        if (ReferenceEquals(first, registry.CurrentCodeImage(CodeImageHostNames.Module)) || !registry.IsExecutingOldCode(oldContext, CodeImageHostNames.Module))
            throw new InvalidOperationException(CodeImageHostNames.Broken);
        release.SetResult();
        await old.Completion.WaitAsync(CodeImageHostParameters.Deadline);
        if (failure is not null)
            throw failure;
        if (oldObservations != 1 || registry.IsExecutingOldCode(oldContext, CodeImageHostNames.Module))
            throw new InvalidOperationException(CodeImageHostNames.Broken);
        var current = runtime.Spawn(async context =>
        {
            try
            {
                await registry.Call(
                    context,
                    CodeImageHostNames.Module,
                    CodeImageHostNames.Run,
                    external,
                    Term.A(CodeImageHostNames.Current)
                );
                if (registry.IsExecutingOldCode(context, CodeImageHostNames.Module))
                    throw new InvalidOperationException(CodeImageHostNames.Broken);
                if (held is null || registry.IsExecutingOldCode(context, CodeImageHostNames.Module))
                    throw new InvalidOperationException(CodeImageHostNames.Broken);

                return Term.A(CodeImageHostNames.Kept);
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }
        });
        await current.Completion.WaitAsync(CodeImageHostParameters.Deadline);
        if (failure is not null)
            throw failure;
        if (oldObservations != 1)
            throw new InvalidOperationException(CodeImageHostNames.Broken);

    }

    private static async Task VerifyCleanup(bool cancel)
    {
        await using var runtime = new ProcessRuntime();
        var module = new Parser("-module(code_image_cleanup). -export([run/0]). run()->code_image_callback:block(),kept.").ParseModule();
        module.Register(runtime.Modules);
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        runtime.Modules.Register(
            CodeImageHostNames.Callback,
            CodeImageHostNames.Block,
            0,
            async (context, _) =>
            {
                entered.SetResult(context);
                await release.Task.WaitAsync(context.Cancellation);

                throw new ErlangException(CodeImageHostNames.Failure);
            }
        );
        Exception? failure = null;
        bool expectedFailure = false;
        var process = runtime.Spawn(async context =>
        {
            try
            {
                return await runtime.Modules.Call(context, CodeImageHostNames.CleanupModule, CodeImageHostNames.Run);
            }
            catch (ErlangException exception) when (!cancel && exception.Reason.Equals(Term.A(CodeImageHostNames.Failure)))
            {
                expectedFailure = true;

                return Term.A(CodeImageHostNames.Kept);
            }
            catch (OperationCanceledException) when (cancel && context.Cancellation.IsCancellationRequested)
            {
                expectedFailure = true;

                throw;
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }
            finally
            {
                unwound.SetResult();
            }
        });
        ProcessContext context = await entered.Task.WaitAsync(CodeImageHostParameters.Deadline);
        runtime.Modules.DeleteModule(CodeImageHostNames.CleanupModule);
        if (!runtime.Modules.IsExecutingOldCode(context, CodeImageHostNames.CleanupModule))
            throw new InvalidOperationException(CodeImageHostNames.Broken);
        if (cancel)
            runtime.Exit(context, process.Pid, Term.A(ProcessExitSignals.Kill));
        else
            release.SetResult();
        await process.Completion.WaitAsync(CodeImageHostParameters.Deadline);
        await unwound.Task.WaitAsync(CodeImageHostParameters.Deadline);
        if (failure is not null)
            throw failure;
        if (!expectedFailure || runtime.Modules.IsExecutingOldCode(context, CodeImageHostNames.CleanupModule))
            throw new InvalidOperationException(CodeImageHostNames.Broken);
    }
}
