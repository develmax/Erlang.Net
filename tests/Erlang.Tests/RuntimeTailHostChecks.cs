using Erlang.Compiler;

namespace Erlang.Tests;

public static class RuntimeTailHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        var module = new Parser("-module(runtime_tail_host). -export([run/0,non_tail/0,fail/0]). run()->runtime_tail_callback:probe(outer). non_tail()->kept=runtime_tail_callback:probe(non_tail),kept. fail()->runtime_tail_callback:fail().").ParseModule();
        module.Register(runtime.Modules);
        runtime.Modules.Register(
            RuntimeTailHostNames.CallbackModule,
            RuntimeTailHostNames.Probe,
            1,
            async (context, arguments) =>
            {
                string expected = ((Atom)arguments[0]).Name;
                if (context.CurrentFunction != expected)
                    throw new InvalidOperationException(RuntimeTailHostNames.Broken);
                await Task.Yield();
                if (context.CurrentFunction != expected)
                    throw new InvalidOperationException(RuntimeTailHostNames.Broken);

                return Term.A(RuntimeTailHostNames.Kept);
            }
        );
        runtime.Modules.Register(
            RuntimeTailHostNames.CallbackModule,
            RuntimeTailHostNames.Fail,
            0,
            (context, _) =>
            {
                if (context.CurrentFunction != RuntimeTailHostNames.Outer)
                    throw new InvalidOperationException(RuntimeTailHostNames.Broken);

                throw new ErlangException(RuntimeTailHostNames.Failure);
            }
        );
        Exception? failure = null;
        var process = runtime.Spawn(async context =>
        {
            try
            {
                using var outer = context.EnterFunction(RuntimeTailHostNames.Module, RuntimeTailHostNames.Outer, 0);
                foreach (string function in new[] { RuntimeTailHostNames.Run, RuntimeTailHostNames.NonTail })
                {
                    Term value = await Execution.InvokeAsync(
                        module,
                        function,
                        context,
                        []
                    );
                    if (!value.Equals(Term.A(RuntimeTailHostNames.Kept)) || context.CurrentFunction != RuntimeTailHostNames.Outer)
                        throw new InvalidOperationException(RuntimeTailHostNames.Broken);
                }
                try
                {
                    await Execution.InvokeAsync(
                        module,
                        RuntimeTailHostNames.Fail,
                        context,
                        []
                    );
                    throw new InvalidOperationException(RuntimeTailHostNames.Broken);
                }
                catch (ErlangException exception) when (exception.Reason.Equals(Term.A(RuntimeTailHostNames.Failure)))
                {
                    if (context.CurrentFunction != RuntimeTailHostNames.Outer)
                        throw new InvalidOperationException(RuntimeTailHostNames.Broken);
                }

                return Term.A(RuntimeTailHostNames.Kept);
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }
        });
        await process.Completion.WaitAsync(RuntimeTailHostParameters.Deadline);
        if (failure is not null)
            throw failure;
    }
}
