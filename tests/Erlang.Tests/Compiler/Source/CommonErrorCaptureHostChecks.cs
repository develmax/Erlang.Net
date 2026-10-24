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
        ErlangException shared = new(Term.A(CommonErrorCaptureHostNames.Fault));
        ErlangFunction body = (_, _) => throw shared;
        try
        {
            rollback.Apply(outer =>
            {
                outer.InstallPreloaded(inner => inner.Register(
                    CommonErrorCaptureHostNames.Erlang,
                    CommonErrorCaptureHostNames.Head,
                    1,
                    body
                ));
                throw new InvalidOperationException(CommonErrorCaptureHostNames.Rollback);
            });
            throw new Exception(CommonErrorCaptureHostNames.RollbackMissing);
        }
        catch (InvalidOperationException error) when (error.Message == CommonErrorCaptureHostNames.Rollback)
        {
        }
        rollback.Register(
            CommonErrorCaptureHostNames.Erlang,
            CommonErrorCaptureHostNames.Head,
            1,
            body
        );
        await first.InvokeAsync(async context =>
        {
            using var source = context.EnterFunction(CommonErrorCaptureHostNames.CallerModule, CommonErrorCaptureHostNames.CallerFunction, 0);
            ErlangException observed = await Failure(() => rollback.Call(
                context,
                CommonErrorCaptureHostNames.Erlang,
                CommonErrorCaptureHostNames.Head,
                Nil.Value
            ));
            Require(ReferenceEquals(observed, shared) && !observed.AutomaticStackTraceCaptured && observed.StackTraceTerm is Nil);

            return Nil.Value;
        });

        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var asynchronous = new ModuleRegistry();
        asynchronous.InstallPreloaded(registry => registry.Register(
            CommonErrorCaptureHostNames.Erlang,
            CommonErrorCaptureHostNames.Head,
            1,
            async (context, arguments) =>
        {
            using var temporary = context.EnterFunction(CommonErrorCaptureHostNames.TemporaryModule, CommonErrorCaptureHostNames.TemporaryFunction, 0);
            entered.TrySetResult();
            await release.Task.WaitAsync(context.Cancellation);
            throw shared;
        }
        ));
        Term previous = await second.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            CommonErrorCaptureHostNames.Erlang,
            CommonErrorCaptureHostNames.SystemFlag,
            Term.A(CommonErrorCaptureHostNames.Flag),
            Term.I(CommonErrorCaptureHostParameters.DefaultDepth)
        ));
        Task<ErlangException> pending = first.InvokeAsync(async context =>
        {
            using var source = context.EnterFunction(CommonErrorCaptureHostNames.CallerModule, CommonErrorCaptureHostNames.CallerFunction, 0);

            return await Failure(() => asynchronous.Call(
                context,
                CommonErrorCaptureHostNames.Erlang,
                CommonErrorCaptureHostNames.Head,
                Nil.Value
            ));
        });
        try
        {
            await entered.Task.WaitAsync(CommonErrorCaptureHostParameters.Deadline);
            await second.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                CommonErrorCaptureHostNames.Erlang,
                CommonErrorCaptureHostNames.SystemFlag,
                Term.A(CommonErrorCaptureHostNames.Flag),
                Term.I(CommonErrorCaptureHostParameters.MinimumDepth)
            ));
            release.TrySetResult();
            ErlangException observed = await pending.WaitAsync(CommonErrorCaptureHostParameters.Deadline);
            Term[] frames = Cons.Items(observed.StackTraceTerm).ToArray();
            Require(observed.AutomaticStackTraceCaptured && !observed.ExplicitStackTrace && !ReferenceEquals(observed, shared));
            Require(frames.Length == 1 && frames[0] is TupleTerm frame && frame.Items[0].Equals(Term.A(CommonErrorCaptureHostNames.Erlang))
                && frame.Items[1].Equals(Term.A(CommonErrorCaptureHostNames.Head)) && frame.Items[2].Equals(Term.List(Nil.Value)));
            Require(shared.StackTraceTerm is Nil && !shared.AutomaticStackTraceCaptured && !shared.ExplicitStackTrace);
        }
        finally
        {
            release.TrySetResult();
            await pending.WaitAsync(CommonErrorCaptureHostParameters.Deadline);
            await second.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                CommonErrorCaptureHostNames.Erlang,
                CommonErrorCaptureHostNames.SystemFlag,
                Term.A(CommonErrorCaptureHostNames.Flag),
                previous
            ));
        }

        // Explicit trace identity is a public host contract, not a synthetic native expected value.
        foreach (Term trace in new Term[] { Nil.Value, Term.List(Term.Tuple(
            Term.A(CommonErrorCaptureHostNames.SuppliedModule),
            Term.A(CommonErrorCaptureHostNames.SuppliedFunction),
            Term.I(0),
            Nil.Value
        )) })
        {
            ErlangException explicitError = new(Term.A(CommonErrorCaptureHostNames.ExplicitFault)) { ExplicitStackTrace = true, StackTraceTerm = trace };
            var explicitRegistry = new ModuleRegistry();
            explicitRegistry.InstallPreloaded(registry => registry.Register(
                CommonErrorCaptureHostNames.Erlang,
                CommonErrorCaptureHostNames.Head,
                1,
                (_, _) => throw explicitError
            ));
            await first.InvokeAsync(async context =>
            {
                ErlangException observed = await Failure(() => explicitRegistry.Call(
                    context,
                    CommonErrorCaptureHostNames.Erlang,
                    CommonErrorCaptureHostNames.Head,
                    Nil.Value
                ));
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
        ErlangException shared = new(Term.A(CommonErrorCaptureHostNames.PriorHostFault));
        unrelated.Register(
            CommonErrorCaptureHostNames.Erlang,
            CommonErrorCaptureHostNames.Head,
            1,
            (_, _) => throw shared
        );
        unrelated.InstallPreloaded(_ => { });
        await actor.InvokeAsync(async context =>
        {
            ErlangException hostFailure = await Failure(() => unrelated.Call(
                context,
                CommonErrorCaptureHostNames.Erlang,
                CommonErrorCaptureHostNames.Head,
                Nil.Value
            ));
            Require(ReferenceEquals(hostFailure, shared) && !hostFailure.AutomaticStackTraceCaptured);
            Require(context.CurrentFunction is null);
            ErlangException bare = await Failure(() => runtime.Modules.Call(
                context,
                CommonErrorCaptureHostNames.Erlang,
                CommonErrorCaptureHostNames.Error,
                Term.A(CommonErrorCaptureHostNames.BareFault)
            ));
            Require(bare.StackTraceTerm is Nil && !bare.ExplicitStackTrace && !bare.AutomaticStackTraceCaptured);
            var erpc = typeof(Erlang.Otp.KernelModules).Assembly.GetType(CommonErrorCaptureHostNames.ErpcType, throwOnError: true)!;
            var fallback = erpc.GetMethod(CommonErrorCaptureHostNames.ErpcStack, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
            Term module = Term.A(CommonErrorCaptureHostNames.ActualTarget), function = Term.A(CommonErrorCaptureHostNames.ActualFunction), arguments = Term.List(Term.I(CommonErrorCaptureHostParameters.Payload));
            Term result = (Term)fallback.Invoke(null, [bare, module, function, arguments])!;
            Require(result.Equals(Term.List(Term.Tuple(
                module,
                function,
                arguments,
                Nil.Value
            ))));

            return Nil.Value;
        });
    }

    private static async Task<ErlangException> Failure(Func<ValueTask<Term>> action)
    {
        try
        {
            await action();
        }
        catch (ErlangException error)
        {
            return error;
        }
        throw new Exception(CommonErrorCaptureHostNames.ExpectedFailure);
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new Exception(CommonErrorCaptureHostNames.ContractFailure);
    }
}
