using Erlang;
using Erlang.Differential;

namespace Erlang.Tests;

public static class CommonSelectedHandlerDiagnostic
{
    public static async Task Run()
    {
        Term repeated = await Repeated();
        Term source = await SourceBridge();
        Console.WriteLine("RAW_SELECTED_HANDLER_ETF " + Convert.ToBase64String(
            ExternalTermFormat.Encode(Term.Tuple(repeated, source))));
    }

    private static async Task<Term> Repeated()
    {
        await using var runtime = new ProcessRuntime();
        int calls = 0;
        runtime.Modules.Register(
            ProcessErrorHandlerHostNames.EqualHandler,
            ProcessErrorHandlerHostNames.Undefined,
            3,
            async (context, arguments) =>
            {
                if (++calls == 1)
                    return await Missing(context);
                await Task.Yield();
                throw new ErlangException(Term.A(ProcessErrorHandlerHostNames.Fault));
            });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        return await actor.InvokeAsync(async context =>
        {
            await Change(context, ProcessErrorHandlerHostNames.EqualHandler);
            ErlangException error = await Failure(() => Missing(context));
            return Term.Tuple(Term.A("repeated_host"),Term.I(calls),error.Reason,error.StackTraceTerm);
        });
    }

    private static async Task<Term> SourceBridge()
    {
        using var compiled = GeneratedModuleCompiler.Compile(ProcessErrorHandlerHostNames.Source);
        await using var runtime = new ProcessRuntime();
        compiled.Definition.Register(runtime.Modules);
        string? liveFunction = null;
        ErlangFunction body = async (context, arguments) =>
        {
            await Task.Yield();
            liveFunction = context.CurrentFunction;
            throw new ErlangException(Term.A(ProcessErrorHandlerHostNames.Fault));
        };
        NifAdmissionHostBridge.Call(
            runtime.Modules,
            ProcessErrorHandlerHostNames.RegisterIntrinsic,
            ProcessErrorHandlerHostNames.Bridge,
            ProcessErrorHandlerHostNames.Held,
            3,
            body);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        return await actor.InvokeAsync(async context =>
        {
            await Change(context, compiled.Definition.Name);
            ErlangException error = await Failure(() => Missing(context));
            return Term.Tuple(Term.A("source_handler_host_bridge"),Term.A(liveFunction ?? "undefined"),error.Reason,error.StackTraceTerm);
        });
    }

    private static ValueTask<Term> Change(ProcessContext context,string handler) => context.Runtime.Modules.Call(
        context,"erlang","process_flag",Term.A("error_handler"),Term.A(handler));

    private static ValueTask<Term> Missing(ProcessContext context) => context.Runtime.Modules.Call(
        context,
        ProcessErrorHandlerHostNames.MissingModule,
        ProcessErrorHandlerHostNames.MissingFunction,
        Term.A(ProcessErrorHandlerHostNames.Payload));

    private static async Task<ErlangException> Failure(Func<ValueTask<Term>> action)
    {
        try { await action(); }
        catch (ErlangException error) { return error; }
        throw new InvalidOperationException("diagnostic expected Erlang exception");
    }
}
