using Erlang.Compiler;

namespace Erlang.Tests;

public static class FunctionReloadHostChecks
{
    public static async Task Run()
    {
        await Check(false);
        await Check(true);
    }

    private static async Task Check(bool changedCode)
    {
        await using var runtime = new ProcessRuntime();
        string source = "-module(function_reload_host). -export([make/0]). make()->{fun hidden/0,fun()->hidden() end}. hidden()->function_reload_callback:probe(),kept.";
        var first = new Parser(source).ParseModule();
        var second = new Parser(changedCode ? source.Replace("probe(),kept", "probe(),different", StringComparison.Ordinal) : source + " -note(second).").ParseModule();
        first.Register(runtime.Modules);
        int probes = 0;
        runtime.Modules.Register(
            FunctionReloadHostNames.Callback,
            FunctionReloadHostNames.Probe,
            0,
            async (context, _) =>
            {
                if (runtime.Modules.IsExecutingOldCode(context, FunctionReloadHostNames.Module) != changedCode)
                    throw new InvalidOperationException(FunctionReloadHostNames.Broken);
                await Task.Yield();
                if (runtime.Modules.IsExecutingOldCode(context, FunctionReloadHostNames.Module) != changedCode)
                    throw new InvalidOperationException(FunctionReloadHostNames.Broken);
                Interlocked.Increment(ref probes);

                return Term.A(FunctionReloadHostNames.Kept);
            }
        );
        Exception? failure = null;
        var process = runtime.Spawn(async context =>
        {
            try
            {
                var functions = (TupleTerm)await runtime.Modules.Call(context, FunctionReloadHostNames.Module, FunctionReloadHostNames.Make);
                runtime.Modules.DeleteModule(FunctionReloadHostNames.Module);
                second.Register(runtime.Modules);
                foreach (var item in functions.Items)
                {
                    Term value = await ((FunctionTerm)item).Invoke(context, []);
                    if (!value.Equals(Term.A(FunctionReloadHostNames.Kept)) || runtime.Modules.IsExecutingOldCode(context, FunctionReloadHostNames.Module))
                        throw new InvalidOperationException(FunctionReloadHostNames.Broken);
                }
                var original = (FunctionTerm)functions.Items[0];
                var custom = new FunctionTerm(
                    original.LocalIdentity!,
                    original.Arity,
                    original.Environment,
                    (_, _) => ValueTask.FromResult<Term>(Term.A(FunctionReloadHostNames.Custom))
                );
                if (!(await custom.Invoke(context, [])).Equals(Term.A(FunctionReloadHostNames.Custom)))
                    throw new InvalidOperationException(FunctionReloadHostNames.Broken);

                return Term.A(FunctionReloadHostNames.Kept);
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }
        });
        await process.Completion.WaitAsync(FunctionReloadHostParameters.Deadline);
        if (failure is not null)
            throw failure;
        if (probes != 2)
            throw new InvalidOperationException(FunctionReloadHostNames.Broken);
    }
}
