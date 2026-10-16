using System.Reflection;
using Erlang;
using Erlang.Differential;

namespace Erlang.Tests;

internal static class ProcessErrorHandlerHostChecks
{
    internal static async Task Run()
    {
        await RunState();
        await RunFrames();
        await RunNestedFrames();
        await RunSourceLease();
        await RunIsolation();
    }

    internal static async Task RunState()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Require((await Change(context, ProcessErrorHandlerHostNames.Handler)).Equals(Term.A(ProcessErrorHandlerHostNames.Default)));
            Require((await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ProcessErrorHandlerHostNames.ProcessInfo,
                context.Self,
                Term.A(ProcessErrorHandlerHostNames.Flag)
            )).Equals(
                Term.Tuple(Term.A(ProcessErrorHandlerHostNames.Flag), Term.A(ProcessErrorHandlerHostNames.Handler))));
            foreach (Term invalid in new Term[] { Nil.Value, Term.I(42), Term.Tuple(Term.A(ProcessErrorHandlerHostNames.Invalid)) })
            {
                ErlangException failure = await Failure(() => runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ProcessErrorHandlerHostNames.ProcessFlag,
                    Term.A(ProcessErrorHandlerHostNames.Flag),
                    invalid
                ));
                Require(failure.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
            }
            Require((await Change(context, ProcessErrorHandlerHostNames.AbsentHandler)).Equals(Term.A(ProcessErrorHandlerHostNames.Handler)));
            ErlangException missing = await Failure(() => Call(context));
            Require(missing.Reason.Equals(Term.A(ErlangErrorReasons.UndefinedFunction)));
            await Change(context, ProcessErrorHandlerHostNames.Default);

            return Nil.Value;
        });
        foreach (CompiledModuleCase contract in ProcessErrorHandlerCases.Modules.Take(2))
        {
            using var compiled = GeneratedModuleCompiler.Compile(contract.Source);
            CompiledModuleStreamResult result = await CompiledModuleExecution.RunCaptured(compiled);
            Require(result.Result.Equals(contract.Expected) && result.StandardOutputHex.Length == 0 && result.StandardErrorHex.Length == 0);
        }
    }

    internal static async Task RunFrames()
    {
        using var caller = GeneratedModuleCompiler.Compile(ProcessErrorHandlerHostNames.CallerSource);
        await using var runtime = new ProcessRuntime();
        caller.Definition.Register(runtime.Modules);
        bool explicitTrace = false;
        ErlangException explicitFailure = new(Term.A(ProcessErrorHandlerHostNames.Fault))
        {
            ExplicitStackTrace = true
        };
        runtime.Modules.Register(
            ProcessErrorHandlerHostNames.Handler,
            ProcessErrorHandlerHostNames.Undefined,
            3,
            (context, arguments) =>
        {
            CheckArguments(arguments);
            if (explicitTrace)
                throw explicitFailure;
            throw new ErlangException(Term.A(ProcessErrorHandlerHostNames.Fault));
        }
        );
        runtime.Modules.Register(
            ProcessErrorHandlerHostNames.OtherHandler,
            ProcessErrorHandlerHostNames.Undefined,
            3,
            async (context, arguments) =>
        {
            CheckArguments(arguments);
            await Task.Yield();
            if (explicitTrace)
                throw explicitFailure;
            throw new ErlangException(Term.A(ProcessErrorHandlerHostNames.Fault));
        }
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            foreach (string handler in new[] { ProcessErrorHandlerHostNames.Handler, ProcessErrorHandlerHostNames.OtherHandler })
            {
                await Change(context, handler);
                foreach (string route in new[] { ProcessErrorHandlerHostNames.NonTail, ProcessErrorHandlerHostNames.Tail, ProcessErrorHandlerHostNames.Apply })
                {
                    ErlangException error = await Failure(() => runtime.Modules.Call(context, caller.Definition.Name, route));
                    CheckFrame(error, handler);
                }
                ErlangException direct = await Failure(() => Call(context));
                CheckFrame(direct, handler);
            }
            explicitTrace = true;
            ErlangException explicitEmpty = await Failure(() => runtime.Modules.Call(context, caller.Definition.Name, ProcessErrorHandlerHostNames.NonTail));
            Require(ReferenceEquals(explicitEmpty, explicitFailure) && explicitEmpty.ExplicitStackTrace && explicitEmpty.StackTraceTerm is Nil);
            Term customTrace = Term.List(Term.Tuple(
                Term.A(ProcessErrorHandlerHostNames.TraceModule),
                Term.A(ProcessErrorHandlerHostNames.TraceFunction),
                Nil.Value,
                Nil.Value
            ));
            explicitFailure = new(Term.A(ProcessErrorHandlerHostNames.Fault))
            {
                ExplicitStackTrace = true,
                StackTraceTerm = customTrace
            };
            ErlangException explicitCustom = await Failure(() => runtime.Modules.Call(context, caller.Definition.Name, ProcessErrorHandlerHostNames.Apply));
            Require(ReferenceEquals(explicitCustom, explicitFailure) && ReferenceEquals(explicitCustom.StackTraceTerm, customTrace));

            return Nil.Value;
        });
    }

    internal static async Task RunSourceLease()
    {
        await SourceLease(false, false);
        await SourceLease(true, false);
        await SourceLease(false, true);
    }

    internal static async Task RunNestedFrames()
    {
        await using var runtime = new ProcessRuntime();
        runtime.Modules.Register(
            ProcessErrorHandlerHostNames.OuterHandler,
            ProcessErrorHandlerHostNames.Undefined,
            3,
            async (context, arguments) =>
        {
            CheckArguments(arguments);
            await Change(context, ProcessErrorHandlerHostNames.InnerHandler);
            Term value = await Call(context);

            return Term.Tuple(Term.A(ProcessErrorHandlerHostNames.Forwarded), value);
        }
        );
        runtime.Modules.Register(
            ProcessErrorHandlerHostNames.InnerHandler,
            ProcessErrorHandlerHostNames.Undefined,
            3,
            (context, arguments) =>
        {
            CheckArguments(arguments);
            throw new ErlangException(Term.A(ProcessErrorHandlerHostNames.Fault));
        }
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            await Change(context, ProcessErrorHandlerHostNames.OuterHandler);
            ErlangException error = await Failure(() => Call(context));
            CheckFrame(error, ProcessErrorHandlerHostNames.InnerHandler);
            Term[] frames = Cons.Items(error.StackTraceTerm).ToArray();
            Require(frames.Length == 2 && frames[1] is TupleTerm outer &&
                outer.Items[0].Equals(Term.A(ProcessErrorHandlerHostNames.OuterHandler)));

            return Nil.Value;
        });
        await RepeatedFrames();
        await IntermediateFrames();
    }

    private static async Task RepeatedFrames()
    {
        await using var runtime = new ProcessRuntime();
        int calls = 0;
        runtime.Modules.Register(
            ProcessErrorHandlerHostNames.EqualHandler,
            ProcessErrorHandlerHostNames.Undefined,
            3,
            async (context, arguments) =>
        {
            CheckArguments(arguments);
            if (++calls == 1)
            {
                Term value = await Call(context);

                return Term.Tuple(Term.A(ProcessErrorHandlerHostNames.Forwarded), value);
            }
            await Task.Yield();
            throw new ErlangException(Term.A(ProcessErrorHandlerHostNames.Fault));
        }
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            await Change(context, ProcessErrorHandlerHostNames.EqualHandler);
            ErlangException error = await Failure(() => Call(context));
            CheckFrame(error, ProcessErrorHandlerHostNames.EqualHandler);
            Term[] frames = Cons.Items(error.StackTraceTerm).ToArray();
            Require(calls == 2 && frames.Length == 2 && frames[0].Equals(frames[1]));

            return Nil.Value;
        });
    }

    private static async Task IntermediateFrames()
    {
        using var outer = GeneratedModuleCompiler.Compile(ProcessErrorHandlerHostNames.OuterSource);
        using var intermediate = GeneratedModuleCompiler.Compile(ProcessErrorHandlerHostNames.IntermediateSource);
        await using var runtime = new ProcessRuntime();
        outer.Definition.Register(runtime.Modules);
        intermediate.Definition.Register(runtime.Modules);
        runtime.Modules.Register(
            ProcessErrorHandlerHostNames.InnerHandler,
            ProcessErrorHandlerHostNames.Undefined,
            3,
            (context, arguments) =>
        {
            CheckArguments(arguments);
            throw new ErlangException(Term.A(ProcessErrorHandlerHostNames.Fault));
        }
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            await Change(context, outer.Definition.Name);
            ErlangException error = await Failure(() => Call(context));
            CheckFrame(error, ProcessErrorHandlerHostNames.InnerHandler);
            Term[] frames = Cons.Items(error.StackTraceTerm).ToArray();
            Require(frames.Length == 3 && frames[1] is TupleTerm relay && frames[2] is TupleTerm callback &&
                relay.Items[0].Equals(Term.A(ProcessErrorHandlerHostNames.Intermediate)) &&
                relay.Items[1].Equals(Term.A(ProcessErrorHandlerHostNames.Relay)) &&
                callback.Items[0].Equals(Term.A(outer.Definition.Name)) &&
                callback.Items[1].Equals(Term.A(ProcessErrorHandlerHostNames.Undefined)));

            return Nil.Value;
        });
    }

    private static async Task SourceLease(bool fail, bool kill)
    {
        using var source = GeneratedModuleCompiler.Compile(ProcessErrorHandlerHostNames.Source);
        await using var runtime = new ProcessRuntime();
        source.Definition.Register(runtime.Modules);
        ModuleCodeImage image = runtime.Modules.CurrentCodeImage(source.Definition.Name)!;
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ErlangFunction bridge = async (context, arguments) =>
        {
            try
            {
                CheckActive(
                    context,
                    source.Definition,
                    image,
                    arguments
                );
                await Task.Yield();
                CheckActive(
                    context,
                    source.Definition,
                    image,
                    arguments
                );
                entered.TrySetResult(context);
                await release.Task.WaitAsync(context.Cancellation);
                CheckActive(
                    context,
                    source.Definition,
                    image,
                    arguments
                );
                if (fail)
                    throw new ErlangException(Term.A(ProcessErrorHandlerHostNames.Fault));

                return Term.A(ProcessErrorHandlerHostNames.Payload);
            }
            catch (Exception error)
            {
                entered.TrySetException(error);
                throw;
            }
            finally
            {
                try
                {
                    CheckActive(
                        context,
                        source.Definition,
                        image,
                        arguments
                    );
                }
                finally
                {
                    unwound.TrySetResult();
                }
            }
        };
        NifAdmissionHostBridge.Call(
            runtime.Modules,
            ProcessErrorHandlerHostNames.RegisterIntrinsic,
            ProcessErrorHandlerHostNames.Bridge,
            ProcessErrorHandlerHostNames.Held,
            3,
            bridge
        );
        ProcessHandle process = runtime.Spawn(async context =>
        {
            try
            {
                await Change(context, source.Definition.Name);
                result.TrySetResult(await Call(context));
            }
            catch (Exception error)
            {
                result.TrySetException(error);
                throw;
            }

            return Nil.Value;
        });
        ProcessContext? actual = null;
        try
        {
            actual = await entered.Task.WaitAsync(ProcessErrorHandlerHostNames.Deadline);
            Require(References(actual, image) > 0);
            if (kill)
            {
                await NifAdmissionHostBridge.Context(
                    runtime,
                    context =>
                {
                    runtime.Exit(context, process.Pid, Term.A(ProcessErrorHandlerHostNames.Kill));

                    return Task.CompletedTask;
                }
                );
            }
            else
                release.TrySetResult();
            await unwound.Task.WaitAsync(ProcessErrorHandlerHostNames.Deadline);
            await process.Completion.WaitAsync(ProcessErrorHandlerHostNames.Deadline);
            if (kill)
            {
                Require((await process.Completion).Equals(Term.A(ProcessErrorHandlerHostNames.Killed)));
                try
                {
                    await result.Task;
                }
                catch (OperationCanceledException) { }
            }
            else if (fail)
                CheckFrame(await Failure(result.Task), source.Definition.Name);
            else
                Require((await result.Task).Equals(Term.Tuple(Term.A(ProcessErrorHandlerHostNames.Forwarded), Term.A(ProcessErrorHandlerHostNames.Payload))));
            Require(References(actual, image) == 0);
        }
        finally
        {
            release.TrySetResult();
            if (!process.Completion.IsCompleted)
                await NifAdmissionHostBridge.Context(
                    runtime,
                    context =>
                {
                    runtime.Exit(context, process.Pid, Term.A(ProcessErrorHandlerHostNames.Kill));

                    return Task.CompletedTask;
                }
                );
            await process.Completion.WaitAsync(ProcessErrorHandlerHostNames.Deadline);
        }
    }

    internal static async Task RunIsolation()
    {
        await using var runtime = new ProcessRuntime();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var arrived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int count = 0;
        foreach (string name in new[] { ProcessErrorHandlerHostNames.Handler, ProcessErrorHandlerHostNames.OtherHandler })
        {
            string actual = name;
            runtime.Modules.Register(
                actual,
                ProcessErrorHandlerHostNames.Undefined,
                3,
                async (context, arguments) =>
            {
                CheckArguments(arguments);
                if (Interlocked.Increment(ref count) == 2)
                    arrived.TrySetResult();
                await release.Task.WaitAsync(context.Cancellation);

                return Term.Tuple(Term.A(actual), context.Self);
            }
            );
        }
        await using var first = new RuntimeNodeFoundationHostActor(runtime);
        await using var second = new RuntimeNodeFoundationHostActor(runtime);
        Task<Term> a = first.InvokeAsync(async context =>
 {
     await Change(context, ProcessErrorHandlerHostNames.Handler);

     return await Call(context);
 });
        Task<Term> b = second.InvokeAsync(async context =>
 {
     await Change(context, ProcessErrorHandlerHostNames.OtherHandler);

     return await Call(context);
 });
        try
        {
            await arrived.Task.WaitAsync(ProcessErrorHandlerHostNames.Deadline);
            Require(!a.IsCompleted && !b.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
        }
        await Task.WhenAll(a, b).WaitAsync(ProcessErrorHandlerHostNames.Deadline);
        Require(a.Result.Equals(Term.Tuple(Term.A(ProcessErrorHandlerHostNames.Handler), first.Process.Pid)));
        Require(b.Result.Equals(Term.Tuple(Term.A(ProcessErrorHandlerHostNames.OtherHandler), second.Process.Pid)));
        await SharedFailures();
    }

    private static async Task SharedFailures()
    {
        await using var runtime = new ProcessRuntime();
        ErlangException shared = new(Term.A(ProcessErrorHandlerHostNames.Fault));
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var arrived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int count = 0;
        foreach (string handler in new[] { ProcessErrorHandlerHostNames.Handler, ProcessErrorHandlerHostNames.OtherHandler })
            runtime.Modules.Register(
                handler,
                ProcessErrorHandlerHostNames.Undefined,
                3,
                async (context, arguments) =>
            {
                CheckArguments(arguments);
                if (Interlocked.Increment(ref count) == 2)
                    arrived.TrySetResult();
                await release.Task.WaitAsync(context.Cancellation);
                throw shared;
            }
            );
        await using var first = new RuntimeNodeFoundationHostActor(runtime);
        await using var second = new RuntimeNodeFoundationHostActor(runtime);
        Task<ErlangException> a = first.InvokeAsync(async context =>
        {
            await Change(context, ProcessErrorHandlerHostNames.Handler);

            return await Failure(() => Call(context));
        });
        Task<ErlangException> b = second.InvokeAsync(async context =>
        {
            await Change(context, ProcessErrorHandlerHostNames.OtherHandler);

            return await Failure(() => Call(context));
        });
        try
        {
            await arrived.Task.WaitAsync(ProcessErrorHandlerHostNames.Deadline);
        }
        finally
        {
            release.TrySetResult();
        }
        await Task.WhenAll(a, b).WaitAsync(ProcessErrorHandlerHostNames.Deadline);
        CheckFrame(a.Result, ProcessErrorHandlerHostNames.Handler);
        CheckFrame(b.Result, ProcessErrorHandlerHostNames.OtherHandler);
        Require(!ReferenceEquals(a.Result, b.Result) && !ReferenceEquals(a.Result, shared) &&
            shared.StackTraceTerm is Nil && !shared.ExplicitStackTrace);
        ErlangException markedClone = a.Result.WithStackFrame(ProcessErrorHandlerHostNames.Bridge, ProcessErrorHandlerHostNames.Relay, []);
        CheckFrame(markedClone, ProcessErrorHandlerHostNames.Handler);
        Require(Cons.Items(markedClone.StackTraceTerm).Count() == 2);
        ErlangException fresh = new(shared.Reason);
        ErlangException unmarked = fresh.WithStackFrame(ProcessErrorHandlerHostNames.Handler, ProcessErrorHandlerHostNames.Undefined, [])
            .WithStackFrame(ProcessErrorHandlerHostNames.OtherHandler, ProcessErrorHandlerHostNames.Undefined, []);
        Require(((TupleTerm)((Cons)unmarked.StackTraceTerm).Head).Items[0].Equals(Term.A(ProcessErrorHandlerHostNames.OtherHandler)));
    }

    private static ValueTask<Term> Change(ProcessContext context, string module) => context.Runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        ProcessErrorHandlerHostNames.ProcessFlag,
        Term.A(ProcessErrorHandlerHostNames.Flag),
        Term.A(module)
    );

    private static ValueTask<Term> Call(ProcessContext context) => context.Runtime.Modules.Call(
        context,
        ProcessErrorHandlerHostNames.MissingModule,
        ProcessErrorHandlerHostNames.MissingFunction,
        Term.A(ProcessErrorHandlerHostNames.Payload)
    );

    private static async Task<ErlangException> Failure(Task<Term> operation)
    {
        try
        {
            await operation.WaitAsync(ProcessErrorHandlerHostNames.Deadline);
        }
        catch (ErlangException error)
        {
            return error;
        }
        throw new InvalidOperationException(ProcessErrorHandlerHostNames.Failure);
    }

    private static async Task<ErlangException> Failure(Func<ValueTask<Term>> operation)
    {
        try
        {
            await operation().AsTask().WaitAsync(ProcessErrorHandlerHostNames.Deadline);
        }
        catch (ErlangException error)
        {
            return error;
        }
        throw new InvalidOperationException(ProcessErrorHandlerHostNames.Failure);
    }

    private static void CheckArguments(IReadOnlyList<Term> arguments) => Require(arguments.Count == 3 &&
        arguments[0].Equals(Term.A(ProcessErrorHandlerHostNames.MissingModule)) &&
        arguments[1].Equals(Term.A(ProcessErrorHandlerHostNames.MissingFunction)) &&
        arguments[2].Equals(Term.List(Term.A(ProcessErrorHandlerHostNames.Payload))));

    private static void CheckFrame(ErlangException error, string handler)
    {
        Require(error.Reason.Equals(Term.A(ProcessErrorHandlerHostNames.Fault)));
        Require(error.StackTraceTerm is Cons { Head: TupleTerm { Items.Count: 4 } });
        var frame = (TupleTerm)((Cons)error.StackTraceTerm).Head;
        Require(frame.Items[0].Equals(Term.A(handler)) && frame.Items[1].Equals(Term.A(ProcessErrorHandlerHostNames.Undefined)));
        CheckArguments(Cons.Items(frame.Items[2]).ToArray());
    }

    private static void CheckActive(
        ProcessContext context,
        Erlang.Compiler.ModuleDefinition owner,
        ModuleCodeImage image,
        IReadOnlyList<Term> arguments
    )
    {
        CheckArguments(arguments);
        Require(ReferenceEquals(context.ExecutingSourceImage(owner), image));
        Require(References(context, image) > 0);
    }

    private static int References(ProcessContext context, ModuleCodeImage image) =>
        ((Dictionary<ModuleCodeImage, int>)typeof(ProcessContext).GetField(ProcessErrorHandlerHostNames.ActiveCode,
            ProcessErrorHandlerHostNames.Fields)!.GetValue(context)!).GetValueOrDefault(image);

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ProcessErrorHandlerHostNames.Failure);
    }
}
