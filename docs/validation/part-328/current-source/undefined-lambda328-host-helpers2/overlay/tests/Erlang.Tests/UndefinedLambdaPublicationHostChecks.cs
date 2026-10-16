using Erlang;
using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.Tests;

internal static class UndefinedLambdaPublicationHostChecks
{
    internal static async Task Run()
    {
        await RunOriginalIdentity();
        await RunPublicationRace();
        await RunOldAndRetired();
        await RunRetryCancellation();
        await RunCompatibleFactory();
    }

    internal static async Task RunOriginalIdentity()
    {
        foreach (bool anonymous in new[] { false, true })
        {
            using var compiled = GeneratedModuleCompiler.Compile(UndefinedLambdaPublicationHostNames.Source);
            await using var decoding = new ProcessRuntime();
            await using var executing = new ProcessRuntime();
            compiled.Definition.Register(decoding.Modules);
            await using var first = new RuntimeNodeFoundationHostActor(decoding);
            await using var second = new RuntimeNodeFoundationHostActor(executing);
            FunctionTerm original = await first.InvokeAsync(context => Make(context, anonymous));
            FunctionTerm restored = await first.InvokeAsync(context => RoundTrip(context, original));
            int called = 0;
            executing.Modules.Register(UndefinedLambdaPublicationHostNames.Handler, UndefinedLambdaPublicationHostNames.UndefinedLambda, UndefinedLambdaPublicationHostNames.CallbackArity, (context, arguments) =>
            {
                Require(ReferenceEquals(context.Runtime, executing));
                Require(arguments[0].Equals(Term.A(UndefinedLambdaPublicationHostNames.Module)) && ReferenceEquals(arguments[1], restored));
                Require(arguments[2].Equals(Term.List(Term.A(UndefinedLambdaPublicationHostNames.Argument))));
                Interlocked.Increment(ref called);

                return ValueTask.FromResult<Term>(Term.A(UndefinedLambdaPublicationHostNames.Handled));
            });
            await second.InvokeAsync(async context =>
            {
                await Flag(context);
                Require((await restored.Invoke(context, [Term.A(UndefinedLambdaPublicationHostNames.Argument)])).Equals(Term.A(UndefinedLambdaPublicationHostNames.Handled)));
                Require(called == 1);
                executing.Modules.PublishModule(UndefinedLambdaPublicationHostNames.Module);
                ErlangException badfun = await Failure(() => restored.Invoke(context, [Term.A(UndefinedLambdaPublicationHostNames.Argument)]));
                Require(badfun.Reason is TupleTerm tuple && tuple.Items[0].Equals(Term.A(ErlangErrorReasons.BadFunction)) && ReferenceEquals(tuple.Items[1], restored));
                ErlangException badarity = await Failure(() => restored.Invoke(context, []));
                Require(badarity.Reason is TupleTerm arity && arity.Items[0].Equals(Term.A(ErlangErrorReasons.BadArity)) && arity.Items[1] is TupleTerm payload && ReferenceEquals(payload.Items[0], restored));
                Require(called == 1);

                return Nil.Value;
            });
        }
    }

    internal static async Task RunPublicationRace()
    {
        await Race(false, false);
        await Race(true, false);
        await Race(false, true);
        await Race(false, false, false);
    }

