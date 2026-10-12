using Erlang.Compiler;

namespace Erlang.Tests;

public static class FunctionResolutionHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        Exception? failure = null;
        var process = runtime.Spawn(async context =>
        {
            try
            {
                foreach (string source in new[] { "loaded()", "processes_iterator()", "processes_next({0,[]})" })
                {
                    bool rejected = false;
                    try
                    {
                        await Execution.EvaluateAsync(new Parser(source).ParseExpression(), context);
                    }
                    catch (ErlangException exception) when (exception.Reason is Atom { Name: ErlangErrorReasons.UndefinedFunction })
                    {
                        rejected = true;
                    }
                    if (!rejected)
                        throw new InvalidOperationException(FunctionResolutionHostDiagnostics.Implicit);
                }
                var qualified = await Execution.EvaluateAsync(new Parser("erlang:loaded()").ParseExpression(), context);
                var dynamic = await Execution.EvaluateAsync(new Parser("apply(erlang,loaded,[])").ParseExpression(), context);
                var imported = await Execution.EvaluateAsync(new Parser("module_loaded(erlang)").ParseExpression(), context);
                if (qualified is not Cons || !qualified.Equals(dynamic) || imported is not Atom { Name: ErlangBooleanAtoms.True })
                    throw new InvalidOperationException(FunctionResolutionHostDiagnostics.Qualified);
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }

            return Nil.Value;
        });
        await process.Completion.WaitAsync(FunctionResolutionHostParameters.Deadline);
        if (failure is not null)
            throw failure;
    }
}
