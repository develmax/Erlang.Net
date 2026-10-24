using Erlang.Compiler;

namespace Erlang.Tests;

public static class ProcessCodeHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        var source = new Parser("-module(process_code_heap_source). -export([make/0]). make()->fun()->kept end.").ParseModule();
        source.Register(runtime.Modules);
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        runtime.Modules.Register(
            ProcessCodeHostNames.Callback,
            ProcessCodeHostNames.Block,
            1,
            async (context, arguments) =>
            {
                context.Dictionary[Term.A(ProcessCodeHostNames.Held)] = arguments[0];
                entered.SetResult(context);
                await release.Task.WaitAsync(context.Cancellation);

                return Term.A(ProcessCodeHostNames.Kept);
            }
        );
        Exception? failure = null;
        ProcessHandle? child = null;
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var controller = runtime.Spawn(async context =>
        {
            try
            {
                Term held = await runtime.Modules.Call(context, ProcessCodeHostNames.Source, ProcessCodeHostNames.Make);
                child = runtime.Spawn(async target =>
                {
                    try
                    {
                        return await runtime.Modules.Call(
                            target,
                            ProcessCodeHostNames.Callback,
                            ProcessCodeHostNames.Block,
                            held
                        );
                    }
                    finally
                    {
                        unwound.SetResult();
                    }
                });
                ProcessContext targetContext = await entered.Task.WaitAsync(ProcessCodeHostParameters.Deadline);
                runtime.Modules.DeleteModule(ProcessCodeHostNames.Source);
                runtime.Modules.DeleteModule(ProcessCodeHostNames.Callback);
                if (runtime.Modules.IsExecutingOldCode(targetContext, ProcessCodeHostNames.Source) ||
                    !runtime.Modules.IsExecutingOldCode(targetContext, ProcessCodeHostNames.Callback))
                    throw new InvalidOperationException(ProcessCodeHostNames.Broken);
                Term options = Term.List(Term.Tuple(Term.A(ProcessCodeCheckNames.AsyncOption), Term.A(ProcessCodeHostNames.Request)));
                for (int index = 0; index < 2; index++)
                {
                    Term value = await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        ProcessCodeCheckNames.Function,
                        child.Pid,
                        Term.A(ProcessCodeHostNames.Callback),
                        options
                    );
                    if (!value.Equals(Term.A(ProcessCodeCheckNames.AsyncResult)))
                        throw new InvalidOperationException(ProcessCodeHostNames.Broken);
                }
                ValueTask<Term> pending = runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ProcessCodeCheckNames.Function,
                    child.Pid,
                    Term.A(ProcessCodeHostNames.Callback)
                );
                if (pending.IsCompleted)
                    throw new InvalidOperationException(ProcessCodeHostNames.Broken);
                started.SetResult();
                runtime.Exit(context, child.Pid, Term.A(ProcessExitSignals.Kill));
                Term result = await pending.AsTask().WaitAsync(ProcessCodeHostParameters.Deadline);
                if (!result.Equals(Term.A(ErlangBooleanAtoms.False)))
                    throw new InvalidOperationException(ProcessCodeHostNames.Broken);
                for (int index = 0; index < 2; index++)
                {
                    var reply = await context.ReceiveAsync(
                        message => message is TupleTerm { Items.Count: 3 } tuple &&
                        tuple.Items[0].Equals(Term.A(ProcessCodeCheckNames.ReplyTag)) &&
                        tuple.Items[1].Equals(Term.A(ProcessCodeHostNames.Request)) ? tuple : null,
                        ProcessCodeHostParameters.Deadline
                    );
                    if (reply is null || !reply.Items[2].Equals(Term.A(ErlangBooleanAtoms.False)))
                        throw new InvalidOperationException(ProcessCodeHostNames.Broken);
                }
                await child.Completion.WaitAsync(ProcessCodeHostParameters.Deadline);
                await unwound.Task.WaitAsync(ProcessCodeHostParameters.Deadline);
                if (runtime.Modules.IsExecutingOldCode(targetContext, ProcessCodeHostNames.Callback) || context.Mailbox.Count != 0)
                    throw new InvalidOperationException(ProcessCodeHostNames.Broken);

                return Term.A(ProcessCodeHostNames.Completed);
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }
        });
        await controller.Completion.WaitAsync(ProcessCodeHostParameters.Deadline);
        if (failure is not null)
            throw failure;
        if (!started.Task.IsCompletedSuccessfully)
            throw new InvalidOperationException(ProcessCodeHostNames.Broken);
    }
}