    private static async Task Race(bool anonymous, bool interpreted, bool relevant = true)
    {
        using var compiled = GeneratedModuleCompiler.Compile(UndefinedLambdaPublicationHostNames.Source);
        await using var runtime = new ProcessRuntime();
        compiled.Definition.Register(runtime.Modules);
        int bodies = 0;
        runtime.Modules.Register(UndefinedLambdaPublicationHostNames.CallbackModule, UndefinedLambdaPublicationHostNames.Body, 0, (context, _) =>
        {
            Require(!Monitor.IsEntered(UndefinedLambdaPublicationHostReflection.Gate(runtime.Functions)) && !Monitor.IsEntered(UndefinedLambdaPublicationHostReflection.Gate(runtime.Modules)));
            Interlocked.Increment(ref bodies);

            return ValueTask.FromResult<Term>(Nil.Value);
        });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var publisher = new RuntimeNodeFoundationHostActor(runtime);
        FunctionTerm genuine = await actor.InvokeAsync(async context => interpreted
            ? (FunctionTerm)await Execution.EvaluateAsync(new Parser(UndefinedLambdaPublicationHostNames.Interpreted).ParseExpression(), context)
            : await Make(context, anonymous));
        if (interpreted)
        {
            Require((await actor.InvokeAsync(async context => await genuine.Invoke(context, [Term.A(UndefinedLambdaPublicationHostNames.Argument)]))).Equals(Term.A(UndefinedLambdaPublicationHostNames.Argument)));
            Require(bodies == 1);
            bodies = 0;
        }
        Delegate factory = UndefinedLambdaPublicationHostReflection.Factory(runtime, genuine);
        using var decoding = new UndefinedLambdaPublicationHostBarrier(factory, false, () => CheckFactoryGates(runtime));
        UndefinedLambdaPublicationHostReflection.Install(runtime, genuine, decoding);
        FunctionTerm restored = await actor.InvokeAsync(context => RoundTrip(context, genuine));
        Require(decoding.Constructions == 0);
        using var old = new UndefinedLambdaPublicationHostBarrier(factory, true, () => CheckFactoryGates(runtime));
        using var replacement = new UndefinedLambdaPublicationHostBarrier(factory, false, () => CheckFactoryGates(runtime));
        UndefinedLambdaPublicationHostReflection.Install(runtime, genuine, old);
        Task<Term>? invocation = null;
        try
        {
            invocation = actor.InvokeAsync(async context => await restored.Invoke(context, [Term.A(UndefinedLambdaPublicationHostNames.Argument)]));
            await old.Entered.WaitAsync(UndefinedLambdaPublicationHostNames.Deadline);
            Require(bodies == 0 && !invocation.IsCompleted);
            FunctionTerm changed = relevant ? genuine : await publisher.InvokeAsync(async context =>
                (FunctionTerm)await runtime.Modules.Call(context, UndefinedLambdaPublicationHostNames.Module, UndefinedLambdaPublicationHostNames.Other));
            UndefinedLambdaPublicationHostReflection.Install(runtime, changed, replacement);
            old.Release();
            Term result = await invocation.WaitAsync(UndefinedLambdaPublicationHostNames.Deadline);
            Term expected = anonymous ? Term.Tuple(Term.A(UndefinedLambdaPublicationHostNames.Environment), Term.A(UndefinedLambdaPublicationHostNames.Argument)) : Term.A(UndefinedLambdaPublicationHostNames.Argument);
            Require(result.Equals(expected) && old.Constructions == 1 && replacement.Constructions == (relevant ? 1 : 0) && bodies == 1);
        }
        finally
        {
            old.Release();
            if (invocation is not null)
                await invocation.WaitAsync(UndefinedLambdaPublicationHostNames.Deadline);
        }
    }

    internal static async Task RunRetryCancellation()
    {
        using var compiled = GeneratedModuleCompiler.Compile(UndefinedLambdaPublicationHostNames.Source);
        await using var runtime = new ProcessRuntime();
        compiled.Definition.Register(runtime.Modules);
        int bodies = 0;
        runtime.Modules.Register(UndefinedLambdaPublicationHostNames.CallbackModule, UndefinedLambdaPublicationHostNames.Body, 0, (_, _) =>
        {
            Interlocked.Increment(ref bodies);

            return ValueTask.FromResult<Term>(Nil.Value);
        });
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        FunctionTerm genuine = await controller.InvokeAsync(context => Make(context, false));
        FunctionTerm restored = await controller.InvokeAsync(context => RoundTrip(context, genuine));
        Delegate factory = UndefinedLambdaPublicationHostReflection.Factory(runtime, genuine);
        var progress = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        UndefinedLambdaPublicationHostBarrier? retry = null;
        using var mutation = new UndefinedLambdaPublicationHostBarrier(factory, false, () =>
        {
            CheckFactoryGates(runtime);
            UndefinedLambdaPublicationHostReflection.Install(runtime, genuine, retry!);
            if (retry!.Constructions >= UndefinedLambdaPublicationHostNames.RetryObservationCount)
                progress.TrySetResult();
        });
        retry = mutation;
        UndefinedLambdaPublicationHostReflection.Install(runtime, genuine, mutation);
        ProcessHandle process = runtime.Spawn(async context => await restored.Invoke(context, [Term.A(UndefinedLambdaPublicationHostNames.Argument)]));
        try
        {
            await progress.Task.WaitAsync(UndefinedLambdaPublicationHostNames.Deadline);
            Require(bodies == 0 && !process.Completion.IsCompleted);
            await controller.Invoke(context =>
            {
                runtime.Exit(context, process.Pid, Term.A(ProcessExitSignals.Kill));

                return Nil.Value;
            });
            Require((await process.Completion.WaitAsync(UndefinedLambdaPublicationHostNames.Deadline)).Equals(Term.A(ProcessExitReasons.Killed)) && bodies == 0);
        }
        finally
        {
            if (!process.Completion.IsCompleted)
                await controller.Invoke(context =>
                {
                    runtime.Exit(context, process.Pid, Term.A(ProcessExitSignals.Kill));

                    return Nil.Value;
                });
            await process.Completion.WaitAsync(UndefinedLambdaPublicationHostNames.Deadline);
        }
    }

