using Erlang.Compiler;

namespace Erlang.Tests;

public static class CodePurgeHostChecks
{
    public static async Task Run()
    {
        await Check(false, false);
        await Check(true, false);
        await Check(true, true);
        await SelfPurge();
    }

    private static async Task Check(bool reload, bool matching)
    {
        await using var runtime = new ProcessRuntime();
        const string source = "-module(code_purge_host). -export([make/0,run/0]). make()->{fun hidden/0,fun()->hidden() end}. hidden()->kept. run()->code_purge_callback:block(),kept.";
        var first = new Parser(source).ParseModule();
        first.Register(runtime.Modules);
        var oldImage = runtime.Modules.CurrentCodeImage(CodePurgeHostNames.Module)!;
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        runtime.Modules.Register(
            CodePurgeHostNames.Callback,
            CodePurgeHostNames.Block,
            0,
            async (target, _) =>
        {
            entered.SetResult(target);
            await release.Task.WaitAsync(target.Cancellation);
            await target.ReceiveAsync<Term>(message => message);

            return Term.A(CodePurgeHostNames.Kept);
        }
        );
        Exception? failure = null;
        var controller = runtime.Spawn(async context =>
        {
            try
            {
                var held = (TupleTerm)await runtime.Modules.Call(context, CodePurgeHostNames.Module, CodePurgeHostNames.Make);
                var target = runtime.Spawn(child => runtime.Modules.Call(child, CodePurgeHostNames.Module, CodePurgeHostNames.Run));
                var targetContext = await entered.Task.WaitAsync(CodePurgeHostParameters.Deadline);
                runtime.Modules.DeleteModule(CodePurgeHostNames.Module);
                if (reload)
                    new Parser(matching ? source + " -note(new)." : source.Replace("hidden()->kept", "hidden()->different", StringComparison.Ordinal)).ParseModule().Register(runtime.Modules);
                var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var purger = runtime.Spawn(async caller =>
                {
                    started.SetResult();

                    var result = await runtime.Modules.Call(
                        caller,
                        ErlangModuleNames.Module,
                        CodePurgeHostNames.Purge,
                        Term.A(CodePurgeHostNames.Module)
                    );
                    runtime.Send(context.Self, Term.A(CodePurgeHostNames.Purged));

                    return result;
                });
                await started.Task.WaitAsync(CodePurgeHostParameters.Deadline);
                var prepared = WaitForPreparation(runtime.Modules, oldImage);
                await prepared.WaitAsync(CodePurgeHostParameters.Deadline);
                var secondFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                runtime.Spawn(async caller =>
                {
                    try
                    {
                        await runtime.Modules.Call(
                            caller,
                            ErlangModuleNames.Module,
                            CodePurgeHostNames.Purge,
                            Term.A(CodePurgeHostNames.Module)
                        );
                        secondFinished.TrySetException(new InvalidOperationException(CodePurgeHostNames.Broken));
                    }
                    catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
                    {
                        secondFinished.TrySetResult();
                    }
                    catch (Exception exception)
                    {
                        secondFinished.TrySetException(exception);
                    }

                    return Nil.Value;
                });
                var function = (FunctionTerm)held.Items[0];
                var waiting = function.Invoke(context, []);
                if (!matching && waiting.IsCompleted)
                    throw new InvalidOperationException(CodePurgeHostNames.Broken);
                if (matching && !(await waiting).Equals(Term.A(CodePurgeHostNames.Kept)))
                    throw new InvalidOperationException(CodePurgeHostNames.Broken);
                if (runtime.Modules.IsExecutingOldCode(context, CodePurgeHostNames.Module))
                    throw new InvalidOperationException(CodePurgeHostNames.Broken);
                release.SetResult();
                await context.ReceiveAsync(message => message is Atom { Name: CodePurgeHostNames.Purged } atom ? atom : null);
                Term reason = await target.Completion.WaitAsync(CodePurgeHostParameters.Deadline);
                await purger.Completion.WaitAsync(CodePurgeHostParameters.Deadline);
                await secondFinished.Task.WaitAsync(CodePurgeHostParameters.Deadline);
                if (!reason.Equals(Term.A(ProcessExitReasons.Killed)) || runtime.Modules.HasOldCode(CodePurgeHostNames.Module) || runtime.Modules.IsExecutingOldCode(targetContext, CodePurgeHostNames.Module))
                    throw new InvalidOperationException(CodePurgeHostNames.Broken);
                if (!matching)
                    await Rejected(waiting, function, reload);
                foreach (var item in held.Items)
                {
                    var remaining = (FunctionTerm)item;
                    if (matching)
                    {
                        if (!(await remaining.Invoke(context, [])).Equals(Term.A(CodePurgeHostNames.Kept)))
                            throw new InvalidOperationException(CodePurgeHostNames.Broken);
                    }
                    else
                        await Rejected(remaining.Invoke(context, []), remaining, reload);
                }
                if (runtime.Modules.IsModuleLoaded(CodePurgeHostNames.Module) != reload)
                    throw new InvalidOperationException(CodePurgeHostNames.Broken);
                try
                {
                    await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        CodePurgeHostNames.Purge,
                        Term.A(CodePurgeHostNames.Module)
                    );
                    throw new InvalidOperationException(CodePurgeHostNames.Broken);
                }
                catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
                {
                }
                if (reload)
                {
                    runtime.Modules.DeleteModule(CodePurgeHostNames.Module);
                    await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        CodePurgeHostNames.Purge,
                        Term.A(CodePurgeHostNames.Module)
                    );
                }

                return Term.A(CodePurgeHostNames.Kept);
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }
            finally
            {
                release.TrySetResult();
            }
        });
        await controller.Completion.WaitAsync(CodePurgeHostParameters.Deadline);
        if (failure is not null)
            throw failure;
    }

    private static async Task WaitForPreparation(ModuleRegistry registry, ModuleCodeImage image)
    {
        while (registry.PendingPurge(image) is null)
            await Task.Yield();
    }

    private static async Task SelfPurge()
    {
        await using var runtime = new ProcessRuntime();
        var module = new Parser("-module(code_purge_host). -export([run/0]). run()->true=erlang:delete_module(code_purge_host),erlang:purge_module(code_purge_host),impossible.").ParseModule();
        module.Register(runtime.Modules);
        var image = runtime.Modules.CurrentCodeImage(CodePurgeHostNames.Module)!;
        var caller = runtime.Spawn(context => runtime.Modules.Call(context, CodePurgeHostNames.Module, CodePurgeHostNames.Run));
        var reason = await caller.Completion.WaitAsync(CodePurgeHostParameters.Deadline);
        if (!reason.Equals(Term.A(ProcessExitReasons.Killed)))
            throw new InvalidOperationException(CodePurgeHostNames.Broken);
        await WaitForRetirement(runtime.Modules).WaitAsync(CodePurgeHostParameters.Deadline);
        if (runtime.Modules.PendingPurge(image) is not null)
            throw new InvalidOperationException(CodePurgeHostNames.Broken);
    }

    private static async Task WaitForRetirement(ModuleRegistry registry)
    {
        while (registry.HasOldCode(CodePurgeHostNames.Module))
            await Task.Yield();
    }

    private static async Task Rejected(ValueTask<Term> invocation, FunctionTerm function, bool loaded)
    {
        try
        {
            await invocation;
            throw new InvalidOperationException(CodePurgeHostNames.Broken);
        }
        catch (ErlangException exception)
        {
            Term expected = loaded ? Term.Tuple(Term.A(ErlangErrorReasons.BadFunction), function) : Term.A(ErlangErrorReasons.UndefinedFunction);
            if (!exception.Reason.Equals(expected))
                throw new InvalidOperationException(CodePurgeHostNames.Broken);
        }
    }
}
