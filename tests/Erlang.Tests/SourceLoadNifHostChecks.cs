using System.Linq.Expressions;
using System.Reflection;
using Erlang.Compiler;

namespace Erlang.Tests;

public static partial class SourceLoadNifHostChecks
{
    public static async Task Run()
    {
        await RoutesAndOwnership();
        await OwnedCompletion(false);
        await OwnedCompletion(true);
        await PendingImage();
        await ArgumentEvaluationAndGeneratedRoute(false);
        await ArgumentEvaluationAndGeneratedRoute(true);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(SourceLoadNifHostNames.Broken);
    }

    private static SourceCodeEntry Consume(object capability, ProcessContext context)
        => (SourceCodeEntry)NifAdmissionHostBridge.Call(capability, SourceLoadNifHostNames.Consume, context)!;

    private static void Refused(object capability, ProcessContext context)
    {
        try
        {
            Consume(capability, context);
        }
        catch (InvalidOperationException)
        {
            return;
        }
        throw new InvalidOperationException(SourceLoadNifHostNames.Broken);
    }

    private static void Install(
        ModuleRegistry registry,
        Func<ProcessContext, object, Term[], ValueTask<Term>> handler
    )
    {
        MethodInfo method = typeof(ModuleRegistry).GetMethod(SourceLoadNifHostNames.Install, SourceLoadNifHostNames.Members)!;
        Type delegateType = method.GetParameters()[0].ParameterType;
        var parameters = delegateType.GetMethod(nameof(Action.Invoke))!.GetParameters()
            .Select(parameter => Expression.Parameter(parameter.ParameterType)).ToArray();
        var body = Expression.Invoke(
            Expression.Constant(handler),
            parameters[0],
            Expression.Convert(parameters[1], typeof(object)),
            parameters[2]
        );
        Delegate adapter = Expression.Lambda(delegateType, body, parameters).Compile();
        method.Invoke(registry, [adapter]);
    }

    private static int References(ProcessContext context, ModuleCodeImage image)
    {
        var active = (Dictionary<ModuleCodeImage, int>)typeof(ProcessContext)
            .GetField(SourceLoadNifHostFields.ActiveCode, SourceLoadNifHostNames.Members)!.GetValue(context)!;

        return active.GetValueOrDefault(image);
    }