    internal static async Task RunOldAndRetired()
    {
        foreach (bool anonymous in new[] { false, true })
        {
            using var compiled = GeneratedModuleCompiler.Compile(UndefinedLambdaPublicationHostNames.Source);
            await using var runtime = new ProcessRuntime();
            compiled.Definition.Register(runtime.Modules);
            int bodies = 0;
            int callbacks = 0;
            runtime.Modules.Register(UndefinedLambdaPublicationHostNames.CallbackModule, UndefinedLambdaPublicationHostNames.Body, 0, (_, _) =>
            {
                Interlocked.Increment(ref bodies);

                return ValueTask.FromResult<Term>(Nil.Value);
            });
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            FunctionTerm genuine = await actor.InvokeAsync(context => Make(context, anonymous));
            FunctionTerm restored = await actor.InvokeAsync(context => RoundTrip(context, genuine));
            runtime.Modules.Register(UndefinedLambdaPublicationHostNames.Handler, UndefinedLambdaPublicationHostNames.UndefinedLambda, UndefinedLambdaPublicationHostNames.CallbackArity, (_, arguments) =>
            {
                Require(ReferenceEquals(arguments[1], restored));
                Interlocked.Increment(ref callbacks);

                return ValueTask.FromResult<Term>(Term.A(UndefinedLambdaPublicationHostNames.Handled));
            });
            Require(runtime.Modules.DeleteModule(UndefinedLambdaPublicationHostNames.Module).Equals(Term.A(ErlangBooleanAtoms.True)));
            await actor.InvokeAsync(async context =>
            {
                await Flag(context);
                _ = await restored.Invoke(context, [Term.A(UndefinedLambdaPublicationHostNames.Argument)]);
                Require(bodies == 1 && callbacks == 0);
                _ = await runtime.Modules.Call(context, ErlangModuleNames.Module, UndefinedLambdaPublicationHostNames.Purge, Term.A(UndefinedLambdaPublicationHostNames.Module));
                Require((await restored.Invoke(context, [Term.A(UndefinedLambdaPublicationHostNames.Argument)])).Equals(Term.A(UndefinedLambdaPublicationHostNames.Handled)));
                Require(bodies == 1 && callbacks == 1);

                return Nil.Value;
            });
        }
    }

    internal static async Task RunCompatibleFactory()
    {
        foreach (bool anonymous in new[] { false, true })
            foreach (bool retryInsideHandler in new[] { false, true })
                await CompatibleFactory(anonymous, retryInsideHandler);
    }

