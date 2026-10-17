using Erlang;

namespace Erlang.Tests;

internal static class CommonErrorCaptureHostChecks
{
    internal static async Task RunIdentityRollbackAndAsyncSnapshot()
    {
        await using var runtime = new ProcessRuntime();
        await using var first = new RuntimeNodeFoundationHostActor(runtime);
        await using var second = new RuntimeNodeFoundationHostActor(runtime);
        var rollback = new ModuleRegistry();
        ErlangException shared = new(Term.A("host_capture_fault"));
        ErlangFunction body = (_, _) => throw shared;
        try
        {
            rollback.Apply(outer =>
            {
                outer.InstallPreloaded(inner => inner.Register("erlang", "hd", 1, body));
                throw new InvalidOperationException("intentional_rollback");
            });
            throw new Exception("rollback was not exercised");
        }
        catch (InvalidOperationException error) when (error.Message == "intentional_rollback")
        {
        }
        rollback.Register("erlang", "hd", 1, body);
        await first.InvokeAsync(async context =>
        {
            using var source = context.EnterFunction("host_capture_probe", "caller", 0);
            ErlangException observed = await Failure(() => rollback.Call(context, "erlang", "hd", Nil.Value));
            Require(ReferenceEquals(observed, shared) && !observed.AutomaticStackTraceCaptured && observed.StackTraceTerm is Nil);
            return Nil.Value;
        });

        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var asynchronous = new ModuleRegistry();
        asynchronous.InstallPreloaded(registry => registry.Register("erlang", "hd", 1, async (context, arguments) =>
        {
            using var temporary = context.EnterFunction("host_body_only", "temporary", 0);
            entered.TrySetResult();
            await release.Task.WaitAsync(context.Cancellation);
            throw shared;
        }));
        Term previous = await second.InvokeAsync(async context => await runtime.Modules.Call(context, "erlang", "system_flag", Term.A("backtrace_depth"), Term.I(8)));
        Task<ErlangException> pending = first.InvokeAsync(async context =>
        {
            using var source = context.EnterFunction("host_capture_probe", "caller", 0);
            return await Failure(() => asynchronous.Call(context, "erlang", "hd", Nil.Value));
        });
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await second.InvokeAsync(async context => await runtime.Modules.Call(context, "erlang", "system_flag", Term.A("backtrace_depth"), Term.I(1)));
            release.TrySetResult();
            ErlangException observed = await pending.WaitAsync(TimeSpan.FromSeconds(10));
            Term[] frames = Cons.Items(observed.StackTraceTerm).ToArray();
            Require(observed.AutomaticStackTraceCaptured && !observed.ExplicitStackTrace && !ReferenceEquals(observed, shared));
            Require(frames.Length == 1 && frames[0] is TupleTerm frame && frame.Items[0].Equals(Term.A("erlang"))
                && frame.Items[1].Equals(Term.A("hd")) && frame.Items[2].Equals(Term.List(Nil.Value)));
            Require(shared.StackTraceTerm is Nil && !shared.AutomaticStackTraceCaptured && !shared.ExplicitStackTrace);
        }
        finally
        {
            release.TrySetResult();
            await pending.WaitAsync(TimeSpan.FromSeconds(10));
            await second.InvokeAsync(async context => await runtime.Modules.Call(context, "erlang", "system_flag", Term.A("backtrace_depth"), previous));
        }

        // Explicit trace identity is a public host contract, not a synthetic native expected value.
        foreach (Term trace in new Term[] { Nil.Value, Term.List(Term.Tuple(Term.A("supplied"), Term.A("trace"), Term.I(0), Nil.Value)) })
        {
            ErlangException explicitError = new(Term.A("explicit_fault")) { ExplicitStackTrace = true, StackTraceTerm = trace };
            var explicitRegistry = new ModuleRegistry();
            explicitRegistry.InstallPreloaded(registry => registry.Register("erlang", "hd", 1, (_, _) => throw explicitError));
            await first.InvokeAsync(async context =>
            {
                ErlangException observed = await Failure(() => explicitRegistry.Call(context, "erlang", "hd", Nil.Value));
                Require(ReferenceEquals(observed, explicitError) && ReferenceEquals(observed.StackTraceTerm, trace));
                return Nil.Value;
            });
        }
    }

    internal static async Task RunUncapturedFallbackAndPriorHost()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var unrelated = new ModuleRegistry();
        ErlangException shared = new(Term.A("prior_host_fault"));
        unrelated.Register("erlang", "hd", 1, (_, _) => throw shared);
        unrelated.InstallPreloaded(_ => { });
        await actor.InvokeAsync(async context =>
        {
            ErlangException hostFailure = await Failure(() => unrelated.Call(context, "erlang", "hd", Nil.Value));
            Require(ReferenceEquals(hostFailure, shared) && !hostFailure.AutomaticStackTraceCaptured);
            Require(context.CurrentFunction is null);
            ErlangException bare = await Failure(() => runtime.Modules.Call(context, "erlang", "error", Term.A("bare_fault")));
            Require(bare.StackTraceTerm is Nil && !bare.ExplicitStackTrace && !bare.AutomaticStackTraceCaptured);
            var erpc = typeof(Erlang.Otp.OtpModules).Assembly.GetType("Erlang.Otp.Erpc", throwOnError: true)!;
            var fallback = erpc.GetMethod("Stack", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
            Term module = Term.A("actual_target"), function = Term.A("actual_function"), arguments = Term.List(Term.I(42));
            Term result = (Term)fallback.Invoke(null, [bare, module, function, arguments])!;
            Require(result.Equals(Term.List(Term.Tuple(module, function, arguments, Nil.Value))));
            return Nil.Value;
        });
    }

    private static async Task<ErlangException> Failure(Func<ValueTask<Term>> action)
    {
        try { await action(); }
        catch (ErlangException error) { return error; }
        throw new Exception("expected Erlang exception");
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new Exception("common exception capture host contract failed");
    }
}
