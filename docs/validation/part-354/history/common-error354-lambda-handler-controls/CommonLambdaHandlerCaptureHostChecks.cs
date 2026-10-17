using Erlang;
using Erlang.Differential;

namespace Erlang.Tests;

public static class CommonLambdaHandlerCaptureHostChecks
{
    public static async Task Run()
    {
        foreach (bool anonymous in new[] { false, true })
        foreach (bool sourceHandler in new[] { false, true })
        {
            using var target = GeneratedModuleCompiler.Compile(UndefinedLambdaLifecycleHostNames.TargetSource);
            using var handler = GeneratedModuleCompiler.Compile(UndefinedLambdaLifecycleHostNames.HandlerSource);
            await using var runtime = new ProcessRuntime();
            target.Definition.Register(runtime.Modules);
            if (sourceHandler)
                handler.Definition.Register(runtime.Modules);
            FunctionTerm? restored = null;
            int calls = 0;
            ErlangFunction bridge = async (context, arguments) =>
            {
                Require(arguments.Count == 3 && arguments[0].Equals(Term.A(target.Definition.Name))
                    && ReferenceEquals(arguments[1], restored)
                    && arguments[2].Equals(Term.List(Term.A(UndefinedLambdaLifecycleHostNames.Argument))));
                int call = ++calls;
                if (!sourceHandler && call == 1)
                    return await restored!.Invoke(context, [Term.A(UndefinedLambdaLifecycleHostNames.Argument)]);
                await Task.Yield();
                throw new ErlangException(Term.A(UndefinedLambdaLifecycleHostNames.Fault));
            };
            if (sourceHandler)
                NifAdmissionHostBridge.Call(
                    runtime.Modules,
                    UndefinedLambdaLifecycleHostNames.RegisterIntrinsic,
                    UndefinedLambdaLifecycleHostNames.Bridge,
                    UndefinedLambdaLifecycleHostNames.Held,
                    3,
                    bridge
                );
            else
                runtime.Modules.Register(
                    UndefinedLambdaLifecycleHostNames.Handler,
                    UndefinedLambdaLifecycleHostNames.Undefined,
                    3,
                    bridge
                );
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await actor.InvokeAsync(async context =>
            {
                Term genuine = await runtime.Modules.Call(
                    context,
                    target.Definition.Name,
                    anonymous ? UndefinedLambdaLifecycleHostNames.Anonymous : UndefinedLambdaLifecycleHostNames.Named,
                    anonymous ? [Term.A(UndefinedLambdaLifecycleHostNames.Environment)] : []
                );
                Term binary = await runtime.Modules.Call(context, "erlang", "term_to_binary", genuine);
                restored = (FunctionTerm)await runtime.Modules.Call(context, "erlang", "binary_to_term", binary);
                Require(runtime.Modules.DeleteModule(target.Definition.Name).Equals(Term.A("true")));
                Require((await runtime.Modules.Call(context, "erlang", "purge_module", Term.A(target.Definition.Name))).Equals(Term.A("true")));
                await runtime.Modules.Call(context, "erlang", "process_flag", Term.A("error_handler"), Term.A(UndefinedLambdaLifecycleHostNames.Handler));
                ErlangException? observed = null;
                try
                {
                    await restored.Invoke(context, [Term.A(UndefinedLambdaLifecycleHostNames.Argument)]);
                }
                catch (ErlangException error)
                {
                    observed = error;
                }
                Require(observed is not null && observed.Reason.Equals(Term.A(UndefinedLambdaLifecycleHostNames.Fault)));
                Term[] frames = Cons.Items(observed!.StackTraceTerm).ToArray();
                Term expected = Term.Tuple(
                    Term.A(UndefinedLambdaLifecycleHostNames.Handler),
                    Term.A(UndefinedLambdaLifecycleHostNames.Undefined),
                    Term.List(Term.A(target.Definition.Name), restored, Term.List(Term.A(UndefinedLambdaLifecycleHostNames.Argument))),
                    Nil.Value
                );
                Require(frames.Length == (sourceHandler ? 1 : 2) && frames.All(frame => frame.Equals(expected)));
                Require(calls == (sourceHandler ? 1 : 2));
                return Nil.Value;
            });
        }
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException("Selected undefined_lambda provenance/entry arguments contract failed");
    }
}