    private static async Task CompatibleFactory(bool anonymous, bool retryInsideHandler)
    {
        using var compiled = GeneratedModuleCompiler.Compile(UndefinedLambdaPublicationHostNames.Source);
        await using var decoding = new ProcessRuntime();
        await using var executing = new ProcessRuntime();
        compiled.Definition.Register(decoding.Modules);
        int decodingBodies = 0;
        int executingBodies = 0;
        int callbacks = 0;
        decoding.Modules.Register(UndefinedLambdaPublicationHostNames.CallbackModule, UndefinedLambdaPublicationHostNames.Body, 0, (_, _) =>
        {
            Interlocked.Increment(ref decodingBodies);

            return ValueTask.FromResult<Term>(Nil.Value);
        });
        executing.Modules.Register(UndefinedLambdaPublicationHostNames.CallbackModule, UndefinedLambdaPublicationHostNames.Body, 0, (context, _) =>
        {
            Require(ReferenceEquals(context.Runtime, executing));
            Require(ReferenceEquals(context.ExecutingSourceImage(compiled.Definition), executing.Modules.CurrentCodeImage(UndefinedLambdaPublicationHostNames.Module)));
            Interlocked.Increment(ref executingBodies);

            return ValueTask.FromResult<Term>(Nil.Value);
        });
        await using var first = new RuntimeNodeFoundationHostActor(decoding);
        await using var second = new RuntimeNodeFoundationHostActor(executing);
        FunctionTerm original = await first.InvokeAsync(context => Make(context, anonymous));
        FunctionTerm restored = await first.InvokeAsync(context => RoundTrip(context, original));
        executing.Modules.Register(UndefinedLambdaPublicationHostNames.Handler, UndefinedLambdaPublicationHostNames.UndefinedLambda, UndefinedLambdaPublicationHostNames.CallbackArity, async (context, arguments) =>
        {
            CheckFactoryGates(executing);
            Require(retryInsideHandler && ReferenceEquals(arguments[1], restored));
            Require(arguments[0].Equals(Term.A(UndefinedLambdaPublicationHostNames.Module)) && arguments[2].Equals(Term.List(Term.A(UndefinedLambdaPublicationHostNames.Argument))));
            Require(Interlocked.Increment(ref callbacks) == 1);
            compiled.Definition.Register(executing.Modules);
            _ = await Make(context, anonymous);

            return await ((FunctionTerm)arguments[1]).Invoke(context, [Term.A(UndefinedLambdaPublicationHostNames.Argument)]);
        });
        if (!retryInsideHandler)
        {
            compiled.Definition.Register(executing.Modules);
            _ = await second.InvokeAsync(context => Make(context, anonymous));
        }
        await second.InvokeAsync(async context =>
        {
            await Flag(context);
            Term value = await restored.Invoke(context, [Term.A(UndefinedLambdaPublicationHostNames.Argument)]);
            Term expected = anonymous ? Term.Tuple(Term.A(UndefinedLambdaPublicationHostNames.Environment), Term.A(UndefinedLambdaPublicationHostNames.Argument)) : Term.A(UndefinedLambdaPublicationHostNames.Argument);
            Require(value.Equals(expected) && executingBodies == 1 && decodingBodies == 0 && callbacks == (retryInsideHandler ? 1 : 0));

            return Nil.Value;
        });
    }

    private static void CheckFactoryGates(ProcessRuntime runtime)
        => Require(!Monitor.IsEntered(UndefinedLambdaPublicationHostReflection.Gate(runtime.Functions)) && !Monitor.IsEntered(UndefinedLambdaPublicationHostReflection.Gate(runtime.Modules)));

    private static async Task<FunctionTerm> Make(ProcessContext context, bool anonymous)
        => (FunctionTerm)await context.Runtime.Modules.Call(context, UndefinedLambdaPublicationHostNames.Module, anonymous ? UndefinedLambdaPublicationHostNames.Anonymous : UndefinedLambdaPublicationHostNames.Named, anonymous ? [Term.A(UndefinedLambdaPublicationHostNames.Environment)] : []);

    private static async Task<FunctionTerm> RoundTrip(ProcessContext context, FunctionTerm value)
    {
        Term binary = await context.Runtime.Modules.Call(context, ErlangModuleNames.Module, UndefinedLambdaPublicationHostNames.ToBinary, value);

        return (FunctionTerm)await context.Runtime.Modules.Call(context, ErlangModuleNames.Module, UndefinedLambdaPublicationHostNames.FromBinary, binary);
    }

    private static ValueTask<Term> Flag(ProcessContext context)
        => context.Runtime.Modules.Call(context, ErlangModuleNames.Module, UndefinedLambdaPublicationHostNames.ProcessFlag, Term.A(UndefinedLambdaPublicationHostNames.Flag), Term.A(UndefinedLambdaPublicationHostNames.Handler));

    private static async Task<ErlangException> Failure(Func<ValueTask<Term>> operation)
    {
        try
        {
            _ = await operation();
        }
        catch (ErlangException failure)
        {
            return failure;
        }
        throw new InvalidOperationException(UndefinedLambdaPublicationHostNames.Broken);
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(UndefinedLambdaPublicationHostNames.Broken);
    }
}
