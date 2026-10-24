using Erlang.Compiler;

namespace Erlang.Tests;

public static class SourceTailHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        ModuleRegistry registry = runtime.Modules;
        var first = new Parser("-module(source_tail_host). -export([run/0,forward/0]). run()->first. forward()->source_tail_host:run().").ParseModule();
        var second = new Parser("-module(source_tail_host). -export([run/0]). run()->second.").ParseModule();
        var missing = new Parser("-module(source_tail_missing). -export([run/0]). run()->kept.").ParseModule();
        first.Register(registry);
        if (!ReferenceEquals(first, registry.SourceOwner(SourceTailHostNames.Module, SourceTailHostNames.Run, 0)))
            throw new InvalidOperationException(SourceTailHostNames.Broken);
        try
        {
            registry.Apply(target =>
            {
                target.DeleteModule(SourceTailHostNames.Module);
                second.Register(target);
                missing.Register(target);
                throw new InvalidOperationException(SourceTailHostNames.Broken);
            });
        }
        catch (InvalidOperationException exception) when (exception.Message == SourceTailHostNames.Broken)
        {
        }
        if (!ReferenceEquals(first, registry.SourceOwner(SourceTailHostNames.Module, SourceTailHostNames.Run, 0)) ||
            registry.SourceOwner(SourceTailHostNames.Missing, SourceTailHostNames.Run, 0) is not null || registry.HasOldCode(SourceTailHostNames.Module))
            throw new InvalidOperationException(SourceTailHostNames.Broken);
        registry.DeleteModule(SourceTailHostNames.Module);
        if (registry.SourceOwner(SourceTailHostNames.Module, SourceTailHostNames.Run, 0) is not null)
            throw new InvalidOperationException(SourceTailHostNames.Broken);
        second.Register(registry);
        var caller = new Parser("-module(source_tail_callback). -export([run/1]). run(F)->F().").ParseModule();
        caller.Register(registry);
        var identity = new ExternalFunctionIdentity(Term.A(SourceTailHostNames.Module), Term.A(SourceTailHostNames.Run), 0);
        runtime.Functions.External(runtime, identity);
        var custom = new FunctionTerm(identity, (_, _) => ValueTask.FromResult<Term>(Term.A(SourceTailHostNames.Custom)));
        Exception? failure = null;
        var process = runtime.Spawn(async context =>
        {
            try
            {
                Term value = await Execution.InvokeAsync(
                    first,
                    SourceTailHostNames.Forward,
                    context,
                    []
                );
                if (!value.Equals(Term.A(SourceTailHostNames.Second)))
                    throw new InvalidOperationException(SourceTailHostNames.Broken);
                Term callback = await Execution.InvokeAsync(
                    caller,
                    SourceTailHostNames.Run,
                    context,
                    [custom]
                );
                if (!callback.Equals(Term.A(SourceTailHostNames.Custom)))
                    throw new InvalidOperationException(SourceTailHostNames.Broken);

                return value;
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }
        });
        await process.Completion.WaitAsync(SourceTailHostParameters.Deadline);
        if (failure is not null)
            throw failure;
    }
}