    private static async Task RoutesAndOwnership()
    {
        await using var runtime = new ProcessRuntime();
        var first = new Parser(SourceLoadNifHostNames.Source).ParseModule();
        var other = new Parser(SourceLoadNifHostNames.OtherSource).ParseModule();
        first.Register(runtime.Modules);
        other.Register(runtime.Modules);
        ModuleCodeImage image = runtime.Modules.CurrentCodeImage(first.Name)!;
        ModuleCodeImage otherImage = runtime.Modules.CurrentCodeImage(other.Name)!;
        int calls = 0;
        object? escaped = null;
        ProcessContext? originalContext = null;
        bool old = false;
        bool argumentSideEffect = false;
        Term rejectedApply = Term.Tuple(
            Term.A(SourceLoadNifHostNames.Error),
            Term.Tuple(Term.A(SourceLoadNifHostNames.BadLibrary), Term.String(SourceLoadNifHostNames.ApplyRejectedMessage))
        );
        runtime.Modules.Register(
            SourceLoadNifHostNames.Erlang,
            SourceLoadNifHostNames.Function,
            SourceLoadNifHostNames.Arity,
            (_, _) => ValueTask.FromResult(rejectedApply)
        );
        await NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            Term value = await Execution.InvokeAsync(
                first,
                SourceLoadNifHostNames.Direct,
                context,
                [Nil.Value]
            );
            Require(value.Equals(rejectedApply));
        }
        );
        Install(
            runtime.Modules,
            async (context, capability, arguments) =>
        {
            calls++;
            escaped = capability;
            originalContext = context;
            await NifAdmissionHostBridge.Context(runtime, foreign =>
            {
                Refused(capability, foreign);

                return Task.CompletedTask;
            });
            if (argumentSideEffect)
                Require(context.Dictionary[Term.A(SourceLoadNifHostNames.ArgumentDone)].Equals(Term.A(ErlangBooleanAtoms.True)));
            SourceCodeEntry caller = Consume(capability, context);
            Refused(capability, context);
            Require(arguments.Length == SourceLoadNifHostNames.Arity && arguments[1].Equals(Term.I(SourceLoadNifHostNames.Marker)));
            Require(ReferenceEquals(caller.Owner, first) && ReferenceEquals(caller.Image, image)
                || ReferenceEquals(caller.Owner, other) && ReferenceEquals(caller.Image, otherImage));
            Require(References(context, caller.Image) == SourceLoadNifHostNames.OwnedDirectReferences);
            if (old)
                await NifAdmissionHostBridge.Failure(NifAdmissionHostBridge.Reserve(runtime.Modules, context), SourceLoadNifHostNames.OldCode);
            else
            {
                object reservation = await NifAdmissionHostBridge.Reserve(runtime.Modules, context);
                await NifAdmissionHostBridge.Dispose(reservation);
            }

            return arguments[0];
        }
        );
        await NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            var expression = new Expr.Call(SourceLoadNifHostNames.Erlang, SourceLoadNifHostNames.Function, [new Expr.Literal(Nil.Value), new Expr.Literal(Nil.Value)]);
            await NifAdmissionHostBridge.Failure(Execution.EvaluateAsync(expression, context).AsTask(), SourceLoadNifHostNames.NoExecutingCaller);
            Require(calls == 0);
        }
        );
        foreach (string route in new[] { SourceLoadNifHostNames.Direct, SourceLoadNifHostNames.Last, SourceLoadNifHostNames.NonTail, SourceLoadNifHostNames.Cross })
        {
            await NifAdmissionHostBridge.Context(
                runtime,
                async context =>
            {
                Term value = await Execution.InvokeAsync(
                    first,
                    route,
                    context,
                    [Term.I(SourceLoadNifHostNames.Value)]
                );
                Require(value.Equals(route == SourceLoadNifHostNames.NonTail ? Term.Tuple(Term.I(SourceLoadNifHostNames.Value)) : Term.I(SourceLoadNifHostNames.Value)));
                Refused(escaped!, context);
                Require(References(context, image) == 0 && References(context, otherImage) == 0);
            }
            );
        }
        int directCalls = calls;
        foreach (string route in new[] { SourceLoadNifHostNames.Dynamic, SourceLoadNifHostNames.Reference })
        {
            await NifAdmissionHostBridge.Context(
                runtime,
                async context =>
            {
                Term rejection = await Execution.InvokeAsync(
                    first,
                    route,
                    context,
                    [Term.I(SourceLoadNifHostNames.Value)]
                );
                Require(rejection.Equals(rejectedApply) && calls == directCalls);
            }
            );
        }
        await NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            Refused(escaped!, context);
            Require(!ReferenceEquals(originalContext, context));
            Term value = await Execution.InvokeAsync(
                first,
                SourceLoadNifHostNames.Local,
                context,
                [Term.I(SourceLoadNifHostNames.Value)]
            );
            Require(value.Equals(Term.I(SourceLoadNifHostNames.Value)) && calls == directCalls);
            argumentSideEffect = true;
            value = await Execution.InvokeAsync(
                first,
                SourceLoadNifHostNames.Arguments,
                context,
                []
            );
            Require(value.Equals(Term.I(SourceLoadNifHostNames.Value)) && context.Dictionary[Term.A(SourceLoadNifHostNames.ArgumentDone)].Equals(Term.A(ErlangBooleanAtoms.True)));
        }
        );
        runtime.Modules.DeleteModule(first.Name);
        new Parser(SourceLoadNifHostNames.Source).ParseModule().Register(runtime.Modules);
        old = true;
        argumentSideEffect = false;
        await NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            Term value = await Execution.InvokeAsync(
                first,
                SourceLoadNifHostNames.Direct,
                context,
                [Term.I(SourceLoadNifHostNames.Value)]
            );
            Require(value.Equals(Term.I(SourceLoadNifHostNames.Value)));
        }
        );
    }

    private static async Task OwnedCompletion(bool cancel)
    {
        await using var runtime = new ProcessRuntime();
        var module = new Parser(SourceLoadNifHostNames.Source).ParseModule();
        module.Register(runtime.Modules);
        ModuleCodeImage image = runtime.Modules.CurrentCodeImage(module.Name)!;
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finalizing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var marker = new InvalidOperationException(SourceLoadNifHostNames.Broken);
        object? escaped = null;
        Install(
            runtime.Modules,
            async (context, capability, _) =>
        {
            escaped = capability;
            SourceCodeEntry caller = Consume(capability, context);
            Require(ReferenceEquals(caller.Owner, module) && ReferenceEquals(caller.Image, image));
            entered.TrySetResult(context);
            try
            {
                if (cancel)
                    await Task.Delay(Timeout.InfiniteTimeSpan, context.Cancellation);
            }
            finally
            {
                finalizing.TrySetResult();
                await release.Task.WaitAsync(SourceLoadNifHostNames.Deadline);
                Require(References(context, image) == SourceLoadNifHostNames.OwnedDirectReferences);
                unwound.TrySetResult();
            }
            throw marker;
        }
        );
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                bool failed = false;
                try
                {
                    await Execution.InvokeAsync(
                        module,
                        SourceLoadNifHostNames.Direct,
                        context,
                        [Nil.Value]
                    );
                }
                catch (Exception failure) when (ReferenceEquals(failure, marker) || cancel && failure is OperationCanceledException)
                {
                    failed = true;
                }
                Require(failed);
                Refused(escaped!, context);
                Require(References(context, image) == 0);
                completed.TrySetResult();
            }
            catch (Exception failure)
            {
                completed.TrySetException(failure);
            }

            return Nil.Value;
        });
        ProcessContext current = await entered.Task.WaitAsync(SourceLoadNifHostNames.Deadline);
        try
        {
            if (cancel)
            {
                ProcessHandle killer = runtime.Spawn(context =>
                {
                    runtime.Exit(context, actor.Pid, Term.A(SourceLoadNifHostNames.Kill));

                    return ValueTask.FromResult<Term>(Nil.Value);
                });
                await killer.Completion.WaitAsync(SourceLoadNifHostNames.Deadline);
            }
            await finalizing.Task.WaitAsync(SourceLoadNifHostNames.Deadline);
            Require(!unwound.Task.IsCompleted && References(current, image) == SourceLoadNifHostNames.OwnedDirectReferences);
        }
        finally
        {
            release.TrySetResult();
        }
        await unwound.Task.WaitAsync(SourceLoadNifHostNames.Deadline);
        await completed.Task.WaitAsync(SourceLoadNifHostNames.Deadline);
        await actor.Completion.WaitAsync(SourceLoadNifHostNames.Deadline);
        Require(References(current, image) == 0);
    }

    private static async Task PendingImage()
    {
        BeamOnLoadResourceHashChecks.Run();
        await using var runtime = new ProcessRuntime();
        Assembly assembly = typeof(SourceLoadNifHostChecks).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(SourceLoadNifHostNames.PendingResource, StringComparison.Ordinal));
        using Stream resource = assembly.GetManifestResourceStream(name)!;
        using var bytes = new MemoryStream();
        resource.CopyTo(bytes);
        PreparedBeamImage prepared = runtime.Modules.PrepareBeam(bytes.ToArray());
        BeamOnLoadTicket ticket = runtime.Modules.StagePreparedOnLoad(prepared);
        ModuleCodeImage image = (ModuleCodeImage)NifAdmissionHostBridge.Property(ticket, SourceLoadNifHostNames.PendingImage);
        Install(
            runtime.Modules,
            async (context, capability, _) =>
        {
            SourceCodeEntry caller = Consume(capability, context);
            Require(ReferenceEquals(caller.Owner, prepared) && ReferenceEquals(caller.Image, image));
            object reservation = await NifAdmissionHostBridge.Reserve(runtime.Modules, context);
            await NifAdmissionHostBridge.Dispose(reservation);

            return Nil.Value;
        }
        );
        try
        {
            await NifAdmissionHostBridge.Context(
                runtime,
                async context =>
            {
                using var flow = context.EnterSourceCode(image, prepared);
                var call = new Expr.Call(
                    SourceLoadNifHostNames.Erlang,
                    SourceLoadNifHostNames.Function,
                    [new Expr.Literal(Nil.Value), new Expr.Literal(Term.I(SourceLoadNifHostNames.Marker))]
                );
                Require((await Execution.EvaluateAsync(call, context)).Equals(Nil.Value));
            }
            );
        }
        finally
        {
            runtime.Modules.FinishPendingOnLoad(ticket, false);
        }
    }
}
