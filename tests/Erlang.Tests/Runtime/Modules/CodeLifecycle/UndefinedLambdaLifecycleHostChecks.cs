using Erlang;
using Erlang.Differential;

namespace Erlang.Tests;

internal static class UndefinedLambdaLifecycleHostChecks
{
    internal static async Task Run()
    {
        await RunPendingPurge();
        await RunSelectedSourceLease();
        await RunPublicAdmission();
    }

    internal static async Task RunPendingPurge()
    {
        foreach (bool anonymous in new[] { false, true })
        {
            await Pending(
                anonymous,
                false,
                false,
                false
            );
            await Pending(
                anonymous,
                true,
                false,
                false
            );
            await Pending(
                anonymous,
                true,
                true,
                false
            );
            await Pending(
                anonymous,
                true,
                false,
                true
            );
        }
    }

    internal static async Task RunSelectedSourceLease()
    {
        foreach (bool anonymous in new[] { false, true })
        {
            await SelectedLease(anonymous, false, false);
            await SelectedLease(anonymous, true, false);
            await SelectedLease(anonymous, false, true);
        }
    }

    internal static async Task RunPublicAdmission()
    {
        foreach (bool anonymous in new[] { false, true })
        {
            using var source = GeneratedModuleCompiler.Compile(UndefinedLambdaLifecycleHostNames.TargetSource);
            await using var runtime = new ProcessRuntime();
            source.Definition.Register(runtime.Modules);
            ModuleCodeImage image = runtime.Modules.CurrentCodeImage(source.Definition.Name)!;
            FunctionTerm restored = await Restore(runtime, anonymous);
            BitString fingerprint = restored.LocalIdentity!.Unique;
            int callbacks = 0;
            runtime.Modules.Register(
                UndefinedLambdaLifecycleHostNames.Handler,
                UndefinedLambdaLifecycleHostNames.Undefined,
                UndefinedLambdaLifecycleHostNames.CallbackArity,
                (_, arguments) =>
                {
                    CheckArguments(arguments, restored);
                    Interlocked.Increment(ref callbacks);

                    return ValueTask.FromResult<Term>(Term.A(UndefinedLambdaLifecycleHostNames.Handled));
                }
            );
            Require(runtime.Modules.DeleteModule(source.Definition.Name).Equals(Term.A(ErlangBooleanAtoms.True)));
            await NifAdmissionHostBridge.Context(
                runtime,
                async context =>
            {
                await Flag(context);
                Require((await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    UndefinedLambdaLifecycleHostNames.Purge,
                    Term.A(source.Definition.Name)
                )).Equals(Term.A(ErlangBooleanAtoms.True)));
                bool rejected = false;
                try
                {
                    SourceFunctionAdmission admission = await runtime.Modules.AdmitSourceFunction(
                        context,
                        source.Definition.Name,
                        fingerprint,
                        source.Definition,
                        image,
                        restored
                    );
                    admission.Lease?.Dispose();
                }
                catch (ErlangException failure)
                {
                    Require(failure.Reason.Equals(Term.A(ErlangErrorReasons.UndefinedFunction)));
                    rejected = true;
                }
                Require(rejected && callbacks == 0 && References(context, image) == 0);
                Require((await restored.Invoke(context, [Term.A(UndefinedLambdaLifecycleHostNames.Argument)])).Equals(
                    Term.A(UndefinedLambdaLifecycleHostNames.Handled)));
                Require(callbacks == 1 && References(context, image) == 0);
            }
            );
        }
    }

    private static async Task Pending(
        bool anonymous,
        bool retire,
        bool replacement,
        bool kill
    )
    {
        using var source = GeneratedModuleCompiler.Compile(UndefinedLambdaLifecycleHostNames.TargetSource);
        using var current = GeneratedModuleCompiler.Compile(UndefinedLambdaLifecycleHostNames.ReplacementSource);
        await using var runtime = new ProcessRuntime();
        source.Definition.Register(runtime.Modules);
        ModuleCodeImage image = runtime.Modules.CurrentCodeImage(source.Definition.Name)!;
        FunctionTerm restored = await Restore(runtime, anonymous);
        int callbacks = 0;
        runtime.Modules.Register(
            UndefinedLambdaLifecycleHostNames.Handler,
            UndefinedLambdaLifecycleHostNames.Undefined,
            UndefinedLambdaLifecycleHostNames.CallbackArity,
            (_, arguments) =>
            {
                CheckArguments(arguments, restored);
                Interlocked.Increment(ref callbacks);

                return ValueTask.FromResult<Term>(Term.A(UndefinedLambdaLifecycleHostNames.Handled));
            }
        );
        Require(runtime.Modules.DeleteModule(source.Definition.Name).Equals(Term.A(ErlangBooleanAtoms.True)));
        if (replacement)
            current.Definition.Register(runtime.Modules);
        object session = NifAdmissionHostBridge.Call(runtime.Modules, UndefinedLambdaLifecycleHostNames.Prepare, source.Definition.Name)!;
        Require(ReferenceEquals(NifAdmissionHostBridge.Property(session, UndefinedLambdaLifecycleHostNames.Image), image));
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempted = new TaskCompletionSource<Task<Term>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                await Flag(context);
                entered.TrySetResult(context);
                Task<Term> call = restored.Invoke(context, [Term.A(UndefinedLambdaLifecycleHostNames.Argument)]).AsTask();
                attempted.TrySetResult(call);
                result.TrySetResult(await call);
            }
            catch (Exception failure)
            {
                entered.TrySetException(failure);
                attempted.TrySetException(failure);
                result.TrySetException(failure);
            }

            return Nil.Value;
        });
        Exception? primary = null;
        try
        {
            ProcessContext context = await entered.Task.WaitAsync(UndefinedLambdaLifecycleHostNames.Deadline);
            Task<Term> blocked = await attempted.Task.WaitAsync(UndefinedLambdaLifecycleHostNames.Deadline);
            await NifAdmissionHostBridge.Context(
                runtime,
                async observer =>
            {
                Term status = await runtime.Modules.Call(
                    observer,
                    ErlangModuleNames.Module,
                    UndefinedLambdaLifecycleHostNames.ProcessInfo,
                    actor.Pid,
                    Term.A(UndefinedLambdaLifecycleHostNames.Status)
                );
                Require(status is TupleTerm && !blocked.IsCompleted && callbacks == 0 && References(context, image) == 0);
                if (kill)
                    runtime.Exit(observer, actor.Pid, Term.A(UndefinedLambdaLifecycleHostNames.Kill));
            }
            );
            if (kill)
            {
                Require((await actor.Completion.WaitAsync(UndefinedLambdaLifecycleHostNames.Deadline)).Equals(Term.A(UndefinedLambdaLifecycleHostNames.Killed)));
                try
                {
                    await result.Task.WaitAsync(UndefinedLambdaLifecycleHostNames.Deadline);
                    throw new InvalidOperationException(UndefinedLambdaLifecycleHostNames.Broken);
                }
                catch (OperationCanceledException) { }
                Require(callbacks == 0 && References(context, image) == 0);
            }
            NifAdmissionHostBridge.Call(
                runtime.Modules,
                UndefinedLambdaLifecycleHostNames.Finish,
                session,
                retire
            );
            if (!kill)
            {
                if (replacement)
                {
                    ErlangException failure = await Failure(result.Task);
                    Require(failure.Reason is TupleTerm badfun && badfun.Items.Count == 2 &&
                        badfun.Items[0].Equals(Term.A(ErlangErrorReasons.BadFunction)) && ReferenceEquals(badfun.Items[1], restored));
                    Require(callbacks == 0);
                }
                else
                {
                    Term value = await result.Task.WaitAsync(UndefinedLambdaLifecycleHostNames.Deadline);
                    Term expected = retire ? Term.A(UndefinedLambdaLifecycleHostNames.Handled) :
                        anonymous ? Term.Tuple(
                            Term.A(UndefinedLambdaLifecycleHostNames.OldBody),
                            Term.A(UndefinedLambdaLifecycleHostNames.Environment),
                            Term.A(UndefinedLambdaLifecycleHostNames.Argument)
                        ) :
                        Term.Tuple(Term.A(UndefinedLambdaLifecycleHostNames.OldBody), Term.A(UndefinedLambdaLifecycleHostNames.Argument));
                    Require(value.Equals(expected) && callbacks == (retire ? 1 : 0));
                }
                await actor.Completion.WaitAsync(UndefinedLambdaLifecycleHostNames.Deadline);
            }
            Require(callbacks == (kill || replacement || !retire ? 0 : 1) && References(context, image) == 0);
        }
        catch (Exception failure)
        {
            primary = failure;
            throw;
        }
        finally
        {
            try
            {
                if (runtime.Modules.PendingPurge(image) is not null)
                    NifAdmissionHostBridge.Call(
                        runtime.Modules,
                        UndefinedLambdaLifecycleHostNames.Finish,
                        session,
                        false
                    );
                await Join(runtime, actor);
                if (result.Task.IsCompleted)
                    _ = result.Task.Exception;
            }
            catch (Exception cleanup) when (primary is not null)
            {
                throw new AggregateException(primary, cleanup);
            }
        }
    }

    private static async Task SelectedLease(bool anonymous, bool fail, bool kill)
    {
        using var target = GeneratedModuleCompiler.Compile(UndefinedLambdaLifecycleHostNames.TargetSource);
        using var handler = GeneratedModuleCompiler.Compile(UndefinedLambdaLifecycleHostNames.HandlerSource);
        await using var runtime = new ProcessRuntime();
        target.Definition.Register(runtime.Modules);
        handler.Definition.Register(runtime.Modules);
        ModuleCodeImage image = runtime.Modules.CurrentCodeImage(handler.Definition.Name)!;
        FunctionTerm restored = await Restore(runtime, anonymous);
        Require(runtime.Modules.DeleteModule(target.Definition.Name).Equals(Term.A(ErlangBooleanAtoms.True)));
        await NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            Require((await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                UndefinedLambdaLifecycleHostNames.Purge,
                Term.A(target.Definition.Name)
            )).Equals(Term.A(ErlangBooleanAtoms.True)));
        }
        );
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        int callbacks = 0;

        void Check(ProcessContext context, IReadOnlyList<Term> arguments)
        {
            CheckArguments(arguments, restored);
            Require(ReferenceEquals(context.ExecutingSourceImage(handler.Definition), image) && References(context, image) > 0);
        }
        ErlangFunction bridge = async (context, arguments) =>
        {
            try
            {
                Check(context, arguments);
                Interlocked.Increment(ref callbacks);
                await Task.Yield();
                Check(context, arguments);
                entered.TrySetResult(context);
                await release.Task.WaitAsync(context.Cancellation);
                Check(context, arguments);
                if (fail)
                    throw new ErlangException(Term.A(UndefinedLambdaLifecycleHostNames.Fault));

                return Term.A(UndefinedLambdaLifecycleHostNames.Handled);
            }
            catch (Exception failure)
            {
                entered.TrySetException(failure);
                throw;
            }
            finally
            {
                try
                {
                    Check(context, arguments);
                }
                finally
                {
                    unwound.TrySetResult();
                }
            }
        };
        NifAdmissionHostBridge.Call(
            runtime.Modules,
            UndefinedLambdaLifecycleHostNames.RegisterIntrinsic,
            UndefinedLambdaLifecycleHostNames.Bridge,
            UndefinedLambdaLifecycleHostNames.Held,
            UndefinedLambdaLifecycleHostNames.CallbackArity,
            bridge
        );
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                await Flag(context);
                result.TrySetResult(await restored.Invoke(context, [Term.A(UndefinedLambdaLifecycleHostNames.Argument)]));
            }
            catch (Exception failure)
            {
                result.TrySetException(failure);
                throw;
            }

            return Nil.Value;
        });
        Exception? primary = null;
        try
        {
            ProcessContext context = await entered.Task.WaitAsync(UndefinedLambdaLifecycleHostNames.Deadline);
            Require(callbacks == 1 && References(context, image) > 0);
            if (kill)
                await Kill(runtime, actor);
            else
                release.TrySetResult();
            await unwound.Task.WaitAsync(UndefinedLambdaLifecycleHostNames.Deadline);
            Term reason = await actor.Completion.WaitAsync(UndefinedLambdaLifecycleHostNames.Deadline);
            if (kill)
            {
                Require(reason.Equals(Term.A(UndefinedLambdaLifecycleHostNames.Killed)));
                try
                {
                    await result.Task.WaitAsync(UndefinedLambdaLifecycleHostNames.Deadline);
                    throw new InvalidOperationException(UndefinedLambdaLifecycleHostNames.Broken);
                }
                catch (OperationCanceledException) { }
            }
            else if (fail)
                Require((await Failure(result.Task)).Reason.Equals(Term.A(UndefinedLambdaLifecycleHostNames.Fault)));
            else
                Require((await result.Task.WaitAsync(UndefinedLambdaLifecycleHostNames.Deadline)).Equals(
                    Term.Tuple(Term.A(UndefinedLambdaLifecycleHostNames.Forwarded), Term.A(UndefinedLambdaLifecycleHostNames.Handled))));
            Require(callbacks == 1 && References(context, image) == 0);
        }
        catch (Exception failure)
        {
            primary = failure;
            throw;
        }
        finally
        {
            release.TrySetResult();
            try
            {
                await Join(runtime, actor);
                if (result.Task.IsCompleted)
                    _ = result.Task.Exception;
            }
            catch (Exception cleanup) when (primary is not null)
            {
                throw new AggregateException(primary, cleanup);
            }
        }
    }

    private static async Task<FunctionTerm> Restore(ProcessRuntime runtime, bool anonymous)
    {
        FunctionTerm? restored = null;
        await NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            Term genuine = await runtime.Modules.Call(
                context,
                UndefinedLambdaLifecycleHostNames.Target,
                anonymous ? UndefinedLambdaLifecycleHostNames.Anonymous : UndefinedLambdaLifecycleHostNames.Named,
                anonymous ? [Term.A(UndefinedLambdaLifecycleHostNames.Environment)] : []
            );
            Term binary = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                UndefinedLambdaLifecycleHostNames.ToBinary,
                genuine
            );
            restored = (FunctionTerm)await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                UndefinedLambdaLifecycleHostNames.FromBinary,
                binary
            );
        }
        );

        return restored!;
    }

    private static ValueTask<Term> Flag(ProcessContext context) => context.Runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        UndefinedLambdaLifecycleHostNames.ProcessFlag,
        Term.A(UndefinedLambdaLifecycleHostNames.Flag),
        Term.A(UndefinedLambdaLifecycleHostNames.Handler)
    );

    private static async Task Kill(ProcessRuntime runtime, ProcessHandle actor) => await NifAdmissionHostBridge.Context(
        runtime,
        observer =>
    {
        runtime.Exit(observer, actor.Pid, Term.A(UndefinedLambdaLifecycleHostNames.Kill));

        return Task.CompletedTask;
    }
    );

    private static async Task Join(ProcessRuntime runtime, ProcessHandle actor)
    {
        if (!actor.Completion.IsCompleted)
            await Kill(runtime, actor);
        await actor.Completion.WaitAsync(UndefinedLambdaLifecycleHostNames.Deadline);
    }

    private static int References(ProcessContext context, ModuleCodeImage image)
    {
        object gate = typeof(ProcessContext).GetField(UndefinedLambdaLifecycleHostNames.CodeGate, UndefinedLambdaLifecycleHostNames.Fields)!.GetValue(context)!;
        lock (gate)
            return ((Dictionary<ModuleCodeImage, int>)typeof(ProcessContext).GetField(UndefinedLambdaLifecycleHostNames.ActiveCode,
                UndefinedLambdaLifecycleHostNames.Fields)!.GetValue(context)!).GetValueOrDefault(image);
    }

    private static void CheckArguments(IReadOnlyList<Term> arguments, FunctionTerm restored)
    {
        Require(arguments.Count == UndefinedLambdaLifecycleHostNames.CallbackArity &&
            arguments[0].Equals(Term.A(UndefinedLambdaLifecycleHostNames.Target)) && ReferenceEquals(arguments[1], restored) &&
            arguments[2].Equals(Term.List(Term.A(UndefinedLambdaLifecycleHostNames.Argument))));
    }

    private static async Task<ErlangException> Failure(Task<Term> operation)
    {
        try
        {
            await operation.WaitAsync(UndefinedLambdaLifecycleHostNames.Deadline);
        }
        catch (ErlangException failure)
        {
            return failure;
        }
        throw new InvalidOperationException(UndefinedLambdaLifecycleHostNames.Broken);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(UndefinedLambdaLifecycleHostNames.Broken);
    }
}
