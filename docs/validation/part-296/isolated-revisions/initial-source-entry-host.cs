using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using Erlang.Compiler;

namespace Erlang.Tests;

public static class NifSourceEntryHostChecks
{
    public static async Task Run()
    {
        await RoutesAndImages();
        await OwnedDrain(false);
        await OwnedDrain(true);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifSourceEntryHostNames.Broken);
    }

    private static object? Call(object target, string name, params object?[] arguments)
    {
        try
        {
            return target.GetType().GetMethod(name, NifSourceEntryHostNames.Members)!.Invoke(target, arguments);
        }
        catch (TargetInvocationException failure) when (failure.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static object Bind(ModuleCodeImage image, ErlangFunction body, NifSourceEntryTestOwner owner)
    {
        object table = typeof(ModuleCodeImage).GetProperty(NifSourceEntryHostNames.Table, NifSourceEntryHostNames.Members)!.GetValue(image)!;
        var descriptor = new ModuleCodeDescriptor(
            [(NifSourceEntryHostNames.Target, 1)],
            [(NifSourceEntryHostNames.Target, 1)],
            new BitString(new byte[MD5.HashSizeInBytes]),
            Nil.Value
        );
        using var reservation = (IDisposable)Call(table, NifSourceEntryHostNames.Begin, descriptor)!;
        KeyValuePair<(string Name, int Arity), ErlangFunction>[] bindings = [new((NifSourceEntryHostNames.Target, 1), body)];
        Call(table, NifSourceEntryHostNames.Publish, bindings, owner);

        return table;
    }

    private static bool IsCreated(ModuleCodeImage image)
    {
        Type store = typeof(ModuleCodeImage).Assembly.GetType(NifSourceEntryHostNames.Store)!;

        return (bool)store.GetMethod(NifSourceEntryHostNames.IsCreated, NifSourceEntryHostNames.StaticMembers)!.Invoke(null, [image])!;
    }

    private static async Task<Term> Invoke(ProcessRuntime runtime, Func<ProcessContext, ValueTask<Term>> body)
    {
        var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        var actor = runtime.Spawn(async context =>
        {
            try
            {
                Term value = await body(context);
                result.TrySetResult(value);

                return value;
            }
            catch (Exception failure)
            {
                result.TrySetException(failure);
                throw;
            }
        });
        try
        {
            return await result.Task.WaitAsync(NifSourceEntryHostNames.Deadline);
        }
        finally
        {
            await actor.Completion.WaitAsync(NifSourceEntryHostNames.Deadline);
        }
    }

    private static async Task RoutesAndImages()
    {
        await using var runtime = new ProcessRuntime();
        var first = new Parser(NifSourceEntryHostNames.Source).ParseModule();
        first.Register(runtime.Modules);
        var oldImage = runtime.Modules.CurrentCodeImage(NifSourceEntryHostNames.Module)!;
        Require(!IsCreated(oldImage));
        Term plain = await Invoke(runtime, context => Execution.InvokeAsync(first, NifSourceEntryHostNames.Plain, context, []));
        Require(plain.Equals(Term.I(NifSourceEntryHostNames.PlainValue)) && !IsCreated(oldImage));
        var oldOwner = new NifSourceEntryTestOwner();
        object oldTable = Bind(oldImage, (context, arguments) =>
        {
            Require(ReferenceEquals(context.ExecutingSourceImage(first), oldImage));

            return ValueTask.FromResult(arguments[0]);
        }, oldOwner);
        try
        {
            Term fallback = await Invoke(runtime, context => Execution.InvokeAsync(first, NifSourceEntryHostNames.Plain, context, []));
            Require(fallback.Equals(Term.I(NifSourceEntryHostNames.PlainValue)));
            foreach (string route in NifSourceEntryHostNames.Routes)
            {
                Term value = await Invoke(runtime, context => Execution.InvokeAsync(first, route, context, [Term.I(NifSourceEntryHostNames.Argument)]));
                Require(value.Equals(Term.I(NifSourceEntryHostNames.Argument)));
            }
            Term local = await Invoke(runtime, context => Execution.InvokeAsync(first, NifSourceEntryHostNames.Local, context, [Term.I(NifSourceEntryHostNames.Argument)]));
            Require(local.Equals(Term.Tuple(Term.I(NifSourceEntryHostNames.Argument))));
            Term exported = await Invoke(runtime, context => runtime.Modules.Call(context, NifSourceEntryHostNames.Module, NifSourceEntryHostNames.Target, [Term.I(NifSourceEntryHostNames.Argument)]));
            Require(exported.Equals(Term.I(NifSourceEntryHostNames.Argument)));

            runtime.Modules.DeleteModule(NifSourceEntryHostNames.Module);
            var second = new Parser(NifSourceEntryHostNames.Source).ParseModule();
            second.Register(runtime.Modules);
            var currentImage = runtime.Modules.CurrentCodeImage(NifSourceEntryHostNames.Module)!;
            Require(!ReferenceEquals(currentImage, oldImage) && !IsCreated(currentImage));
            var currentOwner = new NifSourceEntryTestOwner();
            object currentTable = Bind(currentImage, (_, _) => ValueTask.FromResult<Term>(Term.I(NifSourceEntryHostNames.Replacement)), currentOwner);
            try
            {
                Term old = await Invoke(runtime, context => Execution.InvokeAsync(first, NifSourceEntryHostNames.Target, context, [Term.I(NifSourceEntryHostNames.Argument)]));
                Term current = await Invoke(runtime, context => runtime.Modules.Call(context, NifSourceEntryHostNames.Module, NifSourceEntryHostNames.Target, [Term.I(NifSourceEntryHostNames.Argument)]));
                Require(old.Equals(Term.I(NifSourceEntryHostNames.Argument)) && current.Equals(Term.I(NifSourceEntryHostNames.Replacement)));
            }
            finally
            {
                await ((Task)Call(currentTable, NifSourceEntryHostNames.Retire)!).WaitAsync(NifSourceEntryHostNames.Deadline);
            }
            Require(currentOwner.Calls == 1);
        }
        finally
        {
            await ((Task)Call(oldTable, NifSourceEntryHostNames.Retire)!).WaitAsync(NifSourceEntryHostNames.Deadline);
        }
        Require(oldOwner.Calls == 1);
    }

    private static async Task OwnedDrain(bool cancel)
    {
        await using var runtime = new ProcessRuntime();
        var module = new Parser(NifSourceEntryHostNames.Source).ParseModule();
        module.Register(runtime.Modules);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = new NifSourceEntryTestOwner();
        object table = Bind(runtime.Modules.CurrentCodeImage(NifSourceEntryHostNames.Module)!, async (context, arguments) =>
        {
            entered.TrySetResult();
            try
            {
                if (cancel)
                    await Task.Delay(Timeout.InfiniteTimeSpan, context.Cancellation);

                return arguments[0];
            }
            finally
            {
                cancelled.TrySetResult();
                await release.Task.WaitAsync(NifSourceEntryHostNames.Deadline);
            }
        }, owner);
        var victim = runtime.Spawn(context => Execution.InvokeAsync(module, NifSourceEntryHostNames.Target, context, [Term.I(NifSourceEntryHostNames.Argument)]));
        Task? retirement = null;
        try
        {
            await entered.Task.WaitAsync(NifSourceEntryHostNames.Deadline);
            retirement = (Task)Call(table, NifSourceEntryHostNames.Retire)!;
            Require(!retirement.IsCompleted && owner.Calls == 0);
            if (cancel)
            {
                var killer = runtime.Spawn(context =>
                {
                    runtime.Exit(context, victim.Pid, Term.A(NifSourceEntryHostNames.Kill));

                    return ValueTask.FromResult<Term>(Nil.Value);
                });
                await killer.Completion.WaitAsync(NifSourceEntryHostNames.Deadline);
                await cancelled.Task.WaitAsync(NifSourceEntryHostNames.Deadline);
                Require(!retirement.IsCompleted && owner.Calls == 0);
            }
        }
        finally
        {
            release.TrySetResult();
            await victim.Completion.WaitAsync(NifSourceEntryHostNames.Deadline);
            retirement ??= (Task)Call(table, NifSourceEntryHostNames.Retire)!;
            await retirement.WaitAsync(NifSourceEntryHostNames.Deadline);
        }
        Require(owner.Calls == 1);
    }
}
